using System;
using System.Collections.Generic;
using System.Linq;
using Kuriatnyk.Render;
using SkiaSharp;
using static Kuriatnyk.D;
using static Kuriatnyk.Store;

namespace Kuriatnyk;

// Карты: земля, забор, препятствия, реки с притоками и мостами, поиск пути (порт web/js/world.js).

public sealed class RiverDef { public char axis; public double @base, amp, freq, phase, hw; public string end; public (double u, int rot)[] bridges; }
public sealed class MapDef
{
    public string nameRu, grass, patch, @out, bank, water, deep; public string[] tuft, obs; public int flowers; public long seed;
    public RiverDef river, branch; public string name => L.T(nameRu);
}
public sealed class Obs { public double x, y, r, hp, hit; public string type; public bool open; } // open — сундук Soul Knight открыт; hp — у ящика, который разбивается
public sealed class Bridge
{
    public double u, hw = 32, wob; public bool rotten, broken; public int passes; public River R; public BEnd[] ends;
}
public sealed class BEnd { public double x, y; public int sd; public string reg; }
public sealed class River
{
    public char axis; public double @base, amp, freq, phase, hw, lo, hi; public string end; public List<Bridge> bridges; public River parent; public int pside;
    public Room room; // речка в комнате подземелья: берега считаются только внутри неё
}

public static partial class G
{
    // размер мира: карты сюжета 1500×1500, подземелье Soul Knight больше (комнаты просторнее)
    public static double WW = 1500, WH = 1500;
    public const double FENCE = 46, MAP_SIZE = 1500;
    public static List<Obs> OBS = new();
    public static SKImage ground;
    public static River RIVER;
    public static string MAP = "yard";

    public static readonly Dictionary<string, MapDef> MAPS = new()
    {
        ["yard"] = new() { nameRu = "Двор", grass = "#7cc451", tuft = new[] { "#5fa63b", "#9ad866" }, patch = "rgba(200,160,95,.55)", flowers = 140, @out = "#5f9e3c", seed = 20261009,
            obs = new[] { "hay", "stump", "barrel", "bush", "rock", "hay", "stump", "barrel" }, bank = "#d8c38a", water = "#4aa8d8", deep = "#3a8fc2",
            river = new() { axis = 'h', @base = 420, amp = 55, freq = .0055, phase = .4, hw = 40, bridges = new[] { (230.0, 0), (600, 1), (960, 0), (1290, 1) } },
            branch = new() { axis = 'v', @base = 1120, amp = 40, freq = .006, phase = .7, hw = 32, end = "hi", bridges = new[] { (780.0, 0), (1180, 1) } } },
        ["farm"] = new() { nameRu = "Ферма", grass = "#82c653", tuft = new[] { "#63aa3c", "#a2dc6c" }, patch = "rgba(200,160,95,.6)", flowers = 180, @out = "#62a23e", seed = 777101,
            obs = new[] { "hay", "hay", "stump", "barrel", "bush", "rock", "hay", "stump" }, bank = "#dcc58c", water = "#4aa8d8", deep = "#3a8fc2",
            river = new() { axis = 'v', @base = 1110, amp = 70, freq = .005, phase = 1.2, hw = 42, bridges = new[] { (260.0, 1), (620, 0), (980, 1), (1300, 0) } },
            branch = new() { axis = 'h', @base = 440, amp = 35, freq = .006, phase = 2.4, hw = 32, end = "lo", bridges = new[] { (320.0, 1), (760, 0) } } },
        ["forest"] = new() { nameRu = "Тёмный лес", grass = "#5e9e3e", tuft = new[] { "#4a8a30", "#78b850" }, patch = "rgba(90,70,40,.45)", flowers = 60, @out = "#3f7a2a", seed = 555202,
            obs = new[] { "bush", "stump", "rock", "bush", "stump", "bush", "stump" }, bank = "#a89060", water = "#3d8fb8", deep = "#2f7aa0",
            river = new() { axis = 'h', @base = 1120, amp = 90, freq = .006, phase = 2.1, hw = 44, bridges = new[] { (200.0, 0), (520, 1), (860, 1), (1180, 0), (1380, 1) } },
            branch = new() { axis = 'v', @base = 360, amp = 35, freq = .007, phase = 1.1, hw = 32, end = "lo", bridges = new[] { (330.0, 0), (760, 1) } } },
        ["factory"] = new() { nameRu = "Птицефабрика", grass = "#b9b5aa", tuft = new[] { "#a29e93", "#cfcbc0" }, patch = "rgba(50,50,50,.22)", flowers = 0, @out = "#8a867b", seed = 333303,
            obs = new[] { "barrel", "barrel", "rock", "barrel", "hay", "barrel" }, bank = "#8f8b80", water = "#5f8f8a", deep = "#4d7a74",
            river = new() { axis = 'v', @base = 400, amp = 14, freq = .004, phase = .2, hw = 38, bridges = new[] { (300.0, 0), (700, 1), (1100, 0), (1350, 1) } },
            branch = new() { axis = 'h', @base = 1225, amp = 14, freq = .004, phase = .5, hw = 30, end = "hi", bridges = new[] { (760.0, 1), (1200, 0) } } },
        ["mountain"] = new() { nameRu = "Горы", grass = "#cfe0c2", tuft = new[] { "#b2c8a6", "#eef6ea" }, patch = "rgba(255,255,255,.65)", flowers = 40, @out = "#a7bd9f", seed = 111404,
            obs = new[] { "rock", "stump", "bush", "rock", "rock", "stump" }, bank = "#b9b2a2", water = "#7cc8ea", deep = "#5fb2dc",
            river = new() { axis = 'h', @base = 1010, amp = 110, freq = .0045, phase = 3.3, hw = 36, bridges = new[] { (240.0, 1), (560, 0), (900, 1), (1230, 0) } },
            branch = new() { axis = 'v', @base = 1065, amp = 30, freq = .006, phase = 2.9, hw = 30, end = "lo", bridges = new[] { (300.0, 1), (700, 0) } } },
    };
    public static readonly string[] MAP_OF_CH = { "farm", "forest", "factory", "mountain" };
    public static string menuMap() => MAPS.ContainsKey(SAVE.menuBg) ? SAVE.menuBg : "yard"; // фон главного меню выбирает игрок

