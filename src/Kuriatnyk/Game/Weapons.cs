using System;
using System.Collections.Generic;
using System.Linq;
using static Kuriatnyk.D;
using static Kuriatnyk.Store;

namespace Kuriatnyk;

// Оружие, гранаты, ульты, отряд, кротовые ямки (порт второй половины web/js/game.js).
public static partial class G
{
    static double aimAssist(double ang)
    {
        double bestA = ang, bd = .26;
        foreach (var e in enemies)
        {
            if (e.dead) continue; double dx = e.x - P.x, dy = e.y - P.y, d = hypot(dx, dy); if (d > 540) continue;
            var a = Math.Atan2(dy, dx); var df = Math.Abs(((a - ang + Math.PI * 3) % TAU) - Math.PI);
            if (df < bd && (e.T.fly || los(P.x, P.y, e.x, e.y))) { bd = df; bestA = a; }
        }
        return bestA;
    }
    static Bullet pBullet(double x, double y, double a, double sp, double dmg, double r, double life, string kind, int pierce = 0, int bounce = 0, int shape = 0)
    {
        var b = new Bullet { x = x, y = y, vx = Math.Cos(a) * sp, vy = Math.Sin(a) * sp, r = r, dmg = dmg, fromP = true, life = life, max = life, kind = kind, pierce = pierce, hits = pierce > 0 ? new() : null, bounce = bounce, shape = shape };
        bullets.Add(b); return b;
    }
    static void shoot()
    {
        var W = WEAP[P.w]; double rm = P.rateMul * (P.buff == "turbo" ? 2 : 1);
        double rate = W.rate; if (W.special == "mini") { P.spin = Math.Min(1, P.spin + .09); rate = .22 - (.22 - W.rate) * P.spin; }
        P.cd = rate / rm;
        double ang = input.mouse ? P.ang : aimAssist(P.ang); P.ang = ang;
        double wm = P.w == P.prim ? wMul(P.w) : 1;
        if (W.special == "melee") meleeSwing(ang, W.dmg * P.dmgMul);
        else if (W.special == "rail") fireRail(ang);
        else if (W.special == "mortar")
        {
            double tx, ty; var t = nearestTarget();
            if (input.mouse && input.firing) { tx = camX + input.mx / SC; ty = camY + input.my / SC; }
            else if (t != null) { tx = t.x; ty = t.y; }
            else { tx = P.x + Math.Cos(ang) * 260; ty = P.y + Math.Sin(ang) * 260; }
            var d = hypot(tx - P.x, ty - P.y); if (d > 380) { tx = P.x + (tx - P.x) / d * 380; ty = P.y + (ty - P.y) / d * 380; }
            bombs.Add(new Bomb { x = clamp(tx, FENCE + 10, WW - FENCE - 10), y = clamp(ty, FENCE + 10, WH - FENCE - 10), t = .55, max = .55, r = 72, dmg = W.dmg * P.dmgMul * wm, fromP = true, heal = W.heal, lob = true, sx = P.x, sy = P.y });
        }
        else if (W.special == "cat" && (++P.catN) % 6 == 0)
        {
            var b = pBullet(P.x + Math.Cos(ang) * 22, P.y + Math.Sin(ang) * 22, ang, W.spd * .8, W.dmg * 3 * P.dmgMul * wm, 8, W.life * 1.3 * P.rangeMul, "yarn", 2); b.spin = 0; Sfx.meow();
        }
        else
        {
            var kind = W.special == "flame" ? "flame" : W.special == "ice" ? "ice" : P.w;
            for (int i = 0; i < W.pellets; i++)
            {
                double a = ang + (rnd() - .5) * W.spread, sp = W.spd * (W.pellets > 1 || kind == "flame" ? rand(.85, 1.12) : 1);
                double r = kind == "flame" ? 7 : W.pellets > 1 ? 3.5 : (P.w is "sheriff" or "crossbow" or "cornrifle" ? 5 : 4.5);
                double life = W.life * P.rangeMul * (W.pellets > 1 || kind == "flame" ? rand(.8, 1.1) : 1);
                pBullet(P.x + Math.Cos(ang) * 22, P.y + Math.Sin(ang) * 22, a, sp, W.dmg * P.dmgMul * wm, r, life, kind, kind == "flame" ? 99 : W.pierce, W.bounce, W.bounce > 0 ? i % 4 : 0);
            }
        }
        if (P.hid == "guinea" && (++P.gN) % 8 == 0) for (int k = -2; k <= 2; k++) pBullet(P.x + Math.Cos(ang) * 22, P.y + Math.Sin(ang) * 22, ang + k * .16, 720, 18 * P.dmgMul, 4.5, .6 * P.rangeMul, "pfeather", 1);
        if (P.hid == "adren" && (++P.aN) % 5 == 0) pBullet(P.x + Math.Cos(ang) * 22, P.y + Math.Sin(ang) * 22, ang, 620, 30 * P.dmgMul, 7, .75, "fireball");
        if (W.special != "flame" && W.special != "melee")
        {
            double fl = (P.w == "shotgun" || P.w == "sawed" || P.w == "rail") ? 1.5 : 1;
            parts.Add(new Part { k = "flash", x = P.x + Math.Cos(ang) * 28 * fl, y = P.y + Math.Sin(ang) * 28 * fl, z = 19, t = 0, max = .06, a = ang, s = fl });
        }
        Sfx.Play(W.sfx); S.shake += W.kick * .5; P.x -= Math.Cos(ang) * W.kick * .6; P.y -= Math.Sin(ang) * W.kick * .6;
        if (!double.IsInfinity(P.ammo)) { P.ammo--; if (P.ammo <= 0) { P.w = P.prim; P.ammo = double.PositiveInfinity; ftext(P.x, P.y - 50, T("Патроны кончились"), "#ffc93a", 13); } }
    }
    static void fireRail(double ang) => fireBeam(P.x, P.y, ang, WEAP["rail"].dmg * P.dmgMul * wMul("rail"), true);
    static void fireBeam(double sx, double sy, double ang, double dmg, bool fromP, Fighter owner = null)
    {
        double ca = Math.Cos(ang), sa = Math.Sin(ang), x0 = sx + ca * 22, y0 = sy + sa * 22;
        double Lb = 1000;
        if (ca > 1e-4) Lb = Math.Min(Lb, (WW - FENCE - x0) / ca); if (ca < -1e-4) Lb = Math.Min(Lb, (FENCE - x0) / ca);
        if (sa > 1e-4) Lb = Math.Min(Lb, (WH - FENCE - y0) / sa); if (sa < -1e-4) Lb = Math.Min(Lb, (FENCE - y0) / sa);
        Lb = Math.Max(40, Lb);
        double x1 = x0 + ca * Lb, y1 = y0 + sa * Lb;
        foreach (var e in enemies.ToList())
        {
            if (e.dead) continue; var t = clamp((e.x - x0) * ca + (e.y - y0) * sa, 0, Lb); double px = x0 + ca * t - e.x, py = y0 + sa * t - e.y;
            if (px * px + py * py < (e.r + 9) * (e.r + 9)) { hitEnemy(e, dmg, ca, sa, fromP, 2); puff(e.x, e.y, "#bff4ff", 4, .8); if (owner != null) owner.ult = Math.Min(100, owner.ult + dmg * .08); }
        }
        parts.Add(new Part { k = "beam", x = x0, y = y0, x2 = x1, y2 = y1, t = 0, max = .3 });
        for (int i = 0; i < 12; i++) { var t = rand(0, Lb); parts.Add(new Part { k = "puff", x = x0 + ca * t, y = y0 + sa * t, z = 19, vx = rand(-20, 20), vy = rand(-20, 20), vz = rand(-10, 20), t = 0, max = rand(.25, .5), r = rand(2, 4), col = "#bff4ff" }); }
        Sfx.rail();
    }
    static void eBullet(double x, double y, double a, double sp, double dmg, string kind = "egg")
    {
        kind ??= "egg";
        bullets.Add(new Bullet { x = x, y = y, vx = Math.Cos(a) * sp, vy = Math.Sin(a) * sp, r = kind == "fire" ? 6 : kind == "rock" ? 7 : kind == "pellet" ? 3.5 : 5, dmg = dmg, fromP = false, life = 2.4, kind = kind });
    }
    static double eDmg(double v) => v * (1.3 + (S.diff - 1) * .07); // урон врагов: база ×1.3 и +7% за ступень сложности
    static void enemyShoot(Enemy e, ET Tt, int spreadN, double gap = .22, string kind = null)
    {
        var tg = curTg ?? P; double lead = hypot(tg.x - e.x, tg.y - e.y) / Tt.bspd * rand(.3, .9);
        double vx = tg == P ? P.svx : tg.vx, vy = tg == P ? P.svy : tg.vy, tx = tg.x + vx * lead, ty = tg.y + vy * lead;
        var bas = Math.Atan2(ty - e.y, tx - e.x) + rand(-.07, .07);
        for (int i = 0; i < spreadN; i++) { var a = bas + (i - (spreadN - 1) / 2.0) * gap; eBullet(e.x + Math.Cos(a) * 20, e.y + Math.Sin(a) * 20, a, Tt.bspd, eDmg(Tt.dmg), kind ?? Tt.bul ?? "egg"); }
        e.ang = bas; Sfx.egg();
        parts.Add(new Part { k = "flash", x = e.x + Math.Cos(bas) * 26, y = e.y + Math.Sin(bas) * 26, z = 19, t = 0, max = .05, a = bas, s = .8 });
    }
    public static void throwNade()
    {
        if (S.mode != "play" || !P.alive || P.nades <= 0) return;
        P.nades--; Sfx.throw_();
        double ang = P.ang, dist = 240;
        if (input.mouse && !pcAuto()) { double wx = camX + input.mx / SC, wy = camY + input.my / SC; ang = Math.Atan2(wy - P.y, wx - P.x); dist = clamp(hypot(wx - P.x, wy - P.y), 80, 320); }
        else { var t = nearestTarget(); if (t != null) { ang = Math.Atan2(t.y - P.y, t.x - P.x); dist = clamp(hypot(t.x - P.x, t.y - P.y), 80, 320); } else ang = aimAssist(ang); }
        double tt = .7;
        nades.Add(new Nade { x = P.x, y = P.y, z = 24, vx = Math.Cos(ang) * dist / tt, vy = Math.Sin(ang) * dist / tt, vz = 240, t = 1.05, rot = 0 });
    }
    static void healFx(Unit t) { t._hf--; if (t._hf > 0) return; t._hf = 8; parts.Add(new Part { k = "plus", x = t.x + rand(-10, 10), y = t.y, z = rand(34, 46), vz = 34, t = 0, max = .7 }); }
    static void healTeam(double cx, double cy, double v, double R = 240, bool inclP = true) // лечит игрока и всех живых бойцов отряда в радиусе
    {
        if (v == 0) return;
        if (inclP && P != null && P.alive && P.hp < P.max && hypot(P.x - cx, P.y - cy) < R) { P.hp = Math.Min(P.max, P.hp + v); healFx(P); }
        foreach (var a in allies) if (a.down <= 0 && a.hp < a.max && hypot(a.x - cx, a.y - cy) < R) { a.hp = Math.Min(a.max, a.hp + v); healFx(a); }
    }
    static void healHit(double v) { if (P == null || !P.alive || v == 0) return; healTeam(P.x, P.y, v, 240, true); }
    static void blast(double x, double y, double R, double dmg, bool fromP, bool selfDmg, double heal = 0)
    {
        S.shake += R > 100 ? 16 : 9; Sfx.boom(); Platform.Buzz(40);
        int n = R > 100 ? 16 : 9;
        for (int i = 0; i < n; i++) { double a = rand(0, TAU), sp = rand(60, 260) * R / 130; parts.Add(new Part { k = "shell", x = x, y = y, z = 10, vx = Math.Cos(a) * sp, vy = Math.Sin(a) * sp * .6, vz = rand(120, 300), rot = rand(0, TAU), vr = rand(-10, 10), t = 0, max = rand(.8, 1.3) }); }
        puff(x, y, "#fff7d6", R > 100 ? 18 : 10, R / 60); puff(x, y, "#ffc93a", R > 100 ? 10 : 6, R / 75);
        ring(x, y, fromP ? "#fff6c8" : "#ffb3a3", R, .35); splat(x, y, R / 45);
        if (fromP)
            for (int i = 0; i < enemies.Count; i++)
            {
                var e = enemies[i]; if (e.dead) continue; var d = hypot(e.x - x, e.y - y);
                if (d < R + e.r * .5)
                {
                    hitEnemy(e, Math.Round(dmg * (1 - d / (R * 1.35)), MidpointRounding.AwayFromZero), e.x - x, e.y - y); if (heal != 0) healHit(heal);
                    if (!e.T.boss) { var dd = d == 0 ? 1 : d; e.kx += (e.x - x) / dd * 260; e.ky += (e.y - y) / dd * 260; }
                }
            }
        var dp = hypot(P.x - x, P.y - y);
        if (!fromP && dp < R + P.r * .5) hurtPlayer(dmg, "bomb");
        if (!fromP) foreach (var a in allies) if (a.down <= 0 && hypot(a.x - x, a.y - y) < R + a.r * .5) hurtAlly(a, dmg * .7);
        if (fromP && selfDmg && dp < 85) hurtPlayer(Math.Round(18 * (1 - dp / 110), MidpointRounding.AwayFromZero), "self");
    }
    static void collect(Pickup p)
    {
        Sfx.pick();
        if (p.kind == "corn") { double heal = P.hid == "hen" && hasP(10) ? 40 : 20; var h = Math.Min(heal, P.max - P.hp); P.hp = Math.Min(P.max, P.hp + heal); ftext(P.x, P.y - 50, "+" + Math.Max(Math.Round(h, MidpointRounding.AwayFromZero), 0) + T(" здоровья"), "#7df05a", 14); }
        else if (p.kind == "nade") { P.nades = Math.Min(6, P.nades + 1); ftext(P.x, P.y - 50, T("+1 яйцо"), "#ffc93a", 14); }
        else { var W = WEAP[p.kind]; if (P.w == p.kind) P.ammo += W.ammo; else { P.w = p.kind; P.ammo = W.ammo; } ftext(P.x, P.y - 50, W.name, "#ffc93a", 15); }
    }

