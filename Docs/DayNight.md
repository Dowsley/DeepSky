# Day and night

Select **Day and night** in Main's Hierarchy.

- **Time of day** is a 0..24-hour slider. Outside Play mode it sets the starting
  hour and previews the atmosphere in Scene view. In Play mode it changes the
  running clock without changing the saved starting hour.
- **Advance Time** enables automatic progression. Turn it off to hold a chosen hour.
- **Day Length Minutes** sets the duration of a complete day at normal game speed.
  The default is 20 minutes, starting at noon. Pausing game time pauses this clock.
- **Daylight By Hour** controls the lighting curve. Defaults retain full daytime
  lighting from 08:00 to 16:00, with smooth dawn/dusk transitions and 30% ambient
  daylight overnight. Keep the values at hours 0 and 24 equal for a seamless loop.

The camera's Depth Atmosphere component references the clock. Its profile's
Daylight remains a maximum-brightness multiplier. A missing or disabled clock
uses that manual profile value. Depth bands still control water color, fog range,
ambient light and effect strength. Minimum Water Daylight keeps the night water
readable; it does not keep caustics or sunbeams lit.

The cycle affects water/background colors, terrain and wildlife ambient light,
caustics, sunbeams and suspended-cloud opacity. Base lighting, mineral emission,
the dive torch and the player's nearby light remain independent. Materials and
the shared atmosphere profile are not rewritten by the clock.

The clock is local to the Play session. It does not alter wildlife behavior,
audio, world generation or construction.

See [Atmosphere tuning](Atmosphere.md) for background colors, ambient light,
distance haze, bloom and material controls.
