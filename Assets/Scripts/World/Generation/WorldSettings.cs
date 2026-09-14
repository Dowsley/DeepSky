using UnityEngine;
using UnityEngine.Serialization;

namespace DeepSky.World.Generation
{
    /// <summary>Authoring settings sampled once per generation. Distances and depths are metres.</summary>
    [CreateAssetMenu(menuName = "DeepSky/World Settings")]
    public sealed class WorldSettings : ScriptableObject
    {
        [Header("Identity and dimensions")]
        [SerializeField] private int seed = 2404;
        [SerializeField, Min(8)] private int chunksPerSide = 52;
        [SerializeField, Min(8f)] private float chunkSize = 32f;
        [SerializeField, Range(8, 128)] private int subdivisions = 64;

        [Header("Depth shelves")]
        [Tooltip("Depth below sea level for shallow central basins, middle regions and deep outer regions.")]
        [SerializeField] private Vector3 shelfDepths = new Vector3(100f, 200f, 300f);
        [Tooltip("Outer edges of the middle (X) and shallow (Y) regions, as fractions of world width.")]
        [SerializeField] private Vector2 shelfRadii = new Vector2(0.2625f, 0.15f);
        [SerializeField, Range(0.3f, 0.48f)] private float exteriorRadius = 0.375f;
        [SerializeField, Min(300f)] private float exteriorDepth = 400f;
        [Tooltip("Horizontal displacement of shelf boundaries in metres. Higher values make their outlines less circular.")]
        [SerializeField, Min(0f)] private float coastIrregularity = 180f;
        [Tooltip("Spacing of boundary noise in metres. Smaller values create more frequent bends and can steepen local transitions.")]
        [SerializeField, Min(1f)] private float boundaryWavelength = 180f;
        [Tooltip("Horizontal distance over which a shelf changes depth, outside descent corridors. Increase to soften drops.")]
        [InspectorName("Shelf Transition Width")]
        [SerializeField, Min(1f)] private float cliffWidth = 32f;
        [Tooltip("Wide transitions along two opposite directions form walkable routes between shelves.")]
        [SerializeField, Min(1f)] private float rampWidth = 96f;
        [Tooltip("Half-width in radians of each gradual descent corridor.")]
        [SerializeField, Range(0.1f, 1f)] private float rampAngularWidth = 0.45f;

        [Header("Basin layout")]
        [Tooltip("Coarse elevation lattice, independent of rendering chunk size.")]
        [SerializeField, Min(4f)] private float layoutCellSize = 12.8f;
        [SerializeField, Min(12.8f)] private float terraceWavelength = 64f;
        [Tooltip("Maximum extra basin depth within the shallow and middle stages. Interiors stay level.")]
        [SerializeField, Min(0f)] private float terraceDepth = 50f;
        [SerializeField, Range(1, 8)] private int terraceSteps = 3;
        [SerializeField, Range(0, 12)] private int boundarySmoothingPasses = 3;

        [Header("Dune noise")]
        [Tooltip("Base noise wavelength in metres. Each octave doubles frequency.")]
        [FormerlySerializedAs("hillWavelength")]
        [SerializeField, Min(1f)] private float duneWavelength = 30f;
        [Tooltip("Multiplier for additive smoothed noise. Octaves are not normalized; this is not a hill height guarantee.")]
        [FormerlySerializedAs("hillHeight")]
        [SerializeField, Min(0f)] private float duneHeight = 10f;
        [SerializeField, Range(1, 6)] private int duneOctaves = 4;
        [Tooltip("Amplitude multiplier per successive octave.")]
        [SerializeField, Range(0f, 1f)] private float noisePersistence = 0.7f;

        [Header("Localized outcrop noise")]
        [SerializeField, Min(1f)] private float outcropWavelength = 12f;
        [SerializeField, Min(0f)] private float outcropHeight = 45f;
        [SerializeField, Range(1, 6)] private int outcropOctaves = 4;
        [Tooltip("Only noise above this threshold adds height. Increase for fewer outcrops.")]
        [SerializeField, Range(0f, 1f)] private float outcropThreshold = 0.3f;
        [SerializeField, Min(0f)] private float outcropSoftCap = 22.5f;
        [SerializeField, Min(1f)] private float outcropCompression = 20f;

        [Header("Terrain material")]
        [Tooltip("Surface slope angles where sand starts and finishes transitioning to rock.")]
        [SerializeField] private Vector2 rockSlope = new Vector2(22f, 48f);

