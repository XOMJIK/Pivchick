using System;
using System.Collections.Generic;
using System.Linq;
using Kuriatnyk.Render;
using static Kuriatnyk.D;

namespace Kuriatnyk;

/// <summary>Волна землетрясения: кольцо трещин расходится от точки удара. Бьёт того, кого накрыла кромка, один раз.</summary>
public sealed class Quake { public double x, y, r, spd, max, dmg, delay; public HashSet<object> hit = new(); }

// Босс Хомяк-Трясун: прыгает и бьёт лапами о землю — от него расходятся кольца землетрясения, с неба падают камни,
// по земле бегут трещины к герою. Ещё сворачивается в шар и катится тараном и плюётся семечками.
public static partial class G
{
    public static List<Quake> quakes = new();

    /// <summary>Хомяк подпрыгивает (quakeT — сколько ещё в воздухе), потом удар.</summary>
    static void hamsterJump(Enemy e)
    {
        e.quakeT = e.hp < e.max * .5 ? .6 : .8; e.quakeA = e.quakeT; Sfx.squeak();
        ftext(e.x, e.y - 100, T("Земля, трясись!"), "#ffc93a", 14);
    }
    static void hamsterSlam(Enemy e)
    {
        var Tt = e.T; bool rage = e.hp < e.max * .5; double dmg = eDmg(Tt.dmg * 1.7);
        S.shake += 22; Sfx.boom(); Sfx.crack(); Platform.Buzz(90);
        ring(e.x, e.y, "#c9a06a", 140, .5); puff(e.x, e.y, "#c9a06a", 16, 2.2);
        int rings = rage ? 4 : 3;
        for (int i = 0; i < rings; i++) quakes.Add(new Quake { x = e.x, y = e.y, r = 30, spd = 300 + i * 25, max = 640, dmg = dmg, delay = i * .42 });
        // камни с неба вокруг цели и трещины по земле к ней
        var tg = (Unit)e.tgt ?? P; if (tg == null) return;
        int rocks = rage ? 8 : 5;
        for (int i = 0; i < rocks; i++) { double a = rand(0, TAU), d = rand(30, 220); addBomb(tg.x + Math.Cos(a) * d, tg.y + Math.Sin(a) * d, rand(.8, 1.7), 48, dmg * .9); }
        double ang = Math.Atan2(tg.y - e.y, tg.x - e.x);
        foreach (var off in rage ? new[] { -.35, 0, .35 } : new[] { 0.0 })
            for (int k = 1; k <= 9; k++) addBomb(e.x + Math.Cos(ang + off) * k * 62, e.y + Math.Sin(ang + off) * k * 62, .35 + k * .09, 40, dmg * .8);
        foreach (var o in OBS.Where(o => o.type == "crate").ToList()) if (hypot(o.x - e.x, o.y - e.y) < 260) hitCrate(o, 30);
    }
    static void updateQuakes(double dt)
    {
        for (int i = quakes.Count - 1; i >= 0; i--)
        {
            var q = quakes[i];
            if (q.delay > 0) { q.delay -= dt; continue; }
            q.r += q.spd * dt;
            if (rnd() < dt * 40) { double a = rand(0, TAU); parts.Add(new Part { k = "shell", x = q.x + Math.Cos(a) * q.r, y = q.y + Math.Sin(a) * q.r * .5, z = 4, vx = Math.Cos(a) * 40, vy = Math.Sin(a) * 20, vz = rand(90, 180), rot = rand(0, TAU), vr = rand(-8, 8), t = 0, max = .6, dirt = true }); }
            // кромка кольца бьёт тех, кто на земле (по эллипсу, как вид сверху)
            bool edge(Unit u) => Math.Abs(hypot(u.x - q.x, (u.y - q.y) * 2) - q.r) < 20 + u.r * .6;
            if (P != null && P.alive && !q.hit.Contains(P) && edge(P)) { q.hit.Add(P); hurtPlayer(q.dmg, "quake"); if (P.alive) { S.shake += 6; } }
            foreach (var a in allies) if (a.down <= 0 && !q.hit.Contains(a) && edge(a)) { q.hit.Add(a); hurtAlly(a, q.dmg * .7); }
            if (q.r >= q.max) quakes.RemoveAt(i);
        }
    }
    static void drawQuakes(Ctx c)
    {
        foreach (var q in quakes)
        {
            if (q.delay > 0) continue; double k = 1 - q.r / q.max;
            c.save(); c.translate(q.x, q.y); c.scale(1, .5);
            c.globalAlpha = .35 * k + .1; c.strokeStyle = "#5a3a1e"; c.lineWidth = 26; c.beginPath(); c.arc(0, 0, q.r, 0, TAU); c.stroke();
            c.globalAlpha = .9 * k + .1; c.strokeStyle = "#2b1d14"; c.lineWidth = 3; c.lineJoin = "round";
            c.beginPath(); int n = Math.Max(24, (int)(q.r / 9));
            for (int i = 0; i <= n; i++) { double a = i * TAU / n, j = (fr(i * 7 + (int)q.max) - .5) * 14; c.lineTo(Math.Cos(a) * (q.r + j), Math.Sin(a) * (q.r + j)); }
            c.stroke();
            c.strokeStyle = "#e9c88f"; c.lineWidth = 2; c.globalAlpha = .7 * k; c.beginPath(); c.arc(0, 0, q.r - 10, 0, TAU); c.stroke();
            c.restore();
        }
    }