    public static Func<double> seeded(long s) => () => { s = (s * 16807) % 2147483647; return (s - 1) / 2147483646.0; };
    static double rand2(Func<double> R, double a, double b) => a + R() * (b - a);

    /* --- реки: основное русло RIVERS[0] и приток RIVERS[1]; вода, берега, мосты --- */
    public static List<River> RIVERS = new();
    public static double rcR(River R, double u) => R.@base + R.amp * Math.Sin(u * R.freq + R.phase) + R.amp * .35 * Math.Sin(u * R.freq * 2.3 + 1.7);
    public static (double u, double v) uvR(River R, double x, double y) => R.axis == 'h' ? (x, y) : (y, x);
    public static (double x, double y) xyR(River R, double u, double v) => R.axis == 'h' ? (u, v) : (v, u);
    public static int sideR(River R, double x, double y) { var (u, v) = uvR(R, x, y); return v < rcR(R, u) ? -1 : 1; }
    static bool inDomain(River R, double x, double y) => (R.room == null || (x >= R.room.x && x <= R.room.x + R.room.w && y >= R.room.y && y <= R.room.y + R.room.h)) && (R.parent == null || sideR(R.parent, x, y) == R.pside);
    public static Bridge bridgeAt(double x, double y)
    {
        foreach (var R in RIVERS)
        {
            var (u, v) = uvR(R, x, y); if (u < R.lo || u > R.hi) continue;
            foreach (var b in R.bridges) { if (b.broken) continue; if (Math.Abs(u - b.u) < b.hw && Math.Abs(v - rcR(R, u)) < R.hw + 14) return b; }
        }
        return null;
    }
    static bool inWaterR(River R, double x, double y)
    {
        var (u, v) = uvR(R, x, y); if (u < R.lo || u > R.hi) return false;
        if (Math.Abs(v - rcR(R, u)) >= R.hw - 3) return false;
        foreach (var b in R.bridges) if (!b.broken && Math.Abs(u - b.u) < b.hw - 2) return false;
        return true;
    }
    public static bool inWater(double x, double y) { foreach (var R in RIVERS) if (inWaterR(R, x, y)) return true; return false; }
    static bool nearWaterR(River R, double x, double y, double m) { var (u, v) = uvR(R, x, y); if (u < R.lo - m || u > R.hi + m) return false; return Math.Abs(v - rcR(R, u)) < R.hw + m; }
    public static bool nearWater(double x, double y, double m) { foreach (var R in RIVERS) if (nearWaterR(R, x, y, m)) return true; return false; }
    // берег, на котором стоит точка: стороны всех рек, в зоне которых она лежит
    public static string regionOf(double x, double y)
    {
        var k = "";
        foreach (var R in RIVERS) k += (inDomain(R, x, y) ? sideR(R, x, y) : 0) + ",";
        return k;
    }
    static void pushOut(Unit e, double pad) // вытолкнуть из воды на ближний берег
    {
        for (int k = 0; k < 3; k++)
        {
            bool moved = false;
            foreach (var R in RIVERS)
                if (inWaterR(R, e.x, e.y)) { var (u, v) = uvR(R, e.x, e.y); var c = rcR(R, u); var p = xyR(R, u, c + (v < c ? -1 : 1) * (R.hw + pad)); e.x = p.x; e.y = p.y; moved = true; }
            if (!moved) return;
        }
    }
    public static void waterBlock(Unit e, double ox, double oy) // ходить по воде нельзя: откатываем шаг, а если занесло в воду — на ближний берег
    {
        if (!inWater(e.x, e.y)) return;
        foreach (var R in RIVERS)
            if (inWaterR(R, e.x, e.y))
            { // скользим вдоль берега: проекция шага на касательную к руслу
                var (uo, vo) = uvR(R, ox, oy); var (du, dv) = uvR(R, e.x - ox, e.y - oy);
                double sl = (rcR(R, uo + 2) - rcR(R, uo - 2)) / 4, tl = hypot(1, sl), tu = 1 / tl, tv = sl / tl, dot = du * tu + dv * tv;
                var p = xyR(R, uo + dot * tu, vo + dot * tv);
                if (!inWater(p.x, p.y)) { e.x = p.x; e.y = p.y; return; }
                break;
            }
        if (!inWater(e.x, oy)) e.y = oy; else if (!inWater(ox, e.y)) e.x = ox; else { e.x = ox; e.y = oy; }
        if (inWater(e.x, e.y)) pushOut(e, 3);
    }
    public static (double x, double y) dryPoint(double x, double y, double m = 50)
    {
        double px = x, py = y;
        for (int k = 0; k < 4; k++)
        {
            bool moved = false;
            foreach (var R in RIVERS)
                if (nearWaterR(R, px, py, m))
                {
                    var (u, v) = uvR(R, px, py); var c = rcR(R, u); var q = xyR(R, u, c + (v < c ? -1 : 1) * (R.hw + m + 20));
                    px = clamp(q.x, FENCE + 30, WW - FENCE - 30); py = clamp(q.y, FENCE + 30, WH - FENCE - 30); moved = true;
                }
            if (!moved) break;
        }
        return (px, py);
    }
    static IEnumerable<Bridge> allBridges() { foreach (var R in RIVERS) foreach (var b in R.bridges) yield return b; }
    static BEnd[] bridgeEnds(Bridge b)
    {
        var R = b.R; var c = rcR(R, b.u);
        return new[] { -1, 1 }.Select(sd => { var p = xyR(R, b.u, c + sd * (R.hw + 34)); return new BEnd { x = p.x, y = p.y, sd = sd, reg = regionOf(p.x, p.y) }; }).ToArray();
    }
    static (Dictionary<string, int> dist, Dictionary<string, string> prev) regionGraph(string from) // поиск в ширину по целым мостам
    {
        var dist = new Dictionary<string, int> { [from] = 0 }; var prev = new Dictionary<string, string>(); var q = new Queue<string>(); q.Enqueue(from);
        while (q.Count > 0)
        {
            var r = q.Dequeue();
            foreach (var b in allBridges())
            {
                if (b.broken) continue; var A = b.ends[0]; var B = b.ends[1];
                foreach (var (x, y) in new[] { (A, B), (B, A) })
                    if (x.reg == r && !dist.ContainsKey(y.reg)) { dist[y.reg] = dist[r] + 1; prev[y.reg] = r; q.Enqueue(y.reg); }
            }
        }
        return (dist, prev);
    }
    public static (double x, double y)? navTarget(Unit e, double tx, double ty, int alt = 0) // куда идти, чтобы попасть на нужный берег через целые мосты
    {
        if (RIVERS.Count == 0) return null;
        var rt = regionOf(tx, ty); var onB = bridgeAt(e.x, e.y);
        if (onB != null && Math.Abs(uvR(onB.R, e.x, e.y).v - rcR(onB.R, onB.u)) < onB.R.hw)
        { // на настиле над водой — доходим до нужного конца моста
            var R = onB.R; var A = onB.ends[0]; var B = onB.ends[1];
            int dA = regionGraph(A.reg).dist.TryGetValue(rt, out var a1) ? a1 : 99, dB = regionGraph(B.reg).dist.TryGetValue(rt, out var b1) ? b1 : 99;
            var pick = dA < dB ? A : dB < dA ? B : (sideR(R, tx, ty) < 0 ? A : B);
            return (pick.x, pick.y);
        }
        var rs = regionOf(e.x, e.y); if (rs == rt) return bankPath(e, tx, ty);
        var Gr = regionGraph(rs); if (!Gr.dist.ContainsKey(rt)) return null;
        var nx = rt; while (Gr.prev[nx] != rs) nx = Gr.prev[nx];
        var ok = allBridges().Where(b => !b.broken && b.ends.Any(p => p.reg == rs) && b.ends.Any(p => p.reg == nx))
            .Select(b => (b, near: b.ends.First(p => p.reg == rs), far: b.ends.First(p => p.reg == nx)))
            .OrderBy(a => hypot(a.near.x - e.x, a.near.y - e.y) + hypot(a.far.x - tx, a.far.y - ty) * .5).ToList();
        if (ok.Count == 0) return null;
        var best = ok[alt % ok.Count]; var Rb = best.b.R; var (ue, ve) = uvR(Rb, e.x, e.y); var nv = uvR(Rb, best.near.x, best.near.y).v;
        if (Math.Abs(ue - best.b.u) < best.b.hw * .55 && Math.Abs(ve - nv) < 45) return (best.far.x, best.far.y);
        return (best.near.x, best.near.y);
    }
    static (double x, double y)? bankPath(Unit e, double tx, double ty) // тот же берег, но прямую перегораживает изгиб реки — идём вдоль берега
    {
        double dx = tx - e.x, dy = ty - e.y, d = hypot(dx, dy); if (d < 30) return null;
        int n = (int)Math.Min(12, Math.Ceiling(d / 40));
        for (int i = 1; i <= n; i++)
        {
            double x = e.x + dx * i / n, y = e.y + dy * i / n;
            foreach (var R in RIVERS)
                if (inWaterR(R, x, y))
                {
                    var (ue, ve) = uvR(R, e.x, e.y); var ut = uvR(R, tx, ty).u; int sd = ve < rcR(R, ue) ? -1 : 1;
                    double u = ue + Math.Sign(ut - ue) * Math.Min(70, Math.Abs(ut - ue)); var p = xyR(R, u, rcR(R, u) + sd * (R.hw + 40));
                    return (p.x, p.y);
                }
        }
        return null;
    }
    public static (double x, double y)? sidePoint(string reg) // сухая точка у края карты на нужном берегу, подальше от игрока
    {
        for (int i = 0; i < 60; i++) { var p = edgePoint(16); if (regionOf(p.x, p.y) == reg && !nearWater(p.x, p.y, 30)) return p; }
        return null;
    }
    static (double lo, double hi) bridgeSpan(Bridge b)
    {
        var R = b.R; double lo = 1e9, hi = -1e9;
        for (double u = b.u - b.hw; u <= b.u + b.hw; u += 4) { var c = rcR(R, u); lo = Math.Min(lo, c); hi = Math.Max(hi, c); }
        return (lo - R.hw - 12, hi + R.hw + 12);
    }
    static void makeRivers(MapDef M)
    {
        double Len(char a) => a == 'h' ? WW : WH;
        River mk(RiverDef d)
        {
            var R = new River { axis = d.axis, @base = d.@base, amp = d.amp, freq = d.freq, phase = d.phase, hw = d.hw, end = d.end };
            R.bridges = d.bridges.Select(t => new Bridge { u = t.u, hw = 32, rotten = t.rot != 0, R = R }).ToList();
            return R;
        }
        var main = mk(M.river); main.lo = -40; main.hi = Len(main.axis) + 40; RIVERS = new() { main }; RIVER = main;
        if (M.branch != null)
        {
            var br = mk(M.branch); double n = Len(br.axis);
            int f(double u) { var p = xyR(br, u, rcR(br, u)); return sideR(main, p.x, p.y); }
            double a = 0, b = n; int fa = f(a);
            for (int i = 0; i < 40; i++) { var m = (a + b) / 2; if (f(m) == fa) a = m; else b = m; }
            var uj = (a + b) / 2; br.parent = main;
            if (br.end == "hi") { br.lo = uj; br.hi = n + 40; } else { br.lo = -40; br.hi = uj; }
            double tip = br.end == "hi" ? n : 0; var tp = xyR(br, tip, rcR(br, tip)); br.pside = sideR(main, tp.x, tp.y);
            RIVERS.Add(br);
        }
        foreach (var b in allBridges()) b.ends = bridgeEnds(b);
    }

