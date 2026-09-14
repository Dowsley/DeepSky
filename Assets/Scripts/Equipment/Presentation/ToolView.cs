using UnityEngine;
using UnityEngine.Assertions;

namespace DeepSky.Equipment.Presentation
{
    /// <summary>Provides shared held-tool pose and idle motion.</summary>
    public abstract class ToolView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform movingParts = null!;

        [Header("Motion")]
        [SerializeField, Min(0f)] private float idleAmplitude = .003f;

        private Vector3 restPosition = Vector3.zero;
        private Quaternion restRotation = Quaternion.identity;

        /// <summary>Validates authoring references and stores the neutral tool pose.</summary>
        protected virtual void Awake()
        {
            Assert.IsNotNull(movingParts, nameof(movingParts));
            restPosition = movingParts.localPosition;
            restRotation = movingParts.localRotation;
        }

        /// <summary>Applies idle motion relative to the neutral pose.</summary>
        protected virtual void LateUpdate()
        {
            ApplyPose(Vector3.zero, Vector3.zero);
        }

        /// <summary>Restores the neutral pose when unequipped.</summary>
        protected virtual void OnDisable()
        {
            movingParts.localPosition = restPosition;
            movingParts.localRotation = restRotation;
        }

        /// <summary>Combines a tool's motion offsets with its neutral pose and idle bob.</summary>
        /// <param name="offset">Translation from rest in parent-local metres.</param>
        /// <param name="euler">Rotation from rest in tool-local Euler degrees.</param>
        protected void ApplyPose(Vector3 offset, Vector3 euler)
        {
            float idle = Mathf.Sin(Time.time * 1.7f) * idleAmplitude;
            movingParts.localPosition = restPosition + offset + Vector3.up * idle;
            movingParts.localRotation = restRotation * Quaternion.Euler(euler);
        }
    }
}
