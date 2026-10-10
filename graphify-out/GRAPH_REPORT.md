# Graph Report - KuriatnukPivchuka  (2026-10-10)

## Corpus Check
- 66 files · ~240,876 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 12 file(s) not represented in the graph (top: .ttf 5, .xml 4, (none) 1)

## Summary
- 1442 nodes · 5949 edges · 65 communities (42 shown, 23 thin omitted)
- Extraction: 65% EXTRACTED · 35% INFERRED · 0% AMBIGUOUS · INFERRED: 2079 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

## Community Hubs (Navigation)
- Ctx & Draw.Enemies
- SkData & World
- Overlays & MpUi
- Input & Update
- Dungeon & Combat
- Panels & Kit
- SkPlay & Combat
- Synth & Sfx
- README
- Weapons & Combat
- Save
- Sfx & Combat
- Kit & MetaUi
- MainView & Panels
- Lobby & Overlays
- Net & Mp
- Flow & MpSync
- State & Weapons
- Save & Offers
- Panels & Kit
- Update & Weapons
- World
- MainActivity
- Meta & Save
- Kuriatnyk & Kuriatnyk.Desktop
- Save & L
- GameView & Kit
- GameView & Kit
- Data
- Chests & Save
- Flow & Combat
- Net & MpSync
- L & Meta
- Meta
- Meta & Save
- Net & MpSync
- DisplayMode & AutoShots
- Input & Hud
- SkData & Data
- Mp & Net
- Save
- Ctx
- Kit & MainView
- serve-web
- Data
- Flow
- App & Program
- Meta
- State & Draw.Sprites
- Data
- Fonts
- Save
- icon
- SkData
- OFL-Rubik & OFL-RubikBubbles
- icon512
- Data
- Meta
- Save
- README
- README
- Data
- Data

## God Nodes (most connected - your core abstractions)
1. `G` - 389 edges
2. `Ctx` - 145 edges
3. `MainView` - 91 edges
4. `Overlays` - 62 edges
5. `SaveData` - 50 edges
6. `Sfx` - 44 edges
7. `Enemy` - 44 edges
8. `Press` - 35 edges
9. `Kuriatnyk` - 32 edges
10. `Part` - 31 edges

## Surprising Connections (you probably didn't know these)
- `BoardPanel` --calls--> `SpaceBetweenWrap`  [INFERRED]
  src/Kuriatnyk/UI/Panels.cs → src/Kuriatnyk/UI/Kit.cs
- `BoardPanel` --calls--> `SkiaView`  [INFERRED]
  src/Kuriatnyk/UI/Panels.cs → src/Kuriatnyk/UI/Kit.cs
- `MainActivity` --references--> `App`  [EXTRACTED]
  src/Kuriatnyk.Android/MainActivity.cs → src/Kuriatnyk/App.cs
- `AndroidAudio` --implements--> `IAudioOut`  [EXTRACTED]
  src/Kuriatnyk.Android/MainActivity.cs → src/Kuriatnyk/Audio/Synth.cs
- `G` --references--> `Dungeon`  [EXTRACTED]
  src/Kuriatnyk/Game/Combat.cs → src/Kuriatnyk/Game/Dungeon.cs

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Game Modes** — readme_story_mode, readme_endless_battle, readme_soul_knight_mode, readme_multiplayer [EXTRACTED 1.00]
- **Soul Knight Run Economy** — readme_pivchiki, readme_merchant, readme_bird_statue_shrine, readme_sk_weapons, readme_golden_eggs [EXTRACTED 1.00]
- **Tech Stack** — readme_dotnet10, readme_avalonia, readme_skiasharp, readme_synthesized_audio, readme_rubik_fonts [EXTRACTED 1.00]

## Communities (65 total, 23 thin omitted)

### Community 0 - "Ctx & Draw.Enemies"
Cohesion: 0.10
Nodes (17): Avatars, Ctx, C, fillStyle, font, globalAlpha, lineCap, lineDashOffset (+9 more)

### Community 1 - "SkData & World"
Cohesion: 0.07
Nodes (14): Kuriatnyk.UI, Kuriatnyk.Render, Kuriatnyk, Kuriatnyk.Desktop, Platform, LangJson, SaveJson, SKP (+6 more)

