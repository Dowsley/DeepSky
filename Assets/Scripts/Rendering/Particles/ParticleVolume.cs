using UnityEngine;
using UnityEngine.Assertions;

namespace DeepSky.Rendering.Particles
{
    /// <summary>
    /// Keeps a world-space particle system inside its spherical emission volume.
    /// The emitter follows the observer; particles retain world-space motion and native module state.
    /// Initial population is simulated after observer placement; disable native Play On Awake and Prewarm.
    /// The system duration must cover a complete particle lifetime for a fully populated initial volume.
    /// Material distance fading must hide particles before they reach the shape radius.
    /// </summary>
    [RequireComponent(typeof(ParticleSystem)), DisallowMultipleComponent]
    public sealed class ParticleVolume : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ParticleSystem particles = null!;
        [SerializeField] private Transform observer = null!;

        private ParticleSystem.Particle[] buffer = System.Array.Empty<ParticleSystem.Particle>();
        private Vector3 preparedPosition = Vector3.zero;
        private bool prepared = false;

        public bool CameraStateCurrent => prepared
            && (observer.position - preparedPosition).sqrMagnitude < .00000001f;

        /// <summary>Validates spherical world-space simulation and allocates a reusable particle buffer.</summary>
        private void Awake()
        {
            Assert.IsNotNull(particles, nameof(particles));
            Assert.IsNotNull(observer, nameof(observer));
            Assert.AreEqual(ParticleSystemSimulationSpace.World, particles.main.simulationSpace);
            Assert.AreEqual(ParticleSystemShapeType.Sphere, particles.shape.shapeType);
            Assert.IsTrue(particles.shape.radius > 0f);
            buffer = new ParticleSystem.Particle[particles.main.maxParticles];
        }

        /// <summary>Clears simulation until the first LateUpdate positions and prewarms the emission volume.</summary>
        private void OnEnable()
        {
            prepared = false;
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        /// <summary>Follows the observer, wraps escaped particles and repopulates the volume after large teleports.</summary>
        private void LateUpdate()
        {
            Vector3 center = observer.position;
            transform.SetPositionAndRotation(center, Quaternion.identity);
            float radius = particles.shape.radius;

            // Start and Update can relocate the observer before the first rendered frame.
            if (!prepared || (center - preparedPosition).sqrMagnitude > 4f * radius * radius)
            {
                particles.Simulate(particles.main.duration, true, true);
                particles.Play();
            }

            if (buffer.Length != particles.main.maxParticles)
            {
                buffer = new ParticleSystem.Particle[particles.main.maxParticles];
            }

            int count = particles.GetParticles(buffer);
            bool changed = false;
            for (int i = 0; i < count; i++)
            {
                Vector3 relative = buffer[i].position - center;
                float distance = relative.magnitude;
                if (distance <= radius)
                {
                    continue;
                }

                // Cross to the opposite hidden edge, preserving overshoot and particle lifetime.
                float wrappedDistance = Mathf.Repeat(distance + radius, 2f * radius) - radius;
                buffer[i].position = center + relative * (wrappedDistance / distance);
                changed = true;
            }

            if (changed)
            {
                particles.SetParticles(buffer, count);
            }

            preparedPosition = center;
            prepared = true;
        }

        /// <summary>Marks the volume as unprepared while updates are disabled.</summary>
        private void OnDisable()
        {
            prepared = false;
        }
    }
}
