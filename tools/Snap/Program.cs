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

G.MenuAnchor = (W * .32, H * .62);
G.toMenu(); Run(1); Shot("01-menu");

G.startGame("endless"); Run(4); Shot("02-endless-4s");
Run(12); Shot("03-endless-16s");
G.input.keys.Add("KeyD"); Run(1.5); G.input.keys.Clear(); Shot("04-endless-moved");
Run(30); Shot("05-endless-46s");

G.startGame("story", 9); Run(6); Shot("06-boss-gturkey");
G.P.ult = 100; G.useUlt(); Run(.5); Shot("07-ult");
G.startGame("story", 6); Run(5); Shot("08-night");
G.startGame("story", 40); Run(5); Shot("09-factory");
G.startGame("story", 79); Run(7); Shot("10-emperor");

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
// Soul Knight: лагерь → портал → все комнаты 0-1 … 0-5 (герой бессмертен, идёт сам по коридорам)
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
    var pt = G.DUN.portals[0]; WalkTo(pt.x, pt.y); Tick(); Tick();
    // на каждом уровне — пара стволов из списка, чтобы прогнать их механику в бою
    string[] testGuns = { "plunger", "gauss", "grinder", "sbolt", "swind", "pumpkin", "rocket", "boomer", "laser", "sice", "spoison", "sroot", "sstar", "bees", "accordion", "cactus", "axe", "sword", "coingun", "bubble" };
    int tg = 0; bool riverShot = false, riverPic = false;
    for (int lv = 1; lv <= 5 && G.S.mode == "play"; lv++)
    {
        G.P.bag = new() { testGuns[tg++ % testGuns.Length], testGuns[tg++ % testGuns.Length] }; G.P.slot = 0; G.P.w = G.P.prim = G.P.bag[0]; G.S.piv += 150;
        var path = G.DUN.rooms.Where(r => r.type is "start" or "fight" or "end" or "boss").ToList();
        Console.WriteLine($"level 0-{G.S.skLv}: rooms={G.DUN.rooms.Count} ({string.Join(",", G.DUN.rooms.Select(r => r.type))}) corridors={G.DUN.cors.Count} bag={string.Join(",", G.P.bag)}");
        if (lv == 1) Shot("21-sk-0-1-start");
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
                while (!R.cleared && simT - t0 < 120 && G.S.mode == "play")
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
                if (i == 1) Shot($"2{lv + 1}-sk-0-{lv}-fight-{G.P.w}");
                if (!riverShot && G.RIVERS.Any(r => r.room == R)) { riverShot = true; Console.WriteLine($"    river room: crates left={G.OBS.Count(o => o.type == "crate")}"); }
            }
        }
        // комнаты в стороне: дойти через комнату, к которой пристроена, и вернуться
        void Visit(Room X, Action inside)
        {
            var link = X.cors[0]; var from = link.a == X ? link.b : link.a;
            Along(path.IndexOf(from)); WalkTo(X.cx, X.cy + 40); inside(); WalkTo(from.cx, from.cy);
        }
        var shopR = G.DUN.rooms.FirstOrDefault(r => r.type == "shop"); var chestR = G.DUN.rooms.FirstOrDefault(r => r.type == "chest"); var opt = G.DUN.rooms.FirstOrDefault(r => r.type == "bossopt");
        if (shopR != null) Visit(shopR, () =>
        {
            long before = G.S.piv; int items = G.pickups.Count(p => p.price > 0);
            var it = G.pickups.FirstOrDefault(p => p.price > 0 && p.kind == "skw");
            if (it != null) { WalkTo(it.x, it.y + 6, 4); Tick(); G.interact(); }
            if (lv == 1) Shot("30-sk-shop");
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
        if (opt != null) Visit(opt, () =>
        {
            WalkTo(opt.cx, opt.cy); double t0 = simT;
            while (!opt.cleared && simT - t0 < 120) Tick();
            Console.WriteLine($"  optional boss: cleared={opt.cleared} eggs={Store.SAVE.eggs}");
        });
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
