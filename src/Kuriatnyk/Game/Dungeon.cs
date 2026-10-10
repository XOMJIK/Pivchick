using System;
using System.Collections.Generic;
using System.Linq;
using Kuriatnyk.Render;
using SkiaSharp;
using static Kuriatnyk.D;
using static Kuriatnyk.Store;

namespace Kuriatnyk;

// Режим Soul Knight: лагерь с порталом, затем два мира по пять уровней: лесное подземелье 0-1 … 0-5 и заводские катакомбы 1-1 … 1-5.
// Уровень — комнаты на сетке 3×3, соединённые коридорами. Заходишь в боевую комнату — двери закрываются,
// пока не победишь все волны. В последней комнате портал на следующий уровень.
// На 0-4 и 1-4 есть необязательная комната с боссом в стороне от пути, на 0-5 и 1-5 босс стоит перед порталом.

public sealed class Room
{
    public double x, y, w, h; public string type; // start, fight, end, boss (обязательный), bossopt (необязательный), shop, chest, shrine (святилище со статуями), hub
    public bool cleared, seen; public int wave, waves; public double waveT; public Enemy boss; // seen — герой тут был (для мини-карты)
    public List<Corr> cors = new();
    public double cx => x + w / 2; public double cy => y + h / 2;
}
public sealed class Corr { public double x, y, w, h; public bool horiz; public Room a, b; }
public sealed class Portal { public double x, y, t; public bool on; public string kind; } // kind: start (из лагеря) или next
public sealed class Dungeon
{
    public List<Room> rooms = new(); public List<Corr> cors = new(); public List<Portal> portals = new();
    public Room active; public bool hub; public double doorX, doorY, enterCd, doorMsgT;
}

public static partial class G
{
    public static Dungeon DUN;
    public const int SK_LEVELS = 10, SK_WORLD = 5; // всего уровней и уровней в одном мире
    const double WALL = 22, CORR_W = 96, SK_SIZE = 2300; // уровень подземелья — мир 2300×2300, чтобы комнаты были просторными
    const string DUN_VOID = "#1b2018";

    /// <summary>Какие враги живут на уровне (чем глубже, тем злее). 1–5 — лес, 6–10 — заводские катакомбы.</summary>
    static Dictionary<string, double> skMix(int lv) => lv switch
    {
        1 => new() { ["hen"] = 1, ["rat"] = .6, ["fox"] = .6 },
        2 => new() { ["hen"] = .8, ["fox"] = .7, ["crow"] = .5, ["raccoon"] = .4, ["rat"] = .4 },
        3 => new() { ["fox"] = .7, ["turkey"] = .4, ["bigrat"] = .6, ["crow"] = .5, ["raccoon"] = .5, ["kamibird"] = .25 },
        4 => new() { ["wolf"] = .8, ["owl"] = .5, ["bigrat"] = .5, ["fox"] = .5, ["turkey"] = .3, ["kamibird"] = .3 },
        5 => new() { ["wolf"] = .7, ["ferret"] = .7, ["robohen"] = .5, ["owl"] = .5, ["raccoon"] = .4, ["kamibird"] = .3 },
        6 => new() { ["ferret"] = .9, ["bigrat"] = .6, ["robohen"] = .5, ["rat"] = .4, ["kamibird"] = .3 },
        7 => new() { ["ferret"] = .7, ["robohen"] = .6, ["owl"] = .4, ["bigrat"] = .4, ["kamibird"] = .35 },
        8 => new() { ["robohen"] = .8, ["ferret"] = .6, ["eagle"] = .4, ["owl"] = .4, ["kamibird"] = .4 },
        9 => new() { ["robohen"] = .7, ["eagle"] = .5, ["wolf"] = .5, ["ferret"] = .5, ["kamibird"] = .4 },
        _ => new() { ["robohen"] = .7, ["eagle"] = .6, ["ferret"] = .6, ["owl"] = .5, ["wolf"] = .4, ["kamibird"] = .45 },
    };
    /// <summary>Номер уровня в мире: 1…5.</summary>
    static int skWl(int lv) => (lv - 1) % SK_WORLD + 1;
    /// <summary>Второй мир — заводские катакомбы.</summary>
    static bool skFactory(int lv) => lv > SK_WORLD;
    public static string skLvName(int lv) => lv > SK_WORLD ? "1-" + (lv - SK_WORLD) : "0-" + lv;

    /* ---------- запуск режима ---------- */
    public static void startSK(string heroId)
    {
        if (heroId == null || !SAVE.heroes.ContainsKey(heroId)) heroId = SAVE.hero;
        lastBridge = null;
        P = newPlayer(heroId); allies = new();
        // оружие сюжета сюда не берём: забег начинается с рогатки и палки
        P.bag = new List<string>(SKW.START); P.slot = 0; P.w = P.prim = P.bag[0]; P.bagMax = 2;
        P.stamMax = P.stam = SK_STAM_MAX; P.nades = 0; // гранат в подземелье нет
        enemies = new(); bullets = new(); parts = new(); pickups = new(); nades = new(); bombs = new(); texts = new(); holes = new(); zones = new(); strikes = new(); puddles = new(); goo = new();
        S.piv = 0; S.skPass = new();
        S.mode = "play"; S.kind = "sk"; S.skHero = heroId; S.lvIdx = 0; S.L = null; S.diff = 1; S.night = false; S.t = 0; S.score = 0; S.wave = 0; S.toSpawn = 0; S.spawnT = 0;
        S.between = 0; S.running = true; S.kills = 0; S.goalKills = 0; S.survT = 0; S.boss = null; S.bossT = 0; S.shake = 0;
        S.combo = 0; S.comboT = 0; S.deadT = 0; S.winT = 0; S.coins = 0; S.tokens = 0; S.banked = false; S.cleanup = false; S.goalDone = false;
        sticks.move = sticks.aim = null; clearBanner(); Ui.ToastClear(); bossBarOn = false;
        enterHub();
        Ui.OnGameStart();
    }

