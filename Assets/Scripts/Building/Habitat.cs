using System.Collections.Generic;
using DeepSky.Building.Layout;
using DeepSky.Building.Presentation;
using DeepSky.World.Generation;
using UnityEngine;
using UnityEngine.Assertions;

namespace DeepSky.Building
{
    /// <summary>Owns a constructed base and its room layout.</summary>
    public sealed class Habitat : MonoBehaviour
    {
        [SerializeField] private BuildingPlan plan = null!;
        [SerializeField] private BuildingKit kit = null!;
        [SerializeField] private Transform shell = null!;
        [SerializeField] private DeepSky.Animals.Movement.SwimmingObstacle swimmingObstacle = null!;

        private readonly List<BuildingPiece> pieces = new();
        private WorldData terrain = null!;

        public BuildingGrid Grid { get; private set; } = null!;
        public BuildingPlan Plan => plan;
        public Bounds Bounds { get; private set; } = new();
        public IEnumerable<BuildingPiece> Pieces => pieces;
        public event System.Action? Changed;

        /// <summary>Checks the saved prefab's references.</summary>
        private void Awake()
        {
            Assert.IsNotNull(plan, nameof(plan));
            Assert.IsNotNull(kit, nameof(kit));
            Assert.IsNotNull(shell, nameof(shell));
            Assert.IsNotNull(swimmingObstacle, nameof(swimmingObstacle));
            kit.ValidateKit();
        }

        /// <summary>Releases layout subscriptions when the base is destroyed.</summary>
        private void OnDestroy()
        {
            if (Grid != null)
            {
                Grid.Changed -= Rebuild;
            }
        }

        /// <summary>Creates session layout state and its physical shell.</summary>
        /// <param name="world">Terrain snapshot for foundation heights; not owned.</param>
        public void Initialize(WorldData world)
        {
            Assert.IsNull(Grid, "A habitat may only be initialized once.");
            terrain = world;
            Grid = plan.CreateLayout();
            Grid.Changed += Rebuild;
            Rebuild();
        }

        /// <summary>Locates a cell's floor center in world space.</summary>
        /// <param name="cell">Room coordinates, whether occupied or not.</param>
        /// <returns>World position in metres.</returns>
        public Vector3 CellCenter(Vector3Int cell)
        {
            return transform.position + new Vector3(cell.x * plan.CellSize, cell.y * plan.LevelHeight, cell.z * plan.CellSize);
        }

        /// <summary>Maps world space to the containing room.</summary>
        /// <param name="position">World position in metres.</param>
        /// <returns>Cell coordinates without occupancy checks.</returns>
        public Vector3Int CellAt(Vector3 position)
        {
            Vector3 local = position - transform.position;
            return new Vector3Int(Mathf.FloorToInt(local.x / plan.CellSize + .5f),
                Mathf.FloorToInt(local.y / plan.LevelHeight), Mathf.FloorToInt(local.z / plan.CellSize + .5f));
        }

        /// <summary>Checks whether a point lies in a dry room, above the entry waterline.</summary>
        /// <param name="position">World position in metres.</param>
        /// <returns>True inside an occupied room volume.</returns>
        public bool ContainsAir(Vector3 position)
        {
            return Grid != null && Grid.TryGet(CellAt(position), out _);
        }

        /// <summary>Builds non-overlapping row volumes for clipping underwater effects.</summary>
        /// <param name="output">Destination list; existing entries are retained.</param>
        public void AppendAirVolumes(List<Bounds> output)
        {
            var pending = new HashSet<Vector3Int>(Grid.Coordinates);
            foreach (Vector3Int cell in Grid.Coordinates)
            {
                if (!pending.Contains(cell))
                {
                    continue;
                }
                Vector3Int first = cell;
                while (pending.Contains(first + Vector3Int.left))
                {
                    first += Vector3Int.left;
                }
                int width = 0;
                while (pending.Remove(first + Vector3Int.right * width))
                {
                    width++;
                }
                Vector3 size = new(width * plan.CellSize, plan.LevelHeight, plan.CellSize);
                Vector3 center = CellCenter(first) + new Vector3((width - 1) * plan.CellSize * .5f, plan.LevelHeight * .5f, 0f);
                output.Add(new Bounds(center, size));
            }
        }

