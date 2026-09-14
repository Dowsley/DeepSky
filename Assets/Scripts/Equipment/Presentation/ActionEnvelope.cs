using UnityEngine;

namespace DeepSky.Equipment.Presentation
{
    /// <summary>Tracks a timed outward-and-return motion weight.</summary>
    internal sealed class ActionEnvelope
    {
        private float duration = 0f;
        private float remaining = 0f;

        /// <summary>Starts or restarts the motion from rest.</summary>
        /// <param name="seconds">Total duration in simulation seconds, clamped to at least 0.01.</param>
        public void Start(float seconds)
        {
            duration = Mathf.Max(.01f, seconds);
            remaining = duration;
        }

        /// <summary>Advances the timer and evaluates its sinusoidal motion weight.</summary>
        /// <param name="deltaTime">Elapsed simulation seconds; negative values do not advance time.</param>
        /// <returns>A weight from zero to one, peaking halfway through and returning to zero on completion.</returns>
        public float Advance(float deltaTime)
        {
            remaining = Mathf.Max(0f, remaining - Mathf.Max(0f, deltaTime));
            return remaining > 0f ? Mathf.Sin(Mathf.PI * (1f - remaining / duration)) : 0f;
        }

        /// <summary>Cancels the motion and returns its weight to zero.</summary>
        public void Reset()
        {
            duration = 0f;
            remaining = 0f;
        }
    }
}