    static void resetField()
    {
        enemies = new(); bullets = new(); pickups = new(); nades = new(); bombs = new(); texts = new(); zones = new(); strikes = new(); puddles = new(); goo = new();
        S.boss = null; bossBarOn = false; RIVERS = new(); RIVER = null; actPick = null; actVend = actStatue = null; fish = new();
        allies = new(); // бойцы «Подкрепления» на следующий уровень не идут
    }
    static void snapCamera() { double vw = VW / SC, vh = VH / SC; camX = P.x - vw / 2; camY = P.y - 28 - vh / 2; }

    static void enterHub()
    {
        resetField(); S.skLv = 0; WW = WH = MAP_SIZE;
        var D = new Dungeon { hub = true };
        var R = new Room { type = "hub", w = 560, h = 420, cleared = true }; R.x = WW / 2 - R.w / 2; R.y = WH / 2 - R.h / 2; D.rooms.Add(R);
        D.portals.Add(new Portal { x = R.cx, y = R.y + 120, on = true, kind = "start" });
        D.doorX = R.x + R.w - 70; D.doorY = R.y + 6; // закрытая дверь в стене: здесь в будущем что-то появится
        OBS = new() { new Obs { x = R.x + 60, y = R.y + R.h - 60, r = 34, type = "hay" }, new Obs { x = R.x + R.w - 60, y = R.y + R.h - 60, r = 22, type = "barrel" }, new Obs { x = R.x + 56, y = R.y + 90, r = 22, type = "barrel" } };
        DUN = D; renderDungeon(); D.enterCd = 1;
        P.x = R.cx; P.y = R.y + R.h - 110; P._px = P.x; P._py = P.y; snapCamera();
        Music.Play("menu");
        showBanner(T("Лагерь"), T("Зайди в портал — начнётся забег"));
    }

    static void enterLevel(int lv)
    {
        resetField(); S.skLv = lv; S.lvIdx = lv; S.diff = 1 + (lv - 1) * 1.3; WW = WH = SK_SIZE;
        buildLevel(lv); renderDungeon();
        var st = DUN.rooms.First(r => r.type == "start"); st.seen = true;
        P.x = st.cx; P.y = st.cy + 30; P._px = P.x; P._py = P.y; P.buff = null; P.inv = 1;
        if (lv > 1) { var h = Math.Round(P.max * .25); P.hp = Math.Min(P.max, P.hp + h); } // между уровнями немного лечимся
        snapCamera(); DUN.enterCd = 1.2;
        Music.Play("battle"); Sfx.wave();
        bool fac = skFactory(lv), last = skWl(lv) == SK_WORLD;
        showBanner(skLvName(lv), fac ? (last ? T("Логово доктора") : T("Заводские катакомбы")) : last ? T("Логово главаря") : T("Лесное подземелье"));
    }

