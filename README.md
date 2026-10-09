# Battle Coop (Боевой Курятник)

A cartoon top-down shooter about chickens defending their coop — for **Windows** and **Android**. Works fully offline: graphics, sound, fonts and saves are all built into the app.

## Game modes
- **Story** — 4 chapters × 20 levels, 8 bosses, night levels; goals get harder from the first chapter to the last
- **Endless battle** — waves that keep growing, a boss every 5 waves
- **Soul Knight mode** — pick a hero, step through the portal in the camp and fight through dungeon levels 0-1 … 0-5: rooms lock until every wave is cleared, an optional boss waits on 0-4 and the final boss on 0-5. The mode has its own 35 weapons (melee, guns and 8 staffs — from a plunger that shoots poop to a gauss gun and a meat grinder); you carry 2 of them, more drop from a chest room and are sold by a merchant on every level for pivchiki, the run's coins. Between levels the portal offers a choice of three passives (the +1 weapon slot one appears only once)
- **Multiplayer** — coming soon

## Features
- 9 heroes in 4 classes (Assault, Tank, Sniper, Medic), each with an ultimate and two passives
- 17 story guns of three rarities, squad of up to 3 fighters; stamina (50 in story and endless, 100 in Soul Knight) is spent on shots
- Chests, hero and weapon cards, upgrades up to level 10; a hero's stats, upgrade table and abilities open from the "Details" button
- Separate Weapons section (story guns plus a guide to the Soul Knight arsenal), shop with two random card packs a day
- Windowed, borderless and fullscreen modes (F11 toggles fullscreen), rebindable keys
- Piranhas in every river leap at anyone by the water or on a bridge; breakable crates with loot in every mode
- Russian and English, switchable on the fly
- Scales to any screen: phones, tablets and large monitors

## Tech
- **.NET 10**, **Avalonia 11.3** — shared UI for desktop and mobile
- **SkiaSharp** — rendering of the battlefield, characters and HUD
- Sound and music are synthesized in code: WASAPI (NAudio) on Windows, AudioTrack on Android
- Rubik and Rubik Bubbles fonts (SIL OFL 1.1, license files in `src/Kuriatnyk/Assets/Fonts`)

## Project layout
```
src/Kuriatnyk/            shared code
  Game/                   game logic: data (Data.cs), maps and rivers (World.cs), dungeons (Dungeon.cs),
                          combat (Combat.cs, Weapons.cs), game loop and AI (Update.cs), flow (Flow.cs),
                          input, chests, saves; Soul Knight weapons and passives (SkData.cs, SkPlay.cs),
                          daily shop cards (Offers.cs)
  Render/                 rendering: Ctx.cs (canvas-style API over Skia), Draw.*.cs, Hud.cs
  Audio/                  synthesizer, sound effects and music
  UI/                     Avalonia UI: lobby, boards, overlays, game view
  Assets/                 fonts, English dictionary (en.json), icon
src/Kuriatnyk.Desktop/    Windows launcher
src/Kuriatnyk.Android/    Android launcher
tools/Snap/               headless check: runs the game and saves frames as PNG; also a perf test and icon generator
tools/export-lang.js      regenerates en.json from web/js/lang.js
web/                      browser version of the game
```

## Windows
Run from source:
```
dotnet run --project src/Kuriatnyk.Desktop
```
Single-file exe (no .NET installation required):
```
dotnet publish src/Kuriatnyk.Desktop -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o dist/windows
```
Controls (default, can be rebound in Settings → Controls): WASD — move, auto-aim fires on its own (T — toggle), hold the mouse button — manual fire, right mouse button, Q or Space — grenade, R — ultimate, F — swap weapon, E — pick up or buy (Soul Knight), 1/2/3 — backpack slot, F11 — fullscreen, Esc/P — pause.

## Android
One-time setup:
```
dotnet workload install android
winget install Microsoft.OpenJDK.21
```
(run the first command in PowerShell as Administrator). Then build the APK:
```
powershell -ExecutionPolicy Bypass -File build-android.ps1
```
On the first run the script downloads the Android SDK; the APK goes to `dist/android`. It is signed with a debug key and can be installed on a phone directly; Google Play requires your own key (`AndroidSigningKeyStore`). The game runs in landscape only.

## Saves
`%LOCALAPPDATA%\BoevoyKuryatnik\save.json` and `settings.json` (on Android — the app's internal folder). The `KUR_DATA_DIR` environment variable overrides the folder, so tests don't touch real progress.

## Testing
- `dotnet run --project tools/Snap -- <folder>` — headless battle run, frames saved as PNG; `-- <folder> perf` — performance test; `-- <folder> sk` — full Soul Knight run through levels 0-1 … 0-5.
- `KUR_AUTOSHOT=<folder>` (optionally `KUR_SIZE=1180x720`) when launching the Windows build — the app walks through all screens, saves screenshots and exits.

## Credits
- nutze (Arthur Dyakonov) — creator, game design, testing
- XOMJIK (Illia Martseniuk) — creator, programming
