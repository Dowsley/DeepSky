using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace DeepSky.Building.Presentation
{
    /// <summary>Supplies dry room volumes to water-effect shaders.</summary>
    public sealed class InteriorVolumes : MonoBehaviour
    {
        private static readonly int VolumeCount = Shader.PropertyToID("_InteriorVolumeCount");
        private static readonly int VolumeBounds = Shader.PropertyToID("_InteriorBounds");

        [SerializeField] private BuildingWorld buildings = null!;

        private readonly List<Bounds> volumes = new();
        private GraphicsBuffer? buffer;

        /// <summary>Subscribes to layout changes and creates the initial shader buffer.</summary>
        private void OnEnable()
        {
            Assert.IsNotNull(buildings, nameof(buildings));
            buildings.Changed += Rebuild;
            Rebuild();
        }

        /// <summary>Clears globals before releasing the owned graphics buffer.</summary>
        private void OnDisable()
        {
            buildings.Changed -= Rebuild;
            Shader.SetGlobalInt(VolumeCount, 0);
            buffer?.Dispose();
            buffer = null;
        }

        /// <summary>Uploads disjoint room row bounds after a layout edit.</summary>
        private void Rebuild()
        {
            volumes.Clear();
            foreach (Habitat habitat in buildings.Habitats)
            {
                habitat.AppendAirVolumes(volumes);
            }
            var data = new Vector4[Mathf.Max(2, volumes.Count * 2)];
            for (int i = 0; i < volumes.Count; i++)
            {
                data[i * 2] = volumes[i].min;
                data[i * 2 + 1] = volumes[i].max;
            }
            Shader.SetGlobalInt(VolumeCount, 0);
            buffer?.Dispose();
            buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, data.Length, 16);
            buffer.SetData(data);
            Shader.SetGlobalBuffer(VolumeBounds, buffer);
            Shader.SetGlobalInt(VolumeCount, volumes.Count);
        }
    }
}
