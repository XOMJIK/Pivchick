using System;
using Kuriatnyk.Render;
using static Kuriatnyk.D;

namespace Kuriatnyk;

// Отрисовка Soul Knight: оружие, снаряды, предметы на земле, сундук, торговец, лужи.
public static partial class G
{
    /// <summary>Оружие Soul Knight в руке (смотрит вправо, рукоять около x = 0..6).</summary>
    static bool drawSkGun(Ctx c, string id)
    {
        void box(double x, double y, double w, double h, double r, string f) { c.fillStyle = f; rr(c, x, y, w, h, r); c.fill(); c.stroke(); }
        void shaft(double x0, double x1, string col, double w = 4.6) { c.lineCap = "round"; c.strokeStyle = INK; c.lineWidth = w + 3; seg(c, x0, 0, x1, 0); c.strokeStyle = col; c.lineWidth = w; seg(c, x0, 0, x1, 0); c.strokeStyle = INK; c.lineWidth = 2; c.lineCap = "butt"; }
        void orb(double x, double r, string a, string b) { var g = c.createRadialGradient(x - r * .3, -r * .3, 0, x, 0, r); g.addColorStop(0, a); g.addColorStop(1, b); c.fillStyle = g; c.beginPath(); c.arc(x, 0, r, 0, TAU); c.fill(); c.stroke(); }
        c.strokeStyle = INK; c.lineWidth = 2; c.lineJoin = "round";
        switch (id)
        {
            case "fork":
                shaft(-4, 30, "#a8743d");
                c.strokeStyle = INK; c.lineWidth = 4.5; c.lineCap = "round"; seg(c, 29, -7, 29, 7); foreach (var y in new[] { -7.0, 0.0, 7.0 }) seg(c, 29, y, 42, y);
                c.strokeStyle = "#b9c2cc"; c.lineWidth = 2.2; seg(c, 29, -7, 29, 7); foreach (var y in new[] { -7.0, 0.0, 7.0 }) seg(c, 29, y, 42, y); c.lineCap = "butt"; break;
            case "bat":
                c.fillStyle = "#d9b27a"; c.beginPath(); c.moveTo(-3, -2); c.lineTo(12, -2.6); c.quadraticCurveTo(30, -6, 34, -4.5); c.quadraticCurveTo(37, 0, 34, 4.5); c.quadraticCurveTo(30, 6, 12, 2.6); c.lineTo(-3, 2); c.closePath(); c.fill(); c.stroke();
                c.fillStyle = "#c0392b"; c.fillRect(-2, -2.4, 6, 4.8); c.strokeStyle = "rgba(43,29,20,.35)"; c.lineWidth = 1; seg(c, 16, -2, 30, -3.5); break;
            case "axe":
                shaft(-3, 30, "#8a5a2b");
                c.fillStyle = "#cfd6dd"; c.beginPath(); c.moveTo(20, -2); c.lineTo(20, -9); c.quadraticCurveTo(28, -16, 34, -18); c.quadraticCurveTo(31, -6, 34, 4); c.quadraticCurveTo(28, 2, 24, -1); c.closePath(); c.fill(); c.stroke();
                c.strokeStyle = "#ffffff"; c.lineWidth = 1.4; seg(c, 32, -15, 31.5, 1); break;
            case "pan":
                shaft(-3, 14, "#3d3f4a", 4); c.fillStyle = "#2a2b33"; c.beginPath(); c.ellipse(26, 0, 13, 11, 0, 0, TAU); c.fill(); c.stroke();
                c.fillStyle = "#55585f"; c.beginPath(); c.ellipse(26, 0, 9.5, 7.6, 0, 0, TAU); c.fill(); c.fillStyle = "rgba(255,255,255,.35)"; c.beginPath(); c.ellipse(23, -3, 3, 1.6, -.5, 0, TAU); c.fill(); break;
            case "scythe":
                shaft(-6, 36, "#7a4a24");
                c.fillStyle = "#dfe5ea"; c.beginPath(); c.moveTo(34, -2); c.quadraticCurveTo(30, 18, 6, 22); c.quadraticCurveTo(24, 14, 30, -2); c.closePath(); c.fill(); c.stroke();
                c.strokeStyle = INK; c.lineWidth = 3; seg(c, 12, -3, 12, 3); break;
            case "sword":
                c.save(); c.globalAlpha = .35 + Math.Sin(S.t * 6) * .15; c.fillStyle = "#9fe3ff"; c.beginPath(); c.ellipse(24, 0, 18, 6, 0, 0, TAU); c.fill(); c.restore();
                box(-6, -2.2, 10, 4.4, 2, "#7a4a24"); box(3, -8, 3.5, 16, 1.5, "#ffc93a");
                c.fillStyle = "#eef7ff"; c.beginPath(); c.moveTo(6.5, -3.4); c.lineTo(36, -2.4); c.lineTo(41, 0); c.lineTo(36, 2.4); c.lineTo(6.5, 3.4); c.closePath(); c.fill(); c.stroke();
                c.strokeStyle = "#9fe3ff"; c.lineWidth = 1.2; seg(c, 9, 0, 35, 0); break;
            case "sling":
                c.lineCap = "round"; c.strokeStyle = INK; c.lineWidth = 6.5; seg(c, -2, 2, 8, 0); seg(c, 8, 0, 17, -7); seg(c, 8, 0, 17, 7);
                c.strokeStyle = "#a8743d"; c.lineWidth = 3.8; seg(c, -2, 2, 8, 0); seg(c, 8, 0, 17, -7); seg(c, 8, 0, 17, 7);
                c.strokeStyle = "#c0392b"; c.lineWidth = 1.6; seg(c, 17, -7, 11, 0); seg(c, 11, 0, 17, 7); c.lineCap = "butt"; break;
            case "water":
                box(2, -4.5, 20, 8, 3, "#43c06b"); box(20, -2.4, 7, 4.5, 1.5, "#ffc93a"); box(5, 0, 5, 8, 1.5, "#2f9a52");
                c.fillStyle = "#7fd4ff"; c.beginPath(); c.ellipse(11, -8, 6, 4, 0, 0, TAU); c.fill(); c.stroke(); break;
            case "peas":
                c.fillStyle = "#6db343"; c.beginPath(); c.moveTo(0, -4); c.quadraticCurveTo(15, -9, 30, -4); c.lineTo(30, 4); c.quadraticCurveTo(15, 9, 0, 4); c.closePath(); c.fill(); c.stroke();
                c.fillStyle = "#9be05a"; foreach (var x in new[] { 6.0, 13.0, 20.0 }) { c.beginPath(); c.arc(x, 0, 2.8, 0, TAU); c.fill(); c.stroke(); }
                box(4, 2, 5, 8, 1.5, "#4f8f2c"); break;
            case "seeds":
                box(-6, -3, 11, 7, 2, "#8a5a2b"); box(4, -3.5, 20, 6.5, 1.5, "#55585f");
                c.fillStyle = "#ffd23a"; for (int i = 0; i < 10; i++) { var a = i * TAU / 10; c.beginPath(); c.ellipse(28 + Math.Cos(a) * 5, Math.Sin(a) * 5, 3, 1.6, a, 0, TAU); c.fill(); }
                c.fillStyle = "#6b4428"; c.beginPath(); c.arc(28, 0, 4, 0, TAU); c.fill(); c.stroke(); break;
            case "honey":
                c.fillStyle = "#f2a91f"; c.beginPath(); c.ellipse(9, 0, 10, 8, 0, 0, TAU); c.fill(); c.stroke();
                box(17, -2.5, 10, 5, 1.5, "#c9973f"); c.fillStyle = "#ffe066"; c.beginPath(); c.ellipse(9, -7, 8, 2.6, 0, 0, TAU); c.fill(); c.stroke();
                c.fillStyle = "#f2a91f"; c.beginPath(); c.ellipse(27.5, 2.5, 1.8, 2.6, 0, 0, TAU); c.fill(); break;
            case "plunger":
                shaft(-2, 22, "#c98a43", 4.2);
                c.fillStyle = "#c0392b"; c.beginPath(); c.moveTo(21, -4); c.quadraticCurveTo(25, -11, 33, -11); c.lineTo(33, 11); c.quadraticCurveTo(25, 11, 21, 4); c.closePath(); c.fill(); c.stroke();
                c.fillStyle = "#8e2a21"; c.beginPath(); c.ellipse(33, 0, 2.5, 11, 0, 0, TAU); c.fill(); c.stroke(); break;
            case "snow":
                box(0, -5, 22, 10, 4, "#e6f6ff"); box(21, -3, 6, 6, 2, "#7fd4ff");
                c.strokeStyle = "#3b8fd1"; c.lineWidth = 1.3; for (int k = 0; k < 3; k++) { var a = k * Math.PI / 3; seg(c, 10 - Math.Cos(a) * 4, -Math.Sin(a) * 4, 10 + Math.Cos(a) * 4, Math.Sin(a) * 4); } break;
            case "bubble":
                box(0, -4, 17, 9, 3.5, "#ff8fc8"); box(5, 1, 5, 8, 1.5, "#d65a9a");
                c.strokeStyle = INK; c.lineWidth = 2; c.beginPath(); c.arc(24, 0, 6, 0, TAU); c.stroke(); c.strokeStyle = "#ff8fc8"; c.lineWidth = 1.4; c.stroke();
                c.save(); c.globalAlpha = .7; c.strokeStyle = "#bfe6ff"; c.lineWidth = 1.4; c.beginPath(); c.arc(30 + Math.Sin(S.t * 3) * 2, -8, 3.5, 0, TAU); c.stroke(); c.restore(); break;
            case "carrot":
                box(-6, -3, 12, 6.5, 2, "#8a5a2b");
                c.fillStyle = "#f28c28"; c.beginPath(); c.moveTo(4, -4.5); c.lineTo(38, -1); c.lineTo(38, 1); c.lineTo(4, 4.5); c.closePath(); c.fill(); c.stroke();
                c.strokeStyle = "#c9661d"; c.lineWidth = 1.1; foreach (var x in new[] { 12, 20, 28 }) seg(c, x, -3, x + 2, -1);
                c.fillStyle = "#6db343"; c.strokeStyle = INK; c.lineWidth = 1.4; c.beginPath(); c.moveTo(4, -2); c.lineTo(-4, -9); c.lineTo(1, -3); c.lineTo(-6, -5); c.lineTo(4, 0); c.closePath(); c.fill(); c.stroke(); break;
            case "boomer":
                c.save(); c.rotate(-.3); c.fillStyle = "#ffd23a"; c.beginPath(); c.moveTo(2, 0); c.quadraticCurveTo(14, -16, 26, -12); c.lineTo(24, -7); c.quadraticCurveTo(15, -9, 8, 2); c.quadraticCurveTo(15, 9, 24, 7); c.lineTo(26, 12); c.quadraticCurveTo(14, 16, 2, 0); c.closePath(); c.fill(); c.stroke(); c.restore(); break;
            case "laser":
                box(0, -2.6, 22, 5.2, 2.6, "#9aa3ad"); box(4, -1.5, 5, 3, 1, "#55585f");
                c.fillStyle = "#ff3b2e"; c.beginPath(); c.arc(23, 0, 2.2, 0, TAU); c.fill(); c.save(); c.globalAlpha = .5; c.beginPath(); c.arc(23, 0, 4.5, 0, TAU); c.fill(); c.restore(); break;
            case "cactus":
                box(2, -6, 22, 12, 6, "#4f9a3c"); box(8, -11, 5, 8, 2.5, "#4f9a3c");
                c.strokeStyle = "#fff4dc"; c.lineWidth = 1.2; for (int i = 0; i < 6; i++) { double x = 5 + i * 3.4, y = i % 2 == 0 ? -6 : 6; seg(c, x, y, x + (i % 3 - 1), y + (y < 0 ? -3 : 3)); }
                c.fillStyle = "#ff6fa8"; c.beginPath(); c.arc(10.5, -12, 2.4, 0, TAU); c.fill(); break;
            case "teapot":
                c.fillStyle = "#e8e3d6"; c.beginPath(); c.ellipse(9, 0, 11, 9, 0, 0, TAU); c.fill(); c.stroke();
                c.fillStyle = "#e8e3d6"; c.beginPath(); c.moveTo(17, -2); c.lineTo(29, -7); c.lineTo(30, -4); c.lineTo(19, 3); c.closePath(); c.fill(); c.stroke();
                c.fillStyle = "#c0392b"; c.beginPath(); c.ellipse(9, -8.5, 6, 2.2, 0, 0, TAU); c.fill(); c.stroke(); circ(c, 9, -11, 1.8, "#c0392b");
                c.fillStyle = "#2f95d0"; c.fillRect(2, -1, 14, 3); break;
            case "pumpkin":
                box(-4, -4, 26, 8, 2.5, "#4a6430"); box(20, -5, 5, 10, 1.5, "#3d3f4a");
                c.fillStyle = "#f28c28"; c.beginPath(); c.ellipse(9, -9, 8, 6.5, 0, 0, TAU); c.fill(); c.stroke(); c.strokeStyle = "#c9661d"; c.lineWidth = 1.2; seg(c, 9, -15, 9, -3);
                c.fillStyle = "#6db343"; c.strokeStyle = INK; c.lineWidth = 1.4; c.fillRect(8, -18, 2.4, 4); break;
            case "rocket":
                box(-4, -4.5, 28, 9, 2, "#c0392b"); c.fillStyle = "#ffc93a"; c.fillRect(2, -4.5, 3, 9); c.fillRect(14, -4.5, 3, 9);
                box(23, -5.5, 4, 11, 1.5, "#3d3f4a"); c.strokeStyle = "#c9973f"; c.lineWidth = 1.6; c.beginPath(); c.moveTo(-4, 0); c.quadraticCurveTo(-9, -4, -11, -1); c.stroke();
                if ((long)(S.t * 18) % 2 == 0) circ(c, -11, -1, 1.8, "#ffd23a", false); break;
            case "bees":
                c.fillStyle = "#ffc93a"; c.beginPath(); c.ellipse(10, 0, 11, 9, 0, 0, TAU); c.fill(); c.stroke();
                c.strokeStyle = "#c98a00"; c.lineWidth = 1.6; foreach (var y in new[] { -4.0, 0.0, 4.0 }) seg(c, 0.5, y, 19.5, y);
                c.fillStyle = INK; c.beginPath(); c.arc(18, 0, 2.6, 0, TAU); c.fill();
                c.strokeStyle = INK; c.lineWidth = 2; box(19, -2, 9, 4, 1.5, "#8a5a2b"); break;
            case "accordion":
                box(-4, -7, 6, 14, 1.5, "#c0392b"); box(22, -7, 6, 14, 1.5, "#c0392b");
                c.fillStyle = "#fff4dc"; c.beginPath(); c.moveTo(2, -6); for (int i = 0; i <= 8; i++) c.lineTo(2 + i * 2.5, i % 2 == 0 ? -6 : -3.5); c.lineTo(22, 6); for (int i = 8; i >= 0; i--) c.lineTo(2 + i * 2.5, i % 2 == 0 ? 6 : 3.5); c.closePath(); c.fill(); c.stroke();
                c.fillStyle = "#fff"; foreach (var y in new[] { -3.0, 0.0, 3.0 }) { c.beginPath(); c.arc(25, y, .9, 0, TAU); c.fill(); } break;
            case "gauss":
                box(-5, -4, 12, 8, 2, "#2a2b33"); box(5, -4.5, 30, 9, 2, "#3d3f4a");
                c.fillStyle = "#c9733a"; foreach (var x in new[] { 10, 16, 22, 28 }) { rr(c, x, -6.5, 3.4, 13, 1.2); c.fill(); c.stroke(); }
                c.save(); c.globalAlpha = .6 + Math.Sin(S.t * 10) * .3; circ(c, 36, 0, 3.4, "#b48cff", false); c.restore(); break;
            case "grinder":
                {
                    box(0, -6, 18, 12, 3, "#9aa3ad"); box(16, -4, 14, 8, 2, "#80848f"); box(28, -5, 4, 10, 1.5, "#55585f");
                    c.fillStyle = "#b9c2cc"; c.beginPath(); c.moveTo(3, -6); c.lineTo(-1, -15); c.lineTo(15, -15); c.lineTo(12, -6); c.closePath(); c.fill(); c.stroke();
                    double sp = P != null && P.w == "grinder" ? P.spin : 0, a = S.t * (2 + sp * 30);
                    c.save(); c.translate(6, 4); c.rotate(a); c.lineWidth = 2.4; seg(c, 0, 0, 0, 9); circ(c, 0, 9, 2.4, "#c0392b"); c.restore();
                    c.fillStyle = "#c0533a"; c.beginPath(); c.arc(7, -14, 3, 0, TAU); c.fill(); break;
                }
            case "coingun":
                box(-6, -3, 12, 7, 2, "#7a4a24");
                c.fillStyle = "#f2b93a"; c.beginPath(); c.moveTo(4, -3.5); c.lineTo(22, -4); c.lineTo(30, -9); c.lineTo(30, 7); c.lineTo(22, 3); c.lineTo(4, 3.5); c.closePath(); c.fill(); c.stroke();
                c.fillStyle = "#fff3b0"; c.fillRect(8, -2.5, 10, 1.6); Hud.Piv(c, 13, 8, 9); break;
            case "sfire": case "sice": case "sheal": case "spoison": case "sbolt": case "sroot": case "swind": case "sstar":
                {
                    string wood = id == "sroot" ? "#5a3418" : id == "sstar" ? "#2b4a8a" : id == "sice" ? "#9fb6c8" : "#8a5a2b";
                    shaft(-8, 30, wood, 4);
                    if (id == "sroot") { c.fillStyle = "#6db343"; foreach (var (x, y, r) in new[] { (30.0, -6.0, .6), (36.0, -3.0, -.4), (34.0, 5.0, .9) }) { c.beginPath(); c.ellipse(x, y, 5, 2.6, r, 0, TAU); c.fill(); c.stroke(); } circ(c, 34, 0, 3.6, "#b8834c"); break; }
                    if (id == "sstar") { c.save(); c.translate(36, 0); c.rotate(S.t * 2); c.fillStyle = "#ffd23a"; figShape(c, 0, 8); c.fill(); c.stroke(); c.restore(); break; }
                    c.fillStyle = "#c9973f"; rr(c, 27, -4, 5, 8, 1.5); c.fill(); c.stroke();
                    var (a, b) = id switch { "sfire" => ("#fff2a0", "#ff5a1f"), "sice" => ("#ffffff", "#5fb2dc"), "sheal" => ("#e8ffd8", "#4fae36"), "spoison" => ("#e6ffb0", "#7d3fb0"), "sbolt" => ("#ffffff", "#8f5cff"), _ => ("#ffffff", "#a9c7d6") };
                    c.save(); c.globalAlpha = .35 + Math.Sin(S.t * 5) * .12; c.fillStyle = b; c.beginPath(); c.arc(37, 0, 10, 0, TAU); c.fill(); c.restore();
                    orb(37, 6.2, a, b);
                    if (id == "swind") { c.strokeStyle = "#fff"; c.lineWidth = 1.3; c.beginPath(); c.arc(37, 0, 3, S.t * 6, S.t * 6 + 4); c.stroke(); }
                    if (id == "sbolt") { c.strokeStyle = "#ffe066"; c.lineWidth = 1.5; c.beginPath(); c.moveTo(35, -4); c.lineTo(38, -1); c.lineTo(36, 1); c.lineTo(39, 4); c.stroke(); }
                    break;
                }
            default: return false;
        }
        c.lineCap = "butt"; c.strokeStyle = INK; c.lineWidth = 2;
        return true;
    }

