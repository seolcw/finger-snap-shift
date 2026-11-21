# Shift Magic Spell

A giant mouse cursor is attacking you on top of two keyboards.
You fight back with the **Shift** key: rotate the row or column you are standing on, line up a spell word like `FIRE` or `MAGIC`, and blast the cursor with it. The final blow is spelled **SHIFT**.

Made for **GitHub Game Off 2026** (theme: **SHIFT**).

- Play: _itch.io link coming soon_ (WebGL, runs in the browser)
- Engine: Unity 6000.0.58f2, 2D (Built-in), WebGL, 320x200 pixel art scaled 3x to 960x600

## How to play

| Key | Action |
| --- | --- |
| WASD / Arrow keys | Move one key |
| **Shift + Arrow keys** | Rotate the row (←→) or column (↑↓) you stand on by one key |
| Space | On an attack slot: cast the stored spell block |
| P | Pause |
| R | Restart the current phase |
| M | Mute / unmute |

1. **Line up the word.** The target word (top of the screen) must be spelled in one row (left → right) or one column (top → bottom). Letters never change; only their positions shift. Letter positions flash every few seconds, so keep moving and watch.
2. **Store it.** A finished word becomes a spell block in an attack slot on the **other** keyboard.
3. **Cast it.** Step on the blue Shift key to teleport to the other keyboard, walk to the slot and press Space. Casting two blocks from the same keyboard within 1.5 s is a combo (+50%).
4. **Dodge.** Every cursor attack is telegraphed in red. The cursor can also shove a whole row, smash your slots, and chase you.
5. After three phases, the final word is **SHIFT**.

The first word (`FIRE`) is a practice round: the cursor does not attack until you cast your first block.

## Building

1. Open the project folder in Unity 6000.0.58f2 (Hub → Add project).
2. Open `Assets/Scenes/Main.unity` and press Play.
3. WebGL build: **Tools → Shift Magic Spell → Build WebGL**, or from the command line:
   ```
   Unity.exe -batchmode -quit -projectPath <this folder> -buildTarget WebGL ^
     -executeMethod ShiftMagicSpell.Editor.WebGLBuildUtils.BuildFromCommandLine ^
     -buildVersion shift-magic-spell-v0.1.0.sha.<commit> -buildOutput Builds/WebGL/<name>
   ```
   - Gzip compression with Decompression Fallback off: the web server must send `Content-Encoding: gzip` for `.gz` files (itch.io does). Opening `index.html` from disk does not work.
   - The custom template `Assets/WebGLTemplates/ShiftMagicSpell` shows only the 960x600 canvas (no footer, no scroll bars).
4. itch.io: zip the build folder so `index.html` is at the top of the zip, upload it as an HTML game, set the embed size to **960 x 600**, and turn on itch.io's **Fullscreen button** (the game itself has no fullscreen button; the canvas scales to fit when the frame gets larger).

Tests: PlayMode tests in `Assets/Tests/PlayMode` (rules, bot playthroughs, input, sound, screen captures). Run them with the Unity Test Runner or in batch mode (`-runTests -testPlatform PlayMode`) with the editor closed.

Pixel art lives in `Assets/Art/*.aseprite` and is imported directly (Aseprite Importer). The generator scripts are in `ArtSource/Scripts/` (run with Aseprite `-b --script`).

## Project docs (Korean)

- `HANDOVER.md`: current status and next steps
- `Docs/DESIGN.md`: game design (rules, numbers, assets)
- `Docs/UNITY-STRUCTURE.md`: what to edit where in the Unity project
- `WebPrototype/`: the HTML prototype the Unity version was ported from

## Credits

- Game design, code and pixel art: seolcw
- Sound effects: made for this project with [jsfxr](https://sfxr.me/) (settings in `Assets/Audio/Sfx/README.md`)
- Music: "Chiptune Grooving" from [Pixabay](https://pixabay.com/) (uploader 8059346, audio ID 142242), used under the Pixabay Content License
- Pixel font (5x7): made for this project

## License

- The source code and the original assets made for this project (pixel art, sound effects, pixel font) are released under the [MIT License](LICENSE).
- **The music file `Assets/Audio/Music/ChiptuneGrooving.mp3` is not covered by the MIT License.** It is used under the Pixabay Content License, which does not allow redistributing the audio on its own. Do not reuse it outside this game; get it from Pixabay instead.
