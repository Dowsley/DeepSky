using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DeepSky.Player;
using DeepSky.World.Chunks;
using DeepSky.World.Generation;
using DeepSky.World.Population;
using UnityEngine;
using UnityEngine.Assertions;

namespace DeepSky.World
{
    /// <summary>
    /// Streams a finite seeded world around the player. One numerical task builds terrain at a time;
    /// mesh upload, collider cooking and prefab activation run on the main thread with bounded work.
    /// Generated objects are transient. Settings and prefabs are the saved authoring data.
    /// </summary>
    public sealed class WorldManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private WorldSettings settings = null!;
        [SerializeField] private WorldChunk chunkPrefab = null!;
        [SerializeField] private Material terrainMaterial = null!;
        [SerializeField] private DiverController player = null!;
        [SerializeField] private Camera observerCamera = null!;

        [Header("Content by depth stage")]
        [SerializeField] private WorldContentProfile shallowContent = null!;
        [SerializeField] private WorldContentProfile middleContent = null!;
        [SerializeField] private WorldContentProfile deepContent = null!;

        [Header("Streaming")]
        [Tooltip("Horizontal distance to chunk bounds, in metres. Must exceed the shaders' visibility range.")]
        [SerializeField, Min(64f)] private float loadRadius = 96f;
        [SerializeField, Min(96f)] private float releaseRadius = 128f;
        [SerializeField, Min(1)] private int populationStepsPerFrame = 64;
        [Tooltip("Soft main-thread population time budget; a single mesh upload or collider cook cannot be preempted.")]
        [SerializeField, Min(0.1f)] private float populationBudgetMilliseconds = 3f;

        [Header("Safety")]
        [SerializeField, Min(0.05f)] private float spawnGroundClearance = 0.15f;
        [SerializeField, Min(1f)] private float fallenBelowWorldDistance = 30f;

        private readonly Dictionary<Vector2Int, WorldChunk> chunks = new();
        private readonly List<Vector2Int> wanted = new();
        private readonly List<Vector2Int> expired = new();
        private Transform generatedRoot = null!;
        private Task<ChunkMeshData>? worker;
        private CancellationTokenSource? cancellation;
        private IEnumerator<int>? population;
        private WorldChunk? populatingChunk;
        private Vector2 previewPosition = Vector2.zero;
        private bool preview = false;
        private bool running = false;
        private bool initialized = false;

        public WorldSettings Settings => settings;

        public WorldData Data { get; private set; } = null!;

        public Camera ObserverCamera => observerCamera;
        public bool HasRuntimeWorld => running && !preview;
        public int LoadedChunkCount => chunks.Count;
        public bool IsGenerating => running && (worker != null || population != null || wanted.Count > 0);
        public bool IsReady => running && NeighborhoodReady(new Vector2(player.transform.position.x, player.transform.position.z));

        /// <summary>Validates player references and starts runtime generation at the selected spawn.</summary>
        private void Start()
        {
            Assert.IsNotNull(player, nameof(player));
            Assert.IsNotNull(observerCamera, nameof(observerCamera));
            initialized = true;
            Regenerate();
        }

        /// <summary>Restarts runtime generation when an initialized manager is re-enabled during Play.</summary>
        private void OnEnable()
        {
            if (Application.isPlaying && initialized)
            {
                Regenerate();
            }
        }

        /// <summary>Advances runtime streaming and returns the player to spawn after leaving the world bounds.</summary>
        private void Update()
        {
            if (!running || preview)
                return;
            Tick();
            Vector3 position = player.transform.position;
            if (!Data.Contains(new Vector2(position.x, position.z))
                || position.y < -Data.MaximumDepth - fallenBelowWorldDistance)
            {
                ReturnToSpawn();
            }
        }

        /// <summary>Cancels generation and releases transient chunks when the manager is disabled.</summary>
        private void OnDisable()
        {
            ClearGenerated();
        }

        /// <summary>Maintains streaming-distance hysteresis and a positive population step budget.</summary>
        private void OnValidate()
        {
            releaseRadius = Mathf.Max(loadRadius + 16f, releaseRadius);
            populationStepsPerFrame = Mathf.Max(1, populationStepsPerFrame);
        }

