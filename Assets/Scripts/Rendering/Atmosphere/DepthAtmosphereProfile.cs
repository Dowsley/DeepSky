using System;
using UnityEngine;

namespace DeepSky.Rendering.Atmosphere
{
    /// <summary>
    /// Camera-depth keyframes in metres below sea level. Colors are display-space values.
    /// Samples blend linearly between ordered bands and clamp outside the authored depth range.
    /// </summary>
    [CreateAssetMenu(menuName = "DeepSky/Depth Atmosphere Profile")]
    public sealed class DepthAtmosphereProfile : ScriptableObject
    {
        [Header("Depth bands")]
        [Tooltip("Order shallow to deep. These are camera depths, independent of terrain or biome identity.")]
        [SerializeField] private DepthBand[] bands =
        {
            new DepthBand(100f, new Color(.175f, .57f, .74f), new Color(.125f, .415f, .55f),
                new Color(.26f, .48f, .56f), 50f, 1f, 1f),
            new DepthBand(200f, new Color(.11f, .35f, .43f), new Color(.056f, .21f, .28f),
                new Color(.2f, .4f, .53f), 45f, .3f, 0f),
            new DepthBand(300f, new Color(.048f, .18f, .24f), new Color(.03f, .1f, .2f),
                new Color(.35f, .35f, .45f), 35f, 0f, 0f)
        };

        [Header("Distance haze")]
        [Tooltip("Width of the haze ramp ending at each band's visibility limit. A smaller width keeps nearby surfaces clear longer.")]
        [SerializeField, Min(.1f)] private float fogTransitionDistance = 40f;
        [SerializeField, Range(0f, 1f)] private float minimumSceneContribution = .1f;
        [SerializeField, Min(.1f)] private float visibilityEdgeFade = 4f;

        [Header("Daylight")]
        [Tooltip("Maximum lighting level, multiplied by the camera's day/night cycle when assigned.")]
        [SerializeField, Range(0f, 1f)] private float daylight = 1f;
        [SerializeField, Range(0f, 1f)] private float minimumWaterDaylight = .5f;

        [Header("Cloud lighting")]
        [Tooltip("Added to ambient color for suspended clouds. Bright specks remain unlit.")]
        [SerializeField] private Color cloudFill = new Color(.5f, .5f, .5f, 1f);

        internal float Daylight => daylight;
        internal Vector4 FogParameters => new Vector4(fogTransitionDistance, minimumSceneContribution, visibilityEdgeFade, 0f);

        /// <summary>Interpolates ordered depth bands and applies daylight and cloud-fill settings.</summary>
        /// <param name="depth">Camera depth in metres below sea level; clamped to the outermost bands.</param>
        /// <param name="cycleDaylight">Daily lighting multiplier, clamped to [0, 1]; one preserves manual profile lighting.</param>
        /// <returns>Display-space colors and effect parameters for the requested depth, without modifying the profile.</returns>
        /// <exception cref="InvalidOperationException">The profile contains no depth bands.</exception>
        internal AtmosphereSample Evaluate(float depth, float cycleDaylight = 1f)
        {
            if (bands.Length == 0)
            {
                throw new InvalidOperationException("An atmosphere profile requires at least one depth band.");
            }

            DepthBand lower = bands[0];
            DepthBand upper = lower;
            for (int i = 1; i < bands.Length && depth > lower.Depth; i++)
            {
                upper = bands[i];
                if (depth <= upper.Depth)
                {
                    break;
                }

                lower = upper;
            }

            float blend = Mathf.InverseLerp(lower.Depth, upper.Depth, depth);
            Color ambient = Color.Lerp(lower.Ambient, upper.Ambient, blend);
            float effectiveDaylight = daylight * Mathf.Clamp01(cycleDaylight);
            float waterDaylight = Mathf.Max(effectiveDaylight, minimumWaterDaylight);
            Color cloud = ambient + cloudFill;
            Color shallowCloud = bands[0].Ambient + cloudFill;
            var cloudMultiplier = new Vector4(cloud.r / Mathf.Max(shallowCloud.r, .0001f),
                cloud.g / Mathf.Max(shallowCloud.g, .0001f), cloud.b / Mathf.Max(shallowCloud.b, .0001f), effectiveDaylight);
            return new AtmosphereSample(Color.Lerp(lower.UpperWater, upper.UpperWater, blend) * waterDaylight,
                Color.Lerp(lower.LowerWater, upper.LowerWater, blend) * waterDaylight, ambient * effectiveDaylight,
                Mathf.Lerp(lower.Visibility, upper.Visibility, blend),
                Mathf.Lerp(lower.Caustics, upper.Caustics, blend) * Mathf.Max(0f, effectiveDaylight * 1.5f - .5f),
                Mathf.Lerp(lower.ShaftOpacityLimit, upper.ShaftOpacityLimit, blend), cloudMultiplier);
        }