        /// <summary>Reconciles the physical shell with occupied rooms and exposed boundaries.</summary>
        private void Rebuild()
        {
            foreach (BuildingPiece piece in pieces)
            {
                piece.gameObject.SetActive(false);
                Destroy(piece.gameObject);
            }
            pieces.Clear();
            bool first = true;
            foreach (Vector3Int cell in Grid.Coordinates)
            {
                Grid.TryGet(cell, out RoomCell room);
                Vector3 center = CellCenter(cell);
                var volume = new Bounds(center + Vector3.up * plan.LevelHeight * .5f,
                    new Vector3(plan.CellSize, plan.LevelHeight, plan.CellSize));
                if (first)
                {
                    Bounds = volume;
                    first = false;
                }
                else
                {
                    Bounds bounds = Bounds;
                    bounds.Encapsulate(volume);
                    Bounds = bounds;
                }
                if (room.Floor == RoomFloor.Solid)
                {
                    Add(kit.Floor, cell, center, Quaternion.identity, new Vector3(plan.CellSize, 1f, plan.CellSize));
                }
                else
                {
                    if (cell.y == 0)
                    {
                        Add(kit.Water, cell, center, Quaternion.identity, new Vector3(plan.CellSize, 1f, plan.CellSize));
                    }
                    if (room.Floor == RoomFloor.Ladder)
                    {
                        // The climbing face points away from the landing so W exits forward onto it.
                        Add(kit.Ladder, cell, center, Quaternion.Euler(0f, (int)room.LadderSide * 90f + 180f, 0f),
                            new Vector3(1f, plan.LevelHeight / 3.5f, 1f), room.LadderSide);
                    }
                }
                if (!Grid.TryGet(cell + Vector3Int.up, out _))
                {
                    Add(kit.Ceiling, cell, center + Vector3.up * plan.LevelHeight, Quaternion.identity,
                        new Vector3(plan.CellSize, 1f, plan.CellSize));
                }
                for (int i = 0; i < 4; i++)
                {
                    RoomSide side = (RoomSide)i;
                    Vector3Int offset = RoomCell.Offset(side);
                    if (!Grid.TryGet(cell + offset, out _))
                    {
                        Add(room.HasWindow(side) ? kit.Window : kit.Wall, cell,
                            center + (Vector3)offset * plan.CellSize * .5f,
                            Quaternion.Euler(0f, i * 90f + 180f, 0f), new Vector3(plan.CellSize, plan.LevelHeight / 3.5f, 1f), side);
                    }
                }
                if (cell.y == 0 && room.Floor == RoomFloor.Solid && cell.x % 2 == 0 && cell.z % 2 == 0)
                {
                    float height = Mathf.Max(.1f, center.y - terrain.Height(center.x, center.z));
                    Add(kit.Support, cell, center, Quaternion.identity, new Vector3(1f, height, 1f));
                }
            }
            swimmingObstacle.SetBounds(Bounds);
            Changed?.Invoke();
        }

        /// <summary>Instantiates one module and binds its interaction identity.</summary>
        /// <param name="prefab">Shared kit prefab.</param>
        /// <param name="cell">Owning room coordinates.</param>
        /// <param name="position">World placement in metres.</param>
        /// <param name="rotation">World rotation.</param>
        /// <param name="scale">Module scale relative to its one-metre grid contract.</param>
        /// <param name="side">Wall direction or the ladder's solid landing direction, if applicable.</param>
        private void Add(BuildingPiece prefab, Vector3Int cell, Vector3 position, Quaternion rotation, Vector3 scale, RoomSide side = RoomSide.North)
        {
            BuildingPiece piece = Instantiate(prefab, position, rotation, shell);
            piece.transform.localScale = scale;
            piece.Bind(this, cell, side);
            pieces.Add(piece);
        }
    }
}