        /// <summary>Draws loaded chunk collider bounds for editor inspection.</summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 0.9f, 0.45f);
            foreach (WorldChunk chunk in chunks.Values)
            {
                if (!chunk)
                    continue;
                Bounds bounds = chunk.GetComponent<MeshCollider>().bounds;
                Gizmos.DrawWireCube(bounds.center, bounds.size);
            }
        }

        /// <summary>Replaces the current seed snapshot and returns the player to a collision-gated spawn.</summary>
        public void Regenerate()
        {
            Begin(false, Vector2.zero);
            ReturnToSpawn();
        }

        /// <summary>Editor-only caller owns ticking and cleanup. Preview objects are never saved into Main.</summary>
        /// <param name="position">World XZ coordinates in metres around which to generate the preview.</param>
        public void BeginPreview(Vector2 position)
        {
            Begin(true, position);
        }

        /// <summary>Advances one main-thread generation tick when an editor preview is running.</summary>
        public void TickPreview()
        {
            if (running && preview)
            {
                Tick();
            }
        }

        /// <summary>Cancels pending work and releases all owned scene objects without waiting for the worker.</summary>
        public void ClearGenerated()
        {
            running = false;
            cancellation?.Cancel();
            cancellation?.Dispose();
            cancellation = null;
            if (worker != null)
            {
                // Observe abandoned failures without touching any Unity objects from a continuation.
                worker.ContinueWith(task => { _ = task.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
                worker = null;
            }

            population?.Dispose();
            population = null;
            populatingChunk = null;
            chunks.Clear();
            wanted.Clear();
            if (generatedRoot)
            {
                generatedRoot.gameObject.SetActive(false);
                foreach (WorldChunk chunk in generatedRoot.GetComponentsInChildren<WorldChunk>(true))
                {
                    chunk.DestroyChunk();
                }
                WorldChunk.Release(generatedRoot.gameObject);
            }

        }

        /// <summary>Relocation waits for the destination's 3x3 collider neighborhood before movement resumes.</summary>
        /// <param name="horizontalPosition">Destination in world XZ metres; ignored outside a running runtime world.</param>
        public void TravelTo(Vector2 horizontalPosition)
        {
            if (!running || preview || !Data.Contains(horizontalPosition))
            {
                return;
            }

            Data.TryGetHeight(new Vector3(horizontalPosition.x, 0f, horizontalPosition.y), out float height);
            player.Relocate(new Vector3(horizontalPosition.x, height + spawnGroundClearance, horizontalPosition.y));
        }

        /// <summary>Selects the authored population profile for the terrain's depth stage.</summary>
        /// <param name="position">World XZ coordinates in metres, sampled from the active generation snapshot.</param>
        /// <returns>The shared shallow, middle or deep profile; the caller must not mutate it.</returns>
        public WorldContentProfile ContentAt(Vector2 position)
        {
            int stage = Data.Stage(Data.Height(position.x, position.y));
            return stage switch
            {
                0 => shallowContent,
                1 => middleContent,
                _ => deepContent
            };
        }

        /// <summary>Replaces active generation with a settings snapshot and a transient chunk root.</summary>
        /// <param name="isPreview">Whether generation is ticked explicitly by the editor rather than Update.</param>
        /// <param name="position">Preview center in world XZ metres; unused for runtime streaming.</param>
        private void Begin(bool isPreview, Vector2 position)
        {
            ClearGenerated();
            Assert.IsNotNull(settings, nameof(settings));
            Assert.IsNotNull(chunkPrefab, nameof(chunkPrefab));
            Assert.IsFalse(chunkPrefab.gameObject.activeSelf, "Chunk prefab must be inactive until population is initialized.");
            Assert.IsNotNull(terrainMaterial, nameof(terrainMaterial));
            Assert.IsNotNull(shallowContent, nameof(shallowContent));
            Assert.IsNotNull(middleContent, nameof(middleContent));
            Assert.IsNotNull(deepContent, nameof(deepContent));
            foreach (WorldContentProfile profile in new HashSet<WorldContentProfile> { shallowContent, middleContent, deepContent })
            {
                profile.ValidateContent();
            }
            Data = settings.CreateWorld();
            preview = isPreview;
            previewPosition = position;
            generatedRoot = new GameObject(isPreview ? "World preview" : "World chunks").transform;
            generatedRoot.SetParent(transform, false);
            generatedRoot.gameObject.hideFlags = HideFlags.DontSave;
            cancellation = new CancellationTokenSource();
            running = true;
        }

        /// <summary>Relocates the player to the active snapshot's spawn with collider-readiness gating.</summary>
        private void ReturnToSpawn()
        {
            TravelTo(Data.Spawn);
        }

        /// <summary>Checks whether every valid chunk in a position's 3x3 neighborhood has completed population.</summary>
        /// <param name="position">World XZ coordinates in metres.</param>
        /// <returns>False outside the world or while a required chunk is missing; otherwise true.</returns>
        private bool NeighborhoodReady(Vector2 position)
        {
            if (!Data.Contains(position))
            {
                return false;
            }

            Vector2Int center = Data.ChunkAt(position);
            for (int z = -1; z <= 1; z++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    Vector2Int coordinate = center + new Vector2Int(x, z);
                    if (Valid(coordinate) && !chunks.ContainsKey(coordinate))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>Advances one streaming phase: population, completed mesh upload, or worker scheduling.</summary>
        private void Tick()
        {
            Vector3 observer = preview ? new Vector3(previewPosition.x, 0f, previewPosition.y) : observerCamera.transform.position;
            Vector2 position = new Vector2(observer.x, observer.z);
            RefreshWanted(position);
            if (population != null)
            {
                if (DistanceToChunk(populatingChunk!.Coordinate, position) > releaseRadius)
                {
                    population.Dispose();
                    population = null;
                    populatingChunk.DestroyChunk();
                    populatingChunk = null;
                    return;
                }

                var timer = System.Diagnostics.Stopwatch.StartNew();
                for (int i = 0; i < populationStepsPerFrame; i++)
                {
                    bool pending;
                    try
                    {
                        pending = population.MoveNext();
                    }
                    catch (Exception error)
                    {
                        Debug.LogException(error, this);
                        ClearGenerated();
                        return;
                    }
                    if (!pending)
                    {
                        population.Dispose();
                        population = null;
                        WorldChunk completed = populatingChunk!;
                        completed.gameObject.SetActive(true);
                        chunks.Add(completed.Coordinate, completed);
                        populatingChunk = null;
                        break;
                    }

                    if (timer.Elapsed.TotalMilliseconds >= populationBudgetMilliseconds)
                    {
                        break;
                    }
                }

                return;
            }

            if (worker != null)
            {
                if (!worker.IsCompleted)
                {
                    return;
                }

                if (worker.IsFaulted)
                {
                    Debug.LogException(worker.Exception!, this);
                    ClearGenerated();
                    return;
                }

                ChunkMeshData result = worker.GetAwaiter().GetResult();
                worker = null;
                if (DistanceToChunk(result.Coordinate, position) > releaseRadius)
                {
                    return;
                }

                // Population finishes before the chunk becomes active.
                WorldChunk chunk = Instantiate(chunkPrefab, generatedRoot);
                Vector2 origin = Data.ChunkOrigin(result.Coordinate);
                chunk.transform.position = new Vector3(origin.x, 0f, origin.y);
                chunk.name = $"Chunk {result.Coordinate.x}, {result.Coordinate.y}";
                chunk.Build(result, terrainMaterial);
                populatingChunk = chunk;
                population = ChunkPopulation.Populate(this, chunk, preview);
                return;
            }

            if (wanted.Count <= 0)
                return;

            Vector2Int coordinate = wanted[0];
            WorldData snapshot = Data;
            CancellationToken token = cancellation!.Token;
            worker = Task.Run(() => ChunkMeshBuilder.Build(snapshot, coordinate, token), token);
        }

        /// <summary>Prioritizes unloaded nearby chunks and releases distant chunks.</summary>
        /// <param name="position">Streaming center in world XZ metres.</param>
        private void RefreshWanted(Vector2 position)
        {
            wanted.Clear();
            Vector2Int center = Data.ChunkAt(position);
            int radius = Mathf.CeilToInt(loadRadius / Data.ChunkSize) + 1;
            for (int z = center.y - radius; z <= center.y + radius; z++)
            {
                for (int x = center.x - radius; x <= center.x + radius; x++)
                {
                    var coordinate = new Vector2Int(x, z);
                    if (Valid(coordinate) && !chunks.ContainsKey(coordinate)
                        && DistanceToChunk(coordinate, position) <= loadRadius)
                    {
                        wanted.Add(coordinate);
                    }
                }
            }

            wanted.Sort((a, b) => DistanceToChunk(a, position).CompareTo(DistanceToChunk(b, position)));
            expired.Clear();
            foreach (KeyValuePair<Vector2Int, WorldChunk> entry in chunks)
            {
                if (DistanceToChunk(entry.Key, position) > releaseRadius)
                {
                    expired.Add(entry.Key);
                }
            }

            foreach (Vector2Int coordinate in expired)
            {
                WorldChunk chunk = chunks[coordinate];
                chunks.Remove(coordinate);
                chunk.DestroyChunk();
            }
        }

        /// <summary>Checks chunk indices against the finite generation grid.</summary>
        /// <param name="coordinate">Zero-based chunk indices.</param>
        /// <returns>Whether both indices belong to the active world.</returns>
        private bool Valid(Vector2Int coordinate)
        {
            return coordinate is { x: >= 0, y: >= 0 }
                   && coordinate.x < Data.ChunksPerSide && coordinate.y < Data.ChunksPerSide;
        }

        /// <summary>Calculates horizontal distance to the nearest point of a chunk's square footprint.</summary>
        /// <param name="coordinate">Chunk indices whose footprint is measured.</param>
        /// <param name="position">World XZ coordinates in metres.</param>
        /// <returns>Distance in metres, or zero when the point is inside or on the footprint.</returns>
        private float DistanceToChunk(Vector2Int coordinate, Vector2 position)
        {
            Vector2 origin = Data.ChunkOrigin(coordinate);
            float x = Mathf.Max(origin.x - position.x, 0f, position.x - origin.x - Data.ChunkSize);
            float z = Mathf.Max(origin.y - position.y, 0f, position.y - origin.y - Data.ChunkSize);
            return Mathf.Sqrt(x * x + z * z);
        }
    }
}
