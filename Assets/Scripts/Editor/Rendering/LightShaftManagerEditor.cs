using DeepSky.Rendering.LightShafts;
using UnityEditor;
using UnityEngine;

namespace DeepSky.Editor.Rendering
{
    /// <summary>Edits camera-relative light shafts.</summary>
    [CustomEditor(typeof(LightShaftManager))]
    public sealed class LightShaftManagerEditor : EffectSettingsEditor
    {
        protected override string TuningHint => "Show Shafts toggles visibility. Maximum Opacity and Fade Speed tune "
            + "strength and motion. Population and prefab changes require disabling and re-enabling the component.";

        /// <summary>Finds the material on the assigned shaft prefab.</summary>
        /// <returns>The shared shaft material, or null when the prefab is unassigned.</returns>
        protected override Object? GetSettingsAsset()
        {
            var renderer = serializedObject.FindProperty("shaftPrefab").objectReferenceValue as Renderer;
            return renderer ? renderer.sharedMaterial : null;
        }
    }
}
