using DeepSky.Rendering;
using UnityEngine;

namespace DeepSky.Vegetation
{
    [ExecuteAlways]
    public sealed class AlgaeAppearance : MonoBehaviour
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        [SerializeField, ColorUsage(false)] private Color tint = Color.white;
        
        [Tooltip("Interpolates between the minimum and maximum brightness multipliers.")]
        [SerializeField, Range(0f, 1f)] private float brightness = 0.5f;
        
        [Tooltip("Tint multipliers at brightness 0 and 1, respectively.")]
        [SerializeField] private Vector2 brightnessRange = new(0.5f, 1.4f);

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
                renderer.SetPropertyBlock(properties);
            }
        }
    }
}
