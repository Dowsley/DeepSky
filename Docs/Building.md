# Building

B toggles construction. Number keys 1 through 7 select Floor, Window, Level,
Opening, Ladder, Remove and Base. The mouse wheel cycles choices. Left-click
applies the outlined operation. Tab opens inventory and leaves construction.

- Floor extends through an exterior wall or closes an existing opening.
- Window applies glass to a selected exterior wall.
- Level connects above a ceiling or below a floor, adding an opening, a ladder
  and landing space. Extend the destination level with Floor.
- Opening removes a floor without deleting its room. Floor closes the opening.
- Ladder targets a solid floor beside an opening. Facing chooses the opening;
  another valid direction is used when that side is unavailable. Movement into
  the ladder climbs, including during a jump or fall. Moving away leaves it;
  gravity handles descent. Floor and wall collisions constrain movement.
- Remove on a floor opens it while keeping the room and walls. On a ladder it
  leaves an opening. On an exterior wall it deletes the room. Separate remaining
  sections are allowed. Removing a ladder's landing also removes that ladder.
  Removing the final room removes that base.
- Base places a starter structure on suitably flat, unoccupied seabed.

Construction is free and lasts for the Play session. The starting base is placed
near the terrain spawn. Walking and ladder climbing remain available during
construction; held-tool use is disabled. Buildings are owned independently of
streamed terrain.
Structural targeting samples grid regions, including floor openings without
colliders. Reach is three grid units, corresponding to about 3 m horizontally
and 10.5 m vertically at the configured scale. Separate-base placement snaps to
a 2 m seabed grid and checks terrain clearance and neighboring bases.
The plan and kit assets under `Assets/Data/Building` define dimensions, starting
rooms and module prefabs. The Buildings object in Main controls placement spacing
and ground clearance.

Floors define occupied rooms. Their exposed boundaries generate walls and ceilings.
Room rows define dry air for movement and water-effect clipping. Fog distance
excludes the dry section of each view ray, preserving underwater visibility through
windows. Bottom openings carry a fixed local waterline. Swimming animals avoid
each base's bounding envelope, including gaps within an irregular footprint.

Entrance water refracts the completed scene and uses depth-dependent absorption
and screen-space reflections. Reflections fade to a subdued tint when geometry
is off-screen. The player camera's Entry Water Rendering component captures
scenery after underwater effects; the PC pipeline uses single-sample depth.
Tune ripples, absorption and refraction on `Assets/Materials/Building/EntryWater.mat`.
The shader's visual ripples do not move the gameplay waterline.

The original kit uses textured metal and painted panels with point-sampled detail.
[Asset provenance](../Assets/Models/Building/Provenance.txt) records its sources.
Editable authoring work is retained in the ignored local Art directory.
