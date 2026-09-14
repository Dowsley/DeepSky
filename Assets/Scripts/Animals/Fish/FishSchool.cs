using System.Collections.Generic;
using UnityEngine;

namespace DeepSky.Animals.Fish
{
    /// <summary>Shares a home area and alarm response among a group of fish.</summary>
    public sealed class FishSchool
    {
        private readonly List<Fish> members = new();
        public Vector3 Center { get; }
        public float Radius { get; }
        public Transform Root { get; }

        /// <summary>Creates a school whose centre remains reserved after its fish are collected.</summary>
        /// <param name="root">Transient parent owned by the wildlife population.</param>
        /// <param name="center">Fixed school centre in world metres.</param>
        /// <param name="radius">Positive horizontal roaming radius in metres.</param>
        public FishSchool(Transform root, Vector3 center, float radius)
        {
            Root = root;
            Center = center;
            Radius = radius;
        }

        /// <summary>Registers a fish and its damage alarm.</summary>
        /// <param name="fish">Initialized fish owned by this school's population.</param>
        public void Join(Fish fish)
        {
            members.Add(fish);
            fish.Life.Damaged += Threaten;
        }

        /// <summary>Releases a member's alarm subscription before destruction.</summary>
        /// <param name="fish">Previously registered fish.</param>
        public void Leave(Fish fish)
        {
            members.Remove(fish);
            fish.Life.Damaged -= Threaten;
        }

        /// <summary>Calculates local avoidance without forcing a rigid formation.</summary>
        /// <param name="position">Member position in world metres.</param>
        /// <param name="spacing">Positive desired neighbour spacing in metres.</param>
        /// <returns>Horizontal avoidance direction, or zero when neighbours are sufficiently distant.</returns>
        public Vector3 Separation(Vector3 position, float spacing)
        {
            Vector3 avoidance = Vector3.zero;
            foreach (Fish fish in members)
            {
                if (!fish || !fish.gameObject.activeInHierarchy || fish.Life.IsDead)
                {
                    continue;
                }
                Vector3 difference = position - fish.transform.position;
                float squared = difference.sqrMagnitude;
                if (squared > .001f && squared < spacing * spacing)
                {
                    avoidance += difference / squared;
                }
            }
            return Vector3.ProjectOnPlane(avoidance, Vector3.up);
        }

        /// <summary>Scatters surviving members away from an attack on the school.</summary>
        /// <param name="source">Attacker position in world metres.</param>
        private void Threaten(Vector3 source)
        {
            foreach (Fish fish in members)
            {
                if (fish && fish.gameObject.activeInHierarchy && !fish.Life.IsDead)
                {
                    fish.Threat.Threaten(source);
                }
            }
        }
    }
}