    /// <summary>Снаряд оружия Soul Knight.</summary>
    static void drawSkBullet(Ctx c, Bullet b)
    {
        double a = Math.Atan2(b.vy, b.vx), q = b.max > 0 ? 1 - b.life / b.max : 0;
        c.save(); c.translate(b.x, b.y - 19); c.strokeStyle = INK; c.lineWidth = 1.6;
        switch (b.kind)
        {
            case "pebble": circ(c, 0, 0, 4, "#a9a39a"); break;
            case "water": c.rotate(a); c.globalAlpha = .85; c.fillStyle = "#7fd4ff"; c.beginPath(); c.ellipse(0, 0, 6, 3.4, 0, 0, TAU); c.fill(); c.fillStyle = "#e6f8ff"; c.beginPath(); c.arc(1.5, -1, 1.3, 0, TAU); c.fill(); break;
            case "pea": circ(c, 0, 0, 4.5, "#7ccf3f"); c.fillStyle = "rgba(255,255,255,.6)"; c.beginPath(); c.arc(-1.4, -1.4, 1.4, 0, TAU); c.fill(); break;
            case "seed": c.rotate(a); c.fillStyle = "#2b2b33"; c.beginPath(); c.ellipse(0, 0, 4.2, 2.4, 0, 0, TAU); c.fill(); c.strokeStyle = "#e8e3d6"; c.lineWidth = 1; seg(c, -3, 0, 3, 0); break;
            case "honey": c.fillStyle = "#f2a91f"; c.beginPath(); c.ellipse(0, 0, 5.5, 4.6, a, 0, TAU); c.fill(); c.stroke(); c.fillStyle = "#ffe066"; c.beginPath(); c.arc(-1.5, -1.5, 1.5, 0, TAU); c.fill(); break;
            case "poop":
                c.rotate(Math.Sin(b.spin) * .3); c.fillStyle = "#7a4a24";
                c.beginPath(); c.ellipse(0, 3, 7.5, 3.6, 0, 0, TAU); c.fill(); c.stroke(); c.beginPath(); c.ellipse(0, -.5, 5.6, 3, 0, 0, TAU); c.fill(); c.stroke();
                c.beginPath(); c.moveTo(-3, -2.5); c.quadraticCurveTo(0, -9, 3, -3); c.closePath(); c.fill(); c.stroke();
                c.fillStyle = "#fff"; c.beginPath(); c.arc(-2, 1, 1.1, 0, TAU); c.arc(2, 1, 1.1, 0, TAU); c.fill(); break;
            case "snow": c.strokeStyle = "#5fb2dc"; circ(c, 0, 0, 5.5, "#ffffff"); break;
            case "bubble":
                c.globalAlpha = .55; c.fillStyle = "#ffd9f0"; c.beginPath(); c.arc(0, 0, b.r, 0, TAU); c.fill(); c.globalAlpha = 1;
                c.strokeStyle = "#ff8fc8"; c.lineWidth = 1.6; c.stroke(); c.fillStyle = "#fff"; c.beginPath(); c.ellipse(-b.r * .35, -b.r * .4, b.r * .28, b.r * .16, -.6, 0, TAU); c.fill(); break;
            case "carrot":
                c.rotate(a); c.fillStyle = "#f28c28"; c.beginPath(); c.moveTo(9, 0); c.lineTo(-7, -3.2); c.lineTo(-7, 3.2); c.closePath(); c.fill(); c.stroke();
                c.fillStyle = "#6db343"; c.beginPath(); c.moveTo(-7, 0); c.lineTo(-12, -4); c.lineTo(-11, 0); c.lineTo(-12, 4); c.closePath(); c.fill(); break;
            case "boomer":
                c.rotate(b.spin * 2); c.fillStyle = "#ffd23a"; c.beginPath(); c.moveTo(0, 0); c.quadraticCurveTo(7, -10, 14, -7); c.lineTo(12, -3); c.quadraticCurveTo(6, -4, 3, 2); c.quadraticCurveTo(-6, 5, -12, 2); c.lineTo(-12, -2); c.quadraticCurveTo(-5, -1, 0, 0); c.closePath(); c.fill(); c.stroke(); break;
            case "spike": c.rotate(a); c.strokeStyle = INK; c.lineWidth = 3.4; seg(c, -6, 0, 6, 0); c.strokeStyle = "#9be05a"; c.lineWidth = 1.8; seg(c, -6, 0, 6, 0); break;
            case "steam": c.globalAlpha = Math.Max(0, .75 - q * .7); c.fillStyle = "#f4fbff"; c.beginPath(); c.arc(0, 0, 4 + q * 14, 0, TAU); c.fill(); break;
            case "rocket":
                c.rotate(a); c.fillStyle = "#c0392b"; rr(c, -7, -3, 13, 6, 2); c.fill(); c.stroke(); c.fillStyle = "#ffc93a"; c.beginPath(); c.moveTo(6, -3); c.lineTo(10, 0); c.lineTo(6, 3); c.closePath(); c.fill(); c.stroke();
                c.fillStyle = (long)(S.t * 30) % 2 == 0 ? "#ffd23a" : "#ff7a1a"; c.beginPath(); c.arc(-9, 0, 2.6, 0, TAU); c.fill(); break;
            case "bee":
                c.rotate(a); c.fillStyle = "rgba(255,255,255,.8)"; c.beginPath(); c.ellipse(-1, -4 - Math.Abs(Math.Sin(S.t * 40)) * 2, 3, 2, -.4, 0, TAU); c.fill();
                c.fillStyle = "#ffc93a"; c.beginPath(); c.ellipse(0, 0, 5, 3.4, 0, 0, TAU); c.fill(); c.stroke(); c.fillStyle = INK; c.fillRect(-1.5, -3.2, 1.6, 6.4); c.fillRect(1.6, -3, 1.4, 6); break;
            case "note":
                c.rotate(a); c.globalAlpha = Math.Max(0, 1 - q * .8); c.strokeStyle = "#fff4dc"; c.lineWidth = 3; c.beginPath(); c.arc(-b.r * .6, 0, b.r, -1, 1); c.stroke();
                c.strokeStyle = "#ffc93a"; c.lineWidth = 1.6; c.stroke(); c.rotate(-a); c.fillStyle = INK; c.beginPath(); c.ellipse(-2, 3, 2.6, 2, -.4, 0, TAU); c.fill(); c.fillRect(0, -6, 1.4, 9); break;
            case "slug":
                c.rotate(a); c.globalAlpha = .45; c.fillStyle = "#b48cff"; c.beginPath(); c.ellipse(-10, 0, 18, 5, 0, 0, TAU); c.fill(); c.globalAlpha = 1;
                c.fillStyle = "#e8dcff"; c.beginPath(); c.ellipse(0, 0, 8, 3.2, 0, 0, TAU); c.fill(); c.stroke(); break;
            case "meat": c.fillStyle = "#a0522d"; c.beginPath(); c.arc(0, 0, 4.4, 0, TAU); c.fill(); c.stroke(); c.fillStyle = "#d9826a"; c.beginPath(); c.arc(-1.2, -1.2, 1.4, 0, TAU); c.fill(); break;
            case "coin": c.scale(Math.Max(.25, Math.Abs(Math.Cos(b.spin))), 1); Hud.Piv(c, 0, 0, 11); break;
            case "fireorb":
                c.fillStyle = "rgba(255,90,30,.35)"; c.beginPath(); c.arc(0, 0, 11, 0, TAU); c.fill(); c.fillStyle = "#ff7a1a"; c.beginPath(); c.arc(0, 0, 6, 0, TAU); c.fill(); c.stroke();
                c.fillStyle = "#ffe066"; c.beginPath(); c.arc(-1.4, -1.4, 2.6, 0, TAU); c.fill(); break;
            case "shard": c.rotate(a); c.fillStyle = "#e6f8ff"; c.strokeStyle = "#3b8fd1"; c.lineWidth = 1.4; c.beginPath(); c.moveTo(8, 0); c.lineTo(-5, -3); c.lineTo(-2, 0); c.lineTo(-5, 3); c.closePath(); c.fill(); c.stroke(); break;
            case "leaf": c.rotate(a + b.spin * .3); c.fillStyle = "#7ccf3f"; c.beginPath(); c.ellipse(0, 0, 6, 3, 0, 0, TAU); c.fill(); c.stroke(); c.strokeStyle = "#e8ffd8"; c.lineWidth = 1; seg(c, -5, 0, 5, 0); break;
            case "poison":
                c.fillStyle = "rgba(125,63,176,.35)"; c.beginPath(); c.arc(0, 0, 10, 0, TAU); c.fill();
                var pg = c.createRadialGradient(-2, -2, 0, 0, 0, 6.5); pg.addColorStop(0, "#e6ffb0"); pg.addColorStop(1, "#7d3fb0"); c.fillStyle = pg; c.beginPath(); c.arc(0, 0, 6.5, 0, TAU); c.fill(); c.stroke(); break;
            case "seedling": c.fillStyle = "#b8834c"; c.beginPath(); c.ellipse(0, 1, 4.5, 3.5, 0, 0, TAU); c.fill(); c.stroke(); c.fillStyle = "#6db343"; c.beginPath(); c.ellipse(2, -4, 3.4, 1.8, -.6, 0, TAU); c.fill(); c.stroke(); break;
            case "tornado":
                c.translate(0, 10); for (int i = 0; i < 5; i++) { double r = 5 + i * 4, y = -i * 7, w = Math.Sin(b.spin * 1.5 + i) * 3; c.globalAlpha = .75; c.strokeStyle = i % 2 == 0 ? "#e3f2f7" : "#a9c7d6"; c.lineWidth = 3.2; c.beginPath(); c.ellipse(w, y, r, r * .35, 0, 0, TAU); c.stroke(); }
                break;
            case "star": c.rotate(b.spin); c.fillStyle = "#ffd23a"; figShape(c, 0, 7); c.fill(); c.stroke(); break;
            case "wave":
                c.rotate(a); c.globalAlpha = Math.Max(0, 1 - q * .9); c.strokeStyle = "rgba(159,227,255,.55)"; c.lineWidth = 9; c.lineCap = "round"; c.beginPath(); c.arc(-14, 0, 22, -.9, .9); c.stroke();
                c.strokeStyle = "#ffffff"; c.lineWidth = 3; c.stroke(); break;
            default: circ(c, 0, 0, 4, "#fff8c8"); break;
        }
        c.restore();
    }

