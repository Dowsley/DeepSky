# Atmosphere tuning

Select **Player / Player Camera** in Main. Its **Depth Atmosphere** and
**Underwater Bloom** components expose their shared assets under **Effect tuning**.
Shared asset edits persist after Play mode; use Undo to reject an experiment.

- **Upper Water / Lower Water** set the background gradient and distance-haze
  palette. These are display-space input colors, not final screenshot colors.
  Bloom, shafts, particles and edge shading also affect the displayed result.
- **Ambient** lights surfaces at each camera-depth band. It is independent of
  the background color; brighten sand through its material rather than using fog
  to brighten the whole image.
- **Visibility** is the distance cutoff. **Fog Transition Distance** is the
  width of the haze ramp ending there, not the distance where haze starts.
  The shallow defaults retain surface color through 10 m, then blend over
  40 m toward the 50 m cutoff. The last 4 m also fade surface coverage.
- **Screen edge shading** on Water bloom controls corner darkening. Its
  strength is 0.2; bloom threshold is 0.1 and intensity is 1.
- **Sand display tint** on `Assets/Materials/Rippled sand.mat` calibrates the
  sand albedo independently of rock. The terrain proximity-light strength is 0.9.
- **Display tint / opacity multiplier**, fourth component, on
  `Assets/Materials/Fine suspended flecks.mat` controls the cloud veil. It is
  0.55 and does not change bright-speck population.

Hold a fixed pose and noon when comparing materials. The depth bands use camera
depth, not biome labels. See [Day and night](DayNight.md) for the live clock slider.
The 200 m and 300 m bands retain their distinct palettes; the shared haze-ramp
width also applies at those depths.
