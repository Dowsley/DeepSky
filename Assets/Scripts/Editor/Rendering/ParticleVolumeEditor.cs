using DeepSky.Rendering.Particles;
using UnityEditor;
using UnityEngine;

namespace DeepSky.Editor.Rendering
{
    /// <summary>Edits the appearance of camera-following particles.</summary>
    [CustomEditor(typeof(ParticleVolume))]
    public sealed class ParticleVolumeEditor : EffectSettingsEditor
    {
        protected override string TuningHint => "Tune opacity, tint and distance fading below. Use this object's "
            + "Particle System for size, emission and lifetime. Distance fades must hide particles before the "
            + "spherical emission boundary. Disable the GameObject to hide this particle layer.";

        /// <summary>Finds the shared material of the referenced particle system.</summary>
        /// <returns>The particle material, or null when the system or renderer is unassigned.</returns>
        protected override Object? GetSettingsAsset()
        {
            var particles = serializedObject.FindProperty("particles").objectReferenceValue as ParticleSystem;
            if (!particles)
            {
                return null;
            }
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            return renderer ? renderer.sharedMaterial : null;
        }
    }
}