    /// <summary>Предметы Soul Knight на земле: оружие (в т. ч. товар), пивчики, стамина.</summary>
    static bool drawSkPickup(Ctx c, Pickup p)
    {
        if (p.kind == "piv")
        {
            c.save(); c.translate(p.x, p.y); c.fillStyle = "rgba(25,50,10,.25)"; c.beginPath(); c.ellipse(0, 0, 5, 2, 0, 0, TAU); c.fill();
            c.translate(0, -7 - p.z); c.scale(Math.Max(.3, Math.Abs(Math.Cos(p.ph * 4))), 1); Hud.Piv(c, 0, 0, 14); c.restore(); return true;
        }
        if (p.kind == "stam" && p.price <= 0)
        {
            double bo = Math.Sin(p.ph * 4) * 3; c.save(); c.translate(p.x, p.y);
            c.globalAlpha = .5 + Math.Sin(p.ph * 6) * .2; c.fillStyle = "#4cc3ff"; c.beginPath(); c.ellipse(0, 0, 12, 4.5, 0, 0, TAU); c.fill(); c.globalAlpha = 1;
            c.translate(0, -14 + bo); c.fillStyle = "#4cc3ff"; c.strokeStyle = INK; c.lineWidth = 2; c.beginPath(); c.moveTo(0, -9); c.lineTo(7, 0); c.lineTo(0, 9); c.lineTo(-7, 0); c.closePath(); c.fill(); c.stroke();
            c.fillStyle = "#e3f7ff"; c.beginPath(); c.moveTo(0, -5); c.lineTo(3, 0); c.lineTo(0, 2); c.closePath(); c.fill(); c.restore(); return true;
        }
        if (p.kind != "skw" && p.kind != "stamp" && p.kind != "hpp" && p.price <= 0) return false;
        double near = hypot(p.x - P.x, p.y - P.y);
        c.save(); c.translate(p.x, p.y);
        string rc = p.kind == "skw" ? RAR[WEAP[p.wid].skr].c : p.kind == "hpp" ? "#ff6b5a" : "#4cc3ff";
        if (p.price > 0)
        { // подставка торговца
            c.fillStyle = "rgba(25,50,10,.3)"; c.beginPath(); c.ellipse(4, 3, 22, 7, 0, 0, TAU); c.fill();
            c.fillStyle = "#8a5a2b"; c.strokeStyle = INK; c.lineWidth = 2; c.beginPath(); c.ellipse(0, 0, 20, 7, 0, 0, Math.PI); c.lineTo(-20, -8); c.ellipse(0, -8, 20, 7, 0, Math.PI, 0, true); c.closePath(); c.fill(); c.stroke();
            c.fillStyle = "#b07a45"; c.beginPath(); c.ellipse(0, -8, 20, 7, 0, 0, TAU); c.fill(); c.stroke();
            c.translate(0, -8);
        }
        double pl = .5 + .5 * Math.Sin(p.ph * 5);
        c.globalAlpha = .45 + pl * .35; c.strokeStyle = rc; c.lineWidth = 3; c.beginPath(); c.ellipse(0, 0, 19, 7, 0, 0, TAU); c.stroke(); c.globalAlpha = 1;
        double bob = Math.Sin(p.ph * 3) * 2.5 - p.z;
        c.save(); c.translate(0, -18 + bob);
        if (p.kind == "skw") { c.save(); c.globalAlpha = .3; c.fillStyle = rc; c.beginPath(); c.arc(0, 0, 18, 0, TAU); c.fill(); c.restore(); c.rotate(-.35); c.scale(1.15, 1.15); c.translate(-13, 0); drawGun(c, p.wid); }
        else if (p.kind == "corn") { c.rotate(-.5); c.strokeStyle = INK; c.lineWidth = 2.2; c.fillStyle = "#f6c42d"; c.beginPath(); c.ellipse(0, 0, 6, 12, 0, 0, TAU); c.fill(); c.stroke(); c.fillStyle = "#6db343"; c.beginPath(); c.moveTo(0, 12); c.quadraticCurveTo(-11, 6, -8, -6); c.quadraticCurveTo(-4, 4, 0, 8); c.closePath(); c.fill(); c.stroke(); }
        else
        { // зелье: синее — стамина, красное с крестиком — здоровье
            bool hp = p.kind == "hpp";
            c.strokeStyle = INK; c.lineWidth = 2; c.fillStyle = hp ? "#ffd9d4" : "#cfefff"; rr(c, -3, -14, 6, 6, 1.5); c.fill(); c.stroke();
            c.fillStyle = hp ? "#e8322a" : "#4cc3ff"; c.beginPath(); c.arc(0, 0, 9, 0, TAU); c.fill(); c.stroke();
            if (hp) { c.fillStyle = "#fff"; c.fillRect(-1.6, -5, 3.2, 10); c.fillRect(-5, -1.6, 10, 3.2); }
            else { c.fillStyle = "#e3f7ff"; c.beginPath(); c.ellipse(-3, -3, 2.4, 3.4, -.4, 0, TAU); c.fill(); }
            c.fillStyle = "#c98a43"; rr(c, -3.5, -17, 7, 4, 1.5); c.fill(); c.stroke();
        }
        c.restore();
        if (p.price > 0)
        { // ценник
            c.font = "900 13px Rubik"; var pt = p.price.ToString(); double tw = c.measureText(pt) + 26, ty = 14;
            bool poor = S.piv < p.price;
            c.fillStyle = INK; rr(c, -tw / 2, ty, tw, 20, 9); c.fill(); Hud.Piv(c, -tw / 2 + 11, ty + 10, 13);
            c.textAlign = "left"; c.fillStyle = poor ? "#ff8a6b" : "#ffd25a"; c.fillText(pt, -tw / 2 + 20, ty + 14.5);
        }
        if (p.kind == "skw" && (p == actPick || (actPick == null && near < 70)))
        { // название над оружием
            var nm = WEAP[p.wid].name; c.font = "900 12px Rubik"; c.textAlign = "center"; c.lineJoin = "round"; c.lineWidth = 3.5; c.strokeStyle = INK;
            double ny = -46 + bob; c.strokeText(nm, 0, ny); c.fillStyle = Hud.Rc(WEAP[p.wid].skr); c.fillText(nm, 0, ny);
        }
        c.restore(); return true;
    }