    public static void buildWorld(string key = "yard")
    {
        WW = WH = MAP_SIZE; MAP = MAPS.ContainsKey(key) ? key : "yard"; var M = MAPS[MAP];
        makeRivers(M);
        var R = seeded(M.seed); OBS = new();
        var kinds = M.obs; double sx = WW / 2, sy = WH / 2 + 40;
        int tries = 0;
        while (OBS.Count < 18 && tries < 1400)
        {
            tries++;
            var k = kinds[OBS.Count % kinds.Length];
            double r = k == "hay" ? rand2(R, 32, 40) : k == "barrel" ? rand2(R, 20, 24) : k == "bush" ? rand2(R, 26, 34) : k == "stump" ? rand2(R, 19, 26) : rand2(R, 22, 30);
            double x = FENCE + 90 + R() * (WW - 2 * FENCE - 180), y = FENCE + 90 + R() * (WH - 2 * FENCE - 180);
            if (hypot(x - sx, y - sy) < 190) continue;
            if (nearWater(x, y, r + 34)) continue;
            if (OBS.Any(o => hypot(o.x - x, o.y - y) < o.r + r + 80)) continue;
            OBS.Add(new Obs { x = x, y = y, r = r, type = k });
        }
        ground?.Dispose();
        ground = RenderGround(M);
        spawnFish();
    }

