using UnityEngine;
using UnityEngine.Assertions;

namespace DeepSky.Harvesting
{
    /// <summary>Plays an authored one-shot underwater impact prefab and releases it after its particles finish.</summary>
    public sealed class HarvestImpact : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ParticleSystem particles = null!;
        [SerializeField] private AudioSource audioSource = null!;

        [Header("Lifetime")]
        [SerializeField, Min(.1f)] private float lifetime = 2f;

        /// <summary>Starts configured debris and audio, then schedules cleanup of this transient instance.</summary>
        private void Start()
        {
            Assert.IsNotNull(particles, nameof(particles));
            Assert.IsNotNull(audioSource, nameof(audioSource));
            particles.Play(true);
            if (audioSource.clip)
            {
                audioSource.Play();
            }
            Destroy(gameObject, lifetime);
        }
    }
}