        /// <summary>Reports depth bands that are not strictly ordered from shallow to deep.</summary>
        private void OnValidate()
        {
            for (int i = 1; i < bands.Length; i++)
            {
                if (bands[i].Depth <= bands[i - 1].Depth)
                {
                    Debug.LogError("Atmosphere depth bands must be ordered shallow to deep with distinct depths.", this);
                    break;
                }
            }
        }

        [Serializable]
        private sealed class DepthBand
        {
            [SerializeField, Min(0f)] private float depth = 100f;
            [SerializeField] private Color upperWater = Color.white;
            [SerializeField] private Color lowerWater = Color.white;
            [SerializeField] private Color ambient = Color.white;
            [SerializeField, Min(1f)] private float visibility = 50f;
            [SerializeField, Range(0f, 1f)] private float caustics = 1f;
            [SerializeField, Range(0f, 1f)] private float shaftOpacityLimit = 1f;

            internal float Depth => depth;
            internal Color UpperWater => upperWater;
            internal Color LowerWater => lowerWater;
            internal Color Ambient => ambient;
            internal float Visibility => visibility;
            internal float Caustics => caustics;
            internal float ShaftOpacityLimit => shaftOpacityLimit;

            /// <summary>Defines one authored atmosphere keyframe.</summary>
            /// <param name="depth">Nonnegative depth in metres below sea level.</param>
            /// <param name="upperWater">Display-space water color above the view horizon.</param>
            /// <param name="lowerWater">Display-space water color below the view horizon.</param>
            /// <param name="ambient">Display-space ambient light color before daylight scaling.</param>
            /// <param name="visibility">Positive visibility distance in metres.</param>
            /// <param name="caustics">Caustic strength in [0, 1] before daylight scaling.</param>
            /// <param name="shaftOpacityLimit">Maximum shaft opacity in [0, 1].</param>
            internal DepthBand(float depth, Color upperWater, Color lowerWater, Color ambient,
                float visibility, float caustics, float shaftOpacityLimit)
            {
                this.depth = depth;
                this.upperWater = upperWater;
                this.lowerWater = lowerWater;
                this.ambient = ambient;
                this.visibility = visibility;
                this.caustics = caustics;
                this.shaftOpacityLimit = shaftOpacityLimit;
            }
        }
    }

    internal readonly struct AtmosphereSample
    {
        internal Color UpperWater { get; }
        internal Color LowerWater { get; }
        internal Color Ambient { get; }
        internal float Visibility { get; }
        internal float Caustics { get; }
        internal float ShaftOpacityLimit { get; }
        internal Vector4 CloudMultiplier { get; }

        /// <summary>Captures the evaluated atmosphere values consumed by camera rendering and light shafts.</summary>
        /// <param name="upperWater">Daylight-adjusted upper water color in display space.</param>
        /// <param name="lowerWater">Daylight-adjusted lower water color in display space.</param>
        /// <param name="ambient">Daylight-adjusted ambient color in display space.</param>
        /// <param name="visibility">Visibility distance in metres.</param>
        /// <param name="caustics">Daylight-adjusted caustic strength.</param>
        /// <param name="shaftOpacityLimit">Depth-dependent maximum shaft opacity.</param>
        /// <param name="cloudMultiplier">RGB cloud multipliers relative to the shallow band, with daylight in W.</param>
        internal AtmosphereSample(Color upperWater, Color lowerWater, Color ambient, float visibility,
            float caustics, float shaftOpacityLimit, Vector4 cloudMultiplier)
        {
            UpperWater = upperWater;
            LowerWater = lowerWater;
            Ambient = ambient;
            Visibility = visibility;
            Caustics = caustics;
            ShaftOpacityLimit = shaftOpacityLimit;
            CloudMultiplier = cloudMultiplier;
        }
    }
}
