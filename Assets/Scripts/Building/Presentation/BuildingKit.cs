using System;
using UnityEngine;

namespace DeepSky.Building.Presentation
{
    /// <summary>Supplies the prefabs used to present a room layout.</summary>
    [CreateAssetMenu(menuName = "DeepSky/Building/Building Kit")]
    public sealed class BuildingKit : ScriptableObject
    {
        [SerializeField] private BuildingPiece floor = null!;
        [SerializeField] private BuildingPiece wall = null!;
        [SerializeField] private BuildingPiece window = null!;
        [SerializeField] private BuildingPiece ceiling = null!;
        [SerializeField] private BuildingPiece ladder = null!;
        [SerializeField] private BuildingPiece support = null!;
        [SerializeField] private BuildingPiece water = null!;

        public BuildingPiece Floor => floor;
        public BuildingPiece Wall => wall;
        public BuildingPiece Window => window;
        public BuildingPiece Ceiling => ceiling;
        public BuildingPiece Ladder => ladder;
        public BuildingPiece Support => support;
        public BuildingPiece Water => water;

        /// <summary>Checks that every structural role has a prefab.</summary>
        /// <exception cref="InvalidOperationException">A required prefab is absent.</exception>
        public void ValidateKit()
        {
            if (!floor || !wall || !window || !ceiling || !ladder || !support || !water)
            {
                throw new InvalidOperationException("Building kit requires all structural prefabs.");
            }
        }
    }
}
