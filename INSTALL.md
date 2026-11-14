# OldSaber 1.0.0 — Beat Saber PC 1.40.8

1. Close Beat Saber.
2. Remove `Plugins/ClassicEnvironment.dll` and any standalone `ClassiNotes.dll` to avoid running duplicate replacements.
3. Install compatible **BSIPA**, **BeatSaberMarkupLanguage (BSML)** and **SiraUtil** for PC **1.40.8** using your mod manager.
4. Extract this ZIP into the game directory, replacing `Plugins/OldSaber.dll` and placing `Plugins/OldSaber.pdb` beside it.
5. In song selection, open the gameplay settings **Mods → OldSaber** tab:
   - **Old Environment** enables the recovered environments. Select **The First**, **Triangle**, **Big Mirror** or **Nice** through environment overrides and enable full environment/main effects.
   - **Old Notes** enables the supplied OldNotes project's note/debris shader, reflection probe and note/bomb trails, together with OldSaber's classic debris physics. This works independently of the environment toggle.
   - **Old Sabers** enables the extracted red/blue 0.10.1 models and original-style trails, independently of the other switches. It defaults to off.
   - **Old GUI (Gameplay HUD)** restores the extracted HUD and Beon/Teko fonts. It defaults to off and does not change menus.
6. Settings save automatically to `UserData/OldSaber.json` and apply to the **next song**. Old Environment and Old Notes default to on. Turn all four off for native visuals.

Set **Mirrors → High** for reflections on both the small player platform and the long runway.

The OldNotes assets are embedded in OldSaber; do not install its source folder or extraction archive. This replaces the 0.3.1 mesh-only approach with the supplied ClassiNotes project's materials and effects. Colored notes retain native meshes and markers. Bombs use the extracted 0.10.1 mesh; all hitboxes remain native.

Look for `OldNotes project assets loaded` at startup and `OldNotes enabled for this song` when notes are enabled. The plugin uses BSIPA's existing Harmony assembly; no additional Harmony DLL is needed.

1.0.0 increases note and sliced-note material brightness by 25% above the original material. Bombs are now matte dark grey without reflection probes or glossy highlights.

0.4.7 keeps bombs neutral black with a small brightness lift, removing the red tint from 0.4.6.

0.4.6 brightens bombs with a charcoal-red base and disables rim dimming. This compensates for the missing legacy rim-light pass in the ported note shader.

0.4.5 restores the runway mirror, including Nice’s previously inactive runway plane.

0.4.4 adds **Nice** through its normal environment selection and applies the OldNotes shader to bombs using compatible recovered material settings. Enable Old Environment and select Nice in environment overrides.

0.4.3 adds the extracted 0.10.1 bomb mesh as the default with Old Notes enabled. Turn Old Notes off to use native bombs on the next song.

0.4.2 keeps the OldNotes material on intact notes when the native material switcher initializes.

0.4.1 fixes the premature bundle-stream disposal that caused the 0.4.0 startup crash. The included PDB adds debug symbols; it is not required for normal execution.

Latest 1.0.0 update fixes the missing saber shader fallback and slightly brightens the matte bomb grey.

The latest 1.0.0 build removes the saber guard ring and keeps the handle neutral dark grey. The ornament, blade and trail follow the player’s saber colors. The alpha-blended trail fallback converts the original texture’s black background to transparency and uses higher opacity for visibility.

Latest 1.0.0 HUD placement update: the score remains at its extracted position on the standing platform, with no forward HUD score. The health bar keeps its extracted classic position, rotation and size. Combo and multiplier follow the current game’s anchors and match native counter text height. Text is white with the archived green glow disabled.

Latest 1.0.0 layout fix: restore anchored positions, canvas dimensions and shader channels after initialization, and hide inactive multiplier animation overlays.

Latest 1.0.0 HUD fix: initialize TextMeshPro renderers before assigning the extracted fonts, preventing the activation failure.

## First test

Enable Old GUI and check score, combo, multiplier progress and energy against normal gameplay. Turn it off to confirm the native HUD returns. Test restart, pause, Hide HUD, No Fail and battery energy.

Test Old Sabers on/off across songs and restarts, including pauses and fast swings. Disable competing custom-saber mods for this test. Check that bombs remain dark grey without reflections. Play short maps with each combination of the environment and note toggles, returning to song selection between tests. Confirm that settings persist after restarting the game. With Old Notes on, check colors, trails, slicing and debris in standard and Pro Mode, then test pause/resume, restart and practice. With Old Notes off, check that native materials, trails and debris behavior return on the next map. With Old Environment off, the next map should use the native selected environment.

Build, bundle-integrity and data/mathematical checks pass, but this build has not been run in Windows/VR. Multiplayer and interactions with other note/environment mods remain unverified.

To uninstall, close the game and remove `Plugins/OldSaber.dll`. Provide the game's `Logs` entries containing `OldSaber` if something fails.

Old Obstacles (1.0.0): enable **Distortions** in the game’s graphics settings first, then enable **Mods → OldSaber → Old Obstacles** and start a song. Without distortions, native obstacles remain. This independent toggle defaults to off. The mod uses the extracted 0.10.1 frame/corner meshes and noise texture, plus the original procedural surface geometry and sizing routines. Materials are adapted to supported current shaders; collisions, movement, colors and dissolve timing remain controlled by the current game. VR rendering still needs in-game verification.

Old GUI now keeps native graphics hidden after combo animations update, preventing the modern full-combo bar from reappearing.

Obstacle rendering correction (1.0.0): use one double-sided surface for the modern distortion shader, retain the extracted world-scaled UVs without scaling them twice, and use a mesh-compatible glow shader with solid edge blending for the extracted frame.

Obstacle loading fix (1.0.0): resolve the supported glow shader from loaded materials and read the frame renderer through its serialized reference. A missing optional frame renderer uses the standard frame render queue. Logs now identify individual missing dependencies and report successful classic obstacle construction.

Old Menu now uses the supplied **OldMenu** project and opens automatically at the main menu, including its environment. Replace `Plugins/OldSaber.dll` and `Plugins/OldSaber.pdb`, then fully restart Beat Saber. If your existing **Old Menu** setting is off, enable it in **Settings → Mod Settings → OldSaber** and restart. New configurations default to on; the version remains 1.0.0.

Solo uses the project's scrolling list with OST 1 and loaded custom songs, including available Expert+ maps. With BeatLeader installed, its native leaderboard is selected on the right, including its filters and detail UI. Native gameplay/results handling remains active. Party and Settings use the game's screens.

Test startup, Solo/mode selection, custom songs, difficulty changes, Play, results/return, Back, Settings, Party and BeatLeader profile/score details. Unity rendering and live gameplay have not been tested here.
