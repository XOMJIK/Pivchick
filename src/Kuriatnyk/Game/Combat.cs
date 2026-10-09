using System;
using System.Collections.Generic;
using System.Linq;
using static Kuriatnyk.D;
using static Kuriatnyk.Store;

namespace Kuriatnyk;

// Бой: спавн, урон, отряд, убийства (порт первой половины web/js/game.js).
public static partial class G
{
    /* ---------- helpers ---------- */
    public static void collideWorld(Unit e, bool noObs = false)
    {
        if (!noObs) foreach (var o in OBS) { double dx = e.x - o.x, dy = e.y - o.y, d = hypot(dx, dy); if (d == 0) d = 1; double m = o.r + e.r * .85; if (d < m) { e.x = o.x + dx / d * m; e.y = o.y + dy / d * m; } }
        e.x = clamp(e.x, FENCE + e.r + 6, WW - FENCE - e.r - 6); e.y = clamp(e.y, FENCE + e.r + 12, WH - FENCE - e.r - 4);
        double px = double.NaN, py = 0; bool hop = false;
        if (e is Fighter f) { px = f._px; py = f._py; hop = f.hop; } else if (e is Enemy en) { px = en._px; py = en._py; hop = en.hop; }
        if (!noObs && RIVER != null && !double.IsNaN(px) && !hop) waterBlock(e, px, py); // курицы и куриные боссы перелетают воду
        if (DUN != null) dunCollide(e);
    }
    public static bool onScreen(Unit e, double m = 24)
    {
        double vw = VW / SC, vh = VH / SC;
        return e.x > camX + m && e.x < camX + vw - m && e.y > camY + m + 30 && e.y < camY + vh - m + 10;
    }
    public static Enemy nearestTarget()
    {
        Enemy bt = null; double bd = 1e9;
        foreach (var e in enemies)
        {
            if (e.dead || !onScreen(e, 0)) continue; var d = hypot(e.x - P.x, e.y - P.y); if (S.night && d > NIGHT_R + 20) continue;
            if (d < bd && (e.T.fly || los(P.x, P.y, e.x, e.y))) { bd = d; bt = e; }
        }
        return bt;
    }
    public static bool los(double ax, double ay, double bx, double by)
    {
        double vx = bx - ax, vy = by - ay, l2 = vx * vx + vy * vy; if (l2 == 0) l2 = 1;
        foreach (var o in OBS)
        {
            var t = clamp(((o.x - ax) * vx + (o.y - ay) * vy) / l2, 0, 1); double px = ax + vx * t - o.x, py = ay + vy * t - o.y;
            if (px * px + py * py < (o.r * .85) * (o.r * .85)) return false;
        }
        return true;
    }
    public static void feathers(double x, double y, string col, int n, double force = 1)
    {
        for (int i = 0; i < n; i++)
        {
            double a = rand(0, TAU), sp = rand(40, 160) * force;
            parts.Add(new Part { k = "feather", x = x, y = y, z = rand(14, 30), vx = Math.Cos(a) * sp, vy = Math.Sin(a) * sp * .6, vz = rand(60, 170) * force, rot = rand(0, TAU), vr = rand(-8, 8), t = 0, max = rand(1.2, 2.2), col = col });
        }
    }
    public static void puff(double x, double y, string col, int n, double size = 1)
    {
        for (int i = 0; i < n; i++)
        {
            double a = rand(0, TAU), sp = rand(10, 60);
            parts.Add(new Part { k = "puff", x = x + rand(-4, 4), y = y, z = rand(4, 20), vx = Math.Cos(a) * sp, vy = Math.Sin(a) * sp * .5, vz = rand(10, 40), t = 0, max = rand(.35, .7), r = rand(5, 10) * size, col = col });
        }
    }
    public static void splat(double x, double y, double size = 1) => parts.Add(new Part { k = "splat", x = x, y = y, t = 0, max = 3, r = rand(8, 12) * size, seed = rnd() });
    public static void ring(double x, double y, string col, double R = 130, double max = .35) => parts.Add(new Part { k = "ring", x = x, y = y, t = 0, max = max, R = R, col = col });
    public static void ftext(double x, double y, string txt, string col = "#fff", double size = 15) => texts.Add(new FText { x = x, y = y, txt = txt, col = col, size = size, t = 0, max = .8, vx = rand(-15, 15) });

