# OldSaber

**OldSaber 1.0.0** restores classic Beat Saber environments and the supplied OldNotes project’s note appearance/effects on **PC Beat Saber 1.40.8**. This project was previously named ClassicEnvironment.

- Mod: [OldSaber-1.0.0-bs1.40.8.zip](dist/OldSaber-1.0.0-bs1.40.8.zip)
- Extracted note assets: [OldSaber-ClassicNotes-0.10.1.zip](dist/OldSaber-ClassicNotes-0.10.1.zip)
- Installation: [INSTALL.md](INSTALL.md)

**Migration:** close the game and remove `Plugins/ClassicEnvironment.dll` before installing `Plugins/OldSaber.dll`. Remove standalone `ClassiNotes.dll` too; its implementation is now integrated.

Latest 1.0.0 update fixes unavailable classic saber shaders by falling back to compatible materials already loaded by the native model. Bomb grey is slightly brighter while remaining matte.

1.0.0 includes an optional **Old Sabers** switch and matte dark-grey bombs. Note and sliced-note brightness remains 25% above the original material.

0.4.7 keeps bombs neutral black with a small brightness lift, removing the red tint from 0.4.6.

0.4.6 brightens bombs with a charcoal-red base and disables rim dimming. This compensates for the missing legacy rim-light pass in the ported note shader.

0.4.5 restores runway mirrors alongside the player-platform mirror in all four supported environments.

0.4.4 adds the recovered Nice environment through its existing game selection (100 rings and two visualizers). Bombs now use the OldNotes shader with compatible recovered bomb material settings, using the same material-switcher protection as colored notes. The shader does not support every property of the original bomb shader, so the appearance is a reconstruction.

0.4.3 uses the actual 0.10.1 BombNote mesh (214 vertices, 100 triangles) by default with Old Notes enabled. The mesh is extracted by following the source prefab reference; native bomb transforms and hitboxes remain intact.

0.4.2 fixes intact notes reverting to native materials during the native material switcher’s Awake. Both switcher branches now use the OldNotes material, with originals preserved for restoration.

0.4.1 fixes a startup crash caused by closing the note bundle stream before Unity finished reading it. The stream now remains open through asset loading and bundle unload. Portable debug symbols (`OldSaber.pdb`) are included for diagnostics.

## Features

The song-selection gameplay settings **Mods → OldSaber** tab provides independent **Old Environment**, **Old Notes**, **Old Sabers** and **Old GUI (Gameplay HUD)** toggles. Environments/notes default to on; Old Sabers and Old GUI default to off. Settings persist through BSIPA in `UserData/OldSaber.json`, and apply to the next song. BSML and SiraUtil are required.

Old Notes uses the supplied `OldNotes/` (ClassiNotes by whatdahopper) project: its bundled `Custom/ClassiNotes/Note` shader, note/debris materials, reflection probe and note/bomb trail prefabs. Its SiraUtil decorators cover normal/Pro Mode notes, burst heads/elements and HD/LW debris. Native colored-note meshes, markers, hitboxes and cut detection remain intact. OldSaber's existing debris physics follows the same toggle. The previous mesh-only note replacement has been removed.

With Old Environment enabled, select The First, Triangle, Big Mirror or Nice through environment overrides. Each loads its recovered 0.10.1 layout, native song-driven lights, eight rotating side lasers, scene-specific rings and two audio visualizers. Triangle has 30 triangular rings plus 15 large rings; The First and Big Mirror have 90 small rings plus 15 large rings.

Side lasers use native glow sprites instead of opaque box cores. Both the small player platform and runway reflect, following the game's Mirrors setting. Both surfaces share the native reflection renderer and a slightly raised plane to avoid the earlier floor flicker. Nice's inactive runway plane is enabled when a compatible mirror template is available.

Classic debris behavior applies to fragments created by the game's `NoteDebris.Init`, independently of the selected environment:

- Direction-dependent separation: 1 m/s downward, 4.5 m/s sideways, 8 m/s upward, plus the old 20% directional jitter.
- Random angular impulse within a sphere, scaled by 4.5.
- Original scale-aware cut-half pivot calculation.
- Actual prefab settings: **1-second lifetime**, **linear dissolve**, mass 1, zero linear/angular drag, interpolated Rigidbody motion and a 0.15 m sphere collider.
- No modern saber-speed or note-movement velocity contribution to debris.
- Original settings and native physics restored when the mod is disabled. Native pooling still manages fragment disposal.

