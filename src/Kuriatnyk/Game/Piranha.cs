using System;
using System.Collections.Generic;
using Kuriatnyk.Render;

namespace Kuriatnyk;

/// <summary>Пиранья: плавает в реке (виден только плавник), а когда кто-то подходит к воде, встаёт на дыбы и прыгает на него.</summary>
public sealed class Fish
{
    public River R; public double u, dir, spd, ph, cd, t, sx, sy, tx, ty; public string st = "swim"; // swim, rise, jump, back
}

// Злые пираньи во всех реках: кусают героя и отряд, когда те стоят у воды, плывут или идут по мосту. Врагов не трогают.
public static partial class G
{
    public static List<Fish> fish = new();
    const double FISH_RISE = .38, FISH_JUMP = .42, FISH_NEAR = 34;

    /// <summary>Расселить пираний по рекам: примерно одна на 420 пикселей русла, в речке комнаты — одна-две.</summary>
    static void spawnFish()
    {
        fish = new();
        foreach (var R in RIVERS)
        {
            double lo = Math.Max(R.lo, 0) + 20, hi = Math.Min(R.hi, R.axis == 'h' ? WW : WH) - 20; if (hi <= lo) continue;
            int n = R.room != null ? 1 + (rnd() < .5 ? 1 : 0) : Math.Max(1, (int)((hi - lo) / 420));
            for (int i = 0; i < n; i++) fish.Add(new Fish { R = R, u = lo + (hi - lo) * (i + rnd()) / n, dir = rnd() < .5 ? -1 : 1, spd = rand(40, 75), ph = rand(0, TAU), cd = rand(1, 3) });
        }
    }
    /// <summary>Где сейчас пиранья в воде.</summary>
    static (double x, double y) fishPos(Fish f) { var v = rcR(f.R, f.u) + Math.Sin(f.ph) * f.R.hw * .45; return xyR(f.R, f.u, v); }
    /// <summary>Стоит ли цель у воды этой реки (у берега, в воде или на мосту).</summary>
    static bool fishCan(Fish f, Unit e)
    {
        var (u, v) = uvR(f.R, e.x, e.y);
        if (u < f.R.lo || u > f.R.hi || Math.Abs(u - f.u) > 210) return false;
        if (f.R.room != null && !inRoom(f.R.room, e.x, e.y, 0)) return false;
        return Math.Abs(v - rcR(f.R, u)) < f.R.hw + FISH_NEAR;
    }
    static void updateFish(double dt)
    {
        bool hunt = S.mode == "play";
        foreach (var f in fish)
        {
            f.ph += dt * 1.3;
            if (f.st == "swim")
            {
                double lo = Math.Max(f.R.lo, 0) + 16, hi = Math.Min(f.R.hi, f.R.axis == 'h' ? WW : WH) - 16;
                f.u += f.dir * f.spd * dt; if (f.u < lo) { f.u = lo; f.dir = 1; } if (f.u > hi) { f.u = hi; f.dir = -1; }
                f.cd -= dt;
                if (!hunt || f.cd > 0) continue;
                // ближайшая цель у воды: герой или боец отряда
                Unit best = null; double bd = 1e9; var (fx, fy) = fishPos(f);
                void consider(Unit e) { if (!fishCan(f, e)) return; var d = hypot(e.x - fx, e.y - fy); if (d < bd) { bd = d; best = e; } }
                foreach (var pl in players) if (pl.alive && pl.inv <= 0) consider(pl);
                foreach (var a in allies) if (a.down <= 0) consider(a);
                if (best == null) { f.cd = .25; continue; }
                f.st = "rise"; f.t = 0; f.tx = best.x; f.ty = best.y; f.sx = fx; f.sy = fy;
                puff(fx, fy, "#bfe6ff", 3, .8); ring(fx, fy, "#e6f6ff", 26, .3);
            }
            else if (f.st == "rise")
            {
                f.t += dt;
                if (f.t >= FISH_RISE)
                { // прыжок туда, где цель сейчас: если отбежать — промахнётся
                    Unit tg = null; double bd = 70;
                    foreach (var e in fishTargets()) { var d = hypot(e.x - f.tx, e.y - f.ty); if (d < bd) { bd = d; tg = e; } }
                    if (tg != null) { f.tx = tg.x; f.ty = tg.y; }
                    f.st = "jump"; f.t = 0; Sfx.Play("swish");
                }
            }
            else if (f.st == "jump")
            {
                f.t += dt;
                if (f.t >= FISH_JUMP)
                {
                    f.st = "back"; f.t = 0; fishBite(f);
                }
            }
            else if (f.st == "back")
            {
                f.t += dt;
                if (f.t >= .3) { f.st = "swim"; f.cd = rand(1.4, 2.4); var (x, y) = fishPos(f); puff(x, y, "#bfe6ff", 3, .7); }
            }
        }
    }
    static IEnumerable<Unit> fishTargets()
    {
        foreach (var pl in players) if (pl.alive) yield return pl;
        foreach (var a in allies) if (a.down <= 0) yield return a;
    }
    static void fishBite(Fish f)
    {
        bool any = false;
        foreach (var pl in players) if (pl.alive && hypot(pl.x - f.tx, pl.y - f.ty) < pl.r + 14) { var old = P; P = pl; hurtPlayer(eDmg(13), "fish"); P = old; any = true; }
        foreach (var a in allies) if (a.down <= 0 && hypot(a.x - f.tx, a.y - f.ty) < a.r + 14) { hurtAlly(a, 14); any = true; }
        if (any) { Sfx.hurt(); feathers(f.tx, f.ty, "#ffffff", 3, .6); ftext(f.tx, f.ty - 46, T("Ам!"), "#ff5a3c", 14); }
        else puff(f.tx, f.ty, "#e9dcc0", 3, .6);
    }

