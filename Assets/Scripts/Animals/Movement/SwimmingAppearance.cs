using UnityEngine;
using UnityEngine.Assertions;

namespace DeepSky.Animals.Movement
{
    /// <summary>Configures shader-driven swimming presentation.</summary>
    public sealed class SwimmingAppearance : MonoBehaviour
    {
        private static readonly int Phase = Shader.PropertyToID("_Phase");

        [SerializeField] private Renderer body = null!;
        [Tooltip("Maximum shader displacement in model-space metres, including all tail directions.")]
        [SerializeField, Min(0f)] private float maximumDisplacement = .6f;

        /// <summary>Expands culling bounds and gives this instance an independent tail phase.</summary>
        private void Awake()
        {
            Assert.IsNotNull(body, nameof(body));
            Bounds bounds = body.localBounds;
            bounds.Expand(maximumDisplacement * 2f);
            body.localBounds = bounds;
            var properties = new MaterialPropertyBlock();
            body.GetPropertyBlock(properties);
            Vector3 position = transform.position;
            properties.SetFloat(Phase, Mathf.Repeat(position.x * 1.37f + position.z * .71f, Mathf.PI * 2f));
            body.SetPropertyBlock(properties);
        }
    }
}
