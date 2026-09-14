using DeepSky.Animals.Movement;
using DeepSky.Player;
using UnityEngine;
using UnityEngine.Assertions;

namespace DeepSky.Animals.Sharks
{
    /// <summary>Steers shark roaming and provoked attacks.</summary>
    public sealed class SharkMovement : SwimmingMovement
    {
        private enum AttackPhase
        {
            Roaming,
            Circling,
            Charging,
            Withdrawing
        }

        [Header("Attack")]
        [SerializeField] private Transform mouth = null!;
        [SerializeField, Min(1f)] private float pursuitRange = 50f;
        [SerializeField, Min(1f)] private float chargeRange = 18f;
        [SerializeField, Min(1f)] private float provokedDuration = 35f;
        [SerializeField, Min(.1f)] private float circlingDuration = 6f;
        [SerializeField, Min(.1f)] private float chargeDuration = 3f;
        [SerializeField, Min(.1f)] private float recoveryDuration = 5f;
        [SerializeField, Min(1f)] private float chargeSpeedMultiplier = 4f;
        [SerializeField, Min(1f)] private float alertSpeedMultiplier = 1.6f;
        [SerializeField, Min(.1f)] private float contactRadius = 1.1f;
        [SerializeField, Min(0f)] private float impactSpeed = 5f;
        [SerializeField, Min(0f)] private float impactLift = 2.5f;

        [Header("Roaming")]
        [SerializeField, Min(.1f)] private float turnInterval = 7f;
        [SerializeField, Range(0f, 90f)] private float wanderAngle = 35f;

        private AnimalLife life = null!;
        private Transform observer = null!;
        private DiverController diver = null!;
        private System.Random random = null!;
        private AttackPhase phase = AttackPhase.Roaming;
        private Vector3 direction = Vector3.forward;
        private Vector3 attackPoint = Vector3.zero;
        private float phaseRemaining = 0f;
        private float provokedRemaining = 0f;
        private float turnRemaining = 0f;
        private float circleSign = 1f;

        protected override float ModelYawOffset => 0f;

        /// <summary>Caches life and validates the bite origin.</summary>
        private void Awake()
        {
            life = GetComponent<AnimalLife>();
            Assert.IsNotNull(life, nameof(life));
            Assert.IsNotNull(mouth, nameof(mouth));
        }

        /// <summary>Listens for attacks while this shark can swim.</summary>
        private void OnEnable()
        {
            life.Damaged += Provoke;
        }

        /// <summary>Releases damage reactions when dead or unloaded.</summary>
        private void OnDisable()
        {
            life.Damaged -= Provoke;
        }

        /// <summary>Updates roaming, circling and committed charge intervals.</summary>
        private void Update()
        {
            float delta = Time.deltaTime;
            provokedRemaining = Mathf.Max(0f, provokedRemaining - delta);
            phaseRemaining -= delta;
            Vector3 toPlayer = observer.position - transform.position;
            if (provokedRemaining <= 0f || toPlayer.sqrMagnitude > pursuitRange * pursuitRange)
            {
                phase = AttackPhase.Roaming;
            }

            switch (phase)
            {
                case AttackPhase.Roaming:
                    Roam(delta);
                    break;
                case AttackPhase.Circling:
                    Circle(toPlayer);
                    break;
                case AttackPhase.Charging:
                    Charge();
                    break;
                case AttackPhase.Withdrawing:
                    Swim(direction, alertSpeedMultiplier, false);
                    if (phaseRemaining <= 0f)
                    {
                        BeginCircling();
                    }
                    break;
            }
        }

        /// <summary>Initializes perception and independent roaming before activation.</summary>
        /// <param name="spawn">World, player and movement context supplied by the population.</param>
        public void JoinPopulation(AnimalSpawn spawn)
        {
            JoinWorld(spawn.World, spawn.Heading, spawn.Clearance);
            observer = spawn.Observer.transform;
            diver = spawn.Observer.GetComponentInParent<DiverController>();
            Assert.IsNotNull(diver, nameof(diver));
            random = new System.Random(spawn.Seed);
            direction = TravelHeading;
            turnRemaining = turnInterval * (float)random.NextDouble();
            circleSign = random.Next(2) == 0 ? -1f : 1f;
        }

        /// <summary>Changes heading occasionally and resumes pursuit while still provoked.</summary>
        /// <param name="delta">Elapsed simulation seconds.</param>
        private void Roam(float delta)
        {
            turnRemaining -= delta;
            if (turnRemaining <= 0f)
            {
                direction = Quaternion.AngleAxis(((float)random.NextDouble() * 2f - 1f) * wanderAngle, Vector3.up) * direction;
                turnRemaining = turnInterval;
            }
            Swim(direction);
            if (provokedRemaining > 0f && Vector3.Distance(transform.position, observer.position) <= pursuitRange)
            {
                BeginCircling();
            }
        }

        /// <summary>Tracks the player obliquely before choosing a clear attack route.</summary>
        /// <param name="toPlayer">Vector from the shark to the player in world metres.</param>
        private void Circle(Vector3 toPlayer)
        {
            Vector3 tangent = Vector3.Cross(Vector3.up, toPlayer.normalized) * circleSign;
            direction = (toPlayer.normalized * .55f + tangent).normalized;
            Swim(direction, alertSpeedMultiplier, false);
            if (phaseRemaining <= 0f && toPlayer.sqrMagnitude <= chargeRange * chargeRange && CanSwimTo(observer.position))
            {
                phase = AttackPhase.Charging;
                phaseRemaining = chargeDuration;
                attackPoint = observer.position;
            }
        }

        /// <summary>Charges the chosen attack point and resolves at most one contact.</summary>
        private void Charge()
        {
            Vector3 previous = mouth.position;
            direction = (attackPoint - previous).normalized;
            Swim(direction, chargeSpeedMultiplier, false);
            Vector3 segment = mouth.position - previous;
            float along = segment.sqrMagnitude > .0001f
                ? Mathf.Clamp01(Vector3.Dot(observer.position - previous, segment) / segment.sqrMagnitude)
                : 0f;
            bool contact = Vector3.Distance(previous + segment * along, observer.position) <= contactRadius;
            if (contact)
            {
                Vector3 away = Vector3.ProjectOnPlane(direction, Vector3.up).normalized;
                diver.ApplyImpact(away * impactSpeed + Vector3.up * impactLift);
            }
            if (contact || phaseRemaining <= 0f || !CanSwimTo(transform.position + direction * 3f))
            {
                phase = AttackPhase.Withdrawing;
                phaseRemaining = recoveryDuration;
                direction = Vector3.ProjectOnPlane(transform.position - observer.position, Vector3.up).normalized;
                if (direction.sqrMagnitude < .01f)
                {
                    direction = -TravelHeading;
                }
            }
        }

        /// <summary>Begins a circling interval without immediately repeating an attack.</summary>
        private void BeginCircling()
        {
            phase = AttackPhase.Circling;
            phaseRemaining = circlingDuration;
        }

        /// <summary>Provokes this shark and interrupts an attack when it takes damage.</summary>
        /// <param name="source">Attacker position in world metres.</param>
        private void Provoke(Vector3 source)
        {
            if (life.IsDead)
            {
                return;
            }
            provokedRemaining = provokedDuration;
            direction = Vector3.ProjectOnPlane(transform.position - source, Vector3.up).normalized;
            phase = AttackPhase.Withdrawing;
            phaseRemaining = recoveryDuration;
        }
    }
}