    /* ---------- генерация уровня ---------- */
    static void buildLevel(int lv)
    {
        double cs = (WW - 2 * FENCE) / 3;
        int wl = skWl(lv), len = lv == 1 ? 4 : 5;
        // в стороне от пути: торговец и сундук на каждом уровне, на 0-4 и 1-4 ещё необязательный босс;
        // святилище со статуями — если для него нашлось место
        var extraTypes = new List<string> { "shop", "chest", "shrine" }; if (wl == 4) extraTypes.Insert(0, "bossopt");
        List<(int x, int y)> path = null, any = null; List<((int x, int y) cell, int at, string type)> extras = null;
        List<(int x, int y)> fbPath = null; List<((int x, int y) cell, int at, string type)> fbExtras = null; // запасной вариант без святилища
        static IEnumerable<(int x, int y)> nbs((int x, int y) c) => new[] { (c.x + 1, c.y), (c.x - 1, c.y), (c.x, c.y + 1), (c.x, c.y - 1) }.Where(q => q.Item1 >= 0 && q.Item1 < 3 && q.Item2 >= 0 && q.Item2 < 3);
        for (int tries = 0; tries < 600 && extras == null; tries++)
        {
            var p = new List<(int x, int y)> { ((int)(rnd() * 3), (int)(rnd() * 3)) };
            while (p.Count < len)
            {
                var nb = nbs(p[^1]).Where(c => !p.Contains(c)).ToList();
                if (nb.Count == 0) break;
                p.Add(nb[(int)(rnd() * nb.Count)]);
            }
            if (p.Count != len) continue;
            any = p;
            var used = new HashSet<(int x, int y)>(p); var ex = new List<((int x, int y), int, string)>(); bool ok = true;
            foreach (var t in extraTypes)
            {
                var opts = new List<((int x, int y) c, int at)>();
                for (int i = t == "bossopt" ? 1 : 0; i < len - 1; i++) foreach (var c in nbs(p[i])) if (!used.Contains(c)) opts.Add((c, i));
                if (opts.Count == 0) { if (t != "shrine") ok = false; break; }
                var o = opts[(int)(rnd() * opts.Count)]; used.Add(o.c); ex.Add((o.c, o.at, t));
            }
            if (!ok) continue;
            if (ex.Count == extraTypes.Count) { path = p; extras = ex; }
            else if (fbPath == null) { fbPath = p; fbExtras = ex; }
        }
        if (path == null && fbPath != null) { path = fbPath; extras = fbExtras; }
        if (path == null) { path = any; extras = new(); } // не нашлось места — уровень без комнат в стороне
        var D = new Dungeon();
        Room mk((int x, int y) g, string type)
        {
            double w = type is "start" or "end" ? 300 : type is "boss" or "bossopt" ? 660 : type is "shop" or "chest" or "shrine" ? 340 : 600,
                h = type is "start" or "end" ? 270 : type is "boss" or "bossopt" ? 600 : type is "shop" or "chest" or "shrine" ? 300 : 520;
            var R = new Room { type = type, w = w, h = h };
            R.x = FENCE + cs * (g.x + .5) - w / 2; R.y = FENCE + cs * (g.y + .5) - h / 2; D.rooms.Add(R); return R;
        }
        void link(Room a, Room b)
        {
            bool hz = Math.Abs(a.cy - b.cy) < 1;
            Corr c;
            if (hz) { var (l, r) = a.cx < b.cx ? (a, b) : (b, a); c = new Corr { horiz = true, x = l.x + l.w - 12, w = r.x - (l.x + l.w) + 24, y = l.cy - CORR_W / 2, h = CORR_W }; }
            else { var (t, bt) = a.cy < b.cy ? (a, b) : (b, a); c = new Corr { horiz = false, y = t.y + t.h - 12, h = bt.y - (t.y + t.h) + 24, x = t.cx - CORR_W / 2, w = CORR_W }; }
            c.a = a; c.b = b; a.cors.Add(c); b.cors.Add(c); D.cors.Add(c);
        }
        var rooms = new List<Room>();
        for (int i = 0; i < path.Count; i++)
        {
            string type = i == 0 ? "start" : i == path.Count - 1 ? (wl == SK_WORLD ? "boss" : "end") : "fight";
            var R = mk(path[i], type); R.waves = lv <= 2 ? 2 : 3; rooms.Add(R);
            if (i > 0) link(rooms[i - 1], R);
        }
        foreach (var (cell, at, type) in extras) { var X = mk(cell, type); X.cleared = type != "bossopt"; link(rooms[at], X); } // отдельный коридор в сторону
        // укрытия в боевых комнатах
        OBS = new();
        // автомат с зельями — не больше одного на уровень: у торговца, в стартовой комнате или вовсе нет
        double vr = rnd(); var vendRoom = vr < .4 ? D.rooms.FirstOrDefault(r => r.type == "shop") : vr < .75 ? D.rooms.First(r => r.type == "start") : null;
        foreach (var R in D.rooms)
        {
            if (R.type == "chest") OBS.Add(new Obs { x = R.cx, y = R.cy - 6, r = 26, type = "skchest" });
            if (R == vendRoom) OBS.Add(new Obs { x = R.type == "shop" ? R.x + R.w - 46 : R.x + 62, y = R.y + 62, r = 24, type = "vend" });
            if (R.type == "shop") OBS.Add(new Obs { x = R.cx, y = R.y + 82, r = 34, type = "counter" });
            if (R.type == "shrine")
            { // две статуи с разными ультами, кроме той, что у героя сейчас
                var pool = SKU.ALL.Where(u => u != (P?.ultId ?? P?.hid)).OrderBy(_ => rnd()).Take(2).ToList();
                for (int i = 0; i < pool.Count; i++) OBS.Add(new Obs { x = R.cx + (i == 0 ? -82 : 82), y = R.cy - 36, r = 26, type = "statue", kind = pool[i] });
            }
        }
        // речки на некоторых уровнях: на втором и четвёртом уровне мира — в двух боевых комнатах, на третьем — в одной
        DUN = D; RIVERS = new(); RIVER = null;
        int nRiv = wl is 2 or 4 ? 2 : wl == 3 ? 1 : 0;
        foreach (var R in D.rooms.Where(r => r.type == "fight").OrderBy(_ => rnd()).Take(nRiv)) addRoomRiver(R);
        foreach (var b in allBridges()) b.ends = bridgeEnds(b);
        spawnFish();
        var kinds = skFactory(lv) ? new[] { "barrel", "rock", "barrel", "rock", "hay" } : new[] { "barrel", "rock", "stump", "bush", "barrel", "rock" };
        bool free(double x, double y, double r) => !OBS.Any(o => hypot(o.x - x, o.y - y) < o.r + r + 60) && !nearWater(x, y, r + 30) && bridgeAt(x, y) == null;
        foreach (var R in D.rooms.Where(r => r.type is "fight" or "bossopt" or "boss"))
        {
            int n = R.type == "fight" ? 4 + (int)(rnd() * 2) : 3;
            for (int k = 0, tries = 0; k < n && tries < 80; tries++)
            {
                var t = kinds[(int)(rnd() * kinds.Length)]; double r = t == "barrel" ? 21 : t == "stump" ? 22 : 26;
                double x = rand(R.x + 80, R.x + R.w - 80), y = rand(R.y + 80, R.y + R.h - 70);
                if (Math.Abs(x - R.cx) < 60 || Math.Abs(y - R.cy) < 60 || !free(x, y, r)) continue; // проходы к дверям — свободны
                OBS.Add(new Obs { x = x, y = y, r = r, type = t }); k++;
            }
            // ящики, которые можно разбить
            for (int k = 0, tries = 0; k < 3 && tries < 60; tries++)
            {
                double x = rand(R.x + 60, R.x + R.w - 60), y = rand(R.y + 60, R.y + R.h - 50);
                if (Math.Abs(x - R.cx) < 55 || Math.Abs(y - R.cy) < 55 || !free(x, y, 20)) continue;
                OBS.Add(newCrate(x, y)); k++;
            }
        }
        var end = rooms[^1];
        D.portals.Add(new Portal { x = end.cx, y = end.cy, on = end.type == "end", kind = "next" });
        foreach (var R in D.rooms) if (R.type == "shop") stockShop(R);
    }
    /// <summary>Речка поперёк боевой комнаты: в стороне от дверей, с мостом напротив прохода и ещё одним сбоку.</summary>
    static void addRoomRiver(Room R)
    {
        bool hz = rnd() < .5; // h — течёт слева направо
        var Rv = new River { axis = hz ? 'h' : 'v', amp = 10, freq = .02, phase = rand(0, TAU), hw = 30, room = R };
        double along0 = hz ? R.x : R.y, len = hz ? R.w : R.h, across0 = hz ? R.y : R.x, across = hz ? R.h : R.w;
        Rv.@base = across0 + across * (rnd() < .5 ? .27 : .73);
        Rv.lo = along0 - 4; Rv.hi = along0 + len + 4;
        Rv.bridges = new[] { along0 + len / 2, along0 + len * (rnd() < .5 ? .17 : .83) }.Select(u => new Bridge { u = u, hw = 34, R = Rv }).ToList();
        RIVERS.Add(Rv); RIVER ??= Rv;
    }

