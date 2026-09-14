using UnityEngine;

namespace DeepSky.Animals.Fish
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class FishAppearance : MonoBehaviour
    {
        private const float TailDisplacementAmplitude = 2f;
        private const float MinimumWaveHeight = 0.01f;
        private static readonly int Phase = Shader.PropertyToID("_Phase");
        private static readonly int WaveHeight = Shader.PropertyToID("_WaveHeight");

        [Header("Tail animation")]
        [SerializeField] private float phase = 0f;
        [SerializeField, Min(MinimumWaveHeight)] private float waveHeight = 3.5f;

        /// <summary>Applies tail animation settings when the component becomes active.</summary>
        private void OnEnable()
        {
            Apply();
        }

        /// <summary>Clamps the wave height and refreshes the renderer settings.</summary>
        private void OnValidate()
        {
            waveHeight = Mathf.Max(MinimumWaveHeight, waveHeight);
            Apply();
        }

        /// <summary>Updates tail shader parameters and culling bounds for the fish's renderers.</summary>
        private void Apply()
        {
            float safeWaveHeight = Mathf.Max(MinimumWaveHeight, waveHeight);
            var properties = new MaterialPropertyBlock();
            foreach (var renderer in GetComponentsInChildren<MeshRenderer>(true))
            {
                renderer.GetPropertyBlock(properties);
                float instancePhase = Application.isPlaying
                    ? Mathf.Repeat(transform.position.sqrMagnitude, Mathf.PI * 2f)
                    : 0f;
                properties.SetFloat(Phase, phase + instancePhase);
                properties.SetFloat(WaveHeight, safeWaveHeight);
                renderer.SetPropertyBlock(properties);

                var filter = renderer.GetComponent<MeshFilter>();
                if (!filter || !filter.sharedMesh)
                {
                    continue;
                }

                Bounds bounds = filter.sharedMesh.bounds;
                float extent = Mathf.Max(Mathf.Abs(bounds.min.z), Mathf.Abs(bounds.max.z)) / safeWaveHeight;
                // Must enclose the tail displacement calculated by Fish.shader.
                float displacement = TailDisplacementAmplitude * extent * extent;
                bounds.Expand(new Vector3(2f * displacement, 0f, 0f));
                renderer.localBounds = bounds;
            }
        }
    }
}