    /// <summary>Русла всех рек на картинке земли: сначала берега, потом вода, потом вода ещё раз без обводки — так устье притока сливается с руслом.</summary>
    static void paintRivers(Ctx g, string bank, string water, string deep)
    {
        void band(River Rv, double off)
        {
            double u0 = Math.Max(Rv.lo, -20), u1 = Math.Min(Rv.hi, (Rv.axis == 'h' ? WW : WH) + 20); g.beginPath();
            for (double u = u0; u <= u1; u += 8) { var p = xyR(Rv, u, rcR(Rv, u) - off); if (u == u0) g.moveTo(p.x, p.y); else g.lineTo(p.x, p.y); }
            { var p = xyR(Rv, u1, rcR(Rv, u1) - off); g.lineTo(p.x, p.y); }
            for (double u = u1; u >= u0; u -= 8) { var p = xyR(Rv, u, rcR(Rv, u) + off); g.lineTo(p.x, p.y); }
            { var p = xyR(Rv, u0, rcR(Rv, u0) + off); g.lineTo(p.x, p.y); }
            g.closePath();
        }
        foreach (var Rv in RIVERS) { band(Rv, Rv.hw + 11); g.fillStyle = bank; g.fill(); g.strokeStyle = "rgba(43,29,20,.35)"; g.lineWidth = 2; g.stroke(); }
        foreach (var Rv in RIVERS) { band(Rv, Rv.hw); g.fillStyle = water; g.fill(); g.strokeStyle = INK; g.lineWidth = 5; g.stroke(); }
        foreach (var Rv in RIVERS) { band(Rv, Rv.hw); g.fillStyle = water; g.fill(); }
        foreach (var Rv in RIVERS) { band(Rv, Rv.hw * .45); g.fillStyle = deep; g.fill(); }
    }
    static SKImage RenderGround(MapDef M)
    {
        // на ПК земля рисуется в двойном разрешении: на большом мониторе поле увеличено и в 1× травинки расплываются
        double res = COARSE ? 1 : 2;
        var info = new SKImageInfo((int)(WW * res), (int)(WH * res), SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surf = SKSurface.Create(info);
        var g = new Ctx(); g.Begin(surf.Canvas); g.scale(res, res);
        g.fillStyle = M.grass; g.fillRect(0, 0, WW, WH);
        var Gr = seeded(77 + M.seed % 97);
        for (int i = 0; i < 26; i++) { g.fillStyle = Gr() < .5 ? "rgba(255,255,200,.07)" : "rgba(30,80,10,.07)"; g.beginPath(); g.ellipse(Gr() * WW, Gr() * WH, 60 + Gr() * 160, 40 + Gr() * 110, Gr() * 3, 0, TAU); g.fill(); }
        for (int i = 0; i < 9; i++)
        {
            double x = FENCE + Gr() * (WW - 2 * FENCE), y = FENCE + Gr() * (WH - 2 * FENCE);
            g.fillStyle = M.patch; g.beginPath(); g.ellipse(x, y, 40 + Gr() * 60, 24 + Gr() * 34, Gr() * 3, 0, TAU); g.fill();
            g.fillStyle = "rgba(120,90,50,.2)"; for (int j = 0; j < 6; j++) { g.beginPath(); g.arc(x + (Gr() - .5) * 60, y + (Gr() - .5) * 30, 3 + Gr() * 4, 0, TAU); g.fill(); }
        }
        g.lineCap = "round";
        for (int i = 0; i < 2600; i++)
        {
            double x = Gr() * WW, y = Gr() * WH; g.strokeStyle = Gr() < .5 ? M.tuft[0] : M.tuft[1]; g.lineWidth = 1.6;
            g.beginPath(); g.moveTo(x, y); g.lineTo(x - 2 + Gr() * 1, y - 5 - Gr() * 3); g.moveTo(x + 2, y); g.lineTo(x + 3 + Gr() * 2, y - 4 - Gr() * 3); g.stroke();
        }
        for (int i = 0; i < M.flowers; i++)
        {
            double x = Gr() * WW, y = Gr() * WH; g.fillStyle = Gr() < .6 ? "#ffffff" : "#ffe066";
            for (int p = 0; p < 5; p++) { g.beginPath(); g.arc(x + Math.Cos(p * 1.256) * 2.6, y + Math.Sin(p * 1.256) * 2.6, 1.8, 0, TAU); g.fill(); }
            g.fillStyle = "#f2a91f"; g.beginPath(); g.arc(x, y, 1.4, 0, TAU); g.fill();
        }
        paintRivers(g, M.bank, M.water, M.deep);
        g.strokeStyle = "rgba(255,255,255,.45)"; g.lineWidth = 2;
        foreach (var Rv in RIVERS)
        {
            double u0 = Math.Max(Rv.lo, 0), span = Math.Min(Rv.hi, Rv.axis == 'h' ? WW : WH) - u0;
            for (int i = 0; i < span / 25; i++)
            {
                double u = u0 + Gr() * span, off = (Gr() - .5) * Rv.hw * 1.4; var a = xyR(Rv, u, rcR(Rv, u) + off); var b = xyR(Rv, u + 14, rcR(Rv, u + 14) + off);
                if (inWater(a.x, a.y) && inWater(b.x, b.y)) { g.beginPath(); g.moveTo(a.x, a.y); g.lineTo(b.x, b.y); g.stroke(); }
            }
            for (int i = 0; i < span / 38; i++)
            {
                double u = u0 + Gr() * span; int sd = Gr() < .5 ? -1 : 1; var p = xyR(Rv, u, rcR(Rv, u) + sd * (Rv.hw + 6 + Gr() * 4));
                if (nearWater(p.x, p.y, -4)) continue; g.fillStyle = "rgba(120,100,70,.5)"; g.beginPath(); g.arc(p.x, p.y, 2 + Gr() * 2.5, 0, TAU); g.fill();
            }
        }
        // забор
        void post(double x, double y)
        {
            g.fillStyle = "rgba(0,0,0,.18)"; g.fillRect(x - 4, y - 2, 12, 6); g.fillStyle = "#9b6537"; g.strokeStyle = INK; g.lineWidth = 2;
            g.beginPath(); g.rect(x - 5, y - 22, 10, 24); g.fill(); g.stroke(); g.fillStyle = "#b97c47"; g.fillRect(x - 3, y - 20, 3, 20);
        }
        void railH(double y)
        {
            g.fillStyle = "#b97c47"; g.strokeStyle = INK; g.lineWidth = 2;
            foreach (var o in new[] { -16, -8 }) { g.beginPath(); g.rect(FENCE, y + o, WW - 2 * FENCE, 5); g.fill(); g.stroke(); }
        }
        railH(FENCE); railH(WH - FENCE);
        g.fillStyle = "#b97c47"; g.strokeStyle = INK; g.lineWidth = 2;
        foreach (var x in new[] { FENCE, WW - FENCE }) { g.beginPath(); g.rect(x - 3, FENCE - 16, 6, WH - 2 * FENCE + 16); g.fill(); g.stroke(); }
        for (double x = FENCE; x <= WW - FENCE; x += 48) { post(x, FENCE); post(x, WH - FENCE); }
        for (double y = FENCE + 48; y < WH - FENCE; y += 48) { post(FENCE, y); post(WW - FENCE, y); }
        surf.Canvas.Flush();
        return surf.Snapshot();
    }

    /* --- мосты: скрип и обрушение гнилых --- */
    static Bridge lastBridge;
    public static void updateBridges(double dt)
    {
        if (RIVER == null) return;
        foreach (var br in allBridges()) if (br.wob > 0) br.wob -= dt;
        var b = P.alive ? bridgeAt(P.x, P.y) : null;
        if (b == lastBridge) return;
        if (lastBridge != null && lastBridge.rotten && !lastBridge.broken) { lastBridge.passes++; if (lastBridge.passes >= 2) breakBridge(lastBridge); }
        if (b != null && b.rotten && !b.broken) { b.wob = .6; Sfx.creak(); S.shake += 2; ftext(P.x, P.y - 52, b.passes == 0 ? T("Скрип… мост гнилой") : T("Трещит! Сейчас рухнет"), "#e8d3a8", 12); }
        lastBridge = b;
    }
    static void breakBridge(Bridge b)
    {
        b.broken = true; Sfx.crack(); S.shake += 9; Platform.Buzz(50);
        var (lo, hi) = bridgeSpan(b);
        for (double v = lo; v < hi; v += 16)
        {
            var p = xyR(b.R, b.u + rand(-b.hw, b.hw), v);
            parts.Add(new Part { k = "shell", x = p.x, y = p.y, z = 6, vx = rand(-60, 60), vy = rand(-40, 40), vz = rand(80, 200), rot = rand(0, TAU), vr = rand(-8, 8), t = 0, max = rand(.6, 1), dirt = true });
            puff(p.x, p.y, "#bfe6ff", 2, 1);
        }
        var m = xyR(b.R, b.u, rcR(b.R, b.u)); ftext(m.x, m.y - 30, T("Мост рухнул!"), "#ffb3a3", 15);
    }
    public static void drawRiverDyn(Ctx c)
    {
        if (RIVER == null) return;
        c.save(); c.strokeStyle = "rgba(255,255,255,.55)"; c.lineWidth = 2; c.lineCap = "round";
        foreach (var R in RIVERS)
        {
            double u0 = Math.Max(R.lo, -20), span = Math.Min(R.hi, (R.axis == 'h' ? WW : WH) + 20) - u0; int n = (int)Math.Round(span / 60);
            for (int i = 0; i < n; i++)
            {
                double u = u0 + JsMod(i * 61 + S.t * 28, span), off = Math.Sin(i * 2.7) * R.hw * .6; var a = xyR(R, u, rcR(R, u) + off); var b = xyR(R, u + 10, rcR(R, u + 10) + off);
                if (!inWater(a.x, a.y)) continue; c.globalAlpha = .4 + .3 * Math.Sin(S.t * 2 + i); c.beginPath(); c.moveTo(a.x, a.y); c.lineTo(b.x, b.y); c.stroke();
            }
        }
        c.restore();
        drawFishWater(c); // плавники пираний — под мостами
        foreach (var b in allBridges())
        {
            var R = b.R; double rc(double u) => rcR(R, u);
            var (lo, hi) = bridgeSpan(b); double h = b.hw, wob = b.wob > 0 ? Math.Sin(S.t * 60) * 1.5 : 0;
            c.save(); if (R.axis == 'v') c.transform(0, 1, 1, 0, 0, 0); c.translate(wob, 0); c.strokeStyle = INK; c.lineJoin = "round";
            if (b.broken)
            {
                c.fillStyle = "#6b5a40"; c.lineWidth = 2;
                foreach (var v in new[] { lo + 2, hi - 12 }) foreach (var x in new[] { -h, h - 7 }) { c.beginPath(); c.rect(b.u + x, v, 7, 10); c.fill(); c.stroke(); }
                var mid = rc(b.u);
                for (int k = 0; k < 3; k++)
                {
                    c.save(); c.translate(b.u + (k - 1) * 16 + Math.Sin(S.t * 1.5 + k) * 5, mid + (k - 1) * 10 + Math.Cos(S.t + k) * 4); c.rotate(.4 * k + Math.Sin(S.t + k) * .2);
                    c.fillStyle = "#7f7a5a"; c.beginPath(); c.rect(-14, -4, 28, 8); c.fill(); c.stroke(); c.restore();
                }
                c.restore(); continue;
            }
            c.fillStyle = "rgba(0,0,0,.18)"; c.fillRect(b.u - h + 4, lo + 6, 2 * h, hi - lo);
            var plank = b.rotten ? new[] { "#8a8360", "#7a7350", "#948c66" } : new[] { "#b07a45", "#a06d3c", "#bd8650" };
            int i2 = 0;
            for (double v = lo; v < hi - 4; v += 11, i2++)
            {
                int ub = (int)Math.Round(b.u, MidpointRounding.AwayFromZero);
                bool gap = b.rotten && ((i2 * 7 + ub) % 6 == 0 || (b.passes > 0 && (i2 * 5 + ub) % 4 == 0));
                if (gap) continue;
                double sag = b.rotten && b.passes > 0 ? Math.Sin(i2 * 1.7) * 1.5 : 0;
                c.fillStyle = plank[i2 % 3]; c.lineWidth = 1.8; c.beginPath(); c.rect(b.u - h, v + sag, 2 * h, 9); c.fill(); c.stroke();
                if (b.rotten) { c.strokeStyle = "rgba(43,29,20,.55)"; c.lineWidth = 1.2; c.beginPath(); c.moveTo(b.u - h + 6 + (i2 * 9) % 20, v + 2); c.lineTo(b.u - h + 14 + (i2 * 9) % 20, v + 7); c.stroke(); c.strokeStyle = INK; }
            }
            c.fillStyle = b.rotten ? "#5f5a40" : "#8a5a2b"; c.lineWidth = 2;
            foreach (var x in new[] { -h - 4, h - 2 }) { c.beginPath(); c.rect(b.u + x, lo - 4, 6, hi - lo + 8); c.fill(); c.stroke(); }
            c.restore();
        }
    }
    /// <summary>JS-остаток (a % b) для положительных b; для отрицательных a результат как в JS.</summary>
    public static double JsMod(double a, double b) => a % b;
}
