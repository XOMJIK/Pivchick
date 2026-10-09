using System;
using System.Collections.Generic;
using System.Linq;
using static Kuriatnyk.D;
using static Kuriatnyk.Store;

namespace Kuriatnyk;

// Состояние боя, игрок и отряд (порт web/js/state.js и утилит из core.js).

public class Unit { public double x, y, r = 1, hp, max, vx, vy; public int _hf; }

public sealed class Look { public string sp, special, body, wing, tail, head, hat; public bool robo; }

/// <summary>Общее у игрока и бойцов отряда.</summary>
public class Fighter : Unit
{
    public string cls, buff; public Look look;
    public double spd, armor, regen, dmgMul, rateMul, buffT, fireT, ult, cd, ang, phase, flash, lastHit = -9, kitT = 8;
    public double _px = double.NaN, _py = double.NaN;
    public int face = 1, catN, gN, aN; public bool moving, hop;
}

public sealed class Player : Fighter
{
    public string hid, prim, w; public int hlv, nades;
    public double medMul, rangeMul, ammo, magnet, ultRate, inv, spin, svx, svy;
    public bool medic, revive, alive, wasFiring;
    public double swing, swingA, swingR; // взмах ближним оружием
    // стамина: тратится на выстрелы и понемногу восстанавливается; в сюжете при нехватке оружие стреляет вдвое реже
    public double stam, stamMax, stamRegen, stamMul = 1, noStamT, stamT; // stamT — сколько секунд стамину не тратили
    // Soul Knight: оружие в рюкзаке (до bagMax) и бонусы пассивок забега
    public List<string> bag; public int slot, bagMax = 2;
    public double crit, vamp, meleeMul = 1, pivMul = 1; public int luck; public bool magnetFar;
}

public sealed class Ally : Fighter
{
    public string id, gun; public int lv; public double range, down;
}

public sealed class Enemy : Unit
{
    public string type, st = "walk", body, wing; public ET T;
    public double cd, ang, phase, flash, strafe, strafeT, bite, kx, ky, stun, burn, chill, burnDps, burnTick, tellT, dashT, dx, dy, dashLen,
        atkT = 2.5, spin, spinA, burstT, emerge, kamiT, iceT, frozen, tgT, navT, s, honey, bubble;
    public double? navBest;
    public int face = 1, pat = -1, burst, dashes, iceN, navAlt;
    public bool moving, dead, hop, kami, kamiRolled, endless, hitThis;
    public Unit tgt; public Room room; // комната подземелья, из которой враг не выходит
    public double _px = double.NaN, _py = double.NaN;
}

public sealed class Bullet
{
    public double x, y, vx, vy, r, dmg, life, max, spin; public bool fromP, ally, frag; public string kind;
    public int pierce, bounce, shape; public List<Enemy> hits; public Ally owner;
    public Weap w; public bool back; public double tick; // снаряд оружия Soul Knight: свойства берутся из w
}

public sealed class Part
{
    public string k, col; public double x, y, z, vx, vy, vz, rot, vr, t, max, r, seed, a, s, x2, y2, sx, sy, R; public bool dirt;
}

public sealed class FText { public double x, y, size, t, max, vx; public string txt, col; }
/// <summary>Предмет на земле. kind: corn, nade, stam, piv (пивчики, n — сколько), skw (оружие wid), а также временные стволы.
/// price &gt; 0 — товар у торговца, берётся кнопкой действия.</summary>
public sealed class Pickup { public double x, y, t, ph, vx, vy, z, vz; public string kind, wid; public int n, price; }
public sealed class Nade { public double x, y, z, vx, vy, vz, t, rot; }
public sealed class Bomb { public double x, y, t, max, r, dmg, heal, sx, sy; public bool fromP, lob, quiet; public string kind; }
public sealed class Hole { public double x, y, t, ct, life, spawnT; public string state; public int made, max; }
public sealed class Zone { public double x, y, t, r, max; }
/// <summary>Лужи и облака от оружия Soul Knight: poop — вонючая лужа, poison — ядовитое облако, roots — корни.</summary>
public sealed class Goo { public double x, y, t, max, r, dps, tick; public string kind; public HashSet<Enemy> held; }

