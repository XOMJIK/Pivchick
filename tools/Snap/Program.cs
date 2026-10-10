using System;
using System.IO;
using System.Linq;
using Kuriatnyk;
using Kuriatnyk.Render;
using SkiaSharp;

// Snap: прогоняет игру без окна и сохраняет кадры в PNG — чтобы сравнить с веб-версией.
// Запуск: dotnet run --project tools/Snap -- <папка для PNG>
var outDir = args.Length > 0 ? args[0] : Path.Combine(Path.GetTempPath(), "kur-snap");
Directory.CreateDirectory(outDir);
var data = Path.Combine(outDir, "data"); Directory.CreateDirectory(data);
foreach (var f in Directory.GetFiles(data)) File.Delete(f);
Environment.SetEnvironmentVariable("KUR_DATA_DIR", data);
Store.Load();
Store.SAVE.slots = 2; Store.SAVE.heroes["rooster"] = new LvRec { lv = 5 }; Store.SAVE.heroes["duck"] = new LvRec { lv = 3 }; Store.SAVE.squad = new() { "rooster", "duck" };

int W = 1000, H = 620;
G.Resize(W, H);

void Shot(string name)
{
    var info = new SKImageInfo(W, H);
    using var surf = SKSurface.Create(info);
    G.render(surf.Canvas);
    using var img = surf.Snapshot(); using var png = img.Encode(SKEncodedImageFormat.Png, 90);
    File.WriteAllBytes(Path.Combine(outDir, name + ".png"), png.ToArray());
    Console.WriteLine($"{name}: mode={G.S.mode} wave={G.S.wave} enemies={G.enemies.Count} bullets={G.bullets.Count} hp={G.P?.hp:0} score={G.S.score} kills={G.S.kills} goal={(G.S.mode == "menu" ? "" : G.goalLabel(false))}");
}
void Run(double sec) { for (double t = 0; t < sec; t += 1 / 60.0) { G.TickTimers(1 / 60.0); G.update(1 / 60.0); } }

