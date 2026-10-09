using System;
using System.IO;
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
    Console.WriteLine($"{name}: mode={G.S.mode} wave={G.S.wave} enemies={G.enemies.Count} bullets={G.bullets.Count} hp={G.P?.hp:0} score={G.S.score} kills={G.S.kills}");
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