    /// <summary>Сундук подземелья и прилавок торговца (укрытия особого вида).</summary>
    static bool drawSkObstacle(Ctx c, Obs o)
    {
        if (o.type == "crate") { drawCrate(c, o); return true; }
        if (o.type == "skchest")
        {
            c.save(); c.translate(o.x, o.y + 12); c.strokeStyle = INK; c.lineWidth = 2.4; c.lineJoin = "round";
            c.fillStyle = "rgba(25,50,10,.3)"; c.beginPath(); c.ellipse(4, 2, 34, 9, 0, 0, TAU); c.fill();
            if (!o.open) { c.save(); c.globalAlpha = .25 + Math.Sin(S.t * 4) * .12; c.fillStyle = "#ffe066"; c.beginPath(); c.ellipse(0, -16, 44, 30, 0, 0, TAU); c.fill(); c.restore(); }
            c.fillStyle = "#a8622f"; rr(c, -28, -28, 56, 28, 4); c.fill(); c.stroke();
            c.strokeStyle = "#7a4320"; c.lineWidth = 1.5; foreach (var y in new[] { -19.0, -10.0 }) seg(c, -26, y, 26, y);
            c.strokeStyle = INK; c.lineWidth = 2.4; c.fillStyle = "#c9a227"; foreach (var x in new[] { -24.0, 18.0 }) { c.fillRect(x, -28, 6, 28); c.strokeRect(x, -28, 6, 28); }
            if (o.open)
            {
                c.fillStyle = "#2b1d14"; rr(c, -26, -32, 52, 8, 3); c.fill();
                c.fillStyle = "#8a4f25"; c.beginPath(); c.moveTo(-28, -30); c.lineTo(28, -30); c.lineTo(24, -54); c.lineTo(-24, -54); c.closePath(); c.fill(); c.stroke();
            }
            else
            {
                c.fillStyle = "#8a4f25"; c.beginPath(); c.moveTo(-29, -28); c.quadraticCurveTo(-29, -46, 0, -46); c.quadraticCurveTo(29, -46, 29, -28); c.closePath(); c.fill(); c.stroke();
                c.fillStyle = "#c9a227"; foreach (var x in new[] { -24.0, 18.0 }) { c.beginPath(); c.moveTo(x, -28); c.lineTo(x, -43); c.lineTo(x + 6, -44); c.lineTo(x + 6, -28); c.closePath(); c.fill(); c.stroke(); }
                c.fillStyle = "#ffc93a"; rr(c, -6, -34, 12, 12, 2); c.fill(); c.stroke(); c.fillStyle = INK; c.fillRect(-1.2, -30, 2.4, 5);
            }
            c.restore(); return true;
        }
        if (o.type == "vend")
        { // автомат с сюрпризом: красный шкаф, витрина с товарами, монетоприёмник; после покупки — пустой и погасший
            c.save(); c.translate(o.x, o.y + 14); c.strokeStyle = INK; c.lineWidth = 2.4; c.lineJoin = "round";
            c.fillStyle = "rgba(25,50,10,.3)"; c.beginPath(); c.ellipse(4, 2, 32, 8, 0, 0, TAU); c.fill();
            bool used = o.open; double gl = used ? 0 : .5 + .5 * Math.Sin(S.t * 3);
            if (!used) { c.save(); c.globalAlpha = .18 + gl * .14; c.fillStyle = "#ffe066"; c.beginPath(); c.ellipse(0, -40, 46, 52, 0, 0, TAU); c.fill(); c.restore(); }
            c.fillStyle = used ? "#7d6a64" : "#c0392b"; rr(c, -26, -86, 52, 86, 7); c.fill(); c.stroke();
            c.fillStyle = used ? "#5f514c" : "#e8322a"; rr(c, -26, -86, 52, 14, 7); c.fill(); c.stroke();
            c.fillStyle = used ? "#9c8f86" : "#ffc93a"; rr(c, -18, -84, 36, 10, 3); c.fill();
            c.font = "900 10px Rubik"; c.textAlign = "center"; c.fillStyle = INK; c.fillText("? ? ?", 0, -76.5);
            c.fillStyle = used ? "#3b3550" : "#cfefff"; rr(c, -19, -68, 30, 40, 4); c.fill(); c.stroke(); // витрина
            if (!used)
            {
                c.strokeStyle = "rgba(43,29,20,.45)"; c.lineWidth = 1.4; seg(c, -18, -55, 10, -55); seg(c, -18, -42, 10, -42);
                foreach (var (x, y, col) in new[] { (-12.0, -61.0, "#4cc3ff"), (-1.0, -61.0, "#e8322a"), (-12.0, -48.0, "#e8322a"), (-1.0, -48.0, "#ffc93a") })
                { c.fillStyle = col; c.strokeStyle = INK; c.lineWidth = 1.2; c.beginPath(); c.arc(x, y, 3.6, 0, TAU); c.fill(); c.stroke(); }
                c.fillStyle = "rgba(255,255,255,.55)"; c.fillRect(-17, -66, 4, 34);
            }
            c.strokeStyle = INK; c.lineWidth = 2; c.fillStyle = "#2b1d14"; rr(c, 15, -60, 6, 12, 2); c.fill(); // монетоприёмник
            c.fillStyle = used ? "#9c8f86" : "#ffd25a"; c.beginPath(); c.arc(18, -40, 3.4, 0, TAU); c.fill(); c.stroke(); // кнопка
            c.fillStyle = "#2b1d14"; rr(c, -18, -22, 36, 10, 3); c.fill(); // лоток
            if (!used)
            { // ценник
                int pr = SKW.Cost(SKW.VEND_PRICE, S.skLv); c.font = "900 13px Rubik"; var pt = pr.ToString(); double tw = c.measureText(pt) + 26, ty = 6;
                c.fillStyle = INK; rr(c, -tw / 2, ty, tw, 20, 9); c.fill(); Hud.Piv(c, -tw / 2 + 11, ty + 10, 13);
                c.textAlign = "left"; c.fillStyle = S.piv < pr ? "#ff8a6b" : "#ffd25a"; c.fillText(pt, -tw / 2 + 20, ty + 14.5);
            }
            c.restore(); return true;
        }
        if (o.type == "counter")
        {
            c.save(); c.translate(o.x, o.y);
            // торговец — гусь в ковбойской шляпе за прилавком
            var near = P != null && hypot(P.x - o.x, P.y - o.y - 100) < 150;
            c.save(); c.translate(0, -18); c.scale(1.1, 1.1);
            drawBird(c, new Sprite { face = P != null && P.x < o.x ? -1 : 1, ang = Math.Sin(S.t) * .2, kind = "player", gun = null, sp = "goose", body = "#f4f1e6", wing = "#d6d0c0", tail = "#d6d0c0", head = "#f4f1e6", hat = "cowboy", phase = S.t * 4, moving = false });
            c.restore();
            c.strokeStyle = INK; c.lineWidth = 2.4; c.lineJoin = "round";
            c.fillStyle = "rgba(25,50,10,.3)"; c.beginPath(); c.ellipse(6, 26, 64, 10, 0, 0, TAU); c.fill();
            c.fillStyle = "#8a5a2b"; rr(c, -60, -4, 120, 30, 5); c.fill(); c.stroke();
            c.fillStyle = "#b07a45"; rr(c, -64, -12, 128, 12, 4); c.fill(); c.stroke();
            c.strokeStyle = "rgba(43,29,20,.4)"; c.lineWidth = 1.4; for (double x = -50; x < 60; x += 20) seg(c, x, 0, x, 22);
            // вывеска с пивчиком — на передней стенке прилавка
            c.strokeStyle = INK; c.lineWidth = 2; c.fillStyle = "#fff4dc"; rr(c, -30, 3, 60, 19, 6); c.fill(); c.stroke(); Hud.Piv(c, -18, 12.5, 13);
            c.font = "900 11px Rubik"; c.textAlign = "left"; c.fillStyle = INK; c.fillText(T("Лавка"), -9, 16.5);
            if (near) { c.font = "800 12px Rubik"; c.textAlign = "center"; c.lineWidth = 3.5; c.strokeStyle = INK; c.strokeText(T("Заходи, покупай!"), 0, -104); c.fillStyle = "#fff"; c.fillText(T("Заходи, покупай!"), 0, -104); }
            c.restore(); return true;
        }
        return false;
    }