// все ли буквы всех языков есть в шрифтах игры
if (args.Length > 1 && args[1] == "glyphs")
{
    var dir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "Kuriatnyk", "Assets");
    var chars = new System.Collections.Generic.SortedSet<char>();
    foreach (var f in Directory.GetFiles(Path.Combine(dir, "Lang"), "*.json"))
        foreach (var ch in File.ReadAllText(f)) if (ch > 127) chars.Add(ch);
    foreach (var font in Directory.GetFiles(Path.Combine(dir, "Fonts"), "*.ttf"))
    {
        using var tf = SKTypeface.FromFile(font); using var fnt = new SKFont(tf);
        var miss = chars.Where(ch => fnt.GetGlyph(ch) == 0).ToList();
        Console.WriteLine($"{Path.GetFileName(font)}: {chars.Count} chars, missing {miss.Count}: {string.Join(" ", miss.Select(ch => ch + "(U+" + ((int)ch).ToString("X4") + ")"))}");
    }
    return;
}
G.MenuAnchor = (W * .32, H * .62);
// Soul Knight: каждое оружие — тратит ли стамину, наносит ли урон, не падает ли (герой стоит, вокруг враги-манекены)
if (args.Length > 1 && args[1] == "skweapons")
{
    int bad = 0;
    foreach (var w in SKW.LIST)
    {
        G.startSK("hen"); G.skEnterLevel(1); G.enemies.Clear(); G.bullets.Clear();
        G.P.inv = 999; G.P.bag = new() { w.id }; G.P.slot = 0; G.P.w = G.P.prim = w.id; G.S.piv = 30; G.P.stam = G.P.stamMax;
        var room = G.DUN.rooms.First(r => r.type == "start"); G.DUN.active = room;
        var dummies = new System.Collections.Generic.List<Enemy>();
        for (int i = 0; i < 4; i++) { var e = G.makeEnemy("fox", G.P.x + 70 + i * 22, G.P.y - 30 + i * 20); e.room = room; e.hp = e.max = 1e6; e.emerge = 0; dummies.Add(e); }
        double stam0 = G.P.stam; long piv0 = G.S.piv; string err = null;
        try
        {
            for (int f = 0; f < 60 * 3; f++)
            {
                foreach (var e in dummies) { e.stun = 1; e.x = Math.Clamp(e.x, room.x + 30, room.x + room.w - 30); }
                G.P.ang = Math.Atan2(dummies[0].y - G.P.y, dummies[0].x - G.P.x); G.input.mouse = true; G.input.firing = true;
                G.input.mx = (dummies[0].x - G.camX) * G.SC; G.input.my = (dummies[0].y - 19 - G.camY) * G.SC;
                G.TickTimers(1 / 60.0); G.update(1 / 60.0);
            }
        }
        catch (Exception ex) { err = ex.GetType().Name + ": " + ex.Message; }
        G.input.firing = false; G.input.mouse = false;
        double dealt = dummies.Sum(e => e.max - e.hp), spent = stam0 - G.P.stam;
        bool okStam = w.stam > 0 ? spent > 0 : spent <= 0;
        bool ok = err == null && dealt > 0 && okStam;
        if (!ok) bad++;
        Console.WriteLine($"{(ok ? "ok " : "BAD")} {w.id,-10} {w.skr,-9} stam/shot={w.stam,-5} spent={spent,6:0.0} dmg={dealt,7:0} piv {piv0}->{G.S.piv}{(err != null ? " ERR " + err : "")}");
    }
    Console.WriteLine($"weapons checked: {SKW.LIST.Length}, problems: {bad}");
    return;
}
// сетевая игра: два процесса — хост (mphost [режим]) и клиент (mpclient) через 127.0.0.1
if (args.Length > 1 && args[1] is "mphost" or "mpclient")
{
    bool hostSide = args[1] == "mphost"; var sw = System.Diagnostics.Stopwatch.StartNew();
    void Pump(int ms) { var t0 = sw.ElapsedMilliseconds; while (sw.ElapsedMilliseconds - t0 < ms) { Mp.Poll(1 / 60.0); G.TickTimers(1 / 60.0); G.update(1 / 60.0); System.Threading.Thread.Sleep(15); } }
    void Wait(Func<bool> ok, int ms) { var t0 = sw.ElapsedMilliseconds; while (!ok() && sw.ElapsedMilliseconds - t0 < ms) { Mp.Poll(1 / 60.0); System.Threading.Thread.Sleep(15); } }
    G.toMenu();
    if (hostSide)
    {
        Store.SAVE.name = "Host"; Console.WriteLine("host: " + Mp.Host() + " ips=" + string.Join(",", Net.LocalIPs().Select(a => a.ip)));
        Wait(() => Mp.roster.Count >= 2, 30000); Console.WriteLine($"host: roster={Mp.roster.Count} ({string.Join(",", Mp.roster.Select(r => r.name + ":" + r.hero))})");
        var mode = args.Length > 2 ? args[2] : "endless"; Mp.SetMode(mode, 0); Mp.StartMatch("rooster");
        Console.WriteLine($"host: started {mode}, players={G.players.Count} hp scale={G.MpHp}");
        Pump(4000); Shot("60-mp-host-a");
        Pump(8000); Shot("61-mp-host-b");
        var rp = G.players.FirstOrDefault(p => p.remote);
        Console.WriteLine($"host: remote at ({rp?.x:0},{rp?.y:0}) alive={rp?.alive} hp={rp?.hp:0} ult={rp?.ult:0} kills={G.S.kills} enemies={G.enemies.Count} mode={G.S.mode}");
        if (mode == "sk") { G.skEnterLevel(1); Pump(3000); Shot("62-mp-host-sk"); Console.WriteLine($"host: sk level players at {string.Join(" ", G.players.Select(p => $"({p.x:0},{p.y:0})"))}"); }
        G.toMenu(); Pump(500); Mp.Leave(); Console.WriteLine("host: done");
    }
    else
    {
        Store.SAVE.name = "Client"; Store.SAVE.heroes["duck"] = new LvRec { lv = 4 }; Store.SAVE.hero = "duck";
        System.Threading.Thread.Sleep(800); Mp.Join("127.0.0.1");
        Wait(() => Mp.state is "lobby" or "game" || Mp.state == "idle" && Mp.status.Length > 0 && !Mp.status.Contains("…"), 15000);
        Console.WriteLine($"client: state={Mp.state} status={Mp.status} pid={Mp.myPid}");
        Wait(() => Mp.state == "game" && G.IsClientWorld && (G.enemies.Count > 0 || G.S.kind == "sk"), 30000);
        Console.WriteLine($"client: in game={G.IsClientWorld} kind={G.S.kind} players={G.players.Count} me={G.Me?.hid}");
        Pump(2000); Shot("65-mp-client-a");
        G.input.keys.Add("KeyD"); Pump(1500); G.input.keys.Clear(); G.act("ult"); Pump(4000); Shot("66-mp-client-b");
        Console.WriteLine($"client: me at ({G.Me.x:0},{G.Me.y:0}) hp={G.Me.hp:0} alive={G.Me.alive} enemies={G.enemies.Count} bullets={G.bullets.Count} parts={G.parts.Count} score={G.S.score}");
        Wait(() => Mp.state != "game", 25000); Pump(1000);
        Console.WriteLine($"client: after host left state={Mp.state} mode={G.S.mode}");
        Mp.Leave();
    }
    return;
}
// босс Хомяк-Трясун: землетрясения, камни, трещины
if (args.Length > 1 && args[1] == "hamster")
{
    G.startGame("endless"); Run(1); G.enemies.Clear();
    var h = G.makeEnemy("hamster", G.P.x + 220, G.P.y - 60); h.atkT = .3; G.S.boss = h; G.bossBarOn = true;
    double hp0 = G.P.hp; int shots = 0;
    for (int f = 0; f < 60 * 14; f++)
    {
        G.P.inv = 0; if (G.P.hp < 40) G.P.hp = G.P.max;
        G.TickTimers(1 / 60.0); G.update(1 / 60.0);
        if (G.quakes.Count > 0 && shots == 0) { Shot("50-hamster-quake"); shots++; }
        if (h.st == "dash" && shots == 1) { Shot("51-hamster-roll"); shots++; }
    }
    Shot("52-hamster-after");
    { // крупно: хомяк стоит, в прыжке и шаром; аватарки
        using var surf = SKSurface.Create(new SKImageInfo(900, 300)); var c = new Ctx(); c.Begin(surf.Canvas); c.fillStyle = "#7cc451"; c.fillRect(0, 0, 900, 300);
        var e2 = new Enemy { type = "hamster", T = D.ETYPE["hamster"], x = 120, y = 230, r = 32, s = 2.2, face = 1 }; G.drawEnemyPublic(c, e2);
        e2.x = 330; e2.st = "dash"; G.drawEnemyPublic(c, e2);
        e2.x = 520; e2.st = "walk"; e2.quakeT = .4; e2.quakeA = .8; G.drawEnemyPublic(c, e2);
        int ai = 0; foreach (var av in Avatars.LIST) { Avatars.Draw(c, av.id, 600 + (ai % 6) * 50, 20 + (ai / 6) * 52, 44); ai++; }
        using var img = surf.Snapshot(); using var png = img.Encode(SKEncodedImageFormat.Png, 90);
        File.WriteAllBytes(Path.Combine(outDir, "53-hamster-sheet.png"), png.ToArray());
    }
    Console.WriteLine($"hamster: hp {h.hp:0}/{h.max:0} quakes={G.quakes.Count} enemies={G.enemies.Count} bombs={G.bombs.Count}");
    return;
}
G.toMenu(); Run(1); Shot("01-menu");