    /* ---------- отрисовка ---------- */
    /// <summary>Плавники в воде (рисуются под мостами).</summary>
    static void drawFishWater(Ctx c)
    {
        foreach (var f in fish)
        {
            if (f.st is "jump" or "back") continue;
            var (x, y) = f.st == "rise" ? (f.sx, f.sy) : fishPos(f);
            double rise = f.st == "rise" ? f.t / FISH_RISE : 0, k = f.R.axis == 'h' ? f.dir : 1;
            c.save(); c.translate(x, y);
            c.globalAlpha = .55; c.strokeStyle = "#e6f6ff"; c.lineWidth = 1.6;
            if (f.st == "swim") { c.beginPath(); c.moveTo(-k * 14, -3); c.lineTo(-k * 5, 0); c.lineTo(-k * 14, 3); c.stroke(); }
            else { double rr0 = 10 + rise * 14; c.beginPath(); c.ellipse(0, 0, rr0, rr0 * .4, 0, 0, TAU); c.stroke(); }
            c.globalAlpha = 1; double h = 7 + rise * 9;
            c.fillStyle = "#4b5560"; c.strokeStyle = INK; c.lineWidth = 1.6;
            c.beginPath(); c.moveTo(-k * 6, 0); c.lineTo(k * 2, -h); c.lineTo(k * 6, 0); c.closePath(); c.fill(); c.stroke();
            if (f.st == "rise") { c.font = "900 16px Rubik"; c.textAlign = "center"; c.lineWidth = 4; c.strokeStyle = INK; c.strokeText("!", 0, -h - 6); c.fillStyle = "#ff5a3c"; c.fillText("!", 0, -h - 6); }
            c.restore();
        }
    }
    /// <summary>Пиранья в прыжке и тень там, куда она упадёт.</summary>
    static void drawFishAir(Ctx c)
    {
        foreach (var f in fish)
        {
            if (f.st is not ("jump" or "back")) continue;
            double q = f.st == "jump" ? f.t / FISH_JUMP : 1 - f.t / .3;
            double x = f.sx + (f.tx - f.sx) * q, y = f.sy + (f.ty - f.sy) * q, z = Math.Sin(Math.Min(1, q) * Math.PI) * 46 + (f.st == "back" ? 6 : 0);
            if (f.st == "jump")
            {
                c.save(); c.globalAlpha = .35 + q * .4; c.strokeStyle = "#ff3b2e"; c.lineWidth = 2.2; c.beginPath(); c.ellipse(f.tx, f.ty, 18, 7, 0, 0, TAU); c.stroke(); c.restore();
            }
            c.save(); c.translate(x, y - 14 - z); c.rotate(Math.Atan2(f.ty - f.sy, f.tx - f.sx) + (q - .5) * 1.2 * (f.tx >= f.sx ? 1 : -1)); if (f.tx < f.sx) c.scale(1, -1);
            c.strokeStyle = INK; c.lineWidth = 1.8; c.lineJoin = "round";
            c.fillStyle = "#6f7c8a"; c.beginPath(); c.moveTo(-12, 0); c.lineTo(-19, -6); c.lineTo(-18, 6); c.closePath(); c.fill(); c.stroke(); // хвост
            c.fillStyle = "#7a8794"; c.beginPath(); c.ellipse(0, 0, 13, 8, 0, 0, TAU); c.fill(); c.stroke();
            c.fillStyle = "#e8322a"; c.beginPath(); c.ellipse(1, 3.5, 9, 3.6, 0, 0, Math.PI); c.fill();
            c.fillStyle = "#4b5560"; c.beginPath(); c.moveTo(-4, -7); c.lineTo(2, -13); c.lineTo(5, -7); c.closePath(); c.fill(); c.stroke();
            circ(c, 7, -2.5, 2.4, "#fff"); circ(c, 7.6, -2.3, 1.1, "#e8322a", false);
            c.fillStyle = "#fff"; c.beginPath(); c.moveTo(9, 1); c.lineTo(10.5, 3.6); c.lineTo(12, 1); c.lineTo(13.5, 3); c.lineTo(13, .5); c.closePath(); c.fill(); // зубы
            c.restore();
        }
    }
}