    /* ---------- ultimates ---------- */
    public static void useUlt()
    {
        if (S.mode != "play" || !P.alive || P.ult < 100 || P.buff != null) return;
        P.ult = 0; Sfx.ult(); Platform.Buzz(50);
        var id = P.hid; var U = HEROES[id].ult[0];
        ftext(P.x, P.y - 62, U.ToUpper(L.Culture), "#ffc93a", 16);
        if (id == "hen") { var (cx, cy) = strikeCenter(P, enemies.Where(e => !e.dead && onScreen(e, 0)).ToList()); eggStrike(cx, cy, 8, 70 * P.dmgMul, 72); }
        else if (id == "chick") { P.buff = "turbo"; P.buffT = 4; ring(P.x, P.y, "#ffe066", 120, .4); }
        else if (id == "rooster")
        {
            showBanner(T("КУКАРЕКУ!"), ""); Sfx.crow(); S.shake += 10; P.buff = "cry"; P.buffT = 4;
            ring(P.x, P.y, "#ff6b4a", 280, .5); ring(P.x, P.y, "#ffc93a", 200, .4);
            for (int i = 0; i < enemies.Count; i++)
            {
                var e = enemies[i]; if (e.dead) continue; var d = hypot(e.x - P.x, e.y - P.y);
                if (d < 280) { hitEnemy(e, 45 * P.dmgMul, e.x - P.x, e.y - P.y); e.stun = e.T.boss ? .8 : 2.5; if (!e.T.boss) { var dd = d == 0 ? 1 : d; e.kx += (e.x - P.x) / dd * 300; e.ky += (e.y - P.y) / dd * 300; } }
            }
        }
        else if (id == "duck")
        {
            var h = Math.Min(P.max * .5, P.max - P.hp); P.hp += h; ftext(P.x, P.y - 50, "+" + Math.Round(h, MidpointRounding.AwayFromZero), "#7df05a", 16);
            foreach (var a in allies) if (!(a.down > 0) && hypot(a.x - P.x, a.y - P.y) < 260) a.hp = Math.Min(a.max, a.hp + a.max * .5);
            ring(P.x, P.y, "#7fd4ff", 240, .5); ring(P.x, P.y, "#ffffff", 160, .4);
            for (int i = bullets.Count - 1; i >= 0; i--) { var b = bullets[i]; if (!b.fromP && hypot(b.x - P.x, b.y - P.y) < 340) { puff(b.x, b.y, "#ffffff", 2, .5); bullets.RemoveAt(i); } }
            puddles.Add(new Zone { x = P.x, y = P.y, r = 150, t = 5, max = 5 });
            for (int i = 0; i < enemies.Count; i++)
            {
                var e = enemies[i]; if (e.dead) continue; var d = hypot(e.x - P.x, e.y - P.y);
                if (d < 240) { hitEnemy(e, 30 * P.dmgMul, e.x - P.x, e.y - P.y); if (!e.T.boss) { var dd = d == 0 ? 1 : d; e.kx += (e.x - P.x) / dd * 420; e.ky += (e.y - P.y) / dd * 420; } }
            }
        }
        else if (id == "goose") { P.buff = "fort"; P.buffT = 5; ring(P.x, P.y, "#cfe8ff", 100, .4); }
        else if (id == "quail")
        {
            var tg = enemies.Where(e => !e.dead && onScreen(e, 0)).OrderByDescending(e => e.hp).Take(3).ToList();
            if (tg.Count == 0) fireBeam(P.x, P.y, P.ang, 200 * P.dmgMul, true);
            foreach (var e in tg) fireBeam(P.x, P.y, Math.Atan2(e.y - P.y, e.x - P.x), 200 * P.dmgMul, true);
            S.shake += 8; P.buff = "shadow"; P.buffT = 3; puff(P.x, P.y, "#3b3550", 10, 1.2);
        }
        else if (id == "guinea") { P.buff = "storm"; P.buffT = 4; ring(P.x, P.y, "#cfe8ff", 120, .4); }
        else if (id == "nurse") { P.buff = "hosp"; P.buffT = 6; zones.Add(new Zone { x = P.x, y = P.y, t = 6, r = 130 }); ring(P.x, P.y, "#9cf27a", 140, .5); }
        else if (id == "adren") { P.buff = "adren"; P.buffT = 6 + (hasP(10) ? 2 : 0); P.fireT = 0; ring(P.x, P.y, "#ff4d2e", 200, .5); S.shake += 8; }
    }
    static void updateBuff(double dt)
    {
        if (P.buff == null) return;
        P.buffT -= dt;
        if (P.buff == "turbo" && rnd() < dt * 30) puff(P.x, P.y, "#ffe066", 1, .6);
        if (P.buff == "adren")
        {
            P.fireT -= dt; if (rnd() < dt * 25) parts.Add(new Part { k = "puff", x = P.x + rand(-12, 12), y = P.y, z = rand(5, 40), vx = 0, vy = 0, vz = 60, t = 0, max = .4, r = rand(3, 6), col = rnd() < .5 ? "#ff7a1a" : "#ffd23a" });
            if (P.fireT <= 0) { P.fireT = .28; var off = rand(0, TAU); for (int i = 0; i < 10; i++) { var a = off + i / 10.0 * TAU; pBullet(P.x + Math.Cos(a) * 16, P.y + Math.Sin(a) * 16, a, 520, 16 * P.dmgMul, 5, .55, "fire"); } Sfx.pop(); }
        }
        if (P.buff == "storm") stormAt(P, dt, 1, P.dmgMul, true, true);
        if (P.buffT <= 0)
        {
            var was = P.buff; P.buff = null;
            if (was == "storm") shockwave(P, 190, 60 * P.dmgMul, 520, "#ffffff");      // вихрь раскидывает врагов
            if (was == "fort") shockwave(P, 200, 70 * P.dmgMul, 420, "#cfe8ff");       // крепость бьёт ударной волной
            if (was == "shadow") puff(P.x, P.y, "#3b3550", 8, 1);
        }
    }
    static void shockwave(Unit c, double R, double dmg, double kb, string col)
    {
        ring(c.x, c.y, col, R, .5); ring(c.x, c.y, "#ffc93a", R * .7, .4); S.shake += 10; Sfx.boom();
        for (int i = 0; i < enemies.Count; i++)
        {
            var e = enemies[i]; if (e.dead) continue; var d = hypot(e.x - c.x, e.y - c.y);
            if (d < R + e.r) { hitEnemy(e, dmg, e.x - c.x, e.y - c.y); if (!e.T.boss) { var dd = d == 0 ? 1 : d; e.kx += (e.x - c.x) / dd * kb; e.ky += (e.y - c.y) / dd * kb; } }
        }
    }
    static void stormAt(Unit c, double dt, double R, double dmgMul, bool fromP, bool pull)
    {
        double rad = 115 * R;
        if (pull) foreach (var e in enemies) { if (e.dead || e.T.boss) continue; var d = hypot(e.x - c.x, e.y - c.y); if (d < 280 && d > 40) { e.kx -= (e.x - c.x) / d * 520 * dt; e.ky -= (e.y - c.y) / d * 520 * dt; } }
        for (int i = 0; i < enemies.Count; i++)
        {
            var e = enemies[i]; if (e.dead) continue; var d = hypot(e.x - c.x, e.y - c.y);
            if (d < rad + e.r) { hitEnemy(e, 70 * dmgMul * dt, e.x - c.x, e.y - c.y, fromP, 0, true); if (!e.T.boss) { var dd = d == 0 ? 1 : d; e.kx += (e.x - c.x) / dd * 900 * dt; e.ky += (e.y - c.y) / dd * 900 * dt; } }
        }
        for (int i = bullets.Count - 1; i >= 0; i--) { var b = bullets[i]; if (!b.fromP && hypot(b.x - c.x, b.y - c.y) < rad) { puff(b.x, b.y, "#ffffff", 1, .5); bullets.RemoveAt(i); } }
        if (rnd() < dt * 30) { var a = rand(0, TAU); parts.Add(new Part { k = "feather", x = c.x + Math.Cos(a) * rad * .8, y = c.y + Math.Sin(a) * rad * .4, z = rand(10, 40), vx = -Math.Sin(a) * 160, vy = Math.Cos(a) * 70, vz = 40, rot = rand(0, TAU), vr = 10, t = 0, max = .6, col = "#fff" }); }
    }
    /* Яичный дождь: одна зона там, где врагов гуще всего, яйца падают только внутри неё */
    const double STRIKE_R = 135;
    static (double x, double y) strikeCenter(Fighter from, List<Enemy> list)
    {
        Enemy best = null; int bn = 0; double bd = 1e9;
        foreach (var e in list)
        {
            int n = 0; foreach (var f in list) if (hypot(f.x - e.x, f.y - e.y) < STRIKE_R) n++;
            var d = hypot(e.x - from.x, e.y - from.y); if (n > bn || (n == bn && d < bd)) { bn = n; bd = d; best = e; }
        }
        if (best == null)
        {
            var a = from == P ? P.ang : rand(0, TAU);
            return (clamp(from.x + Math.Cos(a) * 220, FENCE + 60, WW - FENCE - 60), clamp(from.y + Math.Sin(a) * 220, FENCE + 60, WH - FENCE - 60));
        }
        double sx = 0, sy = 0; int k = 0; foreach (var f in list) if (hypot(f.x - best.x, f.y - best.y) < STRIKE_R) { sx += f.x; sy += f.y; k++; }
        return (clamp(sx / k, FENCE + 40, WW - FENCE - 40), clamp(sy / k, FENCE + 40, WH - FENCE - 40));
    }
    static void eggStrike(double cx, double cy, int n, double dmg, double r)
    {
        double last = .6 + (n - 1) * .11;
        strikes.Add(new Zone { x = cx, y = cy, r = STRIKE_R, t = last + .25, max = last + .25 });
        for (int i = 0; i < n; i++)
        {
            double a = rand(0, TAU), d = Math.Sqrt(rnd()) * STRIKE_R * .78, tt = .6 + i * .11;
            bombs.Add(new Bomb { x = clamp(cx + Math.Cos(a) * d, FENCE + 10, WW - FENCE - 10), y = clamp(cy + Math.Sin(a) * d, FENCE + 10, WH - FENCE - 10), t = tt, max = tt, r = r, dmg = dmg, fromP = true, quiet = true });
        }
    }
    static void nurseKit(Fighter src, double dt) // Гусыня Медсестра: аптечка самому раненому из своих
    {
        src.kitT -= dt; if (src.kitT > 0) return;
        var team = new List<Fighter> { P }.Concat(allies).Where(u => (u == P ? P.alive : ((Ally)u).down <= 0) && u.hp < u.max * .95).ToList();
        if (team.Count == 0) { src.kitT = 1; return; }
        var tg = team.OrderBy(a => a.hp / a.max).First(); src.kitT = 15;
        var h = Math.Min(25, tg.max - tg.hp); tg.hp += h;
        parts.Add(new Part { k = "kit", x = tg.x, y = tg.y, sx = src.x, sy = src.y, z = 0, t = 0, max = .55 });
        ftext(tg.x, tg.y - 54, "+" + Math.Round(h, MidpointRounding.AwayFromZero), "#7df05a", 13);
    }
    static void updateZones(double dt)
    {
        for (int i = puddles.Count - 1; i >= 0; i--) { puddles[i].t -= dt; if (puddles[i].t <= 0) puddles.RemoveAt(i); }
        if (P.alive && P.hid == "nurse") nurseKit(P, dt);
        foreach (var a in allies) if (a.id == "nurse" && a.down <= 0) nurseKit(a, dt);
        for (int i = strikes.Count - 1; i >= 0; i--) { strikes[i].t -= dt; if (strikes[i].t <= 0) strikes.RemoveAt(i); }
        for (int i = zones.Count - 1; i >= 0; i--)
        {
            var z = zones[i]; z.t -= dt;
            double heal = 25 * dt * (P.medMul > 0 ? P.medMul : 1);
            if (P.alive && hypot(P.x - z.x, P.y - z.y) < z.r) P.hp = Math.Min(P.max, P.hp + heal);
            foreach (var a in allies)
            {
                if (hypot(a.x - z.x, a.y - z.y) > z.r + 400 && a.down <= 0) continue;
                if (a.down > 0) { a.down = 0; a.hp = a.max * .6; a.x = clamp(z.x + rand(-30, 30), FENCE + 30, WW - FENCE - 30); a.y = clamp(z.y + rand(-20, 20), FENCE + 30, WH - FENCE - 30); ftext(a.x, a.y - 50, T("Поднят!"), "#9cf27a", 12); puff(a.x, a.y, "#9cf27a", 6, 1); }
                else if (hypot(a.x - z.x, a.y - z.y) < z.r) a.hp = Math.Min(a.max, a.hp + heal);
            }
            if (z.t <= 0) zones.RemoveAt(i);
        }
    }
    /* ---------- оружие и ульты отряда ---------- */
    static void allyFire(Ally a, double ang, Enemy t)
    {
        var W = WEAP.TryGetValue(a.gun, out var w0) ? w0 : WEAP["pistol"]; double turbo = a.buff == "turbo" ? 2 : 1;
        double rate = W.rate; if (W.special == "mini") rate = .09;
        a.cd = rate * 1.25 / (a.rateMul * turbo) * rand(.9, 1.1);
        double dmg = W.dmg * a.dmgMul * .75 * wMul(a.gun);
        if (W.special == "rail") { fireBeam(a.x, a.y, ang, dmg, false, a); return; }
        if (W.special == "mortar")
        {
            double tx = t != null ? t.x : a.x + Math.Cos(ang) * 260, ty = t != null ? t.y : a.y + Math.Sin(ang) * 260;
            bombs.Add(new Bomb { x = clamp(tx, FENCE + 10, WW - FENCE - 10), y = clamp(ty, FENCE + 10, WH - FENCE - 10), t = .55, max = .55, r = 66, dmg = dmg, fromP = true, lob = true, sx = a.x, sy = a.y, heal = W.heal });
            Sfx.throw_(); return;
        }
        if (W.special == "cat" && (++a.catN) % 6 == 0)
        {
            double sp = W.spd * .8, life = W.life * 1.3;
            bullets.Add(new Bullet { x = a.x + Math.Cos(ang) * 20, y = a.y + Math.Sin(ang) * 20, vx = Math.Cos(ang) * sp, vy = Math.Sin(ang) * sp, r = 8, dmg = dmg * 3, fromP = true, life = life, max = life, kind = "yarn", pierce = 2, hits = new(), ally = true, owner = a, spin = 0 });
            return;
        }
        var kind = W.special == "flame" ? "flame" : W.special == "ice" ? "ice" : a.gun; bool multi = W.pellets > 1 || kind == "flame";
        for (int i = 0; i < W.pellets; i++)
        {
            double a2 = ang + (rnd() - .5) * W.spread, sp = W.spd * (multi ? rand(.85, 1.12) : 1), life = W.life * (a.cls == "sniper" ? 1.35 : 1) * (multi ? rand(.8, 1.1) : 1);
            bullets.Add(new Bullet
            {
                x = a.x + Math.Cos(ang) * 20, y = a.y + Math.Sin(ang) * 20, vx = Math.Cos(a2) * sp, vy = Math.Sin(a2) * sp, r = kind == "flame" ? 7 : W.pellets > 1 ? 3.5 : 4.2, dmg = dmg, fromP = true, life = life, max = life, kind = kind,
                pierce = kind == "flame" ? 99 : W.pierce, hits = (kind == "flame" || W.pierce > 0) ? new() : null, ally = true, owner = a, bounce = W.bounce, shape = i % 4
            });
        }
        if (kind != "flame") parts.Add(new Part { k = "flash", x = a.x + Math.Cos(ang) * 26, y = a.y + Math.Sin(ang) * 26, z = 19, t = 0, max = .05, a = ang, s = .8 });
    }
    static void allyUlt(Ally a)
    {
        a.ult = 0; var H = HEROES[a.id]; double m = a.dmgMul;
        ftext(a.x, a.y - 64, H.ult[0].ToUpper(L.Culture), "#ffc93a", 13); Sfx.ult();
        var near = enemies.Where(e => !e.dead && hypot(e.x - a.x, e.y - a.y) < 300).ToList();
        if (a.id == "hen") { var (cx, cy) = strikeCenter(a, near); eggStrike(cx, cy, 5, 55 * m, 66); }
        else if (a.id == "chick") { a.buff = "turbo"; a.buffT = 4; }
        else if (a.id == "rooster") { Sfx.crow(); ring(a.x, a.y, "#ff6b4a", 220, .5); foreach (var e in near) { if (hypot(e.x - a.x, e.y - a.y) < 220) { hitEnemy(e, 35 * m, e.x - a.x, e.y - a.y, false); e.stun = e.T.boss ? .6 : 2; } } }
        else if (a.id == "duck")
        {
            ring(a.x, a.y, "#7fd4ff", 240, .5); puddles.Add(new Zone { x = a.x, y = a.y, r = 130, t = 4, max = 4 });
            if (hypot(P.x - a.x, P.y - a.y) < 240) P.hp = Math.Min(P.max, P.hp + P.max * .4);
            foreach (var b in allies) if (b.down <= 0 && hypot(b.x - a.x, b.y - a.y) < 240) b.hp = Math.Min(b.max, b.hp + b.max * .4);
            for (int i = bullets.Count - 1; i >= 0; i--) { var b = bullets[i]; if (!b.fromP && hypot(b.x - a.x, b.y - a.y) < 260) bullets.RemoveAt(i); }
            foreach (var e in near) { var d = hypot(e.x - a.x, e.y - a.y); if (d == 0) d = 1; if (d < 240 && !e.T.boss) { e.kx += (e.x - a.x) / d * 380; e.ky += (e.y - a.y) / d * 380; } }
        }
        else if (a.id == "goose") { a.buff = "fort"; a.buffT = 5; ring(a.x, a.y, "#cfe8ff", 90, .4); }
        else if (a.id == "adren") { a.buff = "adren"; a.buffT = 4; a.fireT = 0; }
        else if (a.id == "quail") { foreach (var e in near.OrderByDescending(e => e.hp).Take(2).ToList()) fireBeam(a.x, a.y, Math.Atan2(e.y - a.y, e.x - a.x), 160 * m, false, null); }
        else if (a.id == "guinea") { a.buff = "storm"; a.buffT = 3; }
        else if (a.id == "nurse") { zones.Add(new Zone { x = a.x, y = a.y, t = 5, r = 120 }); ring(a.x, a.y, "#9cf27a", 130, .5); }
    }