G.startGame("endless"); Run(4); Shot("02-endless-4s");
Run(12); Shot("03-endless-16s");
G.input.keys.Add("KeyD"); Run(1.5); G.input.keys.Clear(); Shot("04-endless-moved");
Run(30); Shot("05-endless-46s");

G.startGame("story", 9); Run(6); Shot("06a-boss-level-start"); Run(72); Shot("06-boss-gturkey");
G.P.ult = 100; G.useUlt(); Run(.5); Shot("07-ult");
G.startGame("story", 6); Run(5); Shot("08-night");
G.startGame("story", 40); Run(5); Shot("09-factory");
G.startGame("story", 79); Run(7); Shot("10-emperor");

// враги под баффами и стрижи-камикадзе рядом с героем
G.startGame("endless"); Run(2);
foreach (var (b, i) in new[] { "str", "spd", "rate" }.Select((b, i) => (b, i))) { var e = G.makeEnemy(new[] { "fox", "turkey", "hen" }[i], G.P.x - 160 + i * 160, G.P.y - 120); e.buff = b; }
for (int i = 0; i < 2; i++) G.makeEnemy("kamibird", G.P.x - 260 + i * 520, G.P.y + 60);
Run(.4); Shot("13-buffs-kamibird"); Run(1.2); Shot("14-kamibird-close");
// сюжет: уровень не короче трёх минут (герой бессмертен и стреляет сам)
if (args.Length > 1 && args[1] == "story")
{
    foreach (var idx in new[] { 0, 1, 2, 8, 9, 20, 39, 61, 79 })
    {
        G.startGame("story", idx); double t = 0; bool done = false;
        while (t < 600 && G.S.mode is "play" or "won") { G.P.inv = 99; G.P.hp = G.P.max; G.TickTimers(1 / 60.0); G.update(1 / 60.0); t += 1 / 60.0; if (!done && G.S.goalDone) { done = true; Console.WriteLine($"  goal done at {t:0}s"); } }
        Console.WriteLine($"story {idx} '{D.STORY[idx].nameRu}' goal={D.STORY[idx].goal.type}: mode={G.S.mode} t={G.S.t:0}s kills={G.S.kills} wave={G.S.wave}");
    }
}
// листы со спрайтами: все птицы, все стволы, сундуки
{
    var info = new SKImageInfo(1200, 700);
    using var surf = SKSurface.Create(info); var c = new Ctx(); c.Begin(surf.Canvas);
    c.fillStyle = "#7cc451"; c.fillRect(0, 0, 1200, 700);
    int i = 0;
    foreach (var id in D.HERO_IDS)
    {
        c.save(); c.translate(70 + (i % 9) * 125, 120); c.scale(1.8, 1.8);
        G.drawBird(c, new Sprite { face = 1, ang = -.18, kind = "player", gun = Store.gunOf(id) }.With(G.lookFor(id)));
        c.restore(); i++;
    }
    i = 0;
    foreach (var id in D.PRIMARY)
    {
        c.save(); c.translate(40 + (i % 9) * 125, 230 + (i / 9) * 70); c.scale(2.1, 2.1); G.drawGun(c, id); c.restore(); i++;
    }
    for (int t = 0; t < 3; t++) { c.save(); c.translate(150 + t * 330, 660); c.scale(.9, .9); G.drawChest(c, t, 0, 0); c.restore(); }
    c.save(); c.translate(1080, 520); G.drawEggPile(c, 12); c.restore();
    c.save(); c.translate(1080, 620); G.drawSack(c, 2); c.restore();
    using var img = surf.Snapshot(); using var png = img.Encode(SKEncodedImageFormat.Png, 90);
    File.WriteAllBytes(Path.Combine(outDir, "11-sprites.png"), png.ToArray());
}
// лист оружия Soul Knight: все стволы и их снаряды
{
    var info = new SKImageInfo(1200, 760);
    using var surf = SKSurface.Create(info); var c = new Ctx(); c.Begin(surf.Canvas);
    c.fillStyle = "#7d8b63"; c.fillRect(0, 0, 1200, 760);
    int i = 0;
    foreach (var w in SKW.LIST)
    {
        double x = 30 + (i % 6) * 195, y = 60 + (i / 6) * 118;
        c.save(); c.translate(x + 30, y); c.scale(2, 2); G.drawGun(c, w.id); c.restore();
        c.font = "800 13px Rubik"; c.textAlign = "left"; c.fillStyle = "#fff"; c.fillText(w.nameRu + " (" + w.skr + ")", x, y + 40);
        if (w.shape != null) { var b = new Bullet { x = x + 150, y = y + 19, vx = 300, vy = -60, r = w.size > 0 ? w.size : 5, life = .3, max = .6, kind = w.shape, w = w, spin = 1 }; G.drawBulletPublic(c, b); }
        i++;
    }
    using var img = surf.Snapshot(); using var png = img.Encode(SKEncodedImageFormat.Png, 90);
    File.WriteAllBytes(Path.Combine(outDir, "12-sk-guns.png"), png.ToArray());
}
// Soul Knight: лагерь → портал → все комнаты 0-1 … 1-5 (герой бессмертен, идёт сам по коридорам)
if (args.Length > 1 && args[1] == "sk")
{
    double simT = 0;
    void Tick() { G.P.inv = 999; G.TickTimers(1 / 60.0); G.update(1 / 60.0); simT += 1 / 60.0; }
    bool WalkTo(double x, double y, double limit = 20)
    {
        for (double t = 0; t < limit; t += 1 / 60.0)
        {
            double dx = x - G.P.x, dy = y - G.P.y, d = Math.Sqrt(dx * dx + dy * dy);
            if (d < 12 || G.S.mode != "play") { G.sticks.move = null; return true; }
            G.sticks.move = new Stick { ox = 0, oy = 0, x = dx / d * 56, y = dy / d * 56 };
            Tick();
        }
        G.sticks.move = null; return false;
    }
    G.startSK("hen"); for (int i = 0; i < 90; i++) Tick(); Shot("20-sk-hub");
    { // палка сама отбивает летящее в героя яйцо
        G.selectSlot(1); for (int i = 0; i < 20; i++) Tick();
        var egg = new Bullet { x = G.P.x + 140, y = G.P.y, vx = -320, vy = 0, r = 5, dmg = 10, fromP = false, life = 2.4, kind = "egg" }; G.bullets.Add(egg);
        double hp0 = G.P.hp; for (int i = 0; i < 40; i++) Tick();
        Console.WriteLine($"melee vs egg: reflected={egg.fromP} kind={egg.kind} vx={egg.vx:0} hp {hp0}->{G.P.hp}");
        G.selectSlot(0);
    }
    var pt = G.DUN.portals[0]; WalkTo(pt.x, pt.y); Tick(); Tick();
    // на каждом уровне — пара стволов из списка, чтобы прогнать их механику в бою
    string[] testGuns = { "plunger", "gauss", "grinder", "sbolt", "swind", "pumpkin", "rocket", "boomer", "laser", "sice", "spoison", "sroot", "sstar", "bees", "accordion", "cactus", "axe", "sword", "coingun", "bubble" };
    int tg = 0; bool riverShot = false, riverPic = false;
    for (int lv = 1; lv <= G.SK_LEVELS && G.S.mode == "play"; lv++)
    {
        G.P.bag = new() { testGuns[tg++ % testGuns.Length], testGuns[tg++ % testGuns.Length] }; G.P.slot = 0; G.P.w = G.P.prim = G.P.bag[0]; G.S.piv += 150;
        var path = G.DUN.rooms.Where(r => r.type is "start" or "fight" or "end" or "boss").ToList();
        Console.WriteLine($"level {G.skLvName(G.S.skLv)}: rooms={G.DUN.rooms.Count} ({string.Join(",", G.DUN.rooms.Select(r => r.type))}) corridors={G.DUN.cors.Count} bag={string.Join(",", G.P.bag)}");
        if (lv == 1) Shot("21-sk-0-1-start");
        long pivStart = G.S.piv;
        { // автомат в стартовой комнате (если он на этом уровне тут, а не у торговца)
            var vend = G.OBS.FirstOrDefault(o => o.type == "vend" && G.inRoom(G.DUN.rooms.First(r => r.type == "start"), o.x, o.y, 0));
            if (vend != null) { WalkTo(vend.x, vend.y + 40, 4); Tick(); if (lv == 1) Shot("21b-sk-vend"); long pv = G.S.piv; G.interact(); for (int k = 0; k < 40; k++) Tick(); Console.WriteLine($"  vend: used={vend.open} piv {pv}->{G.S.piv} drops={string.Join(",", G.pickups.Where(p => G.hypot(p.x - vend.x, p.y - vend.y) < 120).Select(p => p.kind + (p.wid != null ? ":" + p.wid : "")))}"); if (lv == 1) Shot("21c-sk-vend-used"); pivStart = G.S.piv; }
        }
        Console.WriteLine($"  rivers={G.RIVERS.Count} fish={G.fish.Count} crates={G.OBS.Count(o => o.type == "crate")} world={G.WW}");
        int cur = 0;
        // по пути: в последней комнате не наступаем на портал в центре
        // в комнате с речкой — к центру через ближайший мост
        void ToCenter(Room Q)
        {
            foreach (var rv in G.RIVERS.Where(r => r.room == Q))
                if (G.sideR(rv, G.P.x, G.P.y) != G.sideR(rv, Q.cx, Q.cy))
                {
                    var b = rv.bridges.OrderBy(b => G.hypot(b.ends[0].x - G.P.x, b.ends[0].y - G.P.y)).First();
                    var near = b.ends.OrderBy(e => G.hypot(e.x - G.P.x, e.y - G.P.y)).First(); var far = b.ends.First(e => e != near);
                    WalkTo(near.x, near.y, 6); WalkTo(far.x, far.y, 6);
                }
            WalkTo(Q.cx, Q.cy, 8);
        }
        void Step(int i)
        {
            var R = path[i]; double tx = R.cx, ty = R.cy;
            if (path[cur].type != "end") ToCenter(path[cur]); // в последней комнате в центре портал
            if (R.type == "end") { var Q = path[cur]; double dx = Q.cx - R.cx, dy = Q.cy - R.cy, d = Math.Max(1, Math.Sqrt(dx * dx + dy * dy)); tx += dx / d * 75; ty += dy / d * 75; }
            WalkTo(tx, ty); cur = i;
        }
        void Along(int to) { while (cur != to) Step(cur + Math.Sign(to - cur)); }
        for (int i = 1; i < path.Count; i++)
        {
            var R = path[i]; Step(i);
            if (R.type is "fight" or "boss")
            {
                double t0 = simT;
                while (!R.cleared && simT - t0 < (R.type == "boss" ? 300 : 120) && G.S.mode == "play")
                {   // крутимся в комнате, автонаводка стреляет сама; кончилась стамина — меняем оружие
                    var tgt = G.enemies.FirstOrDefault(e => !e.dead);
                    if (tgt != null && D.WEAP[G.P.w].special == "melee") WalkTo(tgt.x, tgt.y, .3);
                    if (G.P.stam < 5 && G.P.bag.Count > 1 && simT % 3 < 1 / 60.0) G.swapWeapon();
                    if (G.S.t % 2 < 1 / 60.0) G.P.stam = Math.Min(G.P.stamMax, G.P.stam + 30);
                    if (G.S.t % 1.5 < 1 / 60.0) G.swapWeapon(); // оба ствола в деле
                    if (!riverPic && simT - t0 > 5 && G.RIVERS.Any(r => r.room == R)) { riverPic = true; Shot("33-sk-river"); }
                    Tick();
                }
                if (!R.cleared) foreach (var e in G.enemies) Console.WriteLine($"    stuck: {e.type} at ({e.x:0},{e.y:0}) hp={e.hp:0}/{e.max:0} st={e.st}");
                if (!R.cleared) { Console.WriteLine($"    P=({G.P.x:0},{G.P.y:0}) R=({R.x:0},{R.y:0},{R.w:0},{R.h:0}) prev=({path[i - 1].x:0},{path[i - 1].y:0}) inWater={G.inWater(G.P.x, G.P.y)}"); foreach (var rv in G.RIVERS) Console.WriteLine($"    river {rv.axis} base={rv.@base:0} lo={rv.lo:0} hi={rv.hi:0} room=({rv.room.x:0},{rv.room.y:0}) bridges={string.Join(",", rv.bridges.Select(b => b.u.ToString("0")))}"); foreach (var o in G.OBS.Where(o => G.hypot(o.x - G.P.x, o.y - G.P.y) < 80)) Console.WriteLine($"    near obs {o.type} ({o.x:0},{o.y:0}) r={o.r}"); Shot("40-stuck"); }
                Console.WriteLine($"  room {i} {R.type}: cleared={R.cleared} in {simT - t0:0.0}s kills={G.S.kills} piv={G.S.piv} goo={G.goo.Count}");
                if (i == 1) Shot($"{20 + lv}-sk-{G.skLvName(lv)}-fight-{G.P.w}");
                if (!riverShot && G.RIVERS.Any(r => r.room == R)) { riverShot = true; Console.WriteLine($"    river room: crates left={G.OBS.Count(o => o.type == "crate")}"); }
            }
        }
        // комнаты в стороне: дойти через комнату, к которой пристроена, и вернуться
        void Visit(Room X, Action inside)
        {
            var link = X.cors[0]; var from = link.a == X ? link.b : link.a;
            Along(path.IndexOf(from)); WalkTo(X.cx, X.cy + 40); inside(); WalkTo(X.cx, X.cy + 40, 4); WalkTo(from.cx, from.cy); // сначала в центр — мимо статуй и прилавка
        }
        var shopR = G.DUN.rooms.FirstOrDefault(r => r.type == "shop"); var chestR = G.DUN.rooms.FirstOrDefault(r => r.type == "chest"); var opt = G.DUN.rooms.FirstOrDefault(r => r.type == "bossopt"); var shrineR = G.DUN.rooms.FirstOrDefault(r => r.type == "shrine");
        if (shopR != null) Visit(shopR, () =>
        {
            long before = G.S.piv; int items = G.pickups.Count(p => p.price > 0);
            var it = G.pickups.FirstOrDefault(p => p.price > 0 && p.kind == "skw");
            if (it != null) { WalkTo(it.x, it.y + 6, 4); Tick(); if (lv == 1) Shot("30a-sk-compare"); Console.WriteLine($"  shop prices: {string.Join(", ", G.pickups.Where(p => p.price > 0).Select(p => (p.wid ?? p.kind) + "=" + p.price))}"); G.interact(); }
            if (lv == 1) Shot("30-sk-shop");
            var sv = G.OBS.FirstOrDefault(o => o.type == "vend" && G.inRoom(shopR, o.x, o.y, 0));
            if (sv != null) { WalkTo(sv.x, sv.y + 40, 4); Tick(); long pv = G.S.piv; G.interact(); for (int k = 0; k < 40; k++) Tick(); Console.WriteLine($"  shop vend: used={sv.open} piv {pv}->{G.S.piv} drops={string.Join(",", G.pickups.Where(p => G.hypot(p.x - sv.x, p.y - sv.y) < 120 && p.price == 0).Select(p => p.kind))}"); }
            Console.WriteLine($"  shop: items={items} piv {before}->{G.S.piv} bag={string.Join(",", G.P.bag)} on floor={G.pickups.Count(p => p.kind == "skw" && p.price == 0)}");
        });
        if (chestR != null) Visit(chestR, () =>
        {
            var o = G.OBS.First(x => x.type == "skchest"); WalkTo(o.x, o.y + 40, 4); for (int i = 0; i < 40; i++) Tick();
            if (lv == 1) Shot("31-sk-chest");
            var w = G.pickups.FirstOrDefault(p => p.kind == "skw" && p.price == 0 && G.hypot(p.x - o.x, p.y - o.y) < 80);
            Console.WriteLine($"  chest: open={o.open} weapon={w?.wid}");
            if (w != null) { WalkTo(w.x, w.y + 4, 4); Tick(); G.interact(); Console.WriteLine($"    took -> bag={string.Join(",", G.P.bag)}"); }
        });
        if (shrineR != null) Visit(shrineR, () =>
        { // святилище: поклон статуе меняет ульту (на 0-2 — проверяем «Подкрепление»)
            var sts = G.OBS.Where(o => o.type == "statue").ToList(); var s0 = sts[0]; if (lv == 2) s0.kind = SKU.SUMMON;
            WalkTo(s0.x, s0.y + 40, 4); Tick(); if (lv == 1) Shot("32a-sk-shrine");
            string was = G.P.ultId ?? G.P.hid; G.interact(); for (int k = 0; k < 30; k++) Tick();
            Console.WriteLine($"  shrine: statues={string.Join(",", sts.Select(o => o.kind + ":" + o.st))} ult {was}->{G.P.ultId ?? G.P.hid}");
            if (lv == 1) Shot("32b-sk-shrine-bowed");
            G.P.buff = null; G.P.ult = 100; G.useUlt(); for (int k = 0; k < 40; k++) Tick();
            Console.WriteLine($"    ult used: buff={G.P.buff} allies={string.Join(",", G.allies.Select(a => a.id + (a.summon ? "*" : "")))}");
            if (lv == 2) Shot("32c-sk-summon");
        });
        if (opt != null) Visit(opt, () =>
        {
            WalkTo(opt.cx, opt.cy); double t0 = simT;
            while (!opt.cleared && simT - t0 < 300) Tick();
            Console.WriteLine($"  optional boss: cleared={opt.cleared} eggs={Store.SAVE.eggs}");
        });
        Console.WriteLine($"  piv earned on level: {G.S.piv - pivStart} (before shop/extras)");
        Along(path.Count - 1);
        var portal = G.DUN.portals[0];
        Console.WriteLine($"  portal on={portal.on} passives={string.Join(",", G.S.skPass)}");
        WalkTo(portal.x, portal.y); Tick(); Tick();
    }
    Console.WriteLine($"SK finished: mode={G.S.mode} lv={G.S.skLv} coins={Store.SAVE.coins} eggs={Store.SAVE.eggs} time={simT:0}s passives={string.Join(",", G.S.skPass)} bagMax={G.P.bagMax}");
    G.toMenu(); Run(.5); Shot("29-menu-after-sk");
}
Console.WriteLine("done -> " + outDir);

