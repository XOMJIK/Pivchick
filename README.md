# Battle Coop (Боевой Курятник)

A cartoon top-down shooter about chickens defending their coop — for **Windows** and **Android**. Works fully offline: graphics, sound, fonts and saves are all built into the app.

## Game modes
- **Story** — 4 chapters × 20 levels, 8 bosses, night levels; goals get harder from the first chapter to the last. Every level lasts at least 3 minutes: finish the goal early and you hold out until the end; on boss levels the boss shows up at 1:15
- **Endless battle** — waves that keep growing, a boss every 5 waves (including the Hamster Shaker with his earthquakes); grain and tokens for every wave, a big token every 5 waves, golden eggs for waves 5–50, Mega chests for waves 25 and 50 and a bonus for the wave you reach
- **Soul Knight mode** — pick a hero, step through the portal in the camp and fight through two worlds: the forest dungeon 0-1 … 0-5 and the factory catacombs 1-1 … 1-5. Rooms lock until every wave is cleared, an optional boss waits on 0-4 and 1-4 (a turkey or the Hamster Shaker), a boss guards the portal on 0-5 and 1-5. The mode has its own 35 weapons (melee, guns and 8 staffs — from a plunger that shoots poop to a gauss gun and a meat grinder); you carry 2 of them and can drop the extra ones, more drop from a chest room and are sold by a merchant on every level for pivchiki, the run's coins; legendary weapons never drop — the merchant sells them only for golden eggs. A shrine room holds a bird statue: pay pivchiki to bow to it (stronger ultimates cost more) and its ultimate replaces yours for the rest of the run — any hero's ultimate from the main game or Reinforcements, which calls a random fighter to your side for 20 seconds. There are no grenades in this mode, health doesn't regenerate on its own (only corn, potions and abilities heal), and every melee weapon knocks eggs back at enemies; a level has at most one vending machine (in the starting room or at the merchant) that sells a random health or stamina potion, and crates drop stamina (rarely corn or a pivchik). A minimap in the corner shows the rooms you've found, with the passives you've taken under it. Between levels the portal offers a choice of three passives (the +1 weapon slot one appears only once); all passives are listed in Weapons → Soul Knight and in the pause menu
- **Multiplayer** — co-op for up to 4 players over a local network: the same Wi-Fi or cable, or Radmin VPN / Hamachi for friends far away. One player creates the game (the lobby shows the address to share), the others find it with "Find games on the network" or type the address. Endless battle, any unlocked story level or Soul Knight; the host runs the match, every player controls their own hero. Enemies get tougher with more players: 2 — 150% health, 3 — 250%, 4 — 400%. A downed hero comes back after 15 seconds while a teammate is alive. The "Network server" option is locked until our own servers exist. The host needs TCP port 27015 (and UDP 27016 for the search) allowed in the firewall

## Features
- 9 heroes in 4 classes (Assault, Tank, Sniper, Medic), each with an ultimate and two passives; the ultimate charges from hits on enemies, every hero needs their own number of hits (snipers the most)
- 21 story guns of four rarities — every class has a common, rare, epic and legendary gun, and the rarer the gun, the stronger its trick; squad of up to 3 fighters; a ×2 crit chance for everyone (5%, snipers 12%); stamina (100) is spent on shots only in Soul Knight
- Chests, hero and weapon cards, upgrades up to level 10; a hero's stats, upgrade table and abilities open from the "?" button; chest odds are always shown
- Separate Weapons section (story guns plus a guide to the Soul Knight arsenal and passives), shop with two random card packs a day (refresh at 00:00 UTC)
- Profile: in-game name, avatars, account level earned with PTS, statistics and several local accounts on one device
- Achievements, three daily quests (00:00 UTC) and one weekly quest (Monday 00:00 UTC), a monthly battle pass with a free track and premium for 299 golden eggs
- Windowed, borderless and fullscreen modes (F11 toggles fullscreen), rebindable keys
- Buffed enemies with a colored aura: strength, speed or fire rate; the Kamikaze Swift — a fast bird with a bomb that you dodge or shoot down
- Piranhas in the rivers leap at the hero and the squad by the water or on a bridge; bridges and rotten bridges are placed at random every match, always with at least one solid bridge per river; breakable crates with loot in every mode
- Lighting, drifting cloud shadows, pollen, leaves, snow and fireflies, hit sparks
- 8 languages switchable on the fly: Russian, Ukrainian, English, Spanish, German, French, Portuguese and Polish
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
                          daily shop cards (Offers.cs), account progression — PTS, avatars, achievements,
                          quests, battle pass (Meta.cs), the Hamster boss (Hamster.cs), local multiplayer —
                          transport and lobby (Net.cs, Mp.cs) and world snapshots (MpSync.cs)
  Render/                 rendering: Ctx.cs (canvas-style API over Skia), Draw.*.cs, Hud.cs
  Audio/                  synthesizer, sound effects and music
  UI/                     Avalonia UI: lobby, boards, overlays, game view
  Assets/                 fonts, dictionaries (Lang/en, uk, es, de, fr, pt, pl .json — keys are the Russian strings), icon
src/Kuriatnyk.Desktop/    Windows launcher
src/Kuriatnyk.Android/    Android launcher
tools/Snap/               headless check: runs the game and saves frames as PNG; also a perf test and icon generator
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
Controls (default, can be rebound in Settings → Controls): WASD — move, auto-aim fires on its own (T — toggle), hold the mouse button — manual fire, right mouse button, Q or Space — grenade (not in Soul Knight), R — ultimate, F — swap weapon, E — pick up, buy or bow to a statue (Soul Knight), G — drop weapon (Soul Knight), 1/2/3 — backpack slot, F11 — fullscreen, Esc/P — pause.

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
`%LOCALAPPDATA%\BoevoyKuryatnik\save.json` (extra local accounts — `save-<id>.json`) and `settings.json` (on Android — the app's internal folder). The `KUR_DATA_DIR` environment variable overrides the folder, so tests don't touch real progress.

## Testing
- `dotnet run --project tools/Snap -- <folder>` — headless battle run, frames saved as PNG; `-- <folder> perf` — performance test; `-- <folder> sk` — full Soul Knight run through levels 0-1 … 1-5; `-- <folder> story` — checks that story levels last at least 3 minutes; `-- <folder> glyphs` — checks that the fonts have every letter of every language; `-- <folder> skweapons` — fires every Soul Knight weapon and checks damage and stamina; `-- <folder> hamster` — the Hamster boss fight.
- Multiplayer: with `KUR_NET_LOOPBACK=1` start `Snap <folder1> mphost [endless|story|sk]` and `Snap <folder2> mpclient` at the same time — a host and a client play over 127.0.0.1 and save frames from both sides.
- `KUR_AUTOSHOT=<folder>` (optionally `KUR_SIZE=1180x720`, `KUR_AUTOSHOT_NOWIN=1` to skip the fullscreen/borderless steps) when launching the Windows build — the app walks through all screens, saves screenshots and exits.

## Credits
- nutze (Arthur Dyakonov) — creator, game design, testing
- XOMJIK (Illia Martseniuk) — creator, programming