public sealed class GState
{
    public string mode = "menu", kind = "endless"; public int lvIdx; public Level L; public double diff = 1; public bool night;
    public double t; public long score; public int wave, toSpawn; public double spawnT, between; public bool running;
    public int kills, goalKills; public double survT; public Enemy boss; public double shake; public int combo;
    public double comboT, deadT, winT; public long coins, tokens; public bool banked = true;
    public double holeT, bossT, kamiNext; public bool cleanup;
    public int skLv; public string skHero; // Soul Knight: уровень 0-N (0 — лагерь) и выбранный герой
    public long piv; public List<string> skPass = new(); // пивчики забега и взятые пассивки
}

public static partial class G
{
    public const double TAU = Math.PI * 2;
    public const string INK = "#2b1d14";
    public static readonly Random Rng = new();
    public static double rnd() => Rng.NextDouble();
    public static double rand(double a, double b) => a + rnd() * (b - a);
    public static double clamp(double v, double a, double b) => v < a ? a : v > b ? b : v;
    public static double hypot(double x, double y) => Math.Sqrt(x * x + y * y);
    public static string T(string s) => L.T(s);

    // экран: логические пиксели, как CSS px в веб-версии
    public static double VW = 800, VH = 600, SC = 1;
    public static bool COARSE;          // сенсорный экран
    public static bool REDUCED = false; // reduced motion
    public static void Resize(double w, double h)
    {
        VW = Math.Max(1, w); VH = Math.Max(1, h);
        SC = clamp(Math.Min(VW, VH) / 500, .58, 1.3);
    }

    public static readonly GState S = new();
    public static Player P;
    public static List<Enemy> enemies = new();
    public static List<Bullet> bullets = new();
    public static List<Part> parts = new();
    public static List<Pickup> pickups = new();
    public static List<Nade> nades = new();
    public static List<Bomb> bombs = new();
    public static List<FText> texts = new();
    public static List<Hole> holes = new();
    public static List<Zone> zones = new(), strikes = new(), puddles = new(); // puddles — лужи от Кря-волны; strikes — зона яичного дождя
    public static List<Goo> goo = new();
    public static List<Ally> allies = new();
    public static Unit curTg;
    public static double camX, camY;

    public static Look lookFor(string heroId) // у каждого героя свой фирменный образ
    {
        var h = HEROES[heroId];
        return new Look { sp = h.sp, special = h.special, body = h.body, wing = h.wing, tail = h.tail ?? h.wing, head = h.head ?? h.body, hat = h.hat ?? "none" };
    }

    public static double allyRange(Ally a)
    {
        var W = WEAP[a.gun]; if (W.special == "rail") return 520; if (W.special == "mortar") return 380;
        return Math.Min(470, Math.Max(200, W.spd * W.life * (a.cls == "sniper" ? 1.35 : 1)));
    }

    public static void makeAllies()
    {
        var ids = squadIds();
        allies = ids.Select((id, i) =>
        {
            var H = HEROES[id]; int Lv = heroLv(id); var C = H.cls;
            double hpM = .75, armor = H.armor, spd = H.spd * .95;
            if (C == "tank") { hpM *= 1.25; armor += .1; spd *= .92; }
            if (C == "sniper") hpM *= .9; if (C == "assault") spd *= 1.1;
            double max = Math.Round(H.hp * (1 + .06 * (Lv - 1)) * hpM, MidpointRounding.AwayFromZero);
            double a = ((double)i / Math.Max(1, ids.Count)) * TAU + Math.PI / 2;
            var al = new Ally
            {
                id = id, cls = C, lv = Lv, x = P.x + Math.Cos(a) * 60, y = P.y + Math.Sin(a) * 60, r = H.r, hp = max, max = max, spd = spd, armor = armor,
                regen = H.regen + (C == "medic" ? 2 : 0), dmgMul = H.dmg * (1 + .05 * (Lv - 1)) * (C == "sniper" ? 1.2 : 1), rateMul = H.rate * (C == "assault" ? 1.1 : 1),
                look = lookFor(id), gun = gunForAlly(id), hop = id == "duck", kitT = 8, cd = rand(0, .4), ult = rand(10, 40), phase = rand(0, 6), lastHit = -9
            };
            al.range = allyRange(al); return al;
        }).ToList();
        if (S.mode == "menu")
            for (int i = 0; i < allies.Count; i++)
            {
                var a = allies[i]; int side = i % 2 == 1 ? 1 : -1, k = i / 2 + 1;
                a.x = P.x + side * (30 + k * 34); a.y = P.y - 4 - k * 10; a.ang = side > 0 ? 0 : Math.PI; a.face = side;
            }
    }