### Community 3 - "Input & Update"
Cohesion: 0.06
Nodes (13): G, Clock, Quake, Act, name, InputState, PartList, Fish (+5 more)

### Community 4 - "Dungeon & Combat"
Cohesion: 0.07
Nodes (7): Corr, Dungeon, Portal, Room, cx, cy, Hole

### Community 6 - "SkPlay & Combat"
Cohesion: 0.11
Nodes (3): FText, Pickup, Obs

### Community 7 - "Synth & Sfx"
Cohesion: 0.07
Nodes (17): Audio, Music, Want, Track, IAudioOut, Synth, Running, Time (+9 more)

### Community 8 - "README"
Cohesion: 0.07
Nodes (39): Account Progression (PTS, Profile), Android Launcher and APK Build, KUR_AUTOSHOT Screen Walkthrough, Avalonia 11.3, Battle Coop (Boevoy Kuryatnik), Bird Statue Shrine, Breakable Loot Crates, Chests, Cards and Upgrades (+31 more)

### Community 9 - "Weapons & Combat"
Cohesion: 0.15
Nodes (3): Fighter, Part, Zone

### Community 10 - "Save"
Cohesion: 0.05
Nodes (39): SaveData, accLv, ach, avatar, avatars, best, btokens, coins (+31 more)

### Community 11 - "Sfx & Combat"
Cohesion: 0.14
Nodes (3): Sfx, Ms, On

### Community 12 - "Kit & MetaUi"
Cohesion: 0.16
Nodes (5): AutoGrid, K, SkiaView, MetaUi, ProfilePanel

### Community 13 - "MainView & Panels"
Cohesion: 0.11
Nodes (4): MainView, AnyPanel, Safe, View

### Community 14 - "Lobby & Overlays"
Cohesion: 0.13
Nodes (5): Item, Lobby, Land, Short, Svg

### Community 15 - "Net & Mp"
Cohesion: 0.10
Nodes (10): MpPlayer, Conn, Found, Net, IsClient, IsHost, Role, Client (+2 more)

### Community 17 - "State & Weapons"
Cohesion: 0.14
Nodes (7): Ally, Bomb, Bullet, Nade, Player, RemoteIn, Unit

### Community 18 - "Save & Offers"
Cohesion: 0.11
Nodes (15): Offers, Left, Today, Offer, bought, id, kind, n (+7 more)

### Community 19 - "Panels & Kit"
Cohesion: 0.15
Nodes (12): Press, Enabled, Sound, PassPanel, QuestsPanel, ChestAnim, BoardPanel, ChestPanel (+4 more)

### Community 20 - "Update & Weapons"
Cohesion: 0.16
Nodes (5): ET, name, taunt, Enemy, GState

### Community 21 - "World"
Cohesion: 0.24
Nodes (3): BEnd, Bridge, River

### Community 22 - "MainActivity"
Cohesion: 0.09
Nodes (3): Kuriatnyk.Android, AndroidAudio, MainActivity

### Community 23 - "Meta & Save"
Cohesion: 0.15
Nodes (12): Quests, DailyReward, Day, DayLeft, Week, WeekLeft, WeeklyReward, QuestRec (+4 more)

### Community 24 - "Kuriatnyk & Kuriatnyk.Desktop"
Cohesion: 0.12
Nodes (19): net10.0-android, net10.0-windows, Avalonia (11.3.22), Avalonia.Android (11.3.22), Avalonia.Desktop (11.3.22), Avalonia.Skia (11.3.22), Avalonia.Themes.Fluent (11.3.22), NAudio.Core (2.4.0) (+11 more)

### Community 25 - "Save & L"
Cohesion: 0.18
Nodes (5): ProfileInfo, Store, Dir, SavePath, SetPath

### Community 26 - "GameView & Kit"
Cohesion: 0.16
Nodes (4): Op, Bounds, PicOp, Bounds

### Community 28 - "Data"
Cohesion: 0.14
Nodes (10): Chest, name, sub, D, GAME_TAG, GAME_VERSION, HC, Pack (+2 more)

### Community 29 - "Chests & Save"
Cohesion: 0.20
Nodes (7): Chests, Cost, OddsInfo, Result, LvRec, cards, lv

### Community 30 - "Flow & Combat"
Cohesion: 0.24
Nodes (3): OverInfo, WinInfo, WinRow