// иконка приложения: курица на зелёном круге (PNG и ICO с PNG внутри)
if (args.Length > 1 && args[1] == "icon")
{
    var assets = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/Kuriatnyk/Assets"));
    foreach (var size in new[] { 256, 512 })
    {
        using var surf = SKSurface.Create(new SKImageInfo(size, size)); var c = new Ctx(); c.Begin(surf.Canvas);
        double k = size / 256.0; c.scale(k, k);
        c.fillStyle = "#2b1d14"; c.beginPath(); c.arc(128, 134, 118, 0, Ctx.TAU); c.fill();
        var g = c.createLinearGradient(0, 10, 0, 246); g.addColorStop(0, "#9ed46f"); g.addColorStop(1, "#5f9e3c");
        c.fillStyle = g; c.beginPath(); c.arc(128, 128, 114, 0, Ctx.TAU); c.fill();
        c.save(); c.translate(124, 194); c.scale(3.6, 3.6);
        G.drawBird(c, new Sprite { face = 1, ang = -.25, kind = "player", gun = "pistol" }.With(G.lookFor("hen")));
        c.restore();
        using var img = surf.Snapshot(); using var png = img.Encode(SKEncodedImageFormat.Png, 100);
        var bytes = png.ToArray();
        File.WriteAllBytes(Path.Combine(assets, size == 512 ? "icon512.png" : "icon.png"), bytes);
        if (size == 256)
        {
            using var ms = new MemoryStream(); using var bw = new BinaryWriter(ms);
            bw.Write((short)0); bw.Write((short)1); bw.Write((short)1);
            bw.Write((byte)0); bw.Write((byte)0); bw.Write((byte)0); bw.Write((byte)0); bw.Write((short)1); bw.Write((short)32); bw.Write(bytes.Length); bw.Write(22);
            bw.Write(bytes); File.WriteAllBytes(Path.Combine(assets, "icon.ico"), ms.ToArray());
        }
    }
    Console.WriteLine("icon -> " + assets);
}