    /// <summary>Лужи, ядовитые облака и корни на земле.</summary>
    static void drawGoo(Ctx c)
    {
        foreach (var g in goo)
        {
            double k = Math.Min(1, g.t / .5) * Math.Min(1, (g.max - g.t) / .2 + .3);
            c.save(); c.translate(g.x, g.y); c.globalAlpha = Math.Clamp(k, 0, 1);
            if (g.kind == "poop")
            {
                c.fillStyle = "rgba(110,70,30,.6)"; c.beginPath(); for (int i = 0; i < 10; i++) { double a = i / 10.0 * TAU, q = g.r * (.8 + Math.Sin(i * 2.3) * .15); c.lineTo(Math.Cos(a) * q, Math.Sin(a) * q * .45); } c.closePath(); c.fill();
                c.strokeStyle = "rgba(160,200,60,.55)"; c.lineWidth = 2; for (int i = 0; i < 3; i++) { double x = (i - 1) * g.r * .4, y = -6 - ((S.t * 20 + i * 9) % 22); c.beginPath(); c.moveTo(x, y); c.quadraticCurveTo(x + 4, y - 4, x, y - 8); c.stroke(); }
            }
            else if (g.kind == "poison") { c.fillStyle = "rgba(140,220,80,.22)"; c.beginPath(); c.ellipse(0, 0, g.r, g.r * .45, 0, 0, TAU); c.fill(); c.strokeStyle = "rgba(125,63,176,.5)"; c.lineWidth = 2; c.setLineDash(new double[] { 8, 6 }); c.lineDashOffset = -S.t * 20; c.stroke(); c.setLineDash(null); }
            else
            { // корни
                c.fillStyle = "rgba(90,52,24,.35)"; c.beginPath(); c.ellipse(0, 0, g.r, g.r * .45, 0, 0, TAU); c.fill();
                c.strokeStyle = INK; c.lineCap = "round";
                for (int i = 0; i < 7; i++)
                {
                    double a = i / 7.0 * TAU + .3, x = Math.Cos(a) * g.r * .6, y = Math.Sin(a) * g.r * .27, h = 12 + Math.Sin(i * 1.7) * 4;
                    c.lineWidth = 5.5; c.beginPath(); c.moveTo(x, y); c.quadraticCurveTo(x + 6, y - h * .6, x + 2, y - h); c.stroke();
                    c.strokeStyle = "#7a4a24"; c.lineWidth = 3; c.stroke(); c.strokeStyle = INK;
                }
                c.lineCap = "butt";
            }
            c.restore();
        }
    }
}
