# Graph Report - KuriatnukPivchuka  (2026-10-10)

## Corpus Check
- 39 files · ~214,777 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 1539 nodes · 5342 edges · 75 communities (50 shown, 25 thin omitted)
- Extraction: 70% EXTRACTED · 30% INFERRED · 0% AMBIGUOUS · INFERRED: 1595 edges (avg confidence: 0.85)
- Token cost: 89,644 input · 0 output

## Community Hubs (Navigation)
- Skia Canvas Rendering
- C# Imports & Namespaces
- Web Menus & HUD
- Combat & Bullets (C#)
- Web Combat Logic
- Input, Keys & World Setup
- Panel Cards & Tabs
- Main View & Overlays Flow
- Overlay Builders
- Web World & Rivers
- Sound Effects
- Web Drawing
- Lobby Layout
- Game Data Tables
- Dungeon Rooms & Pickups
- Rivers & Bridges (C#)
- Save Data Model
- Web Game Data
- Save Store & Settings
- Web Player State
- Android Activity
- Board Panels
- Enemy AI & Bosses
- Web Enemy Update
- Projects & Build Config
- Story Levels & Passives
- Node Dev Tools
- Game UI Interface
- Web Release History
- Chest Rolls
- Layout Panels
- Shop Offers
- Project Layout (README)
- Synth Voices
- Game Start & Menu Flow
- Skia Draw Operations
- Audio Output
- GameView Tick & Pointer
- Game Features History
- UI Kit Styling
- Localization & Statue Ults
- Display Mode & AutoShots
- Soul Knight Features
- Web Audio
- Colors & Gradients
- Soul Knight Weapons
- Win & Coin Banking
- Web Save
- Web Core Utils
- App Entry Point
- Pause & Volume
- Assets & Localization Docs
- Core Game Features
- Story & Arsenal Docs
- Fonts
- Waveforms
- Web Input
- Web Localization
- App Icon
- Font Licenses
- App Icon 512
- Buffed & Kamikaze Enemies

## God Nodes (most connected - your core abstractions)
1. `G` - 332 edges
2. `Ctx` - 132 edges
3. `MainView` - 83 edges
4. `Overlays` - 50 edges
5. `Sfx` - 42 edges
6. `Enemy` - 38 edges
7. `SaveData` - 36 edges
8. `Press` - 31 edges
9. `D` - 29 edges
10. `Kuriatnyk` - 26 edges

## Surprising Connections (you probably didn't know these)
- `Боевой Курятник web version (v0.4)` --semantically_similar_to--> `Battle Coop (Boevoy Kuryatnik)`  [INFERRED] [semantically similar]
  web/README.md → README.md
- `Rubik / Rubik Bubbles via Google Fonts` --semantically_similar_to--> `Rubik and Rubik Bubbles Fonts (SIL OFL 1.1)`  [INFERRED] [semantically similar]
  web/index.html → README.md
- `Locked Multiplayer and Soul Knight mode buttons` --conceptually_related_to--> `Soul Knight Mode`  [INFERRED]
  web/index.html → README.md
- `Боевой Курятник web version (v0.4)` --references--> `nutze (Arthur Dyakonov)`  [EXTRACTED]
  web/README.md → README.md
- `Боевой Курятник web version (v0.4)` --references--> `XOMJIK (Illia Martseniuk)`  [EXTRACTED]
  web/README.md → README.md

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Battle Coop game modes** — readme_story_mode, readme_soul_knight_mode [EXTRACTED 1.00]
- **Offline C# tech stack (UI, rendering, synthesized audio, bundled fonts)** — readme_synthesized_audio, readme_rubik_fonts [EXTRACTED 1.00]
- **Soul Knight Run Loop** — readme_soul_knight_mode, readme_stamina [EXTRACTED 1.00]
- **Web release history 0.1 to 0.4** — web_changelog_v0_1, web_changelog_v0_2, web_changelog_v0_3, web_changelog_v0_4, web_changelog_patch_notes_versioning [EXTRACTED 1.00]
- **Automated Testing Harness** — readme_snap_tool, readme_kur_autoshot, readme_kur_data_dir [INFERRED 0.85]
- **Battle Coop game modes** — readme_story_mode, readme_endless_battle, readme_soul_knight_mode, readme_multiplayer [EXTRACTED 1.00]
- **Soul Knight run economy (pivchiki, shrine, vending, crates)** — readme_pivchiki, readme_bird_statue_shrine, readme_vending_machine, readme_breakable_crates, readme_stamina [INFERRED 0.85]
- **Cross-platform tech stack** — readme_dotnet_10, readme_avalonia, readme_skiasharp, readme_synthesized_audio, readme_rubik_fonts [EXTRACTED 1.00]

## Communities (75 total, 25 thin omitted)

### Community 0 - "Skia Canvas Rendering"
Cohesion: 0.10
Nodes (15): Ctx, C, fillStyle, font, globalAlpha, lineCap, lineDashOffset, lineJoin (+7 more)

### Community 1 - "C# Imports & Namespaces"
Cohesion: 0.07
Nodes (18): Kuriatnyk.UI, Kuriatnyk.Render, Kuriatnyk, Kuriatnyk.Desktop, Platform, LangJson, SaveJson, SKP (+10 more)

### Community 2 - "Web Menus & HUD"
Cohesion: 0.06
Nodes (68): clearBanner(), fmt(), gameOver(), goalLabel(), hc, pauseGame(), purseHTML(), refreshMenu() (+60 more)

### Community 3 - "Combat & Bullets (C#)"
Cohesion: 0.10
Nodes (6): Ally, Bomb, Bullet, Fighter, Part, Zone

### Community 4 - "Web Combat Logic"
Cohesion: 0.09
Nodes (58): addUlt(), aimAssist(), allyFire(), allyUlt(), blast(), collect(), collideWorld(), eBullet() (+50 more)

### Community 5 - "Input, Keys & World Setup"
Cohesion: 0.06
Nodes (14): G, Clock, Act, name, InputState, Stick, sticks, Goo (+6 more)

### Community 7 - "Main View & Overlays Flow"
Cohesion: 0.09
Nodes (4): MainView, AnyPanel, Safe, View

### Community 9 - "Web World & Rivers"
Cohesion: 0.13
Nodes (37): last, allBridges(), bankPath(), breakBridge(), bridgeAt(), bridgeEnds(), bridgeSpan(), buildWorld() (+29 more)

### Community 10 - "Sound Effects"
Cohesion: 0.14
Nodes (3): Sfx, Ms, On

### Community 11 - "Web Drawing"
Cohesion: 0.18
Nodes (35): chestRays(), circ(), CROWPAL, drawBadger(), drawBigRat(), drawBird(), drawChest(), drawCrow() (+27 more)

### Community 13 - "Lobby Layout"
Cohesion: 0.12
Nodes (6): SkiaView, Item, Lobby, Land, Short, Svg

### Community 14 - "Game Data Tables"
Cohesion: 0.07
Nodes (23): Chest, name, sub, Cls, desc, name, D, GAME_TAG (+15 more)

### Community 15 - "Dungeon Rooms & Pickups"
Cohesion: 0.13
Nodes (8): Corr, Dungeon, Portal, Room, cx, cy, Pickup, Obs

### Community 16 - "Rivers & Bridges (C#)"
Cohesion: 0.20
Nodes (5): Fish, Unit, BEnd, Bridge, River

### Community 17 - "Save Data Model"
Cohesion: 0.07
Nodes (30): Best, score, wave, SaveData, best, btokens, coins, color (+22 more)

### Community 18 - "Web Game Data"
Cohesion: 0.07
Nodes (23): CH_MIX, CHAPTERS, CLASS_ORDER, CLASSES, COLORS, COST, CREDITS, ETYPE (+15 more)

### Community 19 - "Save Store & Settings"
Cohesion: 0.11
Nodes (17): LvRec, cards, lv, Settings, autoAim, display, keys, lang (+9 more)

### Community 20 - "Web Player State"
Cohesion: 0.12
Nodes (22): allies, allyRange(), bombs, bullets, enemies, gunForAlly(), gunOf(), heroLv() (+14 more)

### Community 21 - "Android Activity"
Cohesion: 0.09
Nodes (3): Kuriatnyk.Android, AndroidAudio, MainActivity

### Community 22 - "Board Panels"
Cohesion: 0.19
Nodes (6): BoardPanel, ChestPanel, HeroesPanel, ShopPanel, StoryPanel, WeaponsPanel

### Community 23 - "Enemy AI & Bosses"
Cohesion: 0.20
Nodes (4): ET, name, taunt, Enemy

### Community 24 - "Web Enemy Update"
Cohesion: 0.22
Nodes (18): addBomb(), anchorV, bite(), bossAI(), bossRain(), dropBombs(), menuAnchor(), predictP() (+10 more)

### Community 25 - "Projects & Build Config"
Cohesion: 0.14
Nodes (13): net10.0-android, net10.0-windows, Avalonia (11.3.22), Avalonia.Android (11.3.22), Avalonia.Desktop (11.3.22), Avalonia.Skia (11.3.22), Avalonia.Themes.Fluent (11.3.22), NAudio.Core (2.4.0) (+5 more)

### Community 26 - "Story Levels & Passives"
Cohesion: 0.12
Nodes (12): Chapter, intro, name, outro, Goal, Level, name, text (+4 more)

### Community 27 - "Node Dev Tools"
Cohesion: 0.12
Nodes (12): ctx, fs, out, path, root, src, vm, fs (+4 more)

### Community 28 - "Game UI Interface"
Cohesion: 0.12
Nodes (3): IGameUi, NullUi, OverInfo

### Community 29 - "Web Release History"
Cohesion: 0.14
Nodes (16): English localization (js/lang.js), Unique hero abilities by rarity, Rivers, rotten bridges and pathfinding over river network, Version 0.1 (first release), Version 0.2 (rivers and bridges, moles, About), Version 0.4 (river tributaries, settings, English, music), Web version history (CHANGELOG), Battle HUD (hp, goal, ult, grenade, boss bar) (+8 more)

### Community 30 - "Chest Rolls"
Cohesion: 0.21
Nodes (4): Chests, Cost, OddsInfo, Result

### Community 31 - "Layout Panels"
Cohesion: 0.18
Nodes (3): AutoGrid, OutlineText, SpaceBetweenWrap

### Community 32 - "Shop Offers"
Cohesion: 0.21
Nodes (9): Offers, Left, Today, Offer, bought, id, kind, n (+1 more)

### Community 33 - "Project Layout (README)"
Cohesion: 0.15
Nodes (12): Android AudioTrack, Kuriatnyk.Android (Android launcher), Audio module (synthesizer, SFX, music), Avalonia 11.3, .NET 10, Game module (src/Kuriatnyk/Game), NAudio (WASAPI), Render module (Ctx.cs, Draw.*.cs, Hud.cs) (+4 more)

### Community 34 - "Synth Voices"
Cohesion: 0.19
Nodes (4): Synth, Running, Time, Voice

### Community 36 - "Skia Draw Operations"
Cohesion: 0.20
Nodes (4): Op, Bounds, PicOp, Bounds

### Community 37 - "Audio Output"
Cohesion: 0.16
Nodes (4): IAudioOut, Provider, WaveFormat, WasapiAudio

### Community 39 - "Game Features History"
Cohesion: 0.18
Nodes (14): README: Battle Coop, Battle Coop (Boevoy Kuryatnik), Multiplayer (coming soon), nutze (Arthur Dyakonov), XOMJIK (Illia Martseniuk), Kamikaze enemies (fuse and explode), Version 0.3 (kamikaze, new heroes, chest tiers, weapon rarity), Weapon cards and gun upgrades to level 10 (+6% damage/level) (+6 more)

### Community 40 - "UI Kit Styling"
Cohesion: 0.18
Nodes (4): K, Press, Enabled, Sound

### Community 41 - "Localization & Statue Ults"
Cohesion: 0.19
Nodes (6): L, Culture, En, Qc, Qo, SKU

### Community 43 - "Soul Knight Features"
Cohesion: 0.21
Nodes (13): Bird Statue Shrine, Breakable Crates, Rebindable Controls, Kuriatnyk.Desktop (Windows launcher), KUR_AUTOSHOT screenshot walkthrough, Minimap, Pivchiki (run coins), Screen Scaling and Window Modes (+5 more)

### Community 45 - "Web Audio"
Cohesion: 0.22
Nodes (10): applyVolume(), initAudio(), mf(), MUS, musicDuck(), musicTick(), _noise(), SFX (+2 more)

### Community 46 - "Colors & Gradients"
Cohesion: 0.25
Nodes (4): Grad, Shader, State, Style

### Community 47 - "Soul Knight Weapons"
Cohesion: 0.29
Nodes (5): Weap, desc, meta, name, SKW

### Community 49 - "Web Save"
Cohesion: 0.22
Nodes (4): DEF, SAVE, wLv(), wMul()

### Community 50 - "Web Core Utils"
Cohesion: 0.25
Nodes (5): clamp(), ctx, cv, resize(), SET

### Community 54 - "Assets & Localization Docs"
Cohesion: 0.33
Nodes (6): Assets (fonts, Lang dictionaries, icon), tools/export-lang.js (legacy), Localization (8 languages), Rubik and Rubik Bubbles Fonts (SIL OFL 1.1), web/ (browser version), Rubik / Rubik Bubbles via Google Fonts

### Community 55 - "Core Game Features"
Cohesion: 0.33
Nodes (5): Chests, Cards and Upgrades, Shop with Daily Card Packs, Endless Battle Mode, Heroes (9 in 4 classes), Ultimate Ability

### Community 56 - "Story & Arsenal Docs"
Cohesion: 0.40
Nodes (5): River Piranhas, Soul Knight Arsenal (35 weapons), Squad (up to 3 fighters), Story Guns (17, three rarities), Story Mode

### Community 58 - "Waveforms"
Cohesion: 0.40
Nodes (5): Wave, Saw, Sine, Square, Triangle

### Community 60 - "Web Localization"
Cohesion: 0.50
Nodes (4): applyLang(), EN, I18N, i18nCollect()

### Community 63 - "App Icon"
Cohesion: 0.67
Nodes (3): App Icon (icon.png), Armed Chicken Mascot, Boevoy Kuriatnyk (Battle Chicken Coop) Game

### Community 65 - "Font Licenses"
Cohesion: 0.67
Nodes (3): Rubik font (The Rubik Project Authors, 2015), SIL Open Font License 1.1, Rubik Bubbles font (Rubik Filtered Project Authors, 2020)

## Knowledge Gaps
- **147 isolated node(s):** `localStorage progress save (js/save.js)`, `Squad of up to 3 fighters`, `Panels: story, chests, shop, heroes`, `Unique hero abilities by rarity`, `Kamikaze enemies (fuse and explode)` (+142 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 360 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **25 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `G` connect `Input, Keys & World Setup` to `Skia Canvas Rendering`, `C# Imports & Namespaces`, `Combat & Bullets (C#)`, `Panel Cards & Tabs`, `Sound Effects`, `Waves, Rooms & Spawning`, `Game Data Tables`, `Dungeon Rooms & Pickups`, `Rivers & Bridges (C#)`, `Enemy AI & Bosses`, `Story Levels & Passives`, `Game UI Interface`, `Chest Rolls`, `Synth Voices`, `Game Start & Menu Flow`, `GameView Tick & Pointer`, `Display Mode & AutoShots`, `Damage, Pickups & Interact`, `Soul Knight Weapons`, `Win & Coin Banking`, `Pause & Volume`, `Pointer Release`, `Resize Handling`?**
  _High betweenness centrality (0.199) - this node is a cross-community bridge._
- **Are the 2 inferred relationships involving `Ctx` (e.g. with `.renderDungeon()` and `.RenderGround()`) actually correct?**
  _`Ctx` has 2 INFERRED edges - model-reasoned connections that need verification._
- **What connects `localStorage progress save (js/save.js)`, `Squad of up to 3 fighters`, `Panels: story, chests, shop, heroes` to the rest of the system?**
  _147 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Skia Canvas Rendering` be split into smaller, more focused modules?**
  _Cohesion score 0.09864748819235723 - nodes in this community are weakly interconnected._
- **Why does `Ctx` connect `Skia Canvas Rendering` to `C# Imports & Namespaces`, `Input, Keys & World Setup`, `Panel Cards & Tabs`, `Lobby Layout`, `Colors & Gradients`, `Game Data Tables`, `Render Contexts`?**
  _High betweenness centrality (0.049) - this node is a cross-community bridge._
- **Are the 4 inferred relationships involving `MainView` (e.g. with `.OnFrameworkInitializationCompleted()` and `.B()`) actually correct?**
  _`MainView` has 4 INFERRED edges - model-reasoned connections that need verification._
- **Should `C# Imports & Namespaces` be split into smaller, more focused modules?**
  _Cohesion score 0.0686641697877653 - nodes in this community are weakly interconnected._