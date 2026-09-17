# Ocean life

## Wildlife authoring

`Assets/Data/World/Wildlife.asset` owns population counts, spacing, scale and
seabed clearance. Whale passes start 46..52 m above their local terrain; rays
start 20..35 m above it. These are scene-scale presentation distances, not a
simulation of species-specific diving depths. Counts and cruise speeds are
independent of these height ranges.

Whales gradually return toward their cruise clearance after crossing higher
terrain. Their existing slow tail animation and size variation remain authored
on the material and population profile. Overhead animals receive the same
distance fog as other wildlife, so viewing elevation and terrain affect how
much of a distant whale is visible. Height keeps them beyond normal ground-level
tool reach; it does not give them damage immunity.

SwimmingMovement shares body-clearance checks across fish, sharks, rays and
whales. SwimmingNavigation retains bounded local 3D detours around terrain and
occupied base rooms/supports. Every movement step is swept against the same
clearance rules as the planned route. Numerical terrain-cell heights are cached
per world, independent of loaded chunk colliders. Search work is budgeted across
frames; this is local navigation, not an exhaustive route across the entire map.

Base edits rebuild occupied room and support envelopes. If construction encloses
an animal, overlap recovery selects a clear envelope-face exit that also passes
its terrain checks. Empty space between disconnected rooms is not reserved as
one solid base-sized obstacle. Fish schooling and shark attack decisions remain
their species behaviours; navigation supplies clear movement underneath them.

## Rock coral

`Assets/Data/World/KelpForest.asset` contains **Branching rock coral** and
**Rock hydroid growth** plant-batch rules. Edit their Density, Scale Multiplier,
Rock Weight Range and Region Range in the content-profile Inspector.
Density is candidates per square metre of horizontal terrain footprint, before
surface and region rejection, not a count per rock or per sloped square metre.

Coral uses density 0.5 and scale 0.55..1.05; hydroid growth uses density 0.4 and
scale 0.65..1.25. Both attach to rock weight 0.6..1, allow slopes through 82
degrees and follow the surface normal. Their overlapping intervals in the same
28 m region field produce mixed coverage and areas of bare rock. The spawn
clearing remains clear. Other depth profiles do not acquire these populations.

Each colony has three intersecting, slightly leaning texture cards, with
different crown heights and spans: twelve vertices, six triangles, one material.
Coral is roughly 0.5..1 m across; hydroids are roughly 0.4..0.8 m across.
The existing plant pipeline combines each species into chunk-owned meshes.
There are no per-colony GameObjects, colliders or Update callbacks.

Materials use two-sided alpha clipping with depth writes, point-filtered
256-pixel texture imports and coverage-preserving mipmaps. Vertex alpha encodes
root-to-tip height for subtle rooted sway. Coral retains underwater tint, fog,
torch illumination and interior clipping. **Surface nearby light boost** on
the coral materials limits close-range overexposure without changing the global
player light. The shared shader's default preserves other materials' lighting.
Coral has no emission, harvesting or proximity-retraction behaviour.

### Provenance

BranchingCoral and RockHydroid meshes are original Blender-authored geometry.
The ignored editable source is `Art/Source/CoralCards.blend`; construction and
export settings are in `Art/Source/author_coral_cards.py`. Runtime FBX exports
are under `Assets/Models/Corals`. No recovered game geometry or texture pixels
are included.

`Assets/Textures/Corals/BranchingCoral.png` and `RockHydroid.png` are original
artwork generated with the built-in image-generation tool, without input images.
Their source alpha is preserved; Unity supplies import downsampling. These are
generated images, not third-party CC0 assets or claims of exclusive copyright.

BranchingCoral generation prompt:

> Use case: stylized-concept. Asset type: original game texture for alpha-cutout crossed planes, NOT a scene or concept presentation. Create one low spreading branching coral fan, front orthographic view, width about twice height, trunk/root at bottom center, naturally irregular many tapering forked arms reaching diagonally outward. Rich periwinkle/violet-blue branches with pale lavender and soft ivory growing tips, subtle rough organic tissue, dark purple shaded crevices. Uneven clusters of branch forks, some short fingers and some longer lateral arms. Real marine organic appearance with readable chunky shapes suitable for downsampling to 256 pixels, classic textured PSX game aesthetic without faceted polygon look. It should look like a compact coral colony not a tree or fern. Entire silhouette fits in frame with a small transparent margin, base touches near bottom center. Flat diffuse albedo lighting, no cast shadow, no bloom, no glow halo, no rocks, no sand, no ocean scenery, no text. Genuinely transparent background and transparent holes between branches, clean RGBA cutout. Landscape 2:1 canvas. Original artwork, no referenced images.

RockHydroid generation prompt:

> Use case: stylized-concept. Asset type: original transparent RGBA cutout game texture for crossed vegetation planes. One small low spreading tuft of marine green hydroid-like rock growth, fine irregular forked sprigs, many dense short tiny branches growing from a single root along bottom center, twice as wide as tall. Moss green and olive stems with fresh yellow-green growing tips, subtle natural organic tissue texture. Front orthographic albedo view. Fine coral-like branching sprigs, NOT broad leaves, NOT grass blades, NOT a ball, NOT a tree. Natural asymmetric silhouette, clear transparent gaps, readable clumps suitable for downsampling to 128 pixels in a textured PSX underwater game. Diffuse flat lighting, no glow no bloom no shadows. Transparent background including between branches. No rock or ground, no text, no reference imagery. Landscape 2:1 image, fill frame with small margin, root close to bottom.