        [Header("Starting area")]
        [Tooltip("Preferred normalized XZ. Generation searches nearby shallow basins for a suitable start.")]
        [SerializeField] private Vector2 spawnPosition = new Vector2(-0.08f, 0f);
        [Tooltip("Radius within which the coarse basin floor must remain near level. Dunes are preserved.")]
        [SerializeField, Min(10f)] private float spawnBasinRadius = 38.4f;
        [SerializeField, Min(0.1f)] private float spawnBasinTolerance = 0.5f;
        [Tooltip("Maximum local dune slope at the chosen spawn point, in degrees.")]
        [SerializeField, Range(0f, 45f)] private float spawnMaximumSlope = 20f;
        [Tooltip("Sand-zone radius around spawn. Suppresses outcrops without flattening dunes.")]
        [SerializeField, Min(0f)] private float spawnOutcropRadius = 25.6f;
        [Tooltip("Distance over which outcrops return outside the sand zone.")]
        [SerializeField, Min(0.1f)] private float spawnOutcropFade = 12.8f;
        [Tooltip("Vegetation exclusion radius, independent of terrain relief.")]
        [SerializeField, Min(1f)] private float spawnClearingRadius = 6f;

        /// <summary>Snapshots the authored settings into an immutable, worker-safe world sampler.</summary>
        /// <returns>A world with its basin lattice built and spawn selected.</returns>
        /// <exception cref="System.InvalidOperationException">No shallow basin satisfies the spawn constraints.</exception>
        public WorldData CreateWorld()
        {
            var layout = new BasinLayout(seed, chunksPerSide * chunkSize, layoutCellSize, shelfDepths,
                shelfRadii, exteriorRadius, exteriorDepth, coastIrregularity, boundaryWavelength,
                cliffWidth, rampWidth, rampAngularWidth, terraceWavelength, terraceDepth, terraceSteps, boundarySmoothingPasses);
            var relief = new TerrainRelief(seed, duneWavelength, duneHeight, duneOctaves, noisePersistence,
                outcropWavelength, outcropHeight, outcropOctaves, outcropThreshold, outcropSoftCap, outcropCompression);
            return new WorldData(seed, chunksPerSide, chunkSize, subdivisions, shelfDepths, rockSlope,
                spawnPosition, spawnClearingRadius, spawnBasinRadius, spawnBasinTolerance, spawnMaximumSlope,
                spawnOutcropRadius, spawnOutcropFade,
                exteriorDepth + duneHeight * duneOctaves, layout, relief);
        }

        /// <summary>Constrains dimensions, shelf ordering and sampling intervals to valid authoring ranges.</summary>
        private void OnValidate()
        {
            chunksPerSide = Mathf.Clamp(chunksPerSide, 8, 128);
            chunkSize = Mathf.Max(8f, chunkSize);
            subdivisions = Mathf.Clamp(subdivisions, 8, 128);
            shelfDepths.x = Mathf.Max(10f, shelfDepths.x);
            shelfDepths.y = Mathf.Max(shelfDepths.x, shelfDepths.y);
            shelfDepths.z = Mathf.Max(shelfDepths.y, shelfDepths.z);
            shelfRadii.x = Mathf.Clamp(shelfRadii.x, 0.1f, 0.4f);
            shelfRadii.y = Mathf.Clamp(shelfRadii.y, 0.05f, shelfRadii.x - 0.02f);
            exteriorRadius = Mathf.Clamp(exteriorRadius, shelfRadii.x + 0.02f, 0.48f);
            exteriorDepth = Mathf.Max(shelfDepths.z, exteriorDepth);
            boundaryWavelength = Mathf.Max(1f, boundaryWavelength);
            cliffWidth = Mathf.Max(1f, cliffWidth);
            rampWidth = Mathf.Max(cliffWidth, rampWidth);
            layoutCellSize = Mathf.Max(4f, layoutCellSize);
            terraceWavelength = Mathf.Max(layoutCellSize, terraceWavelength);
            terraceDepth = Mathf.Max(0f, terraceDepth);
            terraceSteps = Mathf.Clamp(terraceSteps, 1, 8);
            boundarySmoothingPasses = Mathf.Clamp(boundarySmoothingPasses, 0, 12);
            duneOctaves = Mathf.Clamp(duneOctaves, 1, 6);
            outcropOctaves = Mathf.Clamp(outcropOctaves, 1, 6);
            noisePersistence = Mathf.Clamp01(noisePersistence);
            spawnBasinRadius = Mathf.Max(10f, spawnBasinRadius);
            spawnBasinTolerance = Mathf.Max(0.1f, spawnBasinTolerance);
            spawnOutcropRadius = Mathf.Max(0f, spawnOutcropRadius);
            spawnOutcropFade = Mathf.Max(0.1f, spawnOutcropFade);
            rockSlope.y = Mathf.Max(rockSlope.x + 1f, rockSlope.y);
            spawnPosition = Vector2.ClampMagnitude(spawnPosition, 0.44f);
        }
    }
}