### Community 32 - "L & Meta"
Cohesion: 0.17
Nodes (6): L, Culture, En, Qc, Qo, Reward

### Community 33 - "Meta"
Cohesion: 0.22
Nodes (5): Ach, Def, desc, name, Stats

### Community 34 - "Meta & Save"
Cohesion: 0.22
Nodes (5): Pass, Left, Season, SeasonNo, Tier

### Community 38 - "SkData & Data"
Cohesion: 0.29
Nodes (5): Weap, desc, meta, name, SKW

### Community 40 - "Save"
Cohesion: 0.17
Nodes (12): Settings, autoAim, display, keys, lang, lastIp, lastWin, music (+4 more)

### Community 41 - "Ctx"
Cohesion: 0.25
Nodes (4): Grad, Shader, State, Style

### Community 43 - "serve-web"
Cohesion: 0.22
Nodes (5): fs, http, path, root, types

### Community 44 - "Data"
Cohesion: 0.22
Nodes (8): Chapter, intro, name, outro, Goal, Level, name, text

### Community 50 - "Data"
Cohesion: 0.33
Nodes (6): Hero, name, p10, p5, perk, ult

### Community 52 - "Save"
Cohesion: 0.40
Nodes (5): QuestState, daily, day, week, weekly

### Community 53 - "icon"
Cohesion: 0.67
Nodes (3): App Icon (icon.png), Armed Chicken Mascot, Boevoy Kuriatnyk (Battle Chicken Coop) Game

### Community 55 - "OFL-Rubik & OFL-RubikBubbles"
Cohesion: 0.67
Nodes (3): Rubik font (The Rubik Project Authors, 2015), SIL Open Font License 1.1, Rubik Bubbles font (Rubik Filtered Project Authors, 2020)

### Community 57 - "Data"
Cohesion: 0.67
Nodes (3): Cls, desc, name

### Community 58 - "Meta"
Cohesion: 0.67
Nodes (3): Av, how, name

### Community 59 - "Save"
Cohesion: 0.67
Nodes (3): Best, score, wave

## Knowledge Gaps
- **199 isolated node(s):** `net10.0-android`, `Avalonia.Android (11.3.22)`, `Microsoft.NET.Sdk`, `net10.0-windows`, `Avalonia.Desktop (11.3.22)` (+194 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 315 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **23 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `G` connect `Input & Update` to `Ctx & Draw.Enemies`, `SkData & World`, `Overlays & MpUi`, `Dungeon & Combat`, `Panels & Kit`, `SkPlay & Combat`, `Synth & Sfx`, `Weapons & Combat`, `Sfx & Combat`, `Flow & MpSync`, `State & Weapons`, `Save & Offers`, `Update & Weapons`, `World`, `Meta & Save`, `GameView & Kit`, `Data`, `Flow & Combat`, `Net & MpSync`, `Net & MpSync`, `DisplayMode & AutoShots`, `Input & Hud`, `SkData & Data`, `State & Draw.Sprites`?**
  _High betweenness centrality (0.403) - this node is a cross-community bridge._
- **Are the 2 inferred relationships involving `Ctx` (e.g. with `.renderDungeon()` and `.RenderGround()`) actually correct?**
  _`Ctx` has 2 INFERRED edges - model-reasoned connections that need verification._
- **What connects `net10.0-android`, `Avalonia.Android (11.3.22)`, `Microsoft.NET.Sdk` to the rest of the system?**
  _199 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Ctx & Draw.Enemies` be split into smaller, more focused modules?**
  _Cohesion score 0.10281879194630872 - nodes in this community are weakly interconnected._
- **Why does `Ctx` connect `Ctx & Draw.Enemies` to `SkData & World`, `Input & Update`, `Panels & Kit`, `Ctx`, `Kit & MetaUi`, `Kit & Ctx`, `State & Weapons`, `Update & Weapons`, `Data`?**
  _High betweenness centrality (0.081) - this node is a cross-community bridge._
- **Are the 4 inferred relationships involving `MainView` (e.g. with `.OnFrameworkInitializationCompleted()` and `.B()`) actually correct?**
  _`MainView` has 4 INFERRED edges - model-reasoned connections that need verification._
- **Should `SkData & World` be split into smaller, more focused modules?**
  _Cohesion score 0.07171052631578947 - nodes in this community are weakly interconnected._