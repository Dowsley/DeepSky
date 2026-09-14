# DeepSky

Underwater exploration+survival game, based on weighted-diver movement.

## Direction

Establish a base, prepare for dive, venture farther and deeper, gradually advance  mission target.

The ocean should feel worth exploring and living in, with beautiful environments and quiet everyday moments punctuated by fear and danger, and mystery.

The setting is near-future. Diving, habitation and construction technology should have consistent capabilities and limitations.

Discoveries should lead logically to further locations. Progression means going further down/deeper.

A conversational companion MIGHT add personality and react to discoveries without requiring a large narrative system, but I can decide later.

## Gameplay intentions

These describe the intended game, not a list of implemented features.

- Weighted seabed walking, buoyant jumping, sinking and terrain navigation.
- Gathering, mining and a small set of tools with distinct purposes.
- Modular base building: A home with storage, crafting, cooking and simple cultivation as an alternative to hunting.
- Ambient wildlife, huntable prey and predators that can force a retreat or change a route.
- Equipment progression that allows deeper and longer expeditions.
- Map is based around depth. Progression means being able to go deeper and deeper.
- Recovering scattered components to `assemble` or restore the mission target, guided by signals, physical clues or recovered information.
- Semi-procedural environments with discoveries whose placement and relationship to generated terrain remain to be designed.

Oxygen and pressure are provisional: favor generous reserves, clear warnings and equipment-dependent depth limits over constant meter-watching or detailed diving physiology. Day/night may focus on lighting, visibility and ambience. Elaborate injuries, branching dialogue, automation and complex base logistics are not part of the initial foundation.

## Visual identity

PSX-style texture presentation combined with volumetrics and modern effects, including underwater fog, bloom, caustics, light shafts, suspended particles.

Low polygon counts are not a style requirement. Texture treatment, pixelation and dithering should work together while preserving readability.

## Open choices

- The retrieval target, component count, final interaction, historical subject and themes. A Roman shipwreck is one possibility, not a selected storyline; an extraordinary explanation or horror story is not required.
- Expedition location, duration, technology limits and what prevents the protagonist from leaving. A storm-damaged supported expedition and an unsupported illegal retrieval operation are possible premises. Illegality alone does not explain being stranded; the reason to continue the mission also needs to make sense.
- Companion identity and means of contact: a human radio buddy or a local AI. Support and rescue limitations need a coherent explanation.
- Survival pressures, failure rules, equipment progression, recipes and construction scope. Whether automation belongs beyond the initial foundation remains open.

## TODO

- [ ] Refactor input using Unity Input System actions and context-aware dispatch. Components consume actions; number keys must not select tools when another context owns input.
- [ ] Improve school visibility and evaluate whether boid-based movement is useful.
- [ ] Explore day/night lighting and ambience.
- [ ] Additional wildlife: giant squid, octopus and crabs.
- [ ] Other biomes, including the abyssal zone. Consult the creator's books for inspiration.

Project setup, authoring and verification instructions are in [AGENTS.md](AGENTS.md).
