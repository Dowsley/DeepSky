using DeepSky.Rendering;
using UnityEngine;

namespace DeepSky.Vegetation
{
    [ExecuteAlways]
    public sealed class KelpAppearance : MonoBehaviour
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int WaveHeight = Shader.PropertyToID("_WaveHeight");

        [SerializeField, ColorUsage(false)] private Color tint = new(0.18f, 0.7f, 0.28f, 1f);
    
        [Tooltip("Interpolates between the minimum and maximum brightness multipliers.")]
        [SerializeField, Range(0f, 1f)] private float brightness = 0.5f;
        
        [Tooltip("Tint multipliers at brightness 0 and 1, respectively.")]
        [SerializeField] private Vector2 brightnessRange = new(0.6f, 1f);
        
        [Tooltip("Height scale used by the plant deformation shader.")]
        [SerializeField, Min(0.01f)] private float waveHeight = 13f;

        private void OnEnable()
        {
            Apply();
        }

        private void OnValidate()
        {
            Apply();
        }

        private void Apply()
        {
            var properties = new MaterialPropertyBlock();
            foreach (var renderer in GetComponentsInChildren<Renderer>())
            {
                renderer.GetPropertyBlock(properties);
                ShaderColors.SetDisplayTint(properties, BaseColor, tint, brightness, brightnessRange);
                properties.SetFloat(WaveHeight, waveHeight);
                renderer.SetPropertyBlock(properties);
            }
        }
    }
}
