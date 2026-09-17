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

The KelpForest content profile contains **Rose coral shelves** and **Cupped coral**
decoration rules. Density, scale, slope, rock weight and regional coverage are
editable there. They use rock weight 0.65..1, separate coverage intervals in the
same 28 m region field, and align to the sampled surface normal. Coral is passive
scenery, not a harvestable resource or collision obstacle.

Meshes are original irregular, layered plate forms, with separate tissue,
growing-margin and underside materials. The original generated tissue image is
`Assets/Textures/Corals/CoralTissue.png`; Unity imports it at 256 pixels with point
filtering and mipmaps. Materials use the shared underwater surface shader.

### Provenance

PlateCoral and CuppedCoral were authored for this project in Blender. The local
authoring file is `Art/Source/PlateCoral.blend`, with geometry construction in
`Art/Source/author_plate_coral.py`. Both are ignored authoring material; the FBX
exports and texture are project assets. No downloaded model or photographic
pixels are embedded in these assets.

[Corals of the World: Montipora capricornis](https://www.coralsoftheworld.org/species_factsheets/species_factsheet_summary/montipora-capricornis/)
was inspected as a shape/color reference only. Its photographs are not licensed
project textures. CoralTissue was generated using the built-in image-generation
tool, without an input image. It is generated artwork, not a third-party CC0 scan.

Final generation prompt:

> Use case: photorealistic-natural. Asset type: seamless square albedo texture for a living plate coral mesh in a textured PSX-style underwater game. Create original artwork, a flat orthographic macro surface of Montipora-like living coral tissue, fine closely spaced tiny porous corallite cups, shallow irregular radiating grooves, granular calcified organic tissue. Muted warm terracotta and dusty salmon with cream tan speckling, subtle mottling. Texture fills entire square edge to edge. Uniform diffuse neutral lighting, no directional shadows, no perspective, no coral silhouette, no sea background, no edge or border, no text. Approximately 35 to 50 irregular tiny polyp pores across width; retain readable microstructure when imported at 256px. Natural surface detail, not cartoon, not gravel or stones, not giant flower polyps. Seamless tiling on both axes. Matte, restrained contrast, no baked blue underwater tint or glow.