    /* ---------- где можно ходить ---------- */
    public static bool inRoom(Room R, double x, double y, double m) => x >= R.x + m && x <= R.x + R.w - m && y >= R.y + m && y <= R.y + R.h - m;
    static bool inCorr(Corr c, double x, double y, double m) => c.horiz ? x >= c.x && x <= c.x + c.w && y >= c.y + m && y <= c.y + c.h - m : y >= c.y && y <= c.y + c.h && x >= c.x + m && x <= c.x + c.w - m;
    public static bool dunWalk(double x, double y, double m)
    {
        if (DUN == null) return true;
        if (DUN.active != null) return inRoom(DUN.active, x, y, m);
        foreach (var R in DUN.rooms) if (inRoom(R, x, y, m)) return true;
        foreach (var c in DUN.cors) if (inCorr(c, x, y, m)) return true;
        return false;
    }
    static void clampRoom(Unit e, Room R, double m) { e.x = clamp(e.x, R.x + m, R.x + R.w - m); e.y = clamp(e.y, R.y + m, R.y + R.h - m); }
    /// <summary>Стены подземелья: враги заперты в своей комнате, игрок скользит вдоль стен.</summary>
    static void dunCollide(Unit e)
    {
        if (e is Enemy en) { var R = en.room ?? DUN.active; if (R != null) clampRoom(e, R, en.r * .8); return; }
        double m = e.r * .8;
        if (dunWalk(e.x, e.y, m)) return;
        double px = e is Fighter f ? f._px : double.NaN, py = e is Fighter f2 ? f2._py : double.NaN;
        if (!double.IsNaN(px))
        {
            if (dunWalk(e.x, py, m)) { e.y = py; return; }
            if (dunWalk(px, e.y, m)) { e.x = px; return; }
            if (dunWalk(px, py, m)) { e.x = px; e.y = py; return; }
        }
        if (DUN.active != null) { clampRoom(e, DUN.active, m); return; }
        // занесло в стену (отдача, отброс) — в ближайшую комнату или коридор
        double bd = 1e9, bx = e.x, by = e.y;
        void tryRect(double x, double y, double w, double h)
        {
            double qx = clamp(e.x, x + m, x + w - m), qy = clamp(e.y, y + m, y + h - m), d = hypot(qx - e.x, qy - e.y);
            if (d < bd) { bd = d; bx = qx; by = qy; }
        }
        foreach (var R in DUN.rooms) tryRect(R.x, R.y, R.w, R.h);
        foreach (var c in DUN.cors) tryRect(c.x, c.y, c.w, c.h);
        e.x = bx; e.y = by;
    }