    /* ---------- game flow ---------- */
    public static void startGame(string kind = "endless", int idx = 0)
    {
        if (kind == "sk") { startSK(S.skHero ?? SAVE.hero); return; } // «Ещё раз» и «Заново» в Soul Knight — новый забег из лагеря
        DUN = null;
        var mk2 = kind == "story" ? MAP_OF_CH[STORY[idx].ch] : "yard"; buildWorld(mk2); lastBridge = null;
        P = newPlayer(); makeAllies(); enemies = new(); bullets = new(); parts = new(); pickups = new(); nades = new(); bombs = new(); texts = new(); holes = new(); zones = new(); strikes = new(); puddles = new(); goo = new();
        S.holeT = rand(8, 12); addCrates(7);
        var Lv = kind == "story" ? STORY[idx] : null;
        S.mode = "play"; S.kind = kind; S.lvIdx = idx; S.L = Lv; S.diff = Lv?.diff ?? 1; S.night = Lv != null && Lv.night; S.t = 0; S.score = 0; S.wave = 0; S.toSpawn = 0; S.spawnT = 0;
        S.between = 1.2; S.running = false; S.kills = 0; S.goalKills = 0; S.survT = Lv != null && Lv.goal.type == "survive" ? Lv.goal.t : 0; S.boss = null; S.bossT = 0; S.shake = 0;
        S.combo = 0; S.comboT = 0; S.deadT = 0; S.winT = 0; S.coins = 0; S.tokens = 0; S.banked = false; S.cleanup = false; S.kamiNext = 0;
        sticks.move = sticks.aim = null; clearBanner(); Ui.ToastClear(); Music.Play("battle"); Music.Duck(1); // старый баннер «Волна N» не всплывает после перезапуска
        bossBarOn = false; Ui.OnGameStart();
        if (Lv != null) showBanner(T("Глава ") + (Lv.ch + 1) + " · " + (Lv.i + 1) + "/20", Lv.name);
    }
    public static bool isWaves() => S.kind == "endless" || S.L.goal.type == "waves";
    public static void bankCoins() { if (S.banked) return; S.banked = true; skCashPiv(); SAVE.coins += S.coins; SAVE.tokens += S.tokens; persist(); }
    /// <summary>Пивчики живут только в забеге: что не потратил, превращается в зёрна (2 пивчика = 1 зерно).</summary>
    public static void skCashPiv() { if (S.kind != "sk" || S.piv <= 0) return; S.coins += S.piv / 2; S.piv = 0; }
    static void startWave()
    {
        S.wave++;
        if (holesOn() && rnd() < .6) spawnHole();
        if (S.kind == "endless" && S.wave == 25 && !SAVE.eggMs.ContainsKey("sup25")) { SAVE.eggMs["sup25"] = 1; SAVE.schests++; persist(); Later(1.6, () => Ui.Toast(T("Волна 25! Сверхбольшой сундук ждёт в меню"))); }
        if (S.kind == "endless")
        {
            int ms = S.wave switch { 10 => 5, 15 => 10, 20 => 15, _ => 0 };
            if (ms > 0 && !SAVE.eggMs.ContainsKey("w" + S.wave)) { SAVE.eggMs["w" + S.wave] = 1; SAVE.eggs += ms; persist(); int w = S.wave; Later(.9, () => Ui.Toast(T("Волна ") + w + T(" впервые! +") + ms + T(" золотых яиц"))); }
        }
        if (S.kind == "endless") S.diff = S.wave;
        S.toSpawn = S.kind == "endless" ? Math.Min(4 + S.wave * 2, 40) : (int)Math.Min(Math.Round(4 + S.diff + S.wave * 2, MidpointRounding.AwayFromZero), 28); S.spawnT = .6;
        var sub = S.kind == "story" ? T("Волна ") + S.wave + T(" из ") + S.L.goal.n : (S.wave == 1 ? T("Защити курятник!") : S.wave == 2 ? T("Лисы почуяли запах") : S.wave == 3 ? T("Пришли индюки") : S.wave == 4 ? T("Вороны в небе!") : S.wave == 5 ? T("Еноты вышли на охоту") : T("Врагов всё больше"));
        bool bossWave = S.kind == "endless" && S.wave % 5 == 0; if (bossWave) { S.bossT = 2.2; S.toSpawn = (int)Math.Round(S.toSpawn * .5, MidpointRounding.AwayFromZero); } // каждые 5 волн — босс
        showBanner(S.kind == "story" ? S.L.name : T("Волна ") + S.wave, bossWave ? T("Волна босса!") : sub);
        Sfx.wave();
        if (S.wave > 1) addCrates(2); // разбитые ящики понемногу появляются снова
        if (S.wave > 1) { spawnPickupNear(new[] { "shotgun", "smg", "nade" }[S.wave % 3]); if (P.hid == "hen" && hasP(5)) { P.nades = Math.Min(6, P.nades + 1); ftext(P.x, P.y - 50, T("+1 яйцо"), "#ffc93a", 14); } }
    }
    static void spawnPickupNear(string kind)
    {
        for (int i = 0; i < 30; i++)
        {
            double a = rand(0, TAU), d = rand(140, 320); double x = P.x + Math.Cos(a) * d, y = P.y + Math.Sin(a) * d;
            if (x < FENCE + 40 || x > WW - FENCE - 40 || y < FENCE + 40 || y > WH - FENCE - 40) continue;
            if (OBS.Any(o => hypot(o.x - x, o.y - y) < o.r + 24) || nearWater(x, y, 24)) continue;
            pickups.Add(new Pickup { x = x, y = y, kind = kind, t = 16, ph = rand(0, 6) }); return;
        }
    }
    static string pickFrom(Dictionary<string, double> mix)
    {
        double tot = 0; foreach (var v in mix.Values) tot += v; double r = rnd() * tot;
        foreach (var kv in mix) { r -= kv.Value; if (r <= 0) return kv.Key; }
        return mix.Keys.First();
    }
    static string pickType()
    {
        if (S.kind == "story") return pickFrom(S.L.mix);
        int w = S.wave; var mix = new Dictionary<string, double> { ["hen"] = 1 };
        if (w >= 2) { mix["fox"] = .35 + w * .05; mix["rat"] = .2 + w * .03; }
        if (w >= 4) mix["bigrat"] = .12 + w * .02; if (w >= 3) mix["turkey"] = .12 + w * .035; if (w >= 4) mix["crow"] = .25 + w * .03; if (w >= 5) mix["raccoon"] = .2 + w * .03;
        if (w >= 6) mix["wolf"] = .2 + w * .03; if (w >= 7) mix["owl"] = .15 + w * .02; if (w >= 8) mix["ferret"] = .2 + w * .02; if (w >= 9) mix["robohen"] = .15 + w * .02; if (w >= 10) mix["eagle"] = .12 + w * .02;
        return pickFrom(mix);
    }
    public static (double x, double y) edgePoint(double r)
    {
        double x = 0, y = 0;
        for (int i = 0; i < 25; i++)
        {
            int side = (int)Math.Floor(rnd() * 4); double m = FENCE + 30;
            x = side < 2 ? rand(m, WW - m) : side == 2 ? m : WW - m; y = side >= 2 ? rand(m, WH - m) : side == 0 ? m + 10 : WH - m;
            if (hypot(x - P.x, y - P.y) > 430 && !OBS.Any(o => hypot(o.x - x, o.y - y) < o.r + r) && !nearWater(x, y, r + 16)) break;
        }
        return nearWater(x, y, r + 16) ? dryPoint(x, y, r + 24) : (x, y);
    }
    public static Enemy makeEnemy(string type, double x, double y)
    {
        var T = ETYPE[type]; double hpMul = T.boss ? 1 : 1 + (S.diff - 1) * .07;
        if (!T.fly && nearWater(x, y, T.r + 6)) (x, y) = dryPoint(x, y, T.r + 14); // никто не появляется в реке
        var e = new Enemy
        {
            type = type, T = T, x = x, y = y, r = T.r, hp = T.hp * hpMul, max = T.hp * hpMul, cd = rand(1.2, 2.2), face = 1, phase = rand(0, 6),
            strafe = rnd() < .5 ? 1 : -1, strafeT = rand(1, 3), st = "walk", atkT = 2.5, pat = -1
        };
        if (type == "hen") { var c = T.cols[(int)Math.Floor(rnd() * T.cols.Length)]; e.body = c[0]; e.wing = c[1]; }
        if (T.s > 0) e.s = T.s; if (T.hop) e.hop = true;
        if (DUN != null) e.room = DUN.active;
        enemies.Add(e); puff(x, y, "#e9dcc0", 8, T.boss ? 2 : 1.2); return e;
    }
    static void spawnEnemy(string type)
    {
        var T = ETYPE[type]; var (x, y) = edgePoint(T.r);
        if (type == "rat" || type == "bigrat")
        {
            int n = type == "rat" ? 3 + (int)Math.Floor(rnd() * 2) : 2;
            for (int i = 0; i < n; i++) makeEnemy(type, clamp(x + rand(-30, 30), FENCE + 20, WW - FENCE - 20), clamp(y + rand(-30, 30), FENCE + 20, WH - FENCE - 20));
        }
        else makeEnemy(type, x, y);
    }
    public static bool bossBarOn;
    static void spawnBoss(string type)
    {
        var (x, y) = dryPoint(P.x < WW / 2 ? WW - FENCE - 180 : FENCE + 180, P.y < WH / 2 ? WH - FENCE - 180 : FENCE + 180);
        S.boss = makeEnemy(type, x, y); S.boss.atkT = 2; Music.Play("boss");
        bossBarOn = true;
        showBanner(ETYPE[type].name, T("Босс вышел на поле!")); Sfx.gobble(); S.shake += 10;
    }
    static readonly string[] ENDLESS_BOSSES = { "gturkey", "ataman", "badger", "wolfboss", "steelturkey", "drferret", "eagleboss", "emperor" };
    static void spawnEndlessBoss()
    {
        int n = S.wave / 5; var type = ENDLESS_BOSSES[(n - 1) % ENDLESS_BOSSES.Length]; int cycle = (n - 1) / ENDLESS_BOSSES.Length;
        spawnBoss(type); var hp = Math.Round((600 + S.wave * 90) * (1 + cycle * .6), MidpointRounding.AwayFromZero);
        S.boss.hp = S.boss.max = hp; S.boss.endless = true;
    }
    public static void hurtPlayer(double d, string src)
    {
        if (!P.alive || P.inv > 0 || S.mode != "play") return;
        if (P.buff == "fort") { ftext(P.x, P.y - 50, T("Блок"), "#cfe8ff", 13); return; }
        if (src == "egg" && P.hid == "chick" && hasP(5) && rnd() < .15) { ftext(P.x, P.y - 50, T("Мимо!"), "#ffe27a", 14); return; }
        if (P.hid == "guinea" && hasP(5) && src != "self" && rnd() < .1) { ftext(P.x, P.y - 50, T("Уклон!"), "#cfe8ff", 14); return; }
        if (P.hid == "duck" && hasP(5) && src == "egg" && inWater(P.x, P.y)) { ftext(P.x, P.y - 40, T("Нырок!"), "#7fd4ff", 13); return; }
        if (P.buff == "cry") d *= .5;
        d *= 1 - P.armor;
        P.hp -= d; P.inv = .3; P.flash = .15; P.lastHit = S.t; S.shake += 7; Sfx.hurt(); Platform.Buzz(25);
        feathers(P.x, P.y, P.look.body, 3, .8);
        if (P.hp <= 0)
        {
            if (P.revive) { P.revive = false; P.hp = P.max * .5; P.inv = 1.5; ring(P.x, P.y, "#9cf27a", 140, .5); showBanner(T("Второе дыхание!"), T("Утка снова в строю")); Sfx.ult(); return; }
            P.hp = 0; P.alive = false; S.mode = "dying"; S.deadT = 1.3; feathers(P.x, P.y, P.look.body, 30, 1.4); puff(P.x, P.y, "#ffffff", 12, 1.6); Sfx.dead(); Platform.Buzz(120);
            sticks.move = sticks.aim = null; input.firing = false; P.buff = null;
        }
    }
    static void hurtAlly(Ally a, double d)
    {
        if (a.down > 0 || S.mode != "play") return;
        if (a.buff == "fort") return;
        d *= 1 - a.armor; a.hp -= d; a.flash = .12; a.lastHit = S.t; feathers(a.x, a.y, a.look.body, 2, .7);
        if (a.hp <= 0)
        {
            a.hp = 0; a.down = P.hid == "nurse" && hasP(5) ? 9 : 18; a.buff = null; feathers(a.x, a.y, a.look.body, 16, 1.1); puff(a.x, a.y, "#ffffff", 8, 1.1);
            ftext(a.x, a.y - 50, HEROES[a.id].name.Split(' ')[0] + T(" выбит"), "#ffb3a3", 12); Sfx.hurt();
        }
    }
    static void hitTarget(Unit tg, double d, Enemy e)
    {
        if (tg == null || tg == P) { hurtPlayer(d, "melee"); if (P.hid == "rooster" && hasP(5) && P.alive && e != null) hitEnemy(e, 20, e.x - P.x, e.y - P.y); }
        else if (tg is Ally a) hurtAlly(a, d);
    }
    static void updateAllies(double dt)
    {
        int n = allies.Count;
        for (int i = 0; i < allies.Count; i++)
        {
            var a = allies[i];
            if (a.down > 0)
            {
                a.down -= dt * ((P.medic && P.alive) || allies.Any(b => b != a && b.cls == "medic" && b.down <= 0) ? 2 : 1);
                if (a.down <= 0) { a.hp = a.max; a.x = clamp(P.x + rand(-40, 40), FENCE + 30, WW - FENCE - 30); a.y = clamp(P.y + rand(-40, 40), FENCE + 30, WH - FENCE - 30); puff(a.x, a.y, "#ffffff", 8, 1); ftext(a.x, a.y - 50, T("Снова в бою!"), "#9cf27a", 12); }
                continue;
            }
            if (a.flash > 0) a.flash -= dt;
            if (a.regen > 0 && (a.cls == "medic" || S.t - a.lastHit > 3)) a.hp = Math.Min(a.max, a.hp + a.regen * dt);
            if (a.cls == "medic") { double hm = a.id == "nurse" && a.lv >= 10 ? 2 : 1; healTeam(a.x, a.y, 2.5 * hm * dt, 240, true); }
            double oa = ((double)i / Math.Max(1, n)) * TAU + Math.PI / 2 + S.t * .15, fx = P.x + Math.Cos(oa) * 62, fy = P.y + Math.Sin(oa) * 52;
            double vx = fx - a.x, vy = fy - a.y; double fd = hypot(vx, vy); if (fd == 0) fd = 1;
            if (fd > 620) { a.x = fx; a.y = fy; puff(a.x, a.y, "#ffffff", 5, .8); continue; }
            Enemy t = null; double bd = S.night ? 220 : a.range;
            foreach (var e in enemies) { if (e.dead) continue; var d = hypot(e.x - a.x, e.y - a.y); if (d < bd && (e.T.fly || los(a.x, a.y, e.x, e.y))) { bd = d; t = e; } }
            a.cd -= dt;
            if (a.buff != null)
            {
                a.buffT -= dt;
                if (a.buff == "storm") stormAt(a, dt, .85, a.dmgMul * .8, false, false);
                if (a.buff == "adren")
                {
                    a.fireT -= dt;
                    if (a.fireT <= 0)
                    {
                        a.fireT = .32; var off = rand(0, TAU);
                        for (int k = 0; k < 8; k++) { var an = off + k / 8.0 * TAU; bullets.Add(new Bullet { x = a.x + Math.Cos(an) * 16, y = a.y + Math.Sin(an) * 16, vx = Math.Cos(an) * 500, vy = Math.Sin(an) * 500, r = 5, dmg = 13 * a.dmgMul, fromP = true, life = .5, max = .5, kind = "fire", ally = true, owner = a }); }
                    }
                }
                if (a.buff == "turbo" && rnd() < dt * 20) puff(a.x, a.y, "#ffe066", 1, .5);
                if (a.buffT <= 0) a.buff = null;
            }
            if (a.buff == null && a.ult >= 100)
            {
                int nearN = enemies.Count(e => !e.dead && hypot(e.x - a.x, e.y - a.y) < 260);
                bool needHeal = a.cls == "medic" && (P.hp < P.max * .6 || allies.Any(b => b.down > 0 || b.hp < b.max * .5));
                if (nearN >= 2 || (S.boss != null && nearN >= 1) || needHeal) allyUlt(a);
            }
            if (t != null) { a.ang = Math.Atan2(t.y - a.y, t.x - a.x); if (a.cd <= 0) allyFire(a, a.ang, t); }
            else if (fd > 8) a.ang = Math.Atan2(vy, vx);
            double ox = a.x, oy = a.y; a._px = ox; a._py = oy;
            if (!a.hop)
            {
                var nt = navTarget(a, fx, fy);
                if (nt != null && fd > 40) { vx = nt.Value.x - a.x; vy = nt.Value.y - a.y; double nd = hypot(vx, vy); if (nd == 0) nd = 1; vx = vx / nd * fd; vy = vy / nd * fd; }
            }
            if (fd > 26) { double sp = a.spd * (fd > 180 ? 1.35 : 1) * Math.Min(1, fd / 60) * (a.buff == "turbo" ? 1.8 : 1); a.x += vx / fd * sp * dt; a.y += vy / fd * sp * dt; a.moving = true; a.phase += dt * 15; }
            else a.moving = false;
            foreach (var e in enemies) { if (e.dead || e.T.fly) continue; double dx = a.x - e.x, dy = a.y - e.y, d = hypot(dx, dy); if (d == 0) d = 1; double m = a.r + e.r; if (d < m) { a.x += dx / d * (m - d) * .5; a.y += dy / d * (m - d) * .5; } }
            foreach (var b2 in allies) { if (b2 == a || b2.down > 0) continue; double dx = a.x - b2.x, dy = a.y - b2.y, d = hypot(dx, dy); if (d == 0) d = 1; double m = a.r + b2.r; if (d < m) { a.x += dx / d * (m - d) * .5; a.y += dy / d * (m - d) * .5; } }
            { double dx = a.x - P.x, dy = a.y - P.y, d = hypot(dx, dy); if (d == 0) d = 1; double m = a.r + P.r; if (d < m) { a.x += dx / d * (m - d); a.y += dy / d * (m - d); } }
            collideWorld(a);
            a.vx = (a.x - ox) / Math.Max(dt, .001); a.vy = (a.y - oy) / Math.Max(dt, .001);
            a.face = Math.Cos(a.ang) >= 0 ? 1 : -1;
        }
    }
    static void addUlt(double v) { if (P == null || P.buff != null || !P.alive) return; P.ult = Math.Min(100, P.ult + v * P.ultRate); }
    public static void hitEnemy(Enemy e, double d, double vx, double vy, bool fromP = true, double kbMul = 1, bool quiet = false)
    {
        if (e.dead || e.emerge > 0) return;
        bool crit = false;
        if (fromP && ((P.hid == "rooster" && hasP(10) && rnd() < .15) || (P.crit > 0 && rnd() < P.crit))) { d *= 2; crit = true; }
        if (fromP && P.hid == "adren" && hasP(5) && P.hp < P.max * .5) d *= 1.3;
        if (fromP && P.hid == "quail" && hasP(5) && hypot(e.x - P.x, e.y - P.y) > 250) d *= 1.25;
        e.hp -= d; e.flash = .09; double l = hypot(vx, vy); if (l == 0) l = 1; double kb = (e.T.boss ? 8 : 90) * kbMul; e.kx += vx / l * kb; e.ky += vy / l * kb;
        if (!quiet || crit)
        {
            var dr = Math.Round(d, MidpointRounding.AwayFromZero).ToString(System.Globalization.CultureInfo.InvariantCulture);
            ftext(e.x, e.y - 44 * (e.s > 0 ? e.s : 1), crit ? T("КРИТ ") + dr : dr, crit ? "#ffc93a" : "#fff", crit ? 16 : 13); Sfx.hit();
        }
        if (fromP) addUlt(d * .16);
        if (!quiet && rnd() < .4) feathers(e.x, e.y, featherCol(e), 1, .6);
        if (e.hp <= 0) { if (e.kami) { e.dead = true; kamiBoom(e, false); killEnemy(e, true); } else killEnemy(e); return; }
        // камикадзе: мелкий шанс, что раненый враг пойдёт на таран
        if (!quiet && !e.kami && !e.kamiRolled && e.hp < e.max * .3)
        {
            e.kamiRolled = true; var T0 = e.T;
            if (!T0.boss && !T0.fly && !(e.emerge > 0) && S.t > S.kamiNext && !enemies.Any(f => f.kami && !f.dead) && rnd() < .05)
            { S.kamiNext = S.t + 12; e.kami = true; e.kamiT = 4.5; e.st = "walk"; e.burst = 0; ftext(e.x, e.y - 60 * (e.s > 0 ? e.s : 1), T("КАМИКАДЗЕ!"), "#ff5a3c", 14); Sfx.fuse(); }
        }
    }
    static readonly Dictionary<string, string> FCOL = new()
    {
        ["fox"] = "#e8742a", ["turkey"] = "#6b4428", ["gturkey"] = "#6b4428", ["ataman"] = "#e8742a", ["crow"] = "#2e2a33", ["raccoon"] = "#8b8f98", ["rat"] = "#a8a097", ["bigrat"] = "#5b4f47",
        ["wolf"] = "#7f8794", ["wolfboss"] = "#5d6470", ["owl"] = "#8a6a45", ["ferret"] = "#d8b48a", ["drferret"] = "#d8b48a", ["robohen"] = "#9aa3ad", ["steelturkey"] = "#9aa3ad",
        ["eagle"] = "#6b4a2b", ["eagleboss"] = "#5a3a1e", ["emperor"] = "#2a2238", ["badger"] = "#8d8f94", ["mole"] = "#6b5a52",
    };
    static string featherCol(Enemy e) => e.type == "hen" ? (e.body ?? "#fff") : FCOL.TryGetValue(e.type, out var c) ? c : "#fff";
    static void kamiBoom(Enemy e, bool contact)
    {
        blast(e.x, e.y, 78, eDmg(contact ? 24 : 18), false, false);
        foreach (var f in enemies.ToList()) { if (f == e || f.dead) continue; var d = hypot(f.x - e.x, f.y - e.y); if (d < 78 + f.r * .5) hitEnemy(f, 40 * (1 - d / 110), f.x - e.x, f.y - e.y, true); }
        ring(e.x, e.y, "#ff7a1a", 90, .4); puff(e.x, e.y, "#ff9a1f", 10, 1.6);
    }
    static void killEnemy(Enemy e, bool force = false)
    {
        if (e.dead && !force) return;
        e.dead = true; S.kills++; S.combo++; S.comboT = 2.4;
        var Tt = e.T;
        if (e.burn > 0) foreach (var f in enemies) if (!f.dead && f != e && hypot(f.x - e.x, f.y - e.y) < 85) { f.burn = Math.Max(f.burn, 2); f.burnDps = Math.Max(f.burnDps, e.burnDps > 0 ? e.burnDps : 9); } // огонь перекидывается на соседей
        if (P.buff == "adren" && !Tt.boss)
        {
            ring(e.x, e.y, "#ff7a1a", 90, .35); puff(e.x, e.y, "#ff7a1a", 6, 1.1);
            foreach (var f in enemies.ToList()) if (!f.dead && f != e && hypot(f.x - e.x, f.y - e.y) < 90) { f.burn = 2; f.burnDps = 10 * P.dmgMul; hitEnemy(f, 30 * P.dmgMul, f.x - e.x, f.y - e.y, true, 1, true); }
        }
        int mult = Math.Min(4, 1 + (int)Math.Floor((S.combo - 1) / 3.0));
        long pts = (long)(Tt.score * mult); double rk = e.endless ? .4 : 1; S.score += pts; S.coins += (long)Math.Round(Tt.coin * rk, MidpointRounding.AwayFromZero); S.tokens += (long)Math.Round(Tt.tok * rk, MidpointRounding.AwayFromZero);
        ftext(e.x, e.y - 56, "+" + pts, mult > 1 ? "#ffc93a" : "#fff", 17);
        addUlt(5);
        if (P.hid == "adren" && hasP(10) && P.alive) P.hp = Math.Min(P.max, P.hp + 3);
        if (P.vamp > 0 && P.alive) P.hp = Math.Min(P.max, P.hp + P.vamp);
        if (P.hid == "quail" && hasP(10) && hypot(e.x - P.x, e.y - P.y) > 250) P.ult = Math.Min(100, P.ult + 10);
        var tp = e.type;
        if (tp is "fox" or "ataman" or "wolf" or "wolfboss") { puff(e.x, e.y, tp.StartsWith("wolf") ? "#b9bfc8" : "#f09a55", 14, Tt.boss ? 2.4 : 1.3); feathers(e.x, e.y, featherCol(e), 6, 1); Sfx.yelp(); }
        else if (tp is "raccoon" or "rat" or "bigrat" or "ferret" or "drferret" or "badger" or "mole") { puff(e.x, e.y, "#c9c4bb", tp == "rat" ? 6 : Tt.boss ? 24 : 12, Tt.boss ? 2.2 : 1); Sfx.squeak(); }
        else if (tp == "robohen" || tp == "steelturkey") { puff(e.x, e.y, "#c9d0d8", Tt.boss ? 26 : 12, Tt.boss ? 2.2 : 1.1); puff(e.x, e.y, "#ffc93a", 6, .8); Sfx.zap(); }
        else
        {
            feathers(e.x, e.y, featherCol(e), Tt.boss ? 50 : tp == "turkey" ? 26 : 18, Tt.boss ? 1.8 : 1.2); puff(e.x, e.y, "#ffffff", 8, 1.1);
            if (tp is "crow" or "eagle" or "eagleboss" or "emperor") Sfx.caw(); else if (tp == "owl") Sfx.hoot(); else if (tp is "turkey" or "gturkey") Sfx.gobble(); else Sfx.cluck();
        }
        if (Tt.boss) { S.shake += 18; Sfx.boom(); ring(e.x, e.y, "#fff6c8", 200, .6); }
        double r = rnd(), cornCh = .06 * (P.hid == "hen" && hasP(10) ? 1.5 : 1);
        bool sk = S.kind == "sk";
        if ((e.type != "rat" || rnd() < .3) && !nearWater(e.x, e.y, 12))
        {
            if (r < cornCh) pickups.Add(new Pickup { x = e.x, y = e.y, kind = "corn", t = 12, ph = 0 });
            else if (r < cornCh + .05 && !sk) pickups.Add(new Pickup { x = e.x, y = e.y, kind = rnd() < .5 ? "shotgun" : "smg", t = 12, ph = 0 }); // стволов сюжета в подземелье нет
            else if (r < cornCh + .09) pickups.Add(new Pickup { x = e.x, y = e.y, kind = "nade", t = 12, ph = 0 });
            else if (r < cornCh + .09 + (sk ? .08 : .04)) pickups.Add(new Pickup { x = e.x + rand(-8, 8), y = e.y, kind = "stam", t = 12, ph = 0 }); // стамина падает нечасто
        }
        if (sk) dropPiv(e.x, e.y, Tt.boss ? 30 : e.type == "rat" ? (rnd() < .5 ? 1 : 0) : 1 + (rnd() < .45 ? 1 : 0) + (Tt.hp >= 100 ? 1 : 0)); // пивчики
        if (S.kind == "story" && S.mode == "play")
        {
            var g = S.L.goal;
            if (g.type == "kill" && (g.what == "any" || g.what == e.type)) { S.goalKills++; if (S.goalKills >= g.n) victory(); }
            if (g.type == "boss" && e == S.boss) { S.boss = null; bossBarOn = false; victory(); }
        }
    }
}