    /* ---------- отрисовка хомяка ---------- */
    /// <summary>Мордочка хомяка — для аватарки (центр в 0,0, ширина ~56).</summary>
    public static void drawHamsterFace(Ctx c)
    {
        c.strokeStyle = INK; c.lineWidth = 3; c.lineJoin = "round";
        foreach (var sd in new[] { -1, 1 }) { circ(c, sd * 17, -18, 8, "#d9a05b"); circ(c, sd * 17, -18, 4, "#f2b8c0", false); }
        c.fillStyle = "#e8b26a"; c.beginPath(); c.ellipse(0, 2, 26, 22, 0, 0, TAU); c.fill(); c.stroke();
        c.fillStyle = "#fff4dc"; c.beginPath(); c.ellipse(-13, 9, 11, 9, 0, 0, TAU); c.ellipse(13, 9, 11, 9, 0, 0, TAU); c.fill();
        circ(c, -9, -4, 3.4, INK, false); circ(c, 9, -4, 3.4, INK, false); circ(c, -8, -5, 1.2, "#fff", false); circ(c, 10, -5, 1.2, "#fff", false);
        circ(c, 0, 3, 3, "#e8708a", false);
        c.fillStyle = "#fffaf0"; c.lineWidth = 1.6; c.beginPath(); c.rect(-3.5, 6, 3.4, 5); c.rect(.1, 6, 3.4, 5); c.fill(); c.stroke();
        c.strokeStyle = "rgba(43,29,20,.7)"; c.lineWidth = 1.2; foreach (var sd in new[] { -1, 1 }) { seg(c, sd * 6, 5, sd * 24, 1); seg(c, sd * 6, 7, sd * 24, 8); }
    }
    static void drawHamster(Ctx c, Enemy e)
    {
        double s = Sz(e), jump = 0;
        if (e.quakeT > 0) { double q = 1 - e.quakeT / Math.Max(.01, e.quakeA); jump = Math.Sin(q * Math.PI) * 60; }
        bool ball = e.st == "dash" || e.st == "tell";
        c.save(); c.translate(e.x, e.y);
        c.save(); c.scale(s, s); double sh = 1 - jump / 140; shadow(c, 22 * sh, 7 * sh); c.restore();
        if (e.st == "tell") c.translate(rand(-2, 2), 0);
        c.translate(0, -jump); c.scale(s * e.face, s); c.lineCap = "round"; c.lineJoin = "round"; c.strokeStyle = INK; c.lineWidth = 2.4;
        bool fl = e.flash > 0; string fur = fl ? "#fff" : "#e8b26a", dk = fl ? "#fff" : "#c98a43", belly = fl ? "#fff" : "#fff4dc", pink = fl ? "#fff" : "#f2b8c0";
        if (ball)
        { // свернулся в шар и катится
            double rot = e.st == "dash" ? S.t * 18 : 0; c.translate(0, -16); c.rotate(rot);
            c.fillStyle = fur; c.beginPath(); c.arc(0, 0, 17, 0, TAU); c.fill(); c.stroke();
            c.fillStyle = dk; for (int i = 0; i < 3; i++) { c.save(); c.rotate(i * TAU / 3); c.beginPath(); c.ellipse(9, 0, 5, 3, 0, 0, TAU); c.fill(); c.restore(); }
            c.fillStyle = belly; c.beginPath(); c.ellipse(0, 6, 9, 6, 0, 0, TAU); c.fill();
            circ(c, 6, -5, 2, INK, false);
            c.restore(); return;
        }
        double bob = e.moving ? Math.Abs(Math.Sin(e.phase)) * 2 : 0, lg = e.moving ? Math.Sin(e.phase) * 4 : 0;
        foreach (var (x, l) in new[] { (-9.0, lg), (8.0, -lg) }) { c.fillStyle = pink; c.beginPath(); c.ellipse(x + l, -2, 5, 3, 0, 0, TAU); c.fill(); c.stroke(); }
        c.translate(0, -bob);
        // пухлое тело
        c.fillStyle = fur; c.beginPath(); c.ellipse(0, -15, 18, 15, 0, 0, TAU); c.fill(); c.stroke();
        c.fillStyle = belly; c.beginPath(); c.ellipse(3, -10, 11, 9, 0, 0, TAU); c.fill();
        c.fillStyle = dk; c.beginPath(); c.ellipse(-8, -24, 7, 4, -.4, 0, TAU); c.fill();
        // лапки подняты для удара
        double paw = e.quakeT > 0 ? -10 : 0;
        foreach (var x in new[] { 6.0, 13.0 }) { c.fillStyle = pink; c.beginPath(); c.ellipse(x, -12 + paw, 3.4, 4, 0, 0, TAU); c.fill(); c.stroke(); }
        // голова и щёки
        c.fillStyle = fur; c.beginPath(); c.ellipse(12, -30, 12, 11, 0, 0, TAU); c.fill(); c.stroke();
        c.fillStyle = belly; c.beginPath(); c.ellipse(17, -25, 7, 6, 0, 0, TAU); c.fill(); c.stroke();
        circ(c, 5, -40, 4.6, fur); circ(c, 5, -40, 2.4, pink, false); circ(c, 16, -41, 4.2, fur); circ(c, 16, -41, 2.2, pink, false);
        circ(c, 15.5, -32, 2.2, INK, false); circ(c, 16.2, -32.8, .8, "#fff", false);
        circ(c, 23, -27, 1.8, "#e8708a", false);
        c.fillStyle = "#fffaf0"; c.lineWidth = 1.2; c.beginPath(); c.rect(20, -24, 2.4, 3.4); c.fill(); c.stroke();
        c.strokeStyle = "rgba(43,29,20,.7)"; c.lineWidth = 1; seg(c, 21, -27, 30, -30); seg(c, 21, -26, 30, -25);
        // каска шахтёра с фонариком — гроза подземелий
        c.strokeStyle = INK; c.lineWidth = 2; c.fillStyle = fl ? "#fff" : "#ffc93a"; c.beginPath(); c.arc(11, -37, 10, Math.PI * 1.05, Math.PI * 1.95); c.closePath(); c.fill(); c.stroke();
        c.fillStyle = "#fff6a0"; c.beginPath(); c.arc(17, -42, 2.6, 0, TAU); c.fill(); c.stroke();
        c.restore();
    }
}

public static partial class G
{
    /// <summary>Сетевая игра: во сколько раз крепче враги (1 игрок — 100%, 2 — 150%, 3 — 250%, 4 — 400%).</summary>
    public static double MpHp = 1;
}
public static partial class G
{
    /// <summary>Для проверок (Snap): сразу на уровень подземелья.</summary>
    public static void skEnterLevel(int lv) => enterLevel(lv);
}
public static partial class G
{
    public static void drawEnemyPublic(Ctx c, Enemy e) => drawEnemy(c, e); // для листов спрайтов в Snap
}
