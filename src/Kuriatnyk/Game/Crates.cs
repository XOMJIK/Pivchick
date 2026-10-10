using System;
using System.Linq;
using Kuriatnyk.Render;

namespace Kuriatnyk;

// Деревянные ящики: укрытие, которое ломается от выстрелов, ударов и взрывов, а внутри — что-нибудь полезное.
public static partial class G
{
    const double CRATE_HP = 45;
    static Obs newCrate(double x, double y) => new Obs { x = x, y = y, r = 20, type = "crate", hp = CRATE_HP };

    /// <summary>Обычный режим: расставить ящики по карте, подальше от героя, воды и других укрытий.</summary>
    static void addCrates(int n, int cap = 9)
    {
        n = Math.Min(n, cap - OBS.Count(o => o.type == "crate"));
        for (int k = 0, tries = 0; k < n && tries < 200; tries++)
        {
            double x = rand(FENCE + 80, WW - FENCE - 80), y = rand(FENCE + 90, WH - FENCE - 80);
            if (hypot(x - P.x, y - P.y) < 170 || nearWater(x, y, 50) || bridgeAt(x, y) != null) continue;
            if (OBS.Any(o => hypot(o.x - x, o.y - y) < o.r + 20 + 50) || enemies.Any(e => hypot(e.x - x, e.y - y) < 40)) continue;
            OBS.Add(newCrate(x, y)); puff(x, y, "#e9dcc0", 5, .9); k++;
        }
    }
    public static void hitCrate(Obs o, double d)
    {
        if (o.type != "crate" || o.hp <= 0) return;
        o.hp -= d; o.hit = .12;
        if (rnd() < .5) parts.Add(new Part { k = "shell", x = o.x + rand(-10, 10), y = o.y, z = rand(10, 26), vx = rand(-80, 80), vy = rand(-40, 40), vz = rand(80, 160), rot = rand(0, TAU), vr = rand(-8, 8), t = 0, max = .6, dirt = true });
        if (o.hp <= 0) breakCrate(o);
    }
    static void breakCrate(Obs o)
    {
        OBS.Remove(o); Sfx.crack(); S.shake += 3;
        for (int i = 0; i < 10; i++) { double a = rand(0, TAU), sp = rand(60, 200); parts.Add(new Part { k = "shell", x = o.x, y = o.y, z = 12, vx = Math.Cos(a) * sp, vy = Math.Sin(a) * sp * .6, vz = rand(120, 260), rot = rand(0, TAU), vr = rand(-10, 10), t = 0, max = rand(.7, 1.1), dirt = true }); }
        puff(o.x, o.y, "#c9a06a", 8, 1.1);
        if (S.kind == "sk")
        { // в подземелье — пивчики, иногда стамина или кукуруза
            dropPiv(o.x, o.y, 2 + (int)(rnd() * 3));
            double r = rnd();
            if (r < .22) pickups.Add(new Pickup { x = o.x + 10, y = o.y, kind = "stam", t = 14, ph = 0 });
            else if (r < .36) pickups.Add(new Pickup { x = o.x - 10, y = o.y, kind = "corn", t = 14, ph = 0 });
            return;
        }
        double q = rnd();
        if (q < .32) pickups.Add(new Pickup { x = o.x, y = o.y, kind = "corn", t = 14, ph = 0 });
        else if (q < .6) pickups.Add(new Pickup { x = o.x, y = o.y, kind = "nade", t = 14, ph = 0 });
        else { int c = 3 + (int)(rnd() * 4); S.coins += c; ftext(o.x, o.y - 40, "+" + c + T(" зёрен"), "#ffc93a", 13); Sfx.coin(); }
    }
    /// <summary>Ящики в секторе удара ближним оружием или в радиусе взрыва.</summary>
    static void hitCratesArea(double x, double y, double R, double dmg, double ang = double.NaN, double arc = 0)
    {
        foreach (var o in OBS.Where(o => o.type == "crate").ToList())
        {
            double dx = o.x - x, dy = o.y - y, d = hypot(dx, dy); if (d - o.r > R) continue;
            if (!double.IsNaN(ang) && Math.Abs(((Math.Atan2(dy, dx) - ang + Math.PI * 3) % TAU) - Math.PI) > arc && d > o.r + 24) continue;
            hitCrate(o, dmg);
        }
    }
    static void drawCrate(Ctx c, Obs o)
    {
        double k = o.hp / CRATE_HP, s = 20;
        c.save(); c.translate(o.x + (o.hit > 0 ? rand(-1.5, 1.5) : 0), o.y);
        c.fillStyle = "rgba(25,50,10,.3)"; c.beginPath(); c.ellipse(5, 3, s * 1.2, s * .45, 0, 0, TAU); c.fill();
        c.strokeStyle = INK; c.lineWidth = 2.4; c.lineJoin = "round";
        bool fl = o.hit > 0;
        c.fillStyle = fl ? "#fff" : "#b98a4e"; rr(c, -s, -s * 1.9, s * 2, s * 1.9, 3); c.fill(); c.stroke(); // перед
        c.fillStyle = fl ? "#fff" : "#d8a868"; c.beginPath(); c.moveTo(-s, -s * 1.9); c.lineTo(-s + 6, -s * 2.35); c.lineTo(s + 6, -s * 2.35); c.lineTo(s, -s * 1.9); c.closePath(); c.fill(); c.stroke(); // крышка
        c.fillStyle = fl ? "#fff" : "#9c7038"; c.beginPath(); c.moveTo(s, -s * 1.9); c.lineTo(s + 6, -s * 2.35); c.lineTo(s + 6, -s * .45); c.lineTo(s, 0); c.closePath(); c.fill(); c.stroke(); // бок
        c.strokeStyle = "#7a5228"; c.lineWidth = 3; seg(c, -s + 3, -s * 1.9 + 3, s - 3, -3); seg(c, -s + 3, -3, s - 3, -s * 1.9 + 3); // перекрестие досок
        c.strokeStyle = INK; c.lineWidth = 1.4; c.strokeRect(-s + 2, -s * 1.9 + 2, s * 2 - 4, s * 1.9 - 4);
        if (k < .7) { c.strokeStyle = INK; c.lineWidth = 1.6; c.beginPath(); c.moveTo(-6, -s * 1.9); c.lineTo(-2, -s * 1.3); c.lineTo(-8, -s * .9); c.stroke(); }
        if (k < .4) { c.beginPath(); c.moveTo(8, -2); c.lineTo(4, -s * .7); c.lineTo(10, -s * 1.1); c.stroke(); }
        c.restore();
    }
}