    /// <summary>Характеристики героя на уровне Lv (класс, прокачка и пассивки, которые меняют числа).</summary>
    public sealed class HStats { public double hp, dmg, rate, spd, armor, regen, range; }
    public static HStats heroStats(string id, int Lv)
    {
        var H = HEROES[id]; var C = H.cls;
        return new HStats
        {
            hp = Math.Round(H.hp * (1 + .06 * (Lv - 1)) * (C == "tank" ? 1.25 : C == "sniper" ? .9 : 1), MidpointRounding.AwayFromZero),
            dmg = H.dmg * (1 + .05 * (Lv - 1)) * (C == "sniper" ? 1.2 : 1),
            rate = H.rate * (C == "assault" ? 1.1 : 1) * (id == "guinea" && Lv >= 10 ? 1.15 : 1),
            spd = H.spd * (C == "assault" ? 1.1 : C == "tank" ? .92 : 1),
            armor = H.armor + (id == "duck" && Lv >= 5 ? .1 : 0) + (id == "goose" && Lv >= 10 ? .15 : 0) + (C == "tank" ? .1 : 0),
            regen = H.regen + (C == "medic" ? 1.2 : 0),
            range = C == "sniper" ? 1.35 : 1,
        };
    }
    // в подземелье стамина восстанавливается медленно и только когда 1,5 с не стреляешь — основное пополнение из кристаллов
    public const double STAM_MAX = 50, STAM_REGEN = 5, SK_STAM_MAX = 100, SK_STAM_REGEN = .6, SK_STAM_PAUSE = 1.5;

    public static Player newPlayer(string heroId = null)
    {
        var id = heroId ?? SAVE.hero; var H = HEROES[id]; int Lv = heroLv(id);
        var C = H.cls; var gun = gunOf(id); double medMul = id == "nurse" && Lv >= 10 ? 2 : 1;
        var st = heroStats(id, Lv);
        return new Player
        {
            hid = id, cls = C, hlv = Lv, x = WW / 2, y = WH / 2 + 40, r = H.r, hp = st.hp, max = st.hp,
            spd = st.spd, dmgMul = st.dmg, rateMul = st.rate, armor = st.armor, regen = H.regen, medMul = medMul,
            medic = C == "medic", rangeMul = st.range,
            nades = Math.Min(6, 2 + H.nade), hop = id == "duck", kitT = 8, prim = gun, w = gun, ammo = double.PositiveInfinity, magnet = 20,
            ult = 0, ultRate = (id == "chick" && Lv >= 10) ? 1.4 : 1, revive = id == "duck" && Lv >= 10,
            look = lookFor(id), lastHit = -9, face = 1, alive = true,
            stam = STAM_MAX, stamMax = STAM_MAX, stamRegen = STAM_REGEN
        };
    }
    public static bool hasP(int n) => P != null && P.hlv >= n;

    /* ---------- отложенные вызовы (setTimeout) ---------- */
    static readonly List<(double at, Action f)> timers = new();
    static double clock;
    public static void Later(double sec, Action f) => timers.Add((clock + sec, f));
    public static void TickTimers(double realDt)
    {
        clock += realDt;
        for (int i = 0; i < timers.Count; i++)
            if (timers[i].at <= clock) { var f = timers[i].f; timers.RemoveAt(i); i--; f(); }
    }
}