    /* ---------- кротовые ямки ---------- */
    static bool holesOn() => S.kind == "endless" ? S.wave >= 2 : S.kind != "sk" && S.diff >= 2;
    static bool holeActive() => isWaves() ? (S.between <= 0 && S.toSpawn > 0) : S.running;
    static void spawnHole()
    {
        if (holes.Count(h => h.state != "close") >= 2) return;
        for (int i = 0; i < 30; i++)
        {
            double a = rand(0, TAU), d = rand(170, 420), x = P.x + Math.Cos(a) * d, y = P.y + Math.Sin(a) * d;
            if (x < FENCE + 50 || x > WW - FENCE - 50 || y < FENCE + 60 || y > WH - FENCE - 50) continue;
            if (OBS.Any(o => hypot(o.x - x, o.y - y) < o.r + 40) || holes.Any(h => hypot(h.x - x, h.y - y) < 90) || nearWater(x, y, 50)) continue;
            holes.Add(new Hole { x = x, y = y, t = 0, state = "dig", ct = 0, life = rand(9, 12), spawnT = rand(1.2, 2), made = 0, max = S.diff > 8 ? 3 : 2 });
            for (int k = 0; k < 14; k++) { double an = rand(0, TAU), sp = rand(60, 170); parts.Add(new Part { k = "shell", x = x, y = y, z = 6, vx = Math.Cos(an) * sp, vy = Math.Sin(an) * sp * .6, vz = rand(120, 240), rot = rand(0, TAU), vr = rand(-8, 8), t = 0, max = rand(.6, 1), dirt = true }); }
            puff(x, y, "#9a6b3c", 8, 1.2); Sfx.chest(); return;
        }
    }
    static void updateHoles(double dt)
    {
        if (holesOn() && holeActive()) { S.holeT -= dt; if (S.holeT <= 0) { S.holeT = rand(20, 28); spawnHole(); } }
        bool canSpawn = holeActive() && enemies.Count(e => e.type == "mole") < 3;
        for (int i = holes.Count - 1; i >= 0; i--)
        {
            var h = holes[i]; h.t += dt;
            if (h.state == "dig" && h.t > .6) h.state = "open";
            if (h.state == "open")
            {
                h.spawnT -= dt;
                if (h.spawnT <= 0 && h.made < h.max && canSpawn) { var e = makeEnemy("mole", h.x, h.y); e.emerge = .6; e.cd = rand(1, 1.8); h.made++; h.spawnT = rand(2.5, 4); puff(h.x, h.y, "#9a6b3c", 6, 1); }
                if (h.t > h.life || (isWaves() && S.toSpawn == 0) || (h.made >= h.max && h.spawnT < -1.5)) { h.state = "close"; h.ct = 0; }
            }
            if (h.state == "close") { h.ct += dt; if (h.ct > 1) { holes.RemoveAt(i); puff(h.x, h.y, "#9a6b3c", 4, .8); } }
        }
    }
}