BSIPA's existing Harmony assembly patches debris initialization; no additional Harmony DLL is bundled. The native forced-simple-physics path is respected for replay/deterministic configurations, retaining the old impulses/lifetime but using the game's simple integrator. Modern chain fragments also receive this behavior when they use `NoteDebris`; there were no chain notes in 0.10.1.

The latest 1.0.0 build removes the saber guard ring and keeps the handle neutral dark grey. The ornament, blade and trail follow the player’s saber colors. The alpha-blended trail fallback converts the original texture’s black background to transparency and uses higher opacity for visibility.

Latest 1.0.0 HUD placement update: the score remains at its extracted position on the standing platform, with no forward HUD score. The health bar keeps its extracted classic position, rotation and size. Combo and multiplier follow the current game’s anchors and match native counter text height. Text is white with the archived green glow disabled.

Latest 1.0.0 layout fix: restore anchored positions, canvas dimensions and shader channels after initialization, and hide inactive multiplier animation overlays.

Latest 1.0.0 HUD fix: initialize TextMeshPro renderers before assigning the extracted fonts, preventing the activation failure.

## Classic gameplay HUD

Enable **Old GUI (Gameplay HUD)** in Mods → OldSaber. It reconstructs the supplied 0.10.1 HUD hierarchy, score/combo/multiplier labels, progress ring and energy bar. Menus remain unchanged. The toggle is independent of environments, notes and sabers, and disabling it restores the native HUD.

The runtime font assets use extracted Beon and Teko SDF atlases, glyph metrics and material settings converted to the current TextMeshPro format. The original Beon TTF and HUD objects are in `extracted/OldSaberGUI/`; the source game supplies Teko as prebuilt font atlases rather than an embedded TTF. `tools/extract_gui.py` regenerates the assets.

Current game controllers continue to calculate score, combo, multiplier and energy; the classic display mirrors their values. Native Hide HUD and No Fail visibility remain in effect; battery mode retains the native energy display. Advanced modern HUD widgets are replaced by the classic layout. VR layout, font rendering, multiplayer and other HUD-mod compatibility still need in-game testing.

## Classic sabers

Enable **Old Sabers** in Mods → OldSaber before starting a song. This is independent of the environment and note switches. The extracted 0.10.1 red/blue models use their original meshes, transforms and material settings with compatible current shaders. The recovered Xft spline/history algorithm drives the original trail texture: 20 frames, 60 segments and four startup frames. This intentionally retains frame-based trail length. Native saber tracking, cut detection and scoring are unchanged.

`extracted/OldSaberSabers/` contains raw objects, readable metadata, OBJ meshes and the trail PNG; `OldSaber/Assets/ClassicSabers.json` embeds the runtime data. The ported trail code is generated from the supplied decompilation. Original 2017 compiled shaders are archived for inspection, not loaded into 1.40.8. Disable competing custom-saber replacements when testing; their later model replacements may conflict. In-game saber rendering and mod compatibility remain unverified.

Bombs use dark grey with reflection probes, glossy reflections and specular highlights disabled. Their mesh and hitboxes are unchanged by this material adjustment.

## Note and shader extraction

`extracted/OldSaberNotes/` contains the GameNote, GhostNote, BombNote and NoteDebris prefab graphs, five mesh exports, nine materials, two texture previews and nine referenced shaders, including `Custom/Note`.

**The shader exports are compiled data and reconstructed inspection text, not original portable shader source.** They are not installed into 1.40.8. Runtime note visuals instead use the supplied OldNotes project’s Unity 2021 bundle. See the extraction README and manifest for details.

The earlier environment extraction remains under `extracted/ClassicEnvironment/` as a historical source archive. Its directory name is not the plugin identity.

## Build and validation

Install `tools/requirements.txt` into a Python environment. Supply 1.40.8's managed assemblies in `Managed/` and the old data in `Beat Saber_Data/`. Set `BEATSABER_PLUGINS` to a 1.40.8 Plugins folder containing BSML.dll and SiraUtil.dll (defaults to the local BSManager 1.40.8 installation).

```sh
python tools/build_scene.py
python tools/extract_notes.py
python tools/build_oldnotes.py
python tools/build_bomb.py
python tools/extract_sabers.py
python tools/extract_gui.py
python tools/port_saber_trail.py
python tools/build_mod.py
python tools/validate_build.py
python tools/validate_oldnotes.py
python tools/validate_sabers.py
python tools/validate_gui.py
python tools/test_gui_glyphs.py
python tools/test_gui_factory.py
python tools/test_gui_layout.py
python tools/test_gui_sizing.py
python tools/test_saber_spline.py
python tools/test_saber_material.py
python tools/test_saber_appearance.py
python tools/test_oldnotes_load.py
python tools/test_oldnotes_material.py
python tools/test_debris.py
python tools/test_lasers.py
python tools/test_spectrum.py
```

