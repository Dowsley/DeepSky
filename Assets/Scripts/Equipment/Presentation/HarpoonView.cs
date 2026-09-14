using UnityEngine;
using UnityEngine.Assertions;

namespace DeepSky.Equipment.Presentation
{
    /// <summary>Presents the harpoon's firing and reload motion.</summary>
    public sealed class HarpoonView : ToolView
    {
        [Header("Harpoon references")]
        [SerializeField] private Transform muzzle = null!;
        [Tooltip("Audio Source with the firing clip assigned, Loop and Play On Awake disabled.")]
        [SerializeField] private AudioSource audioSource = null!;

        [Header("Reload motion")]
        [SerializeField] private Vector3 actionOffset = new(0f, -.025f, -.02f);
        [SerializeField] private Vector3 actionEuler = new(40f, 0f, 0f);

        private readonly ActionEnvelope reload = new();

        public Transform Muzzle => muzzle;

        /// <summary>Validates the muzzle and authored firing audio after capturing the neutral pose.</summary>
        protected override void Awake()
        {
            base.Awake();
            Assert.IsNotNull(muzzle, nameof(muzzle));
            Assert.IsNotNull(audioSource, nameof(audioSource));
            Assert.IsNotNull(audioSource.clip, "Harpoon Audio Source requires a firing clip.");
            Assert.IsFalse(audioSource.loop, "Harpoon firing audio must not loop.");
            Assert.IsFalse(audioSource.playOnAwake, "Harpoon firing audio must not play on awake.");
        }

        /// <summary>Lowers and returns the launcher during reload, with idle motion.</summary>
        protected override void LateUpdate()
        {
            float weight = reload.Advance(Time.deltaTime);
            ApplyPose(actionOffset * weight, actionEuler * weight);
        }

        /// <summary>Stops firing audio, cancels reload motion and resets the held pose when unequipped.</summary>
        protected override void OnDisable()
        {
            audioSource.Stop();
            reload.Reset();
            base.OnDisable();
        }

        /// <summary>Plays firing audio and starts the complete reload motion.</summary>
        /// <param name="reloadSeconds">Reload duration in simulation seconds, clamped to at least 0.01.</param>
        public void PlayShot(float reloadSeconds)
        {
            reload.Start(reloadSeconds);
            audioSource.Play();
        }
    }
}
