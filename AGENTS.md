# DeepSky working instructions

Read [STUDY.md](STUDY.md) when it exists for local research, reference handling, and provenance instructions. It is gitignored and is not required to run the project.

## Project and authoring

This is the creator's first commercial game. Prioritize atmosphere, satisfying controls, pacing and coherent systems over feature count or a convoluted story.

The root README records game-design decisions and open choices, not an implementation roadmap. Discussion does not authorize implementation. Preserve the visual atmosphere, human scale, grounded walking, high buoyant jumping, horizontal air control, and sinking/landing. Do not add an oxygen timer without an explicit implementation request.

Open `Assets/Scenes/Main.unity` and enter Play mode for verification, following the silent-testing rules below. This is the enabled launch scene. The `World` object streams seeded terrain, vegetation and wildlife across three depth shelves; these are prototype settings, not the final map or biome design.

Select `World` for the depth map and local preview controls. Edit its World Settings asset for seed, dimensions and terrain shape; content profiles control prefab distribution. Main contains the player, atmosphere and procedural-world configuration. World Settings, content profiles and prefabs are authoritative. Generated chunks and editor previews are transient, not saved level geometry. Do not add a second scene-rebuilding pipeline.

Game content belongs directly under `Assets`, without a separate game or demo wrapper folder.

## Code

Keep game C# code under `Assets/Scripts`, grouped by gameplay or rendering responsibility. Editor tools have a separate folder and assembly. Avoid generic wrapper folders such as `Runtime`; organize by purpose, not authorship.

Organize larger domains by responsibility, with namespaces matching their subfolders. Components and plain classes may share a feature folder. Generation-specific coordinate randomness belongs with world generation. Use existing Unity math APIs when their contracts fit; do not duplicate library interpolation functions.

Add XML documentation to functions in files being worked on, including summaries, input contracts with `param`, and output contracts with `returns` for non-void methods. Explain relevant units, ranges, ownership and side effects. Constructors document their inputs; parameterless void callbacks need a useful summary, not empty tags. Do not perform an unrelated project-wide documentation pass.

Component settings use `[SerializeField] private`. Editor scene authoring assigns these settings through Unity's serialization API. Runtime methods represent gameplay operations, not editor-only field assignment. Expose read-only properties only when another component needs them. Preserve serialized names and values during refactors; field renames require verified migration of saved data.

Declare accessibility explicitly on every type and member where C# permits it, including Unity callbacks and helpers. Private fields and parameters use camelCase; types, methods, properties, and constants use PascalCase. Initialize primitive-type fields explicitly, including default values such as `0`, `0f`, and `false`. Group declarations as constants/static fields, serialized settings, private runtime fields, then properties. Keep callbacks together, followed by public operations and private helpers. Runtime counters and timers are private state, not serialized authoring settings. Do not use `HideInInspector` to disguise unnecessary serialization.

Nullable reference checking is enabled and enforced by the `csc.rsp` beside each game and editor assembly definition. Unity generates the IDE project files from these compiler settings. Required Inspector references and component caches initialized in `Awake()` use non-nullable fields with `= null!`, paired with initialization-time validation. Reserve nullable references for genuinely optional or lazily created values. Preserve Unity's destroyed-object checks; `?.`, `??`, and `is null` alone do not test native object lifetime. Do not suppress nullable warnings broadly or use `!` to bypass missing validation.

Use `//` for ordinary single-line comments. Put `if`, `else`, `for`, `foreach`, and `while` bodies on separate, indented lines, including single-statement `return` and `continue` branches. Do not compress control-flow statements onto one line.

## Verification

Verification uses Unity compilation, console inspection, and focused Play-mode/visual checks. Do not introduce an automated test suite or expose implementation details for tests without the user's request. Unity Test Framework remains a dependency of installed editor packages, including Unity MCP.

Agent-driven game and editor test runs must be silent unless the task specifically requires testing audio. Enable and verify an editor or game-local mute before playback. Do not mute system audio, change saved gameplay audio settings, or modify audio sources/assets to silence verification. If a safe local mute is unavailable, keep playback stopped and ask before an audible run. Announce intentional audio checks and keep them brief. Stop playback before restoring the previous local audio state; never leave a test playing sound in the background.

Click the Game view to capture the mouse. WASD walks and steers in the air. Space jumps; hold it for buoyant ascent, release to sink. F toggles the dive light; Escape releases the cursor. Use 1/2/3 for knife, harpoon and drill, left mouse for tool use, E to collect, and Tab for inventory. Drag outside the inventory to drop a whole stack; sand is discarded without a world object.

Inventory lasts for the Play session; animal loot lasts while its body remains in the nearby wildlife population. Mining is inexhaustible and does not excavate terrain. Sharks retaliate and knock the diver back; player health, death, oxygen and objectives are not implemented.

## Assets

Materials belong under `Assets/Materials`, including model materials in matching subfolders. Textures belong under `Assets/Textures`, grouped by purpose or model. These rules also apply to generated assets. `Assets/Data` holds world datasets and generated geometry. Preserve visual and movement behavior during structural refactors. Inspect dependencies before deleting content, and do not remove vegetation or other assets incidentally.

Create our own artwork or modify licensed sources with verified commercial-use permission. Record provenance and redistribution restrictions; an asset's location does not establish originality. Search and visually inspect references before modeling or texturing; inspect available licensed source models, UVs and textures where practical. Do not infer PSX style from polygon count or an asset tag alone. Verify modeled candidates in Blender and at gameplay scale in Unity. Save authoring work before rendering, exporting or destructive operations, and leave the inspected work visible in the user's Blender instance.

Images, textures and designated large assets use Git LFS according to `.gitattributes`. Keep `.meta` files with assets so Unity references remain stable. Local Git backups must include `.git/lfs/objects`.

## Git and documentation

Commit or push only when explicitly requested. Remote setup and history cleanup require separate explicit authorization. Do not rewrite history as an incidental step.

Follow the user's global writing conventions, including no em dashes.

Documentation retains hard-to-recover evidence, provenance, non-obvious constraints, decisions, open questions, and a compact completed-milestone list with evidence links. Avoid duplicate folder inventories, code descriptions, intermediate-step logs, and transient test counts. Keep each detailed finding in one authoritative note and link to it elsewhere.