    /* ---------- ход уровня ---------- */
    static void updateSK(double dt)
    {
        var D = DUN; if (D == null || !P.alive) return;
        D.enterCd -= dt;
        findAct();
        foreach (var o in OBS) if (o.type == "skchest" && !o.open && hypot(o.x - P.x, o.y - P.y) < o.r + P.r + 16) openChest(o);
        foreach (var p in D.portals)
        {
            p.t += dt;
            if (p.on && D.enterCd <= 0 && hypot(P.x - p.x, P.y - p.y) < 30) { usePortal(p); return; }
        }
        if (D.hub)
        {
            D.doorMsgT -= dt;
            if (D.doorMsgT <= 0 && hypot(P.x - D.doorX, P.y - D.doorY) < 80) { D.doorMsgT = 3; ftext(D.doorX, D.doorY + 10, T("Закрыто · скоро откроется"), "#e8d3a8", 13); Sfx.creak(); }
            return;
        }
        foreach (var R in D.rooms)
            if (!R.seen && inRoom(R, P.x, P.y, 0))
            { // первый раз в комнате — она появляется на мини-карте
                R.seen = true;
                if (R.type == "shrine" && OBS.Any(o => o.type == "statue" && o.st == 0)) showBanner(T("Святилище"), T("Поклонись статуе — её ульта заменит твою"));
            }
        if (D.active == null)
        {
            foreach (var R in D.rooms)
                if (!R.cleared && R.type is "fight" or "boss" or "bossopt" && inRoom(R, P.x, P.y, P.r + 22)) { lockRoom(R); break; }
            return;
        }
        var A = D.active;
        if (A.boss != null && A.boss.dead)
        { // босс повержен — его свита разбегается
            foreach (var e in enemies) if (!e.dead) { puff(e.x, e.y, "#ffffff", 6, 1); e.dead = true; }
            S.cleanup = true; A.boss = null; A.wave = A.waves;
        }
        if (enemies.Any(e => !e.dead) || A.boss != null) return;
        if (A.wave < A.waves) { A.waveT -= dt; if (A.waveT <= 0) spawnRoomWave(A); }
        else clearRoom(A);
    }
    static void lockRoom(Room R)
    {
        DUN.active = R; R.wave = 0; R.waveT = .6; Sfx.chest(); S.shake += 5;
        if (R.type is "boss" or "bossopt")
        {
            bool fac = skFactory(S.skLv); // во втором мире — роботы и сам доктор
            var type = R.type == "boss" ? (fac ? "drferret" : "ataman") : fac ? "steelturkey" : "gturkey";
            var e = makeEnemy(type, R.cx, R.y + 110); e.room = R; e.atkT = 2; e.hp = e.max = R.type == "boss" ? (fac ? 3200 : 1700) : fac ? 1900 : 950;
            R.boss = e; S.boss = e; bossBarOn = true; R.waves = 0; Music.Play("boss"); S.shake += 8; Sfx.gobble();
            showBanner(ETYPE[type].name, R.type == "boss" ? T("Победи главаря — откроется портал") : T("Необязательный босс · награда — золотые яйца"));
        }
    }
    static void spawnRoomWave(Room R)
    {
        R.wave++; R.waveT = .9; int lv = S.skLv; var mix = skMix(lv);
        if (P.hid == "hen" && hasP(5) && P.alive)
        { // «Запасливая» в подземелье: гранат нет, поэтому каждая волна даёт стамину
            double h = Math.Min(15, P.stamMax - P.stam); P.stam += h;
            if (h > 0) ftext(P.x, P.y - 50, "+" + Math.Round(h) + T(" стамины"), "#7fd4ff", 14);
        }
        int n = 3 + Math.Min(lv, 7) + (rnd() < .5 ? 1 : 0) + (R.wave == R.waves ? 1 : 0);
        for (int i = 0; i < n; i++)
        {
            var type = pickFrom(mix); var Tt = ETYPE[type];
            // появляются подальше от героя (быстрые мыши и крысы — ещё дальше), не в воде и не в укрытиях
            bool fast = type is "rat" or "bigrat" or "fox" or "wolf";
            double x = R.cx, y = R.cy, bestD = -1, bx = R.cx, by = R.cy;
            for (int t = 0; t < 40; t++)
            {
                x = rand(R.x + 40, R.x + R.w - 40); y = rand(R.y + 40, R.y + R.h - 40);
                if (OBS.Any(o => hypot(o.x - x, o.y - y) < o.r + Tt.r + 6) || nearWater(x, y, Tt.r + 10) || bridgeAt(x, y) != null) continue;
                var d = hypot(x - P.x, y - P.y); if (d > bestD) { bestD = d; bx = x; by = y; }
                if (d > (fast ? 320 : 240)) break;
            }
            x = bx; y = by;
            int cnt = type == "rat" ? 2 : 1;
            for (int k = 0; k < cnt; k++)
            {
                var e = makeEnemy(type, clamp(x + (k == 0 ? 0 : rand(-26, 26)), R.x + 30, R.x + R.w - 30), clamp(y + (k == 0 ? 0 : rand(-26, 26)), R.y + 30, R.y + R.h - 30));
                e.room = R; e.emerge = fast ? .9 : .6; ring(e.x, e.y, "#ff5a3c", 34, .5); // юркие вылезают дольше — успеешь отбежать
            }
        }
        if (R.wave > 1) ftext(P.x, P.y - 60, T("Волна ") + R.wave + "/" + R.waves, "#ffc93a", 14);
    }
    static void clearRoom(Room R)
    {
        R.cleared = true; DUN.active = null; Sfx.pick(); S.shake += 3;
        int bonus = 6 + S.skLv * 3; S.coins += bonus;
        ftext(P.x, P.y - 62, T("Комната зачищена!") + " +" + bonus, "#9cf27a", 15);
        if (rnd() < .45) pickups.Add(new Pickup { x = R.cx, y = R.cy, kind = "corn", t = 60, ph = 0 });
        if (rnd() < .6) pickups.Add(new Pickup { x = R.cx + 30, y = R.cy + 10, kind = "stam", t = 60, ph = 0 }); // стамина сама не копится — кристалл за комнату
        dropPiv(R.cx, R.cy, 2 + S.skLv / 2);
        if (R.type == "bossopt")
        {
            int eg = 2; SAVE.eggs += eg; S.coins += 60; persist();
            var w = rollSkWeapon(S.skLv, P.luck + 2, skTaken()); dropWeapon(w, R.cx, R.cy + 40); dropPiv(R.cx, R.cy, 10);
            showBanner(T("Босс повержен!"), "+" + eg + T(" золотых яиц · +60 зёрен")); Music.Play("battle");
        }
        else if (R.type == "boss")
        {
            foreach (var p in DUN.portals) p.on = true;
            string sub = T("Портал открыт");
            if (S.skLv < SK_LEVELS)
            { // конец первого мира: портал ведёт в заводские катакомбы, в первый раз — награда
                sub = T("Портал ведёт в заводские катакомбы");
                if (!SAVE.eggMs.ContainsKey("skW1")) { SAVE.eggMs["skW1"] = 1; SAVE.eggs += 5; persist(); sub += T(" · +5 золотых яиц"); }
            }
            showBanner(T("Главарь повержен!"), sub); Music.Play("battle");
        }
    }
    static void usePortal(Portal p)
    {
        Sfx.ult(); ring(P.x, P.y, "#b48cff", 120, .5); puff(P.x, P.y, "#d9c4ff", 12, 1.2);
        if (p.kind == "start") { enterLevel(1); return; }
        if (S.skLv >= SK_LEVELS) { skFinish(); return; }
        // между уровнями — выбор одной пассивки из трёх
        skOffer = rollPassives(3);
        if (skOffer.Count == 0) { enterLevel(S.skLv + 1); return; }
        S.mode = "pick"; sticks.move = sticks.aim = null; input.firing = false; Music.Duck(.5);
        Ui.ShowSkPick(skOffer);
    }
    static void skFinish()
    {
        S.mode = "result"; sticks.move = sticks.aim = null; input.firing = false;
        skCashPiv(); long sc = S.coins, st = S.tokens; bankCoins();
        bool first = !SAVE.eggMs.ContainsKey("skRun");
        long lc = 300, lt = 80; int le = first ? 15 : 0;
        SAVE.coins += lc; SAVE.tokens += lt; SAVE.eggs += le; if (first) { SAVE.eggMs["skRun"] = 1; SAVE.btokens++; SAVE.lchests++; }
        persist();
        var w = new WinInfo { eyebrow = T("Режим Soul Knight") + " · " + skLvName(SK_LEVELS), lead = T("Оба мира пройдены! Лис-атаман и Доктор Хорёк повержены."), hasNext = false };
        w.rows.Add(new WinRow { icon = "coin", text = "+" + (lc + sc) + T(" зёрен"), sub = T("за забег и врагов") });
        w.rows.Add(new WinRow { icon = "tok", text = "+" + (lt + st) + T(" жетонов"), sub = T("на обычный сундук: ") + L.Fmt(SAVE.tokens) + "/100" });
        if (first) { w.rows.Add(new WinRow { icon = "gegg", text = "+" + le + T(" золотых яиц"), sub = T("только за первое прохождение") }); w.rows.Add(new WinRow { icon = "btok", text = StripIcon(T("<i class=\"btok\"></i>+1 большой жетон")), sub = T("на большой сундук: ") + SAVE.btokens + "/10" }); w.rows.Add(new WinRow { icon = "lch", text = T("Легендарный сундук!"), sub = T("за первое прохождение обоих миров · открой в «Сундуках»") }); }
        Ui.ShowWin(w);
    }
    /// <summary>Подпись цели в HUD: «Лагерь» или «0-3 · комнаты 1/3».</summary>
    static string skGoal()
    {
        if (DUN == null || DUN.hub) return T("Лагерь");
        var f = DUN.rooms.Where(r => r.type is "fight" or "boss").ToList();
        return skLvName(S.skLv) + " · " + T("комнаты ") + f.Count(r => r.cleared) + "/" + f.Count;
    }

