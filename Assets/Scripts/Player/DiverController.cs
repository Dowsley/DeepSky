using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;
using DeepSky.World;
using DeepSky.Building.Traversal;

namespace DeepSky.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class DiverController : MonoBehaviour
    {
        private const float MaximumFrameDuration = 0.25f;
        private const float MaximumCollisionStep = 1f / 90f;
        private const float RemainingTimeEpsilon = 0.000001f;
        
        private static readonly int DiverLightPosition = Shader.PropertyToID("_DiverLightPosition");
        private static readonly int DiverLightDirection = Shader.PropertyToID("_DiverLightDirection");

        [Header("References")]
        [SerializeField] private Camera view = null!;
        [SerializeField] private Light torch = null!;
        [SerializeField] private WorldManager world = null!;
        [SerializeField] private PlayerInputContext inputContext = null!;
        [SerializeField] private HabitatTraversal? habitatTraversal;
        [SerializeField, Min(0f)] private float indoorGravity = 18f;
        [SerializeField, Min(0f)] private float indoorJumpSpeed = 5f;

        [Header("Look")]
        [Tooltip("Camera rotation in degrees per pixel of mouse movement.")]
        [SerializeField, Min(0f)] private float mouseSensitivity = 0.105f;
        [SerializeField, Range(0f, 89f)] private float maximumLookPitch = 80f;

        [Header("Movement")]
        [SerializeField, Min(0)] private float walkSpeed = 4f;
        [SerializeField, Min(0)] private float airSpeed = 6f;
        [SerializeField, Min(0)] private float gravity = 5f;
        [SerializeField, Min(0)] private float jumpSpeed = 5f;
        [SerializeField, Min(0)] private float boostSpeed = 7f;
        [SerializeField, Min(0)] private int maximumBoostCharges = 5;
        [SerializeField, Min(.01f)] private float impactDrag = 5f;

        [Header("Jump and boost timing")]
        [SerializeField, Min(0f)] private float jumpRepeatDelay = 0.3f;
        [SerializeField, Min(0f)] private float boostRepeatDelay = 0.6f;
        [Tooltip("Seconds on the ground without jumping to recharge one boost charge.")]
        [SerializeField, Min(0.01f)] private float boostRechargeInterval = 0.15f;

        [Header("Presentation")]
        [Tooltip("Strength of the player-light contribution in the surface and vegetation shaders.")]
        [SerializeField, Min(0f)] private float torchShaderStrength = 1.7f;
        [SerializeField] private bool showControls = true;
        [SerializeField] private Color controlsColor = new(0.83f, 0.92f, 0.94f, 0.8f);
        [SerializeField, Min(1)] private int controlsFontSize = 13;
        [UnityEngine.Serialization.FormerlySerializedAs("controlsBottomMargin")]
        [SerializeField, Min(0f)] private float controlsTopMargin = 12f;
        [SerializeField, Min(1f)] private float controlsHeight = 28f;

        private CharacterController body = null!;
        private GUIStyle? controls;
        private float yaw = 0f;
        private float pitch = 0f;
        private int boostCharges = 0;
        private float jumpCooldown = 0f;
        private float boostRecharge = 0f;
        private bool grounded = false;
        private float verticalSpeed = 0f;
        private Vector3 impactVelocity = Vector3.zero;

        /// <summary>Validates scene references and initializes movement caches and view orientation.</summary>
        private void Awake()
        {
            Assert.IsNotNull(view, nameof(view));
            Assert.IsNotNull(torch, nameof(torch));
            Assert.IsNotNull(world, nameof(world));
            Assert.IsNotNull(inputContext, nameof(inputContext));

            body = GetComponent<CharacterController>();
            Assert.IsNotNull(body, nameof(body));

            yaw = transform.eulerAngles.y;
            pitch = Mathf.DeltaAngle(0, view.transform.localEulerAngles.x);

        }

        /// <summary>Reads gameplay input when permitted, integrates movement and updates player lighting.</summary>
        private void Update()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            Vector2 input = Vector2.zero;
            Vector2 look = Vector2.zero;
            bool jump = false;
            if (inputContext.GameplayActive && keyboard != null && mouse != null)
            {
                input = new Vector2((keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0),
                    (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0));
                look = mouse.delta.ReadValue() * mouseSensitivity;
                jump = keyboard.spaceKey.isPressed;
                if (keyboard.fKey.wasPressedThisFrame)
                {
                    torch.enabled = !torch.enabled;
                }
            }

            if (world.IsReady)
            {
                StepMovement(input, look, jump, Time.deltaTime);
            }
            Vector3 lightPosition = view.transform.position;
            Shader.SetGlobalVector(DiverLightPosition,
                new Vector4(lightPosition.x, lightPosition.y, lightPosition.z, torch.enabled ? torchShaderStrength : 0f));
            Shader.SetGlobalVector(DiverLightDirection, view.transform.forward);
        }

        /// <summary>Clears the shader contribution when the diver is disabled.</summary>
        private void OnDisable()
        {
            Shader.SetGlobalVector(DiverLightPosition, Vector4.zero);
        }

        /// <summary>Draws optional movement instructions when the field interface is not open.</summary>
        private void OnGUI()
        {
            if (!showControls || inputContext.InventoryOpen || inputContext.ConstructionActive)
            {
                return;
            }
            controls ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperCenter
            };
            controls.fontSize = controlsFontSize;
            string hint = !world.IsReady ? "Preparing seabed..." : Cursor.lockState == CursorLockMode.Locked
                ? habitatTraversal && habitatTraversal.IsDry
                    ? "WASD WALK    SPACE JUMP    B BUILD    F TORCH    ESC CURSOR"
                    : "WASD WALK / STEER    TAP SPACE JUMP / HOLD TO BOOST    B BUILD    F TORCH    ESC CURSOR"
                : "Click to explore    WASD WALK / STEER    TAP SPACE JUMP / HOLD TO BOOST";
            Rect bounds = new Rect(0f, controlsTopMargin, Screen.width, controlsHeight);
            controls.normal.textColor = new Color(0f, 0f, 0f, controlsColor.a * .55f);
            GUI.Label(new Rect(bounds.x + 1f, bounds.y + 1f, bounds.width, bounds.height), hint, controls);
            controls.normal.textColor = controlsColor;
            GUI.Label(bounds, hint, controls);
        }

        /// <summary>Moves the character without sweeping across unloaded terrain. WorldManager gates subsequent movement.</summary>
        /// <param name="feetPosition">Destination in world metres, expected above sampled terrain.</param>
        public void Relocate(Vector3 feetPosition)
        {
            body.enabled = false;
            transform.position = feetPosition;
            body.enabled = true;
            verticalSpeed = 0;
            impactVelocity = Vector3.zero;
            grounded = false;
        }

        /// <summary>Applies a collision-resolved impact without changing movement input.</summary>
        /// <param name="velocity">Added world-space velocity in metres per second.</param>
        public void ApplyImpact(Vector3 velocity)
        {
            impactVelocity += Vector3.ProjectOnPlane(velocity, Vector3.up);
            verticalSpeed += velocity.y;
            if (velocity.y > 0f)
            {
                grounded = false;
            }
        }

        /// <summary>Integrates weighted-diver movement using bounded collision steps.</summary>
        /// <param name="input">Horizontal steering axes, clamped to unit length.</param>
        /// <param name="look">Yaw and pitch delta in degrees.</param>
        /// <param name="jump">Whether the buoyancy/jump control is held.</param>
        /// <param name="deltaTime">Elapsed simulation seconds; nonpositive values do nothing.</param>
        private void StepMovement(Vector2 input, Vector2 look, bool jump, float deltaTime)
        {
            if (deltaTime <= 0)
            {
                return;
            }
            yaw += look.x;
            pitch = Mathf.Clamp(pitch - look.y, -maximumLookPitch, maximumLookPitch);
            transform.rotation = Quaternion.Euler(0, yaw, 0);
            view.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
            input = Vector2.ClampMagnitude(input, 1);
            Vector3 heading = transform.forward * input.y + transform.right * input.x;
            float remaining = Mathf.Min(deltaTime, MaximumFrameDuration);
            // Bound collision steps without tying jump height to render rate.
            while (remaining > RemainingTimeEpsilon)
            {
                float step = Mathf.Min(remaining, MaximumCollisionStep);
                remaining -= step;
                bool dry = habitatTraversal && habitatTraversal.IsDry;
                float acceleration = dry ? indoorGravity : gravity;
                if (jumpCooldown > 0)
                {
                    jumpCooldown -= step;
                }
                else if (jump && (dry ? grounded : boostCharges > 0 && view.transform.position.y <= 0f))
                {
                    verticalSpeed = Mathf.Max(verticalSpeed, dry ? indoorJumpSpeed : grounded ? jumpSpeed : boostSpeed);
                    jumpCooldown = grounded ? jumpRepeatDelay : boostRepeatDelay;
                    if (!grounded)
                    {
                        boostCharges--;
                    }
                    grounded = false;
                }

                if (grounded && !jump)
                {
                    if (boostRecharge > boostRechargeInterval)
                    {
                        if (boostCharges < maximumBoostCharges)
                        {
                            boostRecharge -= boostRechargeInterval;
                            boostCharges++;
                        }
                    }
                    else
                    {
                        boostRecharge += step;
                    }
                }

                if (grounded && verticalSpeed <= 0)
                {
                    verticalSpeed = 0;
                }
                Vector3 motion = heading * ((grounded || dry ? walkSpeed : airSpeed) * step);
                float decay = Mathf.Exp(-impactDrag * step);
                motion += impactVelocity * ((1f - decay) / impactDrag);
                impactVelocity *= decay;
                // Maintain contact down walkable slopes without carrying adhesion velocity into a fall.
                motion.y = grounded ? -walkSpeed * step : verticalSpeed * step - 0.5f * acceleration * step * step;
                verticalSpeed -= acceleration * step;
                if (habitatTraversal && habitatTraversal.ResolveLadderMotion(ref motion, step))
                {
                    verticalSpeed = 0f;
                }
                CollisionFlags collision = body.Move(motion);
                grounded = (collision & CollisionFlags.Below) != 0;
                if (grounded && verticalSpeed < 0)
                {
                    verticalSpeed = 0;
                }
                if ((collision & CollisionFlags.Above) != 0 && verticalSpeed > 0)
                {
                    verticalSpeed = 0;
                }
            }
        }

    }
}
