using DeepSky.Rendering.Atmosphere;
using UnityEditor;
using UnityEngine;

namespace DeepSky.Editor.Rendering
{
    /// <summary>Edits the camera's depth-dependent atmosphere.</summary>
    [CustomEditor(typeof(DepthAtmosphere))]
    public sealed class DepthAtmosphereEditor : EffectSettingsEditor
    {
        protected override string TuningHint => "Expand Depth bands to tune water colors, ambient light, visibility, "
            + "caustics and shaft limits at each depth. Depths and distances are in metres. "
            + "Daylight is a manual multiplier, not a day/night cycle.";

        /// <summary>Finds the atmosphere profile assigned to this camera.</summary>
        /// <returns>The shared profile, or null when unassigned.</returns>
        protected override Object? GetSettingsAsset()
        {
            return serializedObject.FindProperty("profile").objectReferenceValue;
        }
    }
}