    /* ---------- оружие: рюкзак и ближний бой ---------- */
    /// <summary>Следующее оружие из рюкзака (F или кнопка у названия оружия).</summary>
    public static void swapWeapon()
    {
        if (S.mode != "play" || P == null || !P.alive || P.bag == null || P.bag.Count < 2) return;
        selectSlot((P.slot + 1) % P.bag.Count);
    }
    public static void selectSlot(int i)
    {
        if (S.mode != "play" || P == null || !P.alive || P.bag == null || i < 0 || i >= P.bag.Count || (i == P.slot && P.w == P.bag[i])) return;
        P.slot = i; P.w = P.prim = P.bag[i]; P.spin = 0;
        P.ammo = double.PositiveInfinity; P.cd = Math.Max(P.cd, .15); Sfx.click();
        ftext(P.x, P.y - 54, WEAP[P.w].name, "#ffc93a", 13);
    }
    const double MELEE_R = 80, MELEE_ARC = 1.15;
    /// <summary>Взмах ближним оружием: бьёт всех врагов в секторе перед героем и отбивает вражеские яйца
    /// (сковородка — обратно во врагов). Меч-кладенец ещё и пускает волну.</summary>
    static void meleeSwing(double ang, Weap W)
    {
        double R = W.meleeR > 0 ? W.meleeR : MELEE_R, arc = W.arc > 0 ? W.arc : MELEE_ARC, dmg = W.dmg * P.dmgMul * P.meleeMul;
        P.swing = .22; P.swingA = ang; P.swingR = R;
        double hx = P.x, hy = P.y; int hits = 0;
        for (int i = 0; i < enemies.Count; i++)
        {
            var e = enemies[i]; if (e.dead) continue;
            double dx = e.x - hx, dy = e.y - hy, d = hypot(dx, dy); if (d - e.r > R) continue;
            var df = Math.Abs(((Math.Atan2(dy, dx) - ang + Math.PI * 3) % TAU) - Math.PI);
            if (df > arc && d > e.r + P.r + 6) continue;
            hitEnemy(e, dmg, dx, dy, true, 1.8 * W.knock); hits++;
            if (W.stun > 0 && !e.T.boss && !e.dead) e.stun = Math.Max(e.stun, W.stun);
        }
        for (int i = bullets.Count - 1; i >= 0; i--)
        {
            var b = bullets[i]; if (b.fromP) continue;
            double dx = b.x - hx, dy = b.y - hy, d = hypot(dx, dy); if (d > R + 10) continue;
            var df = Math.Abs(((Math.Atan2(dy, dx) - ang + Math.PI * 3) % TAU) - Math.PI); if (df > arc) continue;
            if (W.reflect) { b.fromP = true; b.vx = Math.Cos(ang) * 620; b.vy = Math.Sin(ang) * 620; b.dmg = 26 * P.dmgMul; b.life = 1; b.kind = "refl"; b.pierce = 0; b.hits = null; puff(b.x, b.y, "#ffe066", 2, .5); continue; }
            puff(b.x, b.y, "#fff4dc", 2, .5); bullets.RemoveAt(i);
        }
        parts.Add(new Part { k = "swing", x = hx, y = hy, a = ang, t = 0, max = .18, R = R, s = arc, col = W.id == "sword" ? "#9fe3ff" : null });
        if (W.wave > 0)
        {
            var b = skBullet(W, P.x + Math.Cos(ang) * 30, P.y + Math.Sin(ang) * 30, ang, 700, W.wave * P.dmgMul * P.meleeMul, 0);
            b.kind = "wave"; b.r = 16; b.life = b.max = .45; b.pierce = 99; b.hits = new();
        }
        hitCratesArea(hx, hy, R, dmg, ang, arc);
        if (hits > 0) { S.shake += 3; Platform.Buzz(15); }
    }

