using UnityEngine;

namespace DeepSky.Equipment.Presentation
{
    /// <summary>Presents the knife audiovisually.</summary>
    public sealed class KnifeView : ToolView
    {
        [Header("Swing motion")]
        [SerializeField] private Vector3 actionOffset = new(-.2f, .05f, .12f);
        [SerializeField] private Vector3 actionEuler = new(10f, -45f, -35f);

        private readonly ActionEnvelope swing = new();

        /// <summary>Applies the swing and idle motion to the held knife.</summary>
        protected override void LateUpdate()
        {
            float weight = swing.Advance(Time.deltaTime);
            ApplyPose(actionOffset * weight, actionEuler * weight);
        }

        /// <summary>Cancels the swing and restores the neutral pose when unequipped.</summary>
        protected override void OnDisable()
        {
            swing.Reset();
            base.OnDisable();
        }

        /// <summary>Starts a silent swing that returns to the neutral pose.</summary>
        /// <param name="seconds">Total swing duration in simulation seconds, clamped to at least 0.01.</param>
        public void PlaySwing(float seconds)
        {
            swing.Start(seconds);
        }
    }
}
