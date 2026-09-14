using UnityEngine;

namespace DeepSky.Harvesting
{
    /// <summary>Tracks sustained extraction at one contact point.</summary>
    internal sealed class DrillContact
    {
        private float elapsed = 0f;
        private float lastContact = float.NegativeInfinity;
        private Vector3 position = Vector3.zero;

        /// <summary>Accumulates contact, resetting after interruption or movement to another spot.</summary>
        /// <param name="point">Contact position in world metres.</param>
        /// <param name="seconds">Nonnegative simulation time contributed by this contact.</param>
        /// <param name="duration">Positive simulation seconds required per unit.</param>
        /// <returns>Whether one unit is ready to collect.</returns>
        internal bool Advance(Vector3 point, float seconds, float duration)
        {
            if (Time.time - lastContact > .25f || (point - position).sqrMagnitude > .75f * .75f)
            {
                elapsed = 0f;
                position = point;
            }
            lastContact = Time.time;
            elapsed = Mathf.Min(duration, elapsed + Mathf.Max(0f, seconds));
            return elapsed >= duration;
        }

        /// <summary>Clears extraction time after collection or a target change.</summary>
        internal void Reset()
        {
            elapsed = 0f;
            lastContact = float.NegativeInfinity;
        }
    }
}