    /* ---------- отрисовка ---------- */
    static void renderDungeon()
    {
        double res = COARSE ? 1 : WW > MAP_SIZE ? 1.35 : 2; // большой уровень — чуть ниже разрешение, чтобы картинка не разрасталась
        var info = new SKImageInfo((int)(WW * res), (int)(WH * res), SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surf = SKSurface.Create(info);
        var g = new Ctx(); g.Begin(surf.Canvas); g.scale(res, res);
        bool hub = DUN.hub, fac = !hub && skFactory(S.skLv); // заводские катакомбы — серый камень и металл
        string floor = hub ? "#b88a57" : fac ? "#858c96" : "#7d8b63", floor2 = hub ? "#a97c4b" : fac ? "#7a818b" : "#74825b", wallTop = hub ? "#6b4a2b" : fac ? "#4a4f5c" : "#4c5a44", wallFace = hub ? "#523620" : fac ? "#33363f" : "#36412f";
        g.fillStyle = DUN_VOID; g.fillRect(0, 0, WW, WH);
        var Gr = seeded(4242 + S.skLv * 17 + (long)(rnd() * 1000));
        void wallRect(double x, double y, double w, double h)
        {
            g.fillStyle = wallTop; g.strokeStyle = INK; g.lineWidth = 3; rr(g, x - WALL, y - WALL - 10, w + WALL * 2, h + WALL * 2 + 10, 8); g.fill(); g.stroke();
            g.fillStyle = "rgba(255,255,255,.08)"; for (double bx = x - WALL + 6; bx < x + w + WALL - 20; bx += 34) g.fillRect(bx, y - WALL - 6, 26, 6);
        }
        void floorRect(double x, double y, double w, double h, bool tiles)
        {
            g.fillStyle = floor; g.fillRect(x, y, w, h);
            if (hub)
            { // деревянный пол лагеря
                for (double py = y; py < y + h; py += 28) { g.fillStyle = Gr() < .5 ? floor : floor2; g.fillRect(x, py, w, 26); g.fillStyle = "rgba(43,29,20,.25)"; g.fillRect(x, py + 26, w, 2); }
                return;
            }
            if (!tiles) return;
            for (double ty = y; ty < y + h; ty += 40)
                for (double tx = x; tx < x + w; tx += 40)
                {
                    if (Gr() < .35) { g.fillStyle = Gr() < .5 ? floor2 : "rgba(255,255,255,.05)"; g.fillRect(tx + 1, ty + 1, Math.Min(38, x + w - tx - 1), Math.Min(38, y + h - ty - 1)); }
                    g.fillStyle = "rgba(0,0,0,.13)"; g.fillRect(tx, ty, Math.Min(40, x + w - tx), 1.5); g.fillRect(tx, ty, 1.5, Math.Min(40, y + h - ty));
                }
            g.strokeStyle = fac ? "#646a74" : "#5f9e3c"; g.lineWidth = 1.6; g.lineCap = "round"; // трава в лесу, царапины на заводе
            for (int i = 0; i < w * h / 2600; i++) { double px = x + Gr() * w, py = y + Gr() * h; g.beginPath(); g.moveTo(px, py); g.lineTo(px - 2, py - 5); g.moveTo(px + 2, py); g.lineTo(px + 3, py - 4); g.stroke(); }
            g.fillStyle = "rgba(60,50,40,.35)"; for (int i = 0; i < w * h / 5000; i++) { g.beginPath(); g.arc(x + Gr() * w, y + Gr() * h, 1.5 + Gr() * 2, 0, TAU); g.fill(); }
        }
        foreach (var R in DUN.rooms) wallRect(R.x, R.y, R.w, R.h);
        foreach (var c in DUN.cors) wallRect(c.x, c.y, c.w, c.h);
        foreach (var R in DUN.rooms)
        {
            floorRect(R.x, R.y, R.w, R.h, true);
            g.fillStyle = wallFace; g.fillRect(R.x, R.y - 2, R.w, 14); g.fillStyle = "rgba(0,0,0,.18)"; g.fillRect(R.x, R.y + 12, R.w, 5); // лицевая сторона верхней стены
        }
        foreach (var c in DUN.cors) floorRect(c.x, c.y, c.w, c.h, true);
        if (RIVERS.Count > 0)
        { // речки в комнатах — только по полу своих комнат, не на стенах
            g.save(); g.beginPath(); foreach (var Rv in RIVERS) g.rect(Rv.room.x, Rv.room.y, Rv.room.w, Rv.room.h); g.clip();
            if (fac) paintRivers(g, "#6b6f78", "#4f8f8a", "#3f7a75"); else paintRivers(g, "#8a7a55", "#3d8fb8", "#2f7aa0");
            g.restore();
        }
        if (hub)
        { // закрытая дверь в стене лагеря
            double dx = DUN.doorX, dy = DUN.doorY;
            g.fillStyle = "#6b4a2b"; g.strokeStyle = INK; g.lineWidth = 3; rr(g, dx - 34, dy - 40, 68, 52, 10); g.fill(); g.stroke();
            g.fillStyle = "#8a5a2b"; rr(g, dx - 26, dy - 32, 52, 44, 6); g.fill(); g.stroke();
            g.strokeStyle = "rgba(43,29,20,.6)"; g.lineWidth = 2; foreach (var ox in new[] { -9.0, 9.0 }) seg(g, dx + ox, dy - 30, dx + ox, dy + 10);
            g.fillStyle = "#c9a227"; g.strokeStyle = INK; g.lineWidth = 2; rr(g, dx - 8, dy - 12, 16, 14, 3); g.fill(); g.stroke();
            g.beginPath(); g.arc(dx, dy - 12, 5, Math.PI, TAU); g.stroke();
            // коврик и костёр-фонарь для уюта
            g.fillStyle = "#c0392b"; g.strokeStyle = INK; g.lineWidth = 2; var R0 = DUN.rooms[0]; rr(g, R0.cx - 90, R0.y + R0.h - 150, 180, 90, 18); g.fill(); g.stroke();
            g.strokeStyle = "#ffc93a"; g.lineWidth = 3; rr(g, R0.cx - 80, R0.y + R0.h - 140, 160, 70, 14); g.stroke();
        }
        surf.Canvas.Flush();
        ground?.Dispose(); ground = surf.Snapshot();
    }
    /// <summary>Порталы и закрытые двери поверх пола (меняются каждый кадр).</summary>
    static void drawDungeonDyn(Ctx c)
    {
        if (DUN == null) return;
        foreach (var p in DUN.portals)
        {
            double k = p.on ? 1 : .35, t = S.t;
            c.save(); c.translate(p.x, p.y);
            if (p.on) { var g = c.createRadialGradient(0, 0, 0, 0, 0, 70); g.addColorStop(0, "rgba(180,140,255,.55)"); g.addColorStop(1, "rgba(180,140,255,0)"); c.fillStyle = g; c.beginPath(); c.arc(0, 0, 70, 0, TAU); c.fill(); }
            c.globalAlpha = k;
            c.fillStyle = INK; c.beginPath(); c.ellipse(0, 3, 40, 20, 0, 0, TAU); c.fill();
            c.fillStyle = p.on ? "#5a2aa8" : "#4a4458"; c.beginPath(); c.ellipse(0, 0, 38, 18, 0, 0, TAU); c.fill();
            c.lineWidth = 3; c.lineCap = "round";
            for (int i = 0; i < 3; i++)
            {
                double q = (t * .8 + i / 3.0) % 1, r = 36 * (1 - q);
                c.strokeStyle = p.on ? (i % 2 == 0 ? "#d9c4ff" : "#8f5cff") : "#8a8494"; c.globalAlpha = k * (.3 + q * .7);
                c.beginPath(); c.ellipse(0, 0, r, r * .47, 0, t * (2 + i), t * (2 + i) + 4.2); c.stroke();
            }
            c.globalAlpha = k;
            if (p.on) for (int i = 0; i < 4; i++) { double a = t * 1.5 + i * TAU / 4, z = 10 + ((t * 30 + i * 13) % 40); c.fillStyle = "#e8dcff"; c.beginPath(); c.arc(Math.Cos(a) * 24, Math.Sin(a) * 10 - z, 2.2, 0, TAU); c.fill(); }
            c.restore();
        }
        var A = DUN.active; if (A == null) return;
        foreach (var co in A.cors)
        { // решётки на выходах из запертой комнаты
            double x, y, w, h;
            if (co.horiz) { bool right = co.x > A.cx; x = right ? A.x + A.w - 6 : A.x - 6; y = co.y; w = 12; h = co.h; }
            else { bool down = co.y > A.cy; y = down ? A.y + A.h - 6 : A.y - 6; x = co.x; w = co.w; h = 12; }
            c.save(); c.fillStyle = "rgba(43,29,20,.55)"; c.fillRect(x - 2, y - 2, w + 4, h + 4);
            c.strokeStyle = INK; c.lineWidth = 5; c.lineCap = "round";
            if (co.horiz) for (double yy = y + 8; yy < y + h; yy += 14) seg(c, x + w / 2, yy - 6, x + w / 2, yy + 4);
            c.strokeStyle = "#9aa3ad"; c.lineWidth = 3;
            if (co.horiz) { seg(c, x + w / 2, y, x + w / 2, y + h); for (double yy = y + 10; yy < y + h; yy += 18) seg(c, x - 4, yy, x + w + 4, yy); }
            else { seg(c, x, y + h / 2, x + w, y + h / 2); for (double xx = x + 10; xx < x + w; xx += 18) seg(c, xx, y - 6, xx, y + h + 6); }
            c.restore();
        }
    }
}
