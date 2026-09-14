using DeepSky.World.Generation;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace DeepSky.World.Mapping
{
    /// <summary>
    /// Displays the complete seeded world as a north-up depth chart, with live player heading and spawn.
    /// Rebuilds the chart only when the world is regenerated. UI layout belongs to the minimap prefab.
    /// </summary>
    public sealed class WorldMinimap : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private WorldManager world = null!;
        [SerializeField] private Transform player = null!;
        [SerializeField] private RawImage chart = null!;
        [SerializeField] private RectTransform playerMarker = null!;
        [SerializeField] private RectTransform spawnMarker = null!;
        [SerializeField] private Text coordinates = null!;

        [Header("Depth chart")]
        [SerializeField, Range(64, 512)] private int resolution = 256;
        [SerializeField] private Color shallowColor = new(0.2f, 0.8f, 0.72f, 1f);
        [SerializeField] private Color deepColor = new(0.035f, 0.08f, 0.25f, 1f);
        [SerializeField, Min(0f)] private float contourInterval = 25f;

        private WorldData? displayedWorld;
        private Texture2D? texture;

        /// <summary>Asserts that the minimap's scene references and prefab UI bindings are assigned.</summary>
        private void Awake()
        {
            Assert.IsNotNull(world, nameof(world));
            Assert.IsNotNull(player, nameof(player));
            Assert.IsNotNull(chart, nameof(chart));
            Assert.IsNotNull(playerMarker, nameof(playerMarker));
            Assert.IsNotNull(spawnMarker, nameof(spawnMarker));
            Assert.IsNotNull(coordinates, nameof(coordinates));
        }

        /// <summary>Refreshes the chart when its generation snapshot changes and updates the live player markers.</summary>
        private void LateUpdate()
        {
            if (!world.IsReady)
            {
                coordinates.text = "Loading terrain...";
                return;
            }

            WorldData data = world.Data;
            if (!ReferenceEquals(displayedWorld, data))
            {
                if (texture)
                {
                    Destroy(texture);
                }

                texture = WorldMapTexture.Create(data, resolution, shallowColor, deepColor, contourInterval);
                chart.texture = texture;
                displayedWorld = data;
                PlaceMarker(spawnMarker, WorldMapTexture.ToUV(data, data.Spawn));
            }

            Vector3 position = player.position;
            PlaceMarker(playerMarker, WorldMapTexture.ToUV(data, new Vector2(position.x, position.z)));
            playerMarker.localRotation = Quaternion.Euler(0f, 0f, -player.eulerAngles.y);
            coordinates.text = $"X {position.x:0}   Z {position.z:0}\nDepth {-position.y:0} m";
        }

        /// <summary>Releases the owned chart texture.</summary>
        private void OnDestroy()
        {
            if (texture)
            {
                Destroy(texture);
            }
        }

        /// <summary>Anchors a marker directly to a normalized position within its chart parent.</summary>
        /// <param name="marker">Marker whose parent rectangle corresponds to the complete chart.</param>
        /// <param name="uv">Unclamped chart coordinates, with north toward increasing Y.</param>
        private static void PlaceMarker(RectTransform marker, Vector2 uv)
        {
            marker.anchorMin = uv;
            marker.anchorMax = uv;
            marker.anchoredPosition = Vector2.zero;
        }
    }
}