// замер скорости: тяжёлая сцена, запись кадра в SKPicture (как в приложении) и обновление логики
if (args.Length > 1 && args[1] == "perf")
{
    Store.SAVE.heroes["adren"] = new LvRec { lv = 10 }; Store.SAVE.hero = "adren";
    G.startGame("story", 79); G.P.max = G.P.hp = 1e9;
    var swU = new System.Diagnostics.Stopwatch(); var swR = new System.Diagnostics.Stopwatch(); int frames = 0, maxE = 0, maxB = 0, maxP = 0;
    for (int i = 0; i < 60 * 40; i++)
    {
        G.TickTimers(1 / 60.0);
        if (G.P.ult >= 100) G.useUlt();
        swU.Start(); G.update(1 / 60.0); swU.Stop();
        maxE = Math.Max(maxE, G.enemies.Count); maxB = Math.Max(maxB, G.bullets.Count); maxP = Math.Max(maxP, G.parts.Count);
        if (i % 2 == 0)
        {
            swR.Start();
            using var rec = new SKPictureRecorder(); var cnv = rec.BeginRecording(new SKRect(0, 0, W, H)); G.render(cnv); using var pic = rec.EndRecording();
            swR.Stop(); frames++;
        }
    }
    Console.WriteLine($"update: {swU.Elapsed.TotalMilliseconds / (60 * 40):0.00} ms/frame; record: {swR.Elapsed.TotalMilliseconds / frames:0.00} ms/frame; max enemies {maxE}, bullets {maxB}, parts {maxP}; mode={G.S.mode} kills={G.S.kills}");
    // растеризация записанного кадра на CPU (худший случай без GPU)
    using (var surf = SKSurface.Create(new SKImageInfo(W, H)))
    {
        var sw = System.Diagnostics.Stopwatch.StartNew(); for (int i = 0; i < 30; i++) { G.render(surf.Canvas); surf.Canvas.Flush(); }
        Console.WriteLine($"cpu raster: {sw.Elapsed.TotalMilliseconds / 30:0.00} ms/frame at {W}x{H}");
    }
}
