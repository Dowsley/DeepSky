using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace DeepSky.Building.Presentation
{
    /// <summary>Supplies nearby module lamps to habitat materials.</summary>
    [DisallowMultipleComponent]
    public sealed class HabitatLighting : MonoBehaviour
    {
        private const int LightsPerSurface = 4;
        private static readonly int LampPositions = Shader.PropertyToID("_HabitatLampPositions");
        private static readonly int LampDirections = Shader.PropertyToID("_HabitatLampDirections");
        private static readonly int LampColor = Shader.PropertyToID("_HabitatLampColor");

        [SerializeField] private Habitat habitat = null!;
        [SerializeField, Min(.1f)] private float range = 4.5f;
        [SerializeField, Min(0f)] private float intensity = 1.15f;
        [SerializeField] private Color color = new(1f, .81f, .52f, 1f);

        private readonly List<Transform> lamps = new();
        private readonly Vector4[] positions = new Vector4[LightsPerSurface];
        private readonly Vector4[] directions = new Vector4[LightsPerSurface];
        private readonly float[] distances = new float[LightsPerSurface];
        private MaterialPropertyBlock properties = null!;

        /// <summary>Creates native shader state on the main thread and subscribes to layout changes.</summary>
        private void OnEnable()
        {
            Assert.IsNotNull(habitat, nameof(habitat));
            properties = new MaterialPropertyBlock();
            habitat.Changed += Rebuild;
            Rebuild();
        }

        /// <summary>Releases the subscription and clears owned lighting values on surviving surfaces.</summary>
        private void OnDisable()
        {
            if (!habitat)
            {
                return;
            }
            habitat.Changed -= Rebuild;
            foreach (BuildingPiece piece in habitat.Pieces)
            {
                foreach (Renderer surface in piece.GetComponentsInChildren<Renderer>())
                {
                    surface.GetPropertyBlock(properties);
                    properties.SetVector(LampColor, Vector4.zero);
                    surface.SetPropertyBlock(properties);
                }
            }
        }

        /// <summary>Assigns the four nearest lamps per surface when construction changes.</summary>
        private void Rebuild()
        {
            lamps.Clear();
            foreach (BuildingPiece piece in habitat.Pieces)
            {
                if (piece.Lamp)
                {
                    lamps.Add(piece.Lamp);
                }
            }
            foreach (BuildingPiece piece in habitat.Pieces)
            {
                foreach (Renderer surface in piece.GetComponentsInChildren<Renderer>())
                {
                    SelectLights(surface.bounds.center);
                    surface.GetPropertyBlock(properties);
                    properties.SetVectorArray(LampPositions, positions);
                    properties.SetVectorArray(LampDirections, directions);
                    properties.SetVector(LampColor, new Vector4(color.r, color.g, color.b, intensity));
                    surface.SetPropertyBlock(properties);
                }
            }
        }

        /// <summary>Fills bounded shader arrays in nearest-first order, zeroing unused entries.</summary>
        /// <param name="center">Surface bounds center in world metres.</param>
        private void SelectLights(Vector3 center)
        {
            for (int i = 0; i < LightsPerSurface; i++)
            {
                distances[i] = float.PositiveInfinity;
                positions[i] = Vector4.zero;
                directions[i] = Vector4.zero;
            }
            foreach (Transform lamp in lamps)
            {
                float distance = (lamp.position - center).sqrMagnitude;
                for (int i = 0; i < LightsPerSurface; i++)
                {
                    if (distance >= distances[i])
                    {
                        continue;
                    }
                    for (int j = LightsPerSurface - 1; j > i; j--)
                    {
                        distances[j] = distances[j - 1];
                        positions[j] = positions[j - 1];
                        directions[j] = directions[j - 1];
                    }
                    distances[i] = distance;
                    Vector3 point = lamp.position;
                    positions[i] = new Vector4(point.x, point.y, point.z, range);
                    directions[i] = lamp.forward;
                    break;
                }
            }
        }
    }
}