`tools/build_oldnotes.py` changes only the bundle’s ScriptableObject assembly identity from ClassiNotes.dll to OldSaber.dll. The original `OldNotes/DataStores/PluginDataSO.cs` is compiled directly, preserving its field layout. Shader/material/prefab bytes are checked against the supplied bundle. The original OldNotes sources remain unchanged.

The .NET SDK compiler builds `OldSaber.dll` against local target references without NuGet dependencies. Assembly/resource names, plugin manifest and source namespaces use OldSaber. `OldSaber/Assets/ClassicDebris.json` is generated from the actual old prefab; it intentionally uses the serialized one-second lifetime instead of the decompiled class's 1.5-second field default.

Validation covers the four scene layouts, note/shader dependency closure, serialized physics values, native fields and debris method parameters, package contents, classic impulse/pivot mathematics, laser construction and spectrum processing. Standalone tests do not run Unity physics or rendering.

## Limits

This build has not been run in Windows/VR. Unity 2022's PhysX and the current game's collision layers/world can differ from Unity 2017, so exact old collision trajectories are not guaranteed. Score/cut detection is untouched. Test left/right and horizontal/vertical cuts, restart, pause, practice mode and disabling the mod before testing other mod combinations.

Environment rendering is a reconstruction using current materials/smoke. Event-box lighting conversion, multiplayer, 360-degree maps and interactions with other environment/debris mods are unverified. If no native glow-sprite template is available, side lasers use volumetric bloom only and need full main effects; the log reports that fallback.

Old Obstacles (1.0.0): enable **Distortions** in the game’s graphics settings first, then enable **Mods → OldSaber → Old Obstacles** and start a song. Without distortions, native obstacles remain. This independent toggle defaults to off. The mod uses the extracted 0.10.1 frame/corner meshes and noise texture, plus the original procedural surface geometry and sizing routines. Materials are adapted to supported current shaders; collisions, movement, colors and dissolve timing remain controlled by the current game. VR rendering still needs in-game verification.

Old GUI now keeps native graphics hidden after combo animations update, preventing the modern full-combo bar from reappearing.

Obstacle rendering correction (1.0.0): use one double-sided surface for the modern distortion shader, retain the extracted world-scaled UVs without scaling them twice, and use a mesh-compatible glow shader with solid edge blending for the extracted frame.

Obstacle loading fix (1.0.0): resolve the supported glow shader from loaded materials and read the frame renderer through its serialized reference. A missing optional frame renderer uses the standard frame render queue. Logs now identify individual missing dependencies and report successful classic obstacle construction.


## Old Menu (restart required)

Old Menu uses the supplied `OldMenu/` project's menu and environment prefabs and opens automatically. Enable **Old Menu** in **Settings → Mod Settings → OldSaber**, then fully restart Beat Saber. New configurations default to enabled; existing disabled settings are respected. Version remains **1.0.0**.

Solo displays OST 1 and loaded custom songs, Standard/One Saber/No Arrows modes, and all available difficulties including Expert+. Song and difficulty hover use cyan at 25% opacity; selection uses 50% with white text. Native controllers handle previews, loading, scores and results. Party and Settings use the game's screens.

The classic leaderboard uses the original 0.10.1 online **Highscores** screen (level1 object 567) and row template (341), extracted with their fonts, transforms and separators. BeatLeader supplies its already-loaded score data and Global/Player/Friends, paging and score-detail callbacks. The scrollable list accommodates all ten scores and an optional personal row. Personal PP appears beneath the title. **Details / filters** opens BeatLeader's full interface, with a button to return to the classic display. This is a separate classic display, not a reskin of the modern row layout; BeatLeader's controllers remain alive to handle requests.

Environment meshes use rebuilt static-batch sections; stale imported batch renderers are removed. Generated light boxes are parented locally and initialized with their dimensions, center and color before activation, preventing default-sized boxes from appearing at the world origin before the first tube update.

To regenerate assets, run `tools/build_oldmenu.py` and `tools/extract_menu.py` using `tools/requirements.txt`, then `python3 tools/build_mod.py`. Validate with `tools/validate_oldmenu.py`, `tools/validate_menu.py`, `tools/validate_classic_leaderboard.py`, `tools/test_oldmenu_light_setup.py`, and `tools/test_oldmenu_startup.py`.

Compilation, source-asset provenance, assembly contracts, package integrity and initialization-order checks are available locally. Unity rendering, live BeatLeader responses, pointer input and gameplay transitions still need in-game verification.
