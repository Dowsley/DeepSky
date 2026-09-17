using DeepSky.Rendering.CameraEffects;
using UnityEditor;
using UnityEngine;

namespace DeepSky.Editor.Rendering
{
    /// <summary>Edits camera bloom and edge shading.</summary>
    [CustomEditor(typeof(UnderwaterBloom))]
    public sealed class UnderwaterBloomEditor : EffectSettingsEditor
    {
        protected override string TuningHint => "Bloom intensity 0 disables glow. Screen edge shading 0 disables "
            + "the vignette. The component checkbox disables both. Quality changes the blur-buffer resolution.";

        /// <summary>Finds the bloom material assigned to this camera.</summary>
        /// <returns>The shared material, or null when unassigned.</returns>
        protected override Object? GetSettingsAsset()
        {
            return serializedObject.FindProperty("material").objectReferenceValue;
        }
    }
}
