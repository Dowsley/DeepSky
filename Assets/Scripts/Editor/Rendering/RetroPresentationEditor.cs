using DeepSky.Rendering.CameraEffects;
using UnityEditor;
using UnityEngine;

namespace DeepSky.Editor.Rendering
{
    /// <summary>Edits the camera's retro presentation.</summary>
    [CustomEditor(typeof(RetroPresentation))]
    public sealed class RetroPresentationEditor : EffectSettingsEditor
    {
        protected override string TuningHint => "Use the effect checkboxes independently. Dithering requires color "
            + "quantization. Higher virtual resolution gives finer pixels; higher color levels give smoother gradients. "
            + "Presentation blend 0 removes the treatment.";

        /// <summary>Finds the retro material assigned to this camera.</summary>
        /// <returns>The shared material, or null when unassigned.</returns>
        protected override Object? GetSettingsAsset()
        {
            return serializedObject.FindProperty("material").objectReferenceValue;
        }
    }
}
