using System;
using System.Collections.Generic;
using Kuriatnyk.Render;

namespace Kuriatnyk;

// Отрисовка врагов, препятствий, предметов (порт средней части web/js/draw.js).
public static partial class G
{
    static double Sz(Enemy e) => e.s > 0 ? e.s : 1;

    static void drawStars(Ctx c, double x, double y)
    {
        c.save(); c.translate(x, y);
        for (int i = 0; i < 3; i++)
        {
            var a = S.t * 4 + i * TAU / 3; c.save(); c.translate(Math.Cos(a) * 12, Math.Sin(a) * 4); c.fillStyle = "#ffe066"; c.strokeStyle = INK; c.lineWidth = 1.2; c.beginPath();
            for (int k = 0; k < 10; k++) { double r = k % 2 == 1 ? 2 : 4.5, an = k / 10.0 * TAU - Math.PI / 2; c.lineTo(Math.Cos(an) * r, Math.Sin(an) * r); }
            c.closePath(); c.fill(); c.stroke(); c.restore();
        }
        c.restore();
    }
    sealed class FoxPal { public string fur, belly, eye, leg; }
    static readonly Dictionary<string, FoxPal> FOXPAL = new()
    {
        ["fox"] = new() { fur = "#e8742a", belly = "#fff1de", eye = "#ffe066", leg = "#4a2a14" }, ["ataman"] = new() { fur = "#c95a1f", belly = "#fff1de", eye = "#ffe066", leg = "#4a2a14" },
        ["wolf"] = new() { fur = "#7f8794", belly = "#e3e6ea", eye = "#ffd23a", leg = "#3b3f47" }, ["wolfboss"] = new() { fur = "#5d6470", belly = "#d5d9df", eye = "#ff5a3c", leg = "#2e3138" },
    };
    static void drawFox(Ctx c, Enemy e)
    {
        double s = Sz(e); var pal = FOXPAL.TryGetValue(e.type, out var p0) ? p0 : FOXPAL["fox"]; bool wolf = e.type == "wolf" || e.type == "wolfboss";
        c.save(); c.translate(e.x, e.y); c.scale(s, s); shadow(c, 17, 5);
        double bob = e.moving ? Math.Abs(Math.Sin(e.phase)) * 2.5 : 0, lg = e.moving ? Math.Sin(e.phase) * 6 : 0;
        c.scale(e.face, 1); c.lineCap = "round"; c.lineJoin = "round";
        bool fl = e.flash > 0; string fur = fl ? "#fff" : pal.fur, belly = fl ? "#fff" : pal.belly;
        var legs = new[] { (-9.0, lg), (-4.0, -lg), (6.0, -lg), (11.0, lg) };
        c.strokeStyle = INK; c.lineWidth = 5; foreach (var (x, l) in legs) seg(c, x, -10, x + l * .6, 0);
        c.strokeStyle = pal.leg; c.lineWidth = 3; foreach (var (x, l) in legs) seg(c, x, -10, x + l * .6, 0);
        c.translate(0, -bob); c.strokeStyle = INK; c.lineWidth = 2.4;
        c.fillStyle = fur; c.beginPath(); c.moveTo(-12, -18); c.quadraticCurveTo(-34, -14, -32, -32); c.quadraticCurveTo(-22, -30, -12, -24); c.closePath(); c.fill(); c.stroke();
        c.fillStyle = belly; c.beginPath(); c.moveTo(-28, -25); c.quadraticCurveTo(-34, -27, -32, -32); c.quadraticCurveTo(-27, -31, -25, -29); c.closePath(); c.fill();
        c.fillStyle = fur; c.beginPath(); c.ellipse(0, -17, 16, 9.5, 0, 0, TAU); c.fill(); c.stroke();
        if (wolf) { c.fillStyle = fl ? "#fff" : "rgba(40,44,52,.35)"; c.beginPath(); c.ellipse(-2, -23, 11, 3.5, 0, 0, TAU); c.fill(); }
        c.fillStyle = belly; c.beginPath(); c.ellipse(2, -12.5, 9, 3.5, 0, 0, TAU); c.fill();
        c.fillStyle = fur; double et = wolf ? -44 : -42;
        c.beginPath(); c.moveTo(8, -31); c.lineTo(10, et); c.lineTo(15, -32); c.closePath(); c.fill(); c.stroke();
        c.beginPath(); c.moveTo(14, -31); c.lineTo(19, et + 1); c.lineTo(20, -29); c.closePath(); c.fill(); c.stroke();
        c.beginPath(); c.arc(14, -25, 8.5, 0, TAU); c.fill(); c.stroke();
        c.fillStyle = belly; c.beginPath(); c.moveTo(15, -24); c.lineTo(wolf ? 29 : 28, -22); c.lineTo(16, -18); c.closePath(); c.fill(); c.stroke();
        c.fillStyle = INK; c.beginPath(); c.arc(wolf ? 28.5 : 27.5, -22, 2, 0, TAU); c.fill();
        c.fillStyle = pal.eye; c.lineWidth = 1.4; c.beginPath(); c.ellipse(16, -27, 2.6, 2, 0, 0, TAU); c.fill(); c.stroke();
        c.fillStyle = INK; c.beginPath(); c.arc(16.8, -27, 1.1, 0, TAU); c.fill();
        c.lineWidth = 2.4; seg(c, 12, -31, 19, -29);
        if (e.type == "ataman")
        {
            c.fillStyle = "#1d1d22"; rr(c, 6, -46, 17, 11, 4); c.fill(); c.stroke();
            c.strokeStyle = "#3a3a44"; c.lineWidth = 1.2; for (int i = 0; i < 4; i++) seg(c, 8 + i * 4, -44, 9 + i * 4, -37);
            c.strokeStyle = INK; c.lineWidth = 2; c.fillStyle = "#e8322a"; rr(c, 6, -37, 17, 3, 1); c.fill(); c.stroke();
            c.lineWidth = 1.4; seg(c, 9, -30, 22, -25); circ(c, 16.5, -27, 3.4, "#1d1d22");
        }
        if (e.type == "wolfboss")
        {
            c.strokeStyle = "#e8d6d0"; c.lineWidth = 1.8; seg(c, 12, -33, 20, -22);
            c.fillStyle = fur; c.strokeStyle = INK; c.lineWidth = 2; foreach (var x in new[] { -8, -2, 4 }) { c.beginPath(); c.moveTo(x - 3, -25); c.lineTo(x, -32); c.lineTo(x + 3, -25); c.closePath(); c.fill(); c.stroke(); }
        }
        c.restore();
    }
    static void drawTurkey(Ctx c, Enemy e)
    {
        double s = Sz(e); bool metal = e.type == "steelturkey";
        c.save(); c.translate(e.x, e.y); c.scale(s, s); shadow(c, 20, 6);
        double bob = e.moving ? Math.Abs(Math.Sin(e.phase)) * 2.5 : 0, lg = e.moving ? Math.Sin(e.phase) * 5 : 0;
        c.save(); c.scale(e.face, 1); c.lineCap = "round"; c.lineJoin = "round";
        c.strokeStyle = INK; c.lineWidth = 5.5; seg(c, -5, -12, -5 + lg, 0); seg(c, 5, -12, 5 - lg, 0);
        c.strokeStyle = metal ? "#8a939e" : "#d9862a"; c.lineWidth = 3; seg(c, -5, -12, -5 + lg, 0); seg(c, 5, -12, 5 - lg, 0);
        c.translate(0, -bob); c.strokeStyle = INK; c.lineWidth = 2.4;
        bool fl = e.flash > 0; var fan = metal ? new[] { "#6f7782", "#c9d0d8", "#8a939e", "#c9d0d8", "#6f7782", "#c9d0d8", "#8a939e" } : new[] { "#7a4a26", "#e8d3a8", "#a3592a", "#e8d3a8", "#7a4a26", "#e8d3a8", "#a3592a" };
        for (int i = 0; i < 7; i++) { double a0 = Math.PI * .55 + i * .16, a1 = a0 + .16; c.fillStyle = fl ? "#fff" : fan[i]; c.beginPath(); c.moveTo(-8, -26); c.arc(-8, -26, 30, a0 + Math.PI * .35, a1 + Math.PI * .35); c.closePath(); c.fill(); c.stroke(); }
        c.fillStyle = fl ? "#fff" : metal ? "#7d8590" : "#5b3a22"; c.beginPath(); c.ellipse(0, -23, 18, 14.5, 0, 0, TAU); c.fill(); c.stroke();
        c.fillStyle = fl ? "#fff" : metal ? "#5f6670" : "#46301d"; c.beginPath(); c.ellipse(-3, -22, 9, 7, -.3, 0, TAU); c.fill(); c.stroke();
        if (metal) { c.fillStyle = "#c9d0d8"; foreach (var (x, y) in new[] { (-12.0, -28.0), (-6.0, -14.0), (8.0, -30.0), (10.0, -16.0), (0.0, -34.0) }) { c.beginPath(); c.arc(x, y, 1.3, 0, TAU); c.fill(); } }
        if (e.type == "gturkey") { c.fillStyle = "#ffc93a"; rr(c, 5, -31, 7, 3.5, 1.5); c.fill(); c.stroke(); c.lineWidth = 1; for (int i = 0; i < 3; i++) seg(c, 6 + i * 2.5, -27.5, 6 + i * 2.5, -25); c.lineWidth = 2.4; }
        c.fillStyle = fl ? "#fff" : metal ? "#b8c0ca" : "#9db3d4"; c.beginPath(); c.moveTo(9, -30); c.quadraticCurveTo(12, -40, 15, -44); c.lineTo(20, -41); c.quadraticCurveTo(17, -34, 16, -28); c.closePath(); c.fill(); c.stroke();
        c.beginPath(); c.arc(17, -44, 6.5, 0, TAU); c.fill(); c.stroke();
        c.fillStyle = "#d6312a"; c.beginPath(); c.ellipse(19, -35.5, 3, 5.5, .2, 0, TAU); c.fill(); c.stroke();
        c.beginPath(); c.moveTo(21, -47); c.quadraticCurveTo(26, -44, 24, -36); c.lineTo(22, -37); c.quadraticCurveTo(23, -43, 20, -45); c.closePath(); c.fill(); c.stroke();
        c.fillStyle = metal ? "#8a939e" : "#f0b13a"; c.beginPath(); c.moveTo(22, -45); c.lineTo(27.5, -43); c.lineTo(22, -41); c.closePath(); c.fill(); c.stroke();
        if (metal)
        {
            c.fillStyle = "#ff3b1f"; c.lineWidth = 1.4; c.beginPath(); c.arc(18, -46, 2.8, 0, TAU); c.fill(); c.stroke(); c.fillStyle = "rgba(255,90,60,.4)"; c.beginPath(); c.arc(18, -46, 5, 0, TAU); c.fill();
            c.lineWidth = 1.6; seg(c, 14, -50, 11, -58); circ(c, 11, -59, 2, "#ff3b1f");
        }
        else
        {
            c.fillStyle = "#fff"; c.lineWidth = 1.4; c.beginPath(); c.arc(18, -46, 2.6, 0, TAU); c.fill(); c.stroke();
            c.fillStyle = INK; c.beginPath(); c.arc(18.8, -46, 1.3, 0, TAU); c.fill();
        }
        c.lineWidth = 2.4; seg(c, 14.5, -50, 21, -48);
        if (e.type == "gturkey") { c.fillStyle = "#3e5a2c"; c.beginPath(); c.moveTo(10, -50); c.lineTo(24, -50); c.lineTo(22, -56); c.lineTo(12, -56); c.closePath(); c.fill(); c.stroke(); rr(c, 9, -51, 18, 3, 1.5); c.fillStyle = "#2e4420"; c.fill(); c.stroke(); circ(c, 17, -53.5, 1.6, "#e8322a", false); }
        c.restore();
        c.translate(e.face * 4, -20 - bob); c.rotate(e.ang); if (Math.Cos(e.ang) < 0) c.scale(1, -1);
        drawGun(c, metal ? "rail" : "blunder");
        c.fillStyle = e.flash > 0 ? "#fff" : metal ? "#7d8590" : "#5b3a22"; c.strokeStyle = INK; c.lineWidth = 2; c.beginPath(); c.arc(4, 1, 4.5, 0, TAU); c.fill(); c.stroke();
        c.restore();
    }
    sealed class CrowPal { public string b, w, head, beak, eye; }
    static readonly Dictionary<string, CrowPal> CROWPAL = new()
    {
        ["crow"] = new() { b = "#2e2a33", w = "#211e26", head = "#2e2a33", beak = "#f2b42c", eye = "#ffd23a" }, ["eagle"] = new() { b = "#6b4a2b", w = "#553a20", head = "#f4f1e8", beak = "#f2b42c", eye = "#ffd23a" },
        ["eagleboss"] = new() { b = "#5a3a1e", w = "#3f2812", head = "#ffffff", beak = "#ffc93a", eye = "#ff5a3c" }, ["emperor"] = new() { b = "#2a2238", w = "#1a1424", head = "#2a2238", beak = "#c9a227", eye = "#b06cff" },
    };
    static void drawCrow(Ctx c, Enemy e)
    {
        double s = Sz(e); var pal = CROWPAL.TryGetValue(e.type, out var p0) ? p0 : CROWPAL["crow"]; double fz = (e.st == "dash" ? 18 : 34 + Math.Sin(e.phase * .4) * 5), flap = Math.Sin(e.phase * 1.4);
        c.save(); c.translate(e.x, e.y); c.fillStyle = "rgba(25,50,10,.22)"; c.beginPath(); c.ellipse(0, 1, 11 * s, 4 * s, 0, 0, TAU); c.fill();
        c.scale(s, s); c.translate(0, -fz); c.scale(e.face, 1); c.lineJoin = "round"; c.strokeStyle = INK; c.lineWidth = 2.2;
        bool fl = e.flash > 0; string b = fl ? "#fff" : pal.b, w = fl ? "#fff" : pal.w, hd = fl ? "#fff" : pal.head;
        if (e.type == "emperor") { c.fillStyle = fl ? "#fff" : "#6a2c91"; c.beginPath(); c.moveTo(-2, -12); c.lineTo(-26, 8); c.lineTo(8, 6); c.closePath(); c.fill(); c.stroke(); }
        c.fillStyle = w; c.beginPath(); c.moveTo(-2, -8); c.lineTo(-14, -8 - 14 * flap); c.lineTo(4, -6); c.closePath(); c.fill(); c.stroke();
        c.fillStyle = b; c.beginPath(); c.moveTo(-12, -6); c.lineTo(-23, -11); c.lineTo(-22, -2); c.closePath(); c.fill(); c.stroke();
        c.beginPath(); c.ellipse(0, -6, 13, 8, 0, 0, TAU); c.fill(); c.stroke();
        circ(c, 11, -12, 6.5, hd);
        c.fillStyle = pal.beak; c.beginPath();
        if (e.type == "crow" || e.type == "emperor") { c.moveTo(16, -14.5); c.lineTo(25, -11.5); c.lineTo(16, -9); }
        else { c.moveTo(15.5, -15); c.quadraticCurveTo(25, -15, 23, -8); c.lineTo(20, -10); c.lineTo(15.5, -9); }
        c.closePath(); c.fill(); c.stroke();
        c.lineWidth = 1.2; circ(c, 13, -13.5, 2.4, pal.eye); circ(c, 13.6, -13.5, 1.1, INK, false); c.lineWidth = 2; seg(c, 10, -17, 16, -15);
        if (e.type == "emperor") { c.fillStyle = "#ffc93a"; c.lineWidth = 1.6; c.beginPath(); c.moveTo(6, -17); c.lineTo(6, -24); c.lineTo(9, -20); c.lineTo(11.5, -26); c.lineTo(14, -20); c.lineTo(17, -24); c.lineTo(17, -17); c.closePath(); c.fill(); c.stroke(); circ(c, 11.5, -19, 1.2, "#e8322a", false); }
        c.fillStyle = w; c.lineWidth = 2.2; c.beginPath(); c.moveTo(-3, -7); c.lineTo(-16, -7 + 14 * flap * .8); c.lineTo(5, -4); c.closePath(); c.fill(); c.stroke();
        c.restore();
    }
    static void drawOwl(Ctx c, Enemy e)
    {
        double s = Sz(e); c.save(); c.translate(e.x, e.y); c.scale(s, s); shadow(c, 13, 5); c.scale(e.face, 1); c.lineJoin = "round"; c.strokeStyle = INK; c.lineWidth = 2.4;
        bool fl = e.flash > 0; string b = fl ? "#fff" : "#8a6a45", d = fl ? "#fff" : "#6b4f31", bl = fl ? "#fff" : "#e8d6b0";
        c.strokeStyle = INK; c.lineWidth = 4; seg(c, -3, -3, -3, 0); seg(c, 4, -3, 4, 0); c.strokeStyle = "#f2b42c"; c.lineWidth = 2; seg(c, -3, -3, -3, 0); seg(c, 4, -3, 4, 0); c.strokeStyle = INK; c.lineWidth = 2.4;
        c.fillStyle = b; c.beginPath(); c.moveTo(-5, -32); c.lineTo(-2, -41); c.lineTo(2, -32); c.closePath(); c.fill(); c.stroke(); c.beginPath(); c.moveTo(9, -32); c.lineTo(13, -41); c.lineTo(15, -31); c.closePath(); c.fill(); c.stroke();
        c.beginPath(); c.ellipse(4, -19, 14, 17, 0, 0, TAU); c.fill(); c.stroke();
        c.fillStyle = bl; c.beginPath(); c.ellipse(6, -14, 8, 10, 0, 0, TAU); c.fill();
        c.strokeStyle = d; c.lineWidth = 1.3; foreach (var (x, y) in new[] { (3.0, -16.0), (8.0, -14.0), (5.0, -10.0), (9.0, -8.0) }) { c.beginPath(); c.moveTo(x - 2, y - 1); c.lineTo(x, y + 1); c.lineTo(x + 2, y - 1); c.stroke(); }
        c.strokeStyle = INK; c.lineWidth = 2.4; c.fillStyle = d; c.beginPath(); c.ellipse(-6, -17, 5.5, 11, .15, 0, TAU); c.fill(); c.stroke();
        c.lineWidth = 1.6; foreach (var (x, y) in new[] { (2.5, -26.0), (10.5, -26.0) }) { circ(c, x, y, 4.6, "#fff"); circ(c, x + .6, y, 3, e.st == "aim" ? "#ff5a3c" : "#ff9f1c", false); circ(c, x + .9, y, 1.4, INK, false); }
        c.fillStyle = "#f2b42c"; c.beginPath(); c.moveTo(5, -23); c.lineTo(8, -23); c.lineTo(6.5, -19); c.closePath(); c.fill(); c.stroke();
        c.lineWidth = 2; seg(c, -1, -31, 5, -29); seg(c, 8, -29, 14, -31);
        c.restore();
    }
    static void drawFerret(Ctx c, Enemy e)
    {
        double s = Sz(e); bool dr = e.type == "drferret";
        c.save(); c.translate(e.x, e.y); c.scale(s, s); shadow(c, 17, 4.5);
        double bob = e.moving ? Math.Abs(Math.Sin(e.phase)) * 2 : 0, lg = e.moving ? Math.Sin(e.phase) * 5 : 0;
        c.save(); c.scale(e.face, 1); c.lineCap = "round"; c.lineJoin = "round";
        bool fl = e.flash > 0; string fur = fl ? "#fff" : "#d8b48a", dk = fl ? "#fff" : "#8a5a3a";
        var legs = new[] { (-10.0, lg), (-4.0, -lg), (6.0, -lg), (11.0, lg) };
        c.strokeStyle = INK; c.lineWidth = 4.5; foreach (var (x, l) in legs) seg(c, x, -7, x + l * .5, 0);
        c.strokeStyle = dk; c.lineWidth = 2.6; foreach (var (x, l) in legs) seg(c, x, -7, x + l * .5, 0);
        c.translate(0, -bob); c.strokeStyle = INK; c.lineWidth = 2.2;
        c.fillStyle = dk; c.beginPath(); c.ellipse(-22, -12, 10, 3.6, -.3, 0, TAU); c.fill(); c.stroke();
        c.fillStyle = fur; c.beginPath(); c.ellipse(0, -12, 18, 7.5, 0, 0, TAU); c.fill(); c.stroke();
        c.fillStyle = dk; c.beginPath(); c.ellipse(-2, -17, 13, 2.6, 0, 0, TAU); c.fill();
        if (dr) { c.fillStyle = "#fdfdfb"; c.beginPath(); c.moveTo(-14, -18); c.lineTo(10, -19); c.lineTo(12, -5); c.lineTo(-16, -5); c.closePath(); c.fill(); c.stroke(); c.strokeStyle = "#c9d0d8"; c.lineWidth = 1.2; seg(c, -2, -18, -2, -6); c.strokeStyle = INK; c.lineWidth = 2.2; }
        c.fillStyle = fur; circ(c, 17, -17, 7, fur);
        c.fillStyle = dk; c.beginPath(); c.ellipse(19, -18, 5, 2.6, .1, 0, TAU); c.fill();
        c.fillStyle = fur; c.beginPath(); c.arc(12, -23, 2.6, 0, TAU); c.fill(); c.stroke();
        c.fillStyle = "#f2b8c0"; c.beginPath(); c.arc(24, -16, 1.6, 0, TAU); c.fill();
        c.lineWidth = 1.2; circ(c, 19.5, -18.3, 1.8, "#fff"); circ(c, 20, -18.3, .9, INK, false);
        if (dr) { c.lineWidth = 1.8; circ(c, 15, -24, 3.4, "#7fd4ff"); circ(c, 21, -24, 3.4, "#7fd4ff"); c.fillStyle = "rgba(255,255,255,.7)"; c.fillRect(13.6, -25.6, 1.6, 1.4); }
        else { c.fillStyle = "#4a6430"; c.lineWidth = 2; c.beginPath(); c.arc(17, -20, 7.4, Math.PI * 1.05, Math.PI * 1.95); c.closePath(); c.fill(); c.stroke(); }
        c.restore();
        c.translate(e.face * 6, -13 - bob); c.rotate(e.ang); if (Math.Cos(e.ang) < 0) c.scale(1, -1);
        drawGun(c, dr ? "rail" : "pistol");
        c.restore();
    }
    static void drawBadger(Ctx c, Enemy e)
    {
        double s = Sz(e); c.save(); c.translate(e.x, e.y); c.scale(s, s); shadow(c, 22, 6);
        double bob = e.moving ? Math.Abs(Math.Sin(e.phase)) * 2 : 0, lg = e.moving ? Math.Sin(e.phase) * 5 : 0;
        c.scale(e.face, 1); c.lineCap = "round"; c.lineJoin = "round";
        bool fl = e.flash > 0; string fur = fl ? "#fff" : "#8d8f94", dk = fl ? "#fff" : "#2b2b30";
        var legs = new[] { (-12.0, lg), (-5.0, -lg), (7.0, -lg), (13.0, lg) };
        c.strokeStyle = INK; c.lineWidth = 6; foreach (var (x, l) in legs) seg(c, x, -9, x + l * .5, 0);
        c.strokeStyle = dk; c.lineWidth = 3.6; foreach (var (x, l) in legs) seg(c, x, -9, x + l * .5, 0);
        c.strokeStyle = "#e8e2d8"; c.lineWidth = 1.6; foreach (var x in new[] { 13, 7 }) seg(c, x + lg * .5, 0, x + 3 + lg * .5, 1);
        c.translate(0, -bob); c.strokeStyle = INK; c.lineWidth = 2.4;
        c.fillStyle = fur; c.beginPath(); c.ellipse(0, -15, 21, 12, 0, 0, TAU); c.fill(); c.stroke();
        c.strokeStyle = "rgba(43,29,20,.35)"; c.lineWidth = 1.4; for (int x = -14; x < 12; x += 5) seg(c, x, -25, x - 2, -19); c.strokeStyle = INK; c.lineWidth = 2.4;
        c.fillStyle = "#f4f1e8"; c.beginPath(); c.ellipse(19, -18, 10, 8, 0, 0, TAU); c.fill(); c.stroke();
        c.save(); c.beginPath(); c.ellipse(19, -18, 9, 7, 0, 0, TAU); c.clip(); c.fillStyle = dk; c.fillRect(12, -26, 4, 16); c.fillRect(21, -26, 4, 16); c.restore();
        c.fillStyle = INK; c.beginPath(); c.arc(29, -17, 2.2, 0, TAU); c.fill();
        c.lineWidth = 1.2; circ(c, 22.5, -20, 1.8, "#ff5a3c"); c.lineWidth = 2.2; seg(c, 20, -24, 26, -22);
        c.fillStyle = "#f4f1e8"; c.beginPath(); c.arc(16, -26, 2.6, 0, TAU); c.fill(); c.stroke();
        c.restore();
    }
    static void drawRaccoon(Ctx c, Enemy e)
    {
        c.save(); c.translate(e.x, e.y);
        if (e.st == "tell") c.translate(rand(-1.5, 1.5), 0);
        shadow(c, 16, 5);
        double bob = e.moving ? Math.Abs(Math.Sin(e.phase)) * 2.5 : 0, lg = e.moving ? Math.Sin(e.phase) * 6 : 0;
        c.scale(e.face, 1); c.lineCap = "round"; c.lineJoin = "round";
        bool fl = e.flash > 0; string fur = fl ? "#fff" : "#8b8f98", lt = fl ? "#fff" : "#c9ccd2";
        var legs = new[] { (-8.0, lg), (-3.0, -lg), (6.0, -lg), (10.0, lg) };
        c.strokeStyle = INK; c.lineWidth = 5; foreach (var (x, l) in legs) seg(c, x, -9, x + l * .6, 0);
        c.strokeStyle = "#3b3d44"; c.lineWidth = 3; foreach (var (x, l) in legs) seg(c, x, -9, x + l * .6, 0);
        c.translate(0, -bob); c.strokeStyle = INK; c.lineWidth = 2.4;
        c.save(); c.translate(-14, -18); c.rotate(-.7); c.fillStyle = fur; c.beginPath(); c.ellipse(-8, 0, 12, 5.5, 0, 0, TAU); c.fill(); c.stroke();
        c.fillStyle = fl ? "#fff" : "#3b3d44"; foreach (var x in new[] { -14, -8, -2 }) c.fillRect(x, -5, 2.6, 10); c.strokeStyle = INK; c.beginPath(); c.ellipse(-8, 0, 12, 5.5, 0, 0, TAU); c.stroke(); c.restore();
        c.fillStyle = fur; c.beginPath(); c.ellipse(0, -15, 15, 10, 0, 0, TAU); c.fill(); c.stroke();
        c.fillStyle = lt; c.beginPath(); c.ellipse(2, -11, 8, 3.5, 0, 0, TAU); c.fill();
        c.fillStyle = fur; c.beginPath(); c.moveTo(7, -29); c.lineTo(8, -36); c.lineTo(13, -30); c.closePath(); c.fill(); c.stroke(); c.beginPath(); c.moveTo(14, -30); c.lineTo(18, -36); c.lineTo(19, -28); c.closePath(); c.fill(); c.stroke();
        circ(c, 13, -23, 8.5, fur);
        c.fillStyle = lt; c.beginPath(); c.moveTo(14, -21); c.lineTo(25, -20); c.lineTo(15, -16); c.closePath(); c.fill(); c.stroke();
        c.fillStyle = INK; c.beginPath(); c.arc(24.5, -20, 1.8, 0, TAU); c.fill();
        c.fillStyle = "#1d1d22"; c.beginPath(); c.ellipse(15, -25, 6, 3.2, .15, 0, TAU); c.fill();
        c.lineWidth = 1.2; circ(c, 16, -25.3, 1.9, "#fff"); circ(c, 16.6, -25.3, .9, INK, false);
        c.restore();
    }
    static void drawRat(Ctx c, Enemy e) // мышь: мелкая, светлая, с большими ушами
    {
        c.save(); c.translate(e.x, e.y); shadow(c, 10, 3.5);
        double bob = e.moving ? Math.Abs(Math.Sin(e.phase)) * 1.5 : 0;
        c.scale(e.face, 1); c.lineCap = "round"; c.lineJoin = "round"; c.translate(0, -bob);
        bool fl = e.flash > 0; string fur = fl ? "#fff" : "#a8a097";
        c.strokeStyle = "#e89aa6"; c.lineWidth = 2; c.beginPath(); c.moveTo(-9, -6); c.quadraticCurveTo(-20, -4, -22, -12 + Math.Sin(e.phase) * 2); c.stroke();
        c.strokeStyle = INK; c.lineWidth = 2; c.fillStyle = fur; c.beginPath(); c.ellipse(0, -7, 10, 6, 0, 0, TAU); c.fill(); c.stroke();
        c.beginPath(); c.moveTo(6, -11); c.lineTo(16, -6); c.lineTo(6, -3); c.closePath(); c.fill(); c.stroke();
        circ(c, 15.5, -6, 1.4, "#e89aa6", false);
        c.lineWidth = 1.5; circ(c, 4, -13, 4.4, "#f2b8c0"); circ(c, 9.5, -8.5, 1.3, INK, false);
        c.restore();
    }
    static void drawBigRat(Ctx c, Enemy e) // крыса: крупнее мыши, тёмная, красные глаза, жёлтые резцы
    {
        c.save(); c.translate(e.x, e.y); shadow(c, 16, 5);
        double bob = e.moving ? Math.Abs(Math.Sin(e.phase)) * 2 : 0, lg = e.moving ? Math.Sin(e.phase) * 4 : 0;
        c.scale(e.face, 1); c.lineCap = "round"; c.lineJoin = "round";
        bool fl = e.flash > 0; string fur = fl ? "#fff" : "#5b4f47", belly = fl ? "#fff" : "#8a7d72", pink = fl ? "#fff" : "#d98a96";
        var legs = new[] { (-8.0, lg), (8.0, -lg) };
        c.strokeStyle = INK; c.lineWidth = 4; foreach (var (x, l) in legs) seg(c, x, -6, x + l, 0);
        c.strokeStyle = pink; c.lineWidth = 2.2; foreach (var (x, l) in legs) seg(c, x, -6, x + l, 0);
        c.translate(0, -bob);
        c.strokeStyle = INK; c.lineWidth = 5; c.beginPath(); c.moveTo(-15, -9); c.quadraticCurveTo(-32, -6, -36, -18 + Math.Sin(e.phase) * 3); c.stroke();
        c.strokeStyle = pink; c.lineWidth = 3; c.stroke();
        c.strokeStyle = INK; c.lineWidth = 2.4; c.fillStyle = fur; c.beginPath(); c.ellipse(-1, -11, 16, 9.5, 0, 0, TAU); c.fill(); c.stroke();
        c.fillStyle = belly; c.beginPath(); c.ellipse(2, -6, 10, 3.6, 0, 0, TAU); c.fill();
        c.strokeStyle = "rgba(30,22,18,.5)"; c.lineWidth = 1.6; foreach (var (x, y) in new[] { (-10.0, -17.0), (-5.0, -19.0), (0.0, -19.5) }) seg(c, x, y, x + 3, y + 4);
        c.strokeStyle = INK; c.lineWidth = 2.4; c.fillStyle = fur; c.beginPath(); c.moveTo(9, -18); c.lineTo(25, -10); c.lineTo(9, -5); c.closePath(); c.fill(); c.stroke();
        c.fillStyle = "#fff3a0"; c.lineWidth = 1.3; c.beginPath(); c.rect(20, -8.5, 2.6, 4); c.fill(); c.stroke();
        circ(c, 25, -10, 1.8, pink, false);
        c.lineWidth = 1.8; c.fillStyle = pink; c.beginPath(); c.moveTo(5, -17); c.lineTo(4, -25); c.lineTo(8.5, -23); c.lineTo(10, -26); c.lineTo(12, -18); c.closePath(); c.fill(); c.stroke();
        circ(c, 14, -14, 2.4, fl ? "#fff" : "#ff3b2e", false); circ(c, 14.6, -14.6, .8, "#fff", false);
        c.strokeStyle = INK; c.lineWidth = 2; seg(c, 11, -18, 17, -16.5);
        c.strokeStyle = "rgba(43,29,20,.7)"; c.lineWidth = .9; seg(c, 22, -11, 30, -14); seg(c, 22, -9, 30, -8);
        c.restore();
    }
    public static void drawObstacle(Ctx c, Obs o)
    {
        c.save(); c.translate(o.x, o.y); c.strokeStyle = INK; c.lineWidth = 2.4; c.lineJoin = "round";
        double r = o.r;
        if (o.type == "hay")
        {
            double h = r * 1.05, ry = r * .42;
            c.fillStyle = "rgba(25,50,10,.3)"; c.beginPath(); c.ellipse(7, 3, r * 1.08, ry * 1.1, 0, 0, TAU); c.fill();
            c.fillStyle = "#e3b23f"; c.beginPath(); c.moveTo(-r, -h); c.lineTo(-r, 0); c.ellipse(0, 0, r, ry, 0, Math.PI, 0, true); c.lineTo(r, -h); c.closePath(); c.fill(); c.stroke();
            c.strokeStyle = "#b8862a"; c.lineWidth = 1.6; for (int i = -2; i <= 2; i++) seg(c, i * r * .38, -h + ry * .9, i * r * .38, ry * .9 * Math.Sqrt(1 - (i * .38) * (i * .38)) - 1);
            c.strokeStyle = INK; c.lineWidth = 2.4; c.fillStyle = "#f6d46e"; c.beginPath(); c.ellipse(0, -h, r, ry, 0, 0, TAU); c.fill(); c.stroke();
            c.strokeStyle = "#c99a35"; c.lineWidth = 1.6; c.beginPath(); for (double a = 0; a < TAU * 2.6; a += .25) { var q = a / (TAU * 2.6); c.lineTo(Math.Cos(a) * r * .85 * q, -h + Math.Sin(a) * ry * .85 * q); } c.stroke();
            c.strokeStyle = "#c0392b"; c.lineWidth = 2.2; c.beginPath(); c.moveTo(-r, -h * .55); c.quadraticCurveTo(0, -h * .55 + ry * 1.9, r, -h * .55); c.stroke();
        }
        else if (o.type == "barrel")
        {
            double h = r * 1.7, ry = r * .42;
            c.fillStyle = "rgba(25,50,10,.3)"; c.beginPath(); c.ellipse(6, 3, r * 1.1, ry * 1.1, 0, 0, TAU); c.fill();
            c.fillStyle = "#a8622f"; c.beginPath(); c.moveTo(-r, -h); c.quadraticCurveTo(-r * 1.15, -h / 2, -r, 0); c.ellipse(0, 0, r, ry, 0, Math.PI, 0, true); c.quadraticCurveTo(r * 1.15, -h / 2, r, -h); c.closePath(); c.fill(); c.stroke();
            c.strokeStyle = "#7a4320"; c.lineWidth = 1.5; seg(c, -r * .4, -h + ry, -r * .42, ry * .8); seg(c, r * .4, -h + ry, r * .42, ry * .8);
            c.strokeStyle = "#55555c"; c.lineWidth = 3; foreach (var f in new[] { .25, .75 }) { c.beginPath(); c.ellipse(0, -h * f, r * 1.1, ry, 0, 0, Math.PI); c.stroke(); }
            c.strokeStyle = INK; c.lineWidth = 2.4; c.fillStyle = "#8a4f25"; c.beginPath(); c.ellipse(0, -h, r, ry, 0, 0, TAU); c.fill(); c.stroke();
            c.strokeStyle = "#6a3b1b"; c.lineWidth = 1.4; c.beginPath(); c.ellipse(0, -h, r * .6, ry * .6, 0, 0, TAU); c.stroke();
        }
        else if (o.type == "stump")
        {
            double h = r * .75, ry = r * .42;
            c.fillStyle = "rgba(25,50,10,.3)"; c.beginPath(); c.ellipse(6, 3, r * 1.15, ry * 1.15, 0, 0, TAU); c.fill();
            c.fillStyle = "#7a4a24"; c.beginPath(); c.moveTo(-r, -h); c.lineTo(-r - 4, 0); c.ellipse(0, 0, r + 4, ry + 1, 0, Math.PI, 0, true); c.lineTo(r, -h); c.closePath(); c.fill(); c.stroke();
            c.strokeStyle = "#5a3418"; c.lineWidth = 1.6; for (int i = -2; i <= 2; i++) seg(c, i * r * .35, -h + ry * .8, i * r * .38, ry * .6);
            c.strokeStyle = INK; c.lineWidth = 2.2; c.fillStyle = "#7a4a24";
            foreach (var (a, b, cx, cy) in new[] { (-r - 6, 1.0, -r - 14, 4.0), (r + 5, 0.0, r + 13, 5.0) }) { c.beginPath(); c.moveTo(a, b - 5); c.quadraticCurveTo(cx, cy - 6, cx, cy); c.lineTo(a + 2, b + 2); c.closePath(); c.fill(); c.stroke(); }
            c.fillStyle = "#e8c690"; c.beginPath(); c.ellipse(0, -h, r, ry, 0, 0, TAU); c.fill(); c.stroke();
            c.strokeStyle = "#b88a52"; c.lineWidth = 1.3; foreach (var k in new[] { .72, .48, .24 }) { c.beginPath(); c.ellipse(0, -h, r * k, ry * k, 0, 0, TAU); c.stroke(); }
            c.strokeStyle = "#5a3418"; c.lineWidth = 1.4; seg(c, 0, -h, r * .6, -h - ry * .3);
            c.fillStyle = "#6db343"; c.strokeStyle = INK; c.lineWidth = 1.4; c.beginPath(); c.ellipse(-r * .55, -h * .4, 4, 2.4, -.6, 0, TAU); c.fill(); c.stroke();
        }
        else if (o.type == "bush")
        {
            c.fillStyle = "rgba(25,50,10,.3)"; c.beginPath(); c.ellipse(6, 3, r * 1.15, r * .45, 0, 0, TAU); c.fill();
            var blobs = new[] { (-r * .55, -r * .45, r * .62), (r * .5, -r * .42, r * .6), (0.0, -r * .95, r * .7), (-r * .2, -r * .35, r * .6), (r * .15, -r * .5, r * .55) };
            c.fillStyle = "#4f8f2c"; foreach (var (x, y, b) in blobs) { c.beginPath(); c.arc(x, y, b, 0, TAU); c.fill(); c.stroke(); }
            foreach (var (x, y, b) in blobs) { c.beginPath(); c.arc(x, y, b - 2.4, 0, TAU); c.fill(); }
            c.fillStyle = "#6db343"; foreach (var (x, y, b) in blobs) { c.beginPath(); c.arc(x - b * .25, y - b * .3, b * .45, 0, TAU); c.fill(); }
            c.fillStyle = "#e8322a"; foreach (var (x, y) in new[] { (-r * .5, -r * .6), (r * .35, -r * .9), (r * .55, -r * .35), (-r * .05, -r * 1.2) }) { c.beginPath(); c.arc(x, y, 2.6, 0, TAU); c.fill(); }
        }
        else
        {
            c.fillStyle = "rgba(25,50,10,.3)"; c.beginPath(); c.ellipse(6, 3, r * 1.1, r * .42, 0, 0, TAU); c.fill();
            c.fillStyle = "#a9a39a"; c.beginPath(); c.moveTo(-r, 0); c.lineTo(-r * .95, -r * .55); c.lineTo(-r * .45, -r * 1.05); c.lineTo(r * .3, -r * 1.1); c.lineTo(r * .95, -r * .6); c.lineTo(r, 0); c.quadraticCurveTo(0, r * .4, -r, 0); c.closePath(); c.fill(); c.stroke();
            c.fillStyle = "#c8c2b8"; c.beginPath(); c.moveTo(-r * .75, -r * .55); c.lineTo(-r * .4, -r * .95); c.lineTo(r * .25, -r * 1); c.lineTo(r * .1, -r * .6); c.closePath(); c.fill();
            c.strokeStyle = "#7f796f"; c.lineWidth = 1.5; seg(c, r * .1, -r * .6, r * .5, -r * .2);
        }
        c.restore();
    }
    static void drawPickup(Ctx c, Pickup p)
    {
        double bob = Math.Sin(p.ph * 4) * 3; if (p.t < 3 && Odd(p.t * 8)) return;
        c.save(); c.translate(p.x, p.y);
        c.strokeStyle = "rgba(255,236,150," + Inv(.5 + Math.Sin(p.ph * 6) * .3) + ")"; c.lineWidth = 3; c.beginPath(); c.ellipse(0, 0, 18, 7, 0, 0, TAU); c.stroke();
        c.translate(0, -14 + bob); c.strokeStyle = INK; c.lineWidth = 2.2; c.lineJoin = "round";
        if (p.kind == "corn")
        {
            c.rotate(-.5); c.fillStyle = "#f6c42d"; c.beginPath(); c.ellipse(0, 0, 6, 12, 0, 0, TAU); c.fill(); c.stroke();
            c.fillStyle = "#e0a91f"; for (int y = -8; y <= 8; y += 4) for (int x = -3; x <= 3; x += 3) { c.beginPath(); c.arc(x, y, 1, 0, TAU); c.fill(); }
            c.fillStyle = "#6db343"; c.beginPath(); c.moveTo(0, 12); c.quadraticCurveTo(-11, 6, -8, -6); c.quadraticCurveTo(-4, 4, 0, 8); c.closePath(); c.fill(); c.stroke();
            c.beginPath(); c.moveTo(0, 12); c.quadraticCurveTo(11, 6, 8, -6); c.quadraticCurveTo(4, 4, 0, 8); c.closePath(); c.fill(); c.stroke();
        }
        else if (p.kind == "nade")
        {
            c.fillStyle = "#fffaf0"; c.beginPath(); c.ellipse(0, 0, 8, 10.5, 0, 0, TAU); c.fill(); c.stroke();
            c.fillStyle = "#c0392b"; c.fillRect(-7.6, -1.5, 15.2, 4); c.strokeRect(-7.6, -1.5, 15.2, 4);
        }
        else
        {
            c.fillStyle = "#b97c47"; rr(c, -13, -9, 26, 18, 3); c.fill(); c.stroke();
            c.strokeStyle = "#8a5a2b"; c.lineWidth = 1.6; seg(c, -13, -3, 13, -3); seg(c, -13, 3, 13, 3);
            c.save(); c.translate(-10, -12); c.scale(.62, .62); drawGun(c, p.kind); c.restore();
        }
        c.restore();
    }
    static void drawHole(Ctx c, Hole h)
    {
        double k = h.state == "dig" ? Math.Min(1, h.t / .6) : h.state == "close" ? Math.Max(0, 1 - h.ct) : 1;
        if (k <= 0) return;
        c.save(); c.translate(h.x, h.y); c.scale(k, k); c.strokeStyle = INK; c.lineWidth = 2.2;
        c.fillStyle = "#9a6b3c"; c.beginPath(); c.ellipse(0, 2, 31, 13, 0, 0, TAU); c.fill(); c.stroke();
        c.fillStyle = "#b8834c"; foreach (var (x, y, r) in new[] { (-24.0, -2.0, 6.0), (22.0, -1.0, 5.5), (-8.0, 10.0, 6.0), (13.0, 9.0, 5.0), (0.0, -9.0, 5.0) }) { c.beginPath(); c.arc(x, y, r, 0, TAU); c.fill(); c.stroke(); }
        c.fillStyle = "#2b1d14"; c.beginPath(); c.ellipse(0, 1, 19, 8, 0, 0, TAU); c.fill();
        c.fillStyle = "rgba(255,255,255,.08)"; c.beginPath(); c.ellipse(-4, -1, 10, 3, 0, 0, TAU); c.fill();
        c.restore();
    }
    static void drawMole(Ctx c, Enemy e)
    {
        double s = Sz(e), em = e.emerge > 0 ? Math.Max(.05, 1 - e.emerge / .6) : 1;
        c.save(); c.translate(e.x, e.y);
        if (em >= 1) shadow(c, 13, 4.5);
        c.scale(1, em); c.scale(s * e.face, s); c.lineCap = "round"; c.lineJoin = "round"; c.strokeStyle = INK; c.lineWidth = 2.3;
        bool fl = e.flash > 0; string fur = fl ? "#fff" : "#6b5a52", lt = fl ? "#fff" : "#8a776d", pink = fl ? "#fff" : "#f2b8c0";
        double bob = e.moving ? Math.Abs(Math.Sin(e.phase)) * 2 : 0; c.translate(0, -bob);
        c.fillStyle = fur; c.beginPath(); c.ellipse(0, -13, 13, 13, 0, 0, TAU); c.fill(); c.stroke();
        c.fillStyle = lt; c.beginPath(); c.ellipse(2, -9, 8, 7, 0, 0, TAU); c.fill();
        c.fillStyle = pink; c.beginPath(); c.ellipse(13, -14, 5.5, 3.8, 0, 0, TAU); c.fill(); c.stroke();
        c.fillStyle = "#e8708a"; c.beginPath(); c.arc(17.5, -14.5, 2.2, 0, TAU); c.fill();
        c.fillStyle = INK; foreach (var (x, y) in new[] { (6.0, -19.0), (10.0, -19.5) }) { c.beginPath(); c.arc(x, y, 1.2, 0, TAU); c.fill(); }
        double sw = e.moving ? Math.Sin(e.phase) * 2 : 0;
        foreach (var (x, y) in new[] { (9.0, -5 + sw), (-6.0, -4 - sw) })
        {
            c.fillStyle = pink; c.beginPath(); c.ellipse(x, y, 5, 3.2, 0, 0, TAU); c.fill(); c.stroke();
            c.lineWidth = 1.2; for (int i = -1; i <= 1; i++) seg(c, x + 3, y + i * 1.6, x + 6, y + i * 1.8); c.lineWidth = 2.3;
        }
        c.fillStyle = fl ? "#fff" : "#ffc93a"; c.beginPath(); c.arc(0, -21, 10.5, Math.PI * 1.05, Math.PI * 1.95); c.closePath(); c.fill(); c.stroke();
        c.fillStyle = fl ? "#fff" : "#e0a91f"; c.fillRect(-11, -22.5, 22, 2.6); c.strokeRect(-11, -22.5, 22, 2.6);
        c.fillStyle = "#fff6a0"; c.beginPath(); c.arc(8.5, -27, 3, 0, TAU); c.fill(); c.stroke();
        if (!fl) { c.globalAlpha = .25; c.fillStyle = "#fff6a0"; c.beginPath(); c.moveTo(10, -28); c.lineTo(34, -36); c.lineTo(34, -18); c.closePath(); c.fill(); c.globalAlpha = 1; }
        c.restore();
    }
    static Sprite EnemyBird(Enemy e, string gun, bool robo)
    {
        return new Sprite
        {
            x = e.x, y = e.y, face = e.face, ang = e.ang, phase = e.phase, moving = e.moving, flash = e.flash, sp = "hen", kind = "hen", gun = gun, robo = robo,
            body = robo ? "#9aa3ad" : e.body, wing = robo ? "#6f7782" : e.wing
        };
    }
    static void drawEnemy(Ctx c, Enemy e)
    {
        var Tt = e.T; double s = Sz(e);
        if (e.st == "tell")
        {
            double len = (Tt.dash > 0 ? Tt.dash : 520) * (e.dashLen > 0 ? e.dashLen : .5);
            c.save(); c.globalAlpha = .25 + Math.Sin(S.t * 30) * .1; c.translate(e.x, e.y); c.rotate(Math.Atan2(e.dy, e.dx)); c.fillStyle = "#e8322a"; c.fillRect(0, -e.r * .8, len, e.r * 1.6); c.restore();
        }
        var t = e.type; bool hopW = e.hop && inWater(e.x, e.y); // курица над водой: подлетает, машет крыльями, по воде круги
        if (hopW)
        {
            var k = (S.t * 1.6 + e.phase) % 1; c.save(); c.strokeStyle = "rgba(255,255,255," + Inv(.7 * (1 - k)) + ")"; c.lineWidth = 2; c.beginPath(); c.ellipse(e.x, e.y, e.r * (1 + k * 1.4), e.r * (1 + k * 1.4) * .4, 0, 0, TAU); c.stroke(); c.restore();
            c.save(); c.translate(0, -14 * s - Math.Abs(Math.Sin(S.t * 16 + e.phase)) * 5);
        }
        if (t is "fox" or "ataman" or "wolf" or "wolfboss") drawFox(c, e);
        else if (t is "turkey" or "gturkey" or "steelturkey") drawTurkey(c, e);
        else if (t is "crow" or "eagle" or "eagleboss" or "emperor") drawCrow(c, e);
        else if (t == "raccoon") drawRaccoon(c, e);
        else if (t == "rat") drawRat(c, e);
        else if (t == "bigrat") drawBigRat(c, e);
        else if (t == "owl") drawOwl(c, e);
        else if (t is "ferret" or "drferret") drawFerret(c, e);
        else if (t == "badger") drawBadger(c, e);
        else if (t == "mole") drawMole(c, e);
        else if (t == "robohen") drawBird(c, EnemyBird(e, "zapper", true));
        else drawBird(c, EnemyBird(e, "eggun", false));
        if (hopW)
        {
            double fl = Math.Sin(S.t * 30 + e.phase) * .6; c.strokeStyle = "rgba(255,255,255,.85)"; c.lineWidth = 2.4; c.lineCap = "round";
            foreach (var sd in new[] { -1, 1 }) { c.beginPath(); c.moveTo(e.x + sd * 10 * s, e.y - 24 * s); c.quadraticCurveTo(e.x + sd * 24 * s, e.y - (34 + fl * 10) * s, e.x + sd * 30 * s, e.y - (22 + fl * 12) * s); c.stroke(); }
            c.restore();
        }
        double top = Tt.fly ? -(60 * s) : -(t == "rat" ? 22 : t == "bigrat" ? 30 : t == "ferret" ? 32 : 52) * s;
        if (e.frozen > 0) { c.save(); c.globalAlpha = .7; c.fillStyle = "#d8f3ff"; c.strokeStyle = "#5fb2dc"; c.lineWidth = 2.5; rr(c, e.x - e.r * 1.3, e.y - e.r * 2.6 * s, e.r * 2.6, e.r * 2.7 * s, 6); c.fill(); c.stroke(); c.fillStyle = "rgba(255,255,255,.8)"; c.fillRect(e.x - e.r, e.y - e.r * 2.3 * s, 4, e.r * 1.6); c.restore(); }
        if (e.chill > 0) { c.save(); c.globalAlpha = .32; c.fillStyle = "#9fdcff"; c.beginPath(); c.ellipse(e.x, e.y + (Tt.fly ? -38 * s : -18 * s), e.r * 1.3, e.r * 1.15, 0, 0, TAU); c.fill(); c.restore(); }
        if (e.stun > 0) drawStars(c, e.x, e.y + top + 4);
        if (e.kami)
        {
            bool bl = Odd(S.t * (e.kamiT < 1.2 ? 16 : 8)); c.save(); c.globalAlpha = bl ? .5 : .15; c.fillStyle = "#ff3b1f"; c.beginPath(); c.ellipse(e.x, e.y - 18 * s, e.r * 1.4, e.r * 1.3, 0, 0, TAU); c.fill(); c.restore();
            double bx = e.x, by = e.y + top - 6; c.fillStyle = "#2b2b33"; c.strokeStyle = INK; c.lineWidth = 1.8; c.beginPath(); c.arc(bx, by, 6, 0, TAU); c.fill(); c.stroke();
            c.strokeStyle = "#c9973f"; c.lineWidth = 2; c.beginPath(); c.moveTo(bx + 3, by - 4); c.quadraticCurveTo(bx + 7, by - 10, bx + 10, by - 9); c.stroke();
            c.fillStyle = bl ? "#ffd23a" : "#ff7a1a"; c.beginPath(); c.arc(bx + 10, by - 9, 2.6 + rnd() * 1.2, 0, TAU); c.fill();
        }
        if (e.st == "tell" || e.st == "aim")
        {
            c.font = "900 18px Rubik"; c.textAlign = "center"; c.lineWidth = 4; c.strokeStyle = INK; c.lineJoin = "round"; c.strokeText("!", e.x, e.y + top - 4);
            c.fillStyle = e.st == "aim" ? "#ff5a3c" : "#ffc93a"; c.fillText("!", e.x, e.y + top - 4);
        }
    }
    static void drawWarnings(Ctx c)
    {
        foreach (var e in enemies)
        {
            if (e.dead || e.st != "aim") continue; double s = Sz(e), a = .45 + Math.Sin(S.t * 30) * .25;
            c.save(); c.globalAlpha = a; c.strokeStyle = "#ff3b2e"; c.lineWidth = 2.2; c.setLineDash(new double[] { 10, 6 }); seg(c, e.x, e.y - 20 * s, e.x + e.dx * 720, e.y - 20 * s + e.dy * 720); c.setLineDash(null); c.restore();
        }
        if (S.night)
        {
            foreach (var b in bombs) { if (b.fromP) continue; c.save(); c.globalAlpha = .9; c.strokeStyle = "#ff5a3c"; c.lineWidth = 2.5; c.beginPath(); c.ellipse(b.x, b.y, b.r, b.r * .42, 0, 0, TAU); c.stroke(); c.restore(); }
            foreach (var e in enemies)
            {
                if (e.dead || e.st != "tell") continue; double len = (e.T.dash > 0 ? e.T.dash : 520) * (e.dashLen > 0 ? e.dashLen : .5);
                c.save(); c.globalAlpha = .3; c.translate(e.x, e.y); c.rotate(Math.Atan2(e.dy, e.dx)); c.fillStyle = "#e8322a"; c.fillRect(0, -e.r * .8, len, e.r * 1.6); c.restore();
            }
        }
    }
}
