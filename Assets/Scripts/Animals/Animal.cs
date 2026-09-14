using System;
using DeepSky.Harvesting;
using UnityEngine;

namespace DeepSky.Animals
{
    /// <summary>Coordinates an animal's initialization.</summary>
    [DisallowMultipleComponent]
    public abstract class Animal : MonoBehaviour
    {
        [SerializeField] private HarvestableResource resource = null!;

        private AnimalLife life = null!;

        public AnimalLife Life => life;

        /// <summary>Validates the component references needed before activation.</summary>
        /// <exception cref="InvalidOperationException">The prefab composition is incomplete.</exception>
        public virtual void ValidateComposition()
        {
            if (!resource || resource.gameObject != gameObject || !resource.Item)
            {
                throw new InvalidOperationException($"{name}: assign this animal's harvestable resource and its item.");
            }
            life = GetComponent<AnimalLife>();
            if (!life)
            {
                throw new InvalidOperationException($"{name}: an animal requires a life component.");
            }
        }

        /// <summary>Initializes this animal's lifetime and movement before activation.</summary>
        /// <param name="spawn">Shared world dependencies and initial movement intent.</param>
        public void Initialize(AnimalSpawn spawn)
        {
            ValidateComposition();
            resource.Bind(new ResourceState(resource.InitialQuantity), spawn.World);
            InitializeMovement(spawn);
        }

        /// <summary>Supplies species-specific movement dependencies before activation.</summary>
        /// <param name="spawn">World dependencies and initial movement intent.</param>
        protected abstract void InitializeMovement(AnimalSpawn spawn);
    }
}
