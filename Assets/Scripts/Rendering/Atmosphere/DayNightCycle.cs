using System;
using UnityEngine;

namespace DeepSky.Rendering.Atmosphere
{
    /// <summary>Advances the atmosphere's daily lighting cycle.</summary>
    [DisallowMultipleComponent]
    public sealed class DayNightCycle : MonoBehaviour
    {
        private const float HoursPerDay = 24f;

        [Tooltip("Initial hour, also used for the Scene view preview outside Play mode.")]
        [SerializeField, Range(0f, 24f)] private float startHour = 12f;
        [SerializeField] private bool advanceTime = true;
        [Tooltip("Real-time minutes per complete day at normal game speed.")]
        [SerializeField, Min(.1f)] private float dayLengthMinutes = 20f;
        [Tooltip("Lighting multiplier by hour, clamped to 0..1. Match hours 0 and 24 for a seamless midnight.")]
        [SerializeField] private AnimationCurve daylightByHour = new AnimationCurve(
            new Keyframe(0f, .3f), new Keyframe(5f, .3f), new Keyframe(8f, 1f),
            new Keyframe(16f, 1f), new Keyframe(19f, .3f), new Keyframe(24f, .3f));

        private float elapsedHours = 0f;

        public float TimeOfDay => Mathf.Repeat(startHour + elapsedHours, HoursPerDay);
        internal float Daylight => Mathf.Clamp01(daylightByHour.Evaluate(TimeOfDay));

        /// <summary>Advances the private clock using scaled game time; pausing the game also pauses the cycle.</summary>
        private void Update()
        {
            if (advanceTime)
            {
                elapsedHours = Mathf.Repeat(elapsedHours + Time.deltaTime * HoursPerDay
                    / (Mathf.Max(.1f, dayLengthMinutes) * 60f), HoursPerDay);
            }
        }

        /// <summary>Sets the running clock without changing the authored starting hour.</summary>
        /// <param name="hour">Finite time in hours; values wrap into the 24-hour day.</param>
        /// <exception cref="ArgumentOutOfRangeException">The supplied hour is not finite.</exception>
        public void SetTimeOfDay(float hour)
        {
            if (float.IsNaN(hour) || float.IsInfinity(hour))
            {
                throw new ArgumentOutOfRangeException(nameof(hour), "Time of day must be finite.");
            }

            elapsedHours = Mathf.Repeat(hour - startHour, HoursPerDay);
        }
    }
}
