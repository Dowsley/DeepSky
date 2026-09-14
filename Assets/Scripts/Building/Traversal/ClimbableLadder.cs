using DeepSky.Building.Presentation;
using UnityEngine;

namespace DeepSky.Building.Traversal
{
    /// <summary>Identifies a building piece whose colliders support climbing.</summary>
    [RequireComponent(typeof(BuildingPiece))]
    public sealed class ClimbableLadder : MonoBehaviour
    {
    }
}
