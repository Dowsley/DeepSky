using UnityEngine;
using UnityEngine.Assertions;

namespace DeepSky.Equipment.Presentation
{
    /// <summary>Presents the drill's rotor and motor audio.</summary>
    public sealed class DrillView : ToolView
    {
        [Header("Drill references")]
        [SerializeField] private Transform drillRotor = null!;
        [Tooltip("Audio Source with the motor clip assigned, Loop enabled and Play On Awake disabled.")]
        [SerializeField] private AudioSource audioSource = null!;

        [Header("Rotor motion")]
        [SerializeField, Min(0f)] private float drillDegreesPerSecond = 1100f;
        [Tooltip("Nonzero spin axis in the authored rotor's local coordinates.")]
        [SerializeField] private Vector3 drillAxis = Vector3.up;

        private bool drilling = false;

        /// <summary>Validates the rotor and prefab-authored looping motor audio.</summary>
        protected override void Awake()
        {
            base.Awake();
            Assert.IsNotNull(drillRotor, nameof(drillRotor));
            Assert.IsNotNull(audioSource, nameof(audioSource));
            Assert.IsNotNull(audioSource.clip, "Drill Audio Source requires a motor clip.");
            Assert.IsTrue(audioSource.loop, "Drill motor audio must loop.");
            Assert.IsFalse(audioSource.playOnAwake, "Drill motor audio must not play on awake.");
            Assert.IsTrue(drillAxis.sqrMagnitude > 0f, "Drill spin axis must be nonzero.");
        }

        /// <summary>Applies held-tool motion and turns the rotor while the drill is running.</summary>
        protected override void LateUpdate()
        {
            base.LateUpdate();
            if (drilling)
            {
                drillRotor.Rotate(drillAxis, drillDegreesPerSecond * Time.deltaTime, Space.Self);
            }
        }

        /// <summary>Stops the motor and resets held-tool motion when unequipped.</summary>
        protected override void OnDisable()
        {
            SetDrilling(false);
            base.OnDisable();
        }

        /// <summary>Starts or stops rotor motion and motor audio without changing resource extraction.</summary>
        /// <param name="active">True while the equipped drill's trigger is held during gameplay.</param>
        public void SetDrilling(bool active)
        {
            if (drilling == active)
            {
                return;
            }
            drilling = active;
            if (active)
            {
                audioSource.Play();
            }
            else
            {
                audioSource.Stop();
            }
        }
    }
}
