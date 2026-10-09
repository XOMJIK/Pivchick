using System;
using System.Collections.Generic;
using Kuriatnyk.Render;

namespace Kuriatnyk;

/// <summary>То, что рисует drawBird: поза + образ (в JS это был объект {...P.look, x, y, …}).</summary>
public sealed class Sprite
{
    public double x, y, ang, phase, flash, swing; public int face = 1; public bool moving, blink, swim, ghost, robo;
    public string kind, gun, buff, sp, special, body, wing, tail, head, hat;
    public Sprite With(Look l)
    {
        sp = l.sp; special = l.special; body = l.body; wing = l.wing; tail = l.tail; head = l.head; hat = l.hat; robo = l.robo; return this;
    }
}

// Отрисовка: оружие, птицы, шапки (порт первой трети web/js/draw.js — функции один в один).
public static partial class G
{
    static long Fl(double v) => (long)Math.Floor(v);
    static bool Odd(double v) => Fl(v) % 2 != 0;

    public static void seg(Ctx c, double x1, double y1, double x2, double y2) { c.beginPath(); c.moveTo(x1, y1); c.lineTo(x2, y2); c.stroke(); }
    public static void rr(Ctx c, double x, double y, double w, double h, double r)
    {
        c.beginPath(); c.moveTo(x + r, y); c.arcTo(x + w, y, x + w, y + h, r); c.arcTo(x + w, y + h, x, y + h, r); c.arcTo(x, y + h, x, y, r); c.arcTo(x, y, x + w, y, r); c.closePath();
    }
    static void shadow(Ctx c, double rx, double ry) { c.fillStyle = "rgba(25,50,10,.28)"; c.beginPath(); c.ellipse(0, 1, rx, ry, 0, 0, TAU); c.fill(); }
    public static void circ(Ctx c, double x, double y, double r, string f, bool s = true) { c.fillStyle = f; c.beginPath(); c.arc(x, y, r, 0, TAU); c.fill(); if (s) c.stroke(); }
    // фигурка: 0 звезда, 1 сердце, 2 кубик, 3 треугольник
    static void figShape(Ctx c, int k, double r)
    {
        c.beginPath();
        if (k == 0) { for (int i = 0; i < 10; i++) { double a = -Math.PI / 2 + i * Math.PI / 5, q = i % 2 == 1 ? r * .45 : r; c.lineTo(Math.Cos(a) * q, Math.Sin(a) * q); } }
        else if (k == 1) { c.moveTo(0, r * .9); c.bezierCurveTo(-r * 1.5, -r * .1, -r * .6, -r * 1.2, 0, -r * .35); c.bezierCurveTo(r * .6, -r * 1.2, r * 1.5, -r * .1, 0, r * .9); }
        else if (k == 2) { c.rect(-r * .75, -r * .75, r * 1.5, r * 1.5); }
        else { c.moveTo(0, -r); c.lineTo(r * .95, r * .7); c.lineTo(-r * .95, r * .7); }
        c.closePath();
    }
    static readonly string[] FIGCOL = { "#ffe066", "#ff6fa8", "#6fd8ff", "#9cf27a" };

    public static void drawGun(Ctx c, string type)
    {
        c.strokeStyle = INK; c.lineWidth = 2; c.lineJoin = "round";
        void box(double x, double y, double w, double h, double r, string f) { c.fillStyle = f; rr(c, x, y, w, h, r); c.fill(); c.stroke(); }
        if (type == "pistol" || type == "eggun")
        {
            box(3, -3.5, 15, 7, 2, type == "eggun" ? "#d9822b" : "#3d3f4a"); box(5, 0, 5, 8, 1.5, type == "eggun" ? "#a65e1c" : "#2a2b33");
        }
        else if (type == "sheriff")
        {
            c.fillStyle = "#8a5a2b"; c.beginPath(); c.moveTo(1, -1); c.lineTo(7, -1); c.lineTo(5, 9); c.lineTo(-1, 8); c.closePath(); c.fill(); c.stroke();
            box(9, -2.8, 19, 4.6, 1.5, "#6b6f7c"); box(4, -5, 9, 8, 3, "#4a4d58");
            c.strokeStyle = "#2a2b33"; c.lineWidth = 1; seg(c, 6.5, -4, 6.5, 2); seg(c, 10, -4, 10, 2);
        }
        else if (type == "millet")
        {
            box(2, -4.5, 21, 8, 2.5, "#e0a83a"); box(21, -2.5, 8, 4, 1, "#3d3f4a");
            c.fillStyle = "#8a5a2b"; c.beginPath(); c.arc(11, 6, 5, 0, TAU); c.fill(); c.stroke();
            c.fillStyle = "#ffe066"; foreach (var (x, y) in new[] { (9.0, 5.0), (12.5, 7.0), (11.0, 4.0) }) { c.beginPath(); c.arc(x, y, 1.1, 0, TAU); c.fill(); }
        }
        else if (type == "sawed")
        {
            box(-5, -3, 10, 7.5, 2, "#8a5a2b"); box(4, -5, 16, 4.5, 1.5, "#3d3f4a"); box(4, -.6, 16, 4.5, 1.5, "#3d3f4a");
        }
        else if (type == "popcorn")
        {
            box(0, -6, 20, 11, 4, "#d8302a");
            c.fillStyle = "#fff"; foreach (var x in new[] { 4, 10, 16 }) c.fillRect(x, -5, 2.6, 9);
            c.strokeStyle = INK; c.lineWidth = 2; rr(c, 0, -6, 20, 11, 4); c.stroke();
            box(19, -4, 8, 7, 2, "#fff4dc");
            foreach (var (x, y, r) in new[] { (4.0, -8.0, 3.0), (9.0, -9.0, 3.4), (14.0, -8.0, 3.0) }) circ(c, x, y, r, "#fffbe8");
        }
        else if (type == "freeze")
        {
            box(0, -5, 18, 9, 3, "#e6f6ff"); box(17, -3, 9, 5, 1.5, "#7fd4ff"); circ(c, 6, -9, 4.5, "#9fe3ff");
            c.strokeStyle = "#3b8fd1"; c.lineWidth = 1.2; for (int k = 0; k < 3; k++) { var a = k * Math.PI / 3; seg(c, 6 - Math.Cos(a) * 3, -9 - Math.Sin(a) * 3, 6 + Math.Cos(a) * 3, -9 + Math.Sin(a) * 3); }
        }
        else if (type == "flame")
        {
            box(-7, -6, 9, 13, 4, "#e8322a"); box(1, -3.5, 18, 7, 2, "#55585f"); box(18, -4.5, 6, 9, 2, "#3d3f4a");
            c.fillStyle = Odd(S.t * 20) ? "#ffd23a" : "#ff7a1a"; c.beginPath(); c.arc(27, 0, 2.6, 0, TAU); c.fill();
        }
        else if (type == "minigun")
        {
            box(-3, -6, 13, 12, 3, "#3d3f4a");
            double sp = (P != null && P.w == "minigun" ? P.spin : 0), off = (S.t * sp * 30) % 4;
            for (int k = 0; k < 3; k++) { var y = -4.6 + ((k * 4 + off) % 12) * .8; box(9, y - 1.3, 21, 2.8, 1, k == 1 ? "#80848f" : "#6b6f7c"); }
            box(27, -5.5, 3, 11, 1, "#2a2b33"); circ(c, 3, 8, 5, "#c9a227");
        }
        else if (type == "mortar")
        {
            box(-5, -4, 10, 8, 2, "#8a5a2b");
            c.fillStyle = "#4a6430"; c.beginPath(); c.moveTo(4, -5); c.lineTo(22, -8); c.lineTo(22, 8); c.lineTo(4, 5); c.closePath(); c.fill(); c.stroke();
            c.fillStyle = "#fffaf0"; c.beginPath(); c.ellipse(23.5, 0, 4, 5, 0, 0, TAU); c.fill(); c.stroke();
        }
        else if (type == "rail")
        {
            box(-5, -3, 10, 7, 2, "#2a2b33"); box(4, -4, 27, 8, 2, "#3d3f4a");
            foreach (var x in new[] { 9, 15, 21 }) { c.fillStyle = "#4cc3ff"; rr(c, x, -5.5, 3, 11, 1); c.fill(); c.stroke(); }
            c.save(); c.globalAlpha = .6 + Math.Sin(S.t * 12) * .3; circ(c, 32, 0, 3, "#bff4ff", false); c.restore();
        }
        else if (type == "turbocat")
        {
            box(9, -4.6, 19, 3.4, 1, "#55585f"); box(9, .9, 19, 3.4, 1, "#6b6f7c"); box(26, -5.8, 3.5, 11.5, 1, "#2a2b33");
            c.fillStyle = "#f39a3b"; c.beginPath(); c.moveTo(-3, -6); c.lineTo(-1, -12); c.lineTo(3, -6.5); c.closePath(); c.fill(); c.stroke();
            c.beginPath(); c.moveTo(5, -6.5); c.lineTo(9, -12); c.lineTo(11, -6); c.closePath(); c.fill(); c.stroke();
            box(-5, -6.5, 17, 13, 5, "#f39a3b");
            c.strokeStyle = "#c9661d"; c.lineWidth = 1.6; seg(c, -1, -6, 0, -2.5); seg(c, 3.5, -6.4, 3.5, -2.8); seg(c, 8, -6, 7, -2.5);
            c.fillStyle = INK; c.beginPath(); c.arc(1, 0, 1.1, 0, TAU); c.arc(6, 0, 1.1, 0, TAU); c.fill();
            c.fillStyle = "#ff9ec7"; c.beginPath(); c.arc(3.5, 2.6, 1.2, 0, TAU); c.fill();
            c.strokeStyle = INK; c.lineWidth = .9; seg(c, -1, 3, -5, 2.4); seg(c, -1, 4, -5, 5); seg(c, 8, 3, 12, 2.4);
            c.lineWidth = 2; circ(c, 1, 9, 4, "#ff9ec7");
        }
        else if (type == "figure")
        {
            box(-7, -3, 12, 7, 2.5, "#7b5cd6");
            c.fillStyle = "#ff8fc8"; c.beginPath(); c.moveTo(4, -4.5); c.lineTo(20, -4.5); c.lineTo(27, -8.5); c.lineTo(27, 8.5); c.lineTo(20, 4.5); c.lineTo(4, 4.5); c.closePath(); c.fill(); c.stroke();
            c.save(); c.translate(11, 0); c.fillStyle = "#ffe066"; c.lineWidth = 1.2; figShape(c, 0, 3.6); c.fill(); c.stroke(); c.restore();
            c.save(); c.translate(17, 0); c.fillStyle = "#6fd8ff"; c.lineWidth = 1.2; figShape(c, 1, 3); c.fill(); c.stroke(); c.restore();
        }
        else if (type == "zapper")
        {
            box(3, -3.5, 15, 7, 2, "#6f7782"); box(5, 0, 5, 8, 1.5, "#4a4f58"); circ(c, 19, 0, 2.4, "#ff4d2e", false);
        }
        else if (type == "granny")
        {
            box(-6, -3.5, 12, 8, 2.5, "#a8743d"); box(5, -4.5, 17, 4, 1.5, "#55585f"); box(5, -.8, 17, 4, 1.5, "#55585f");
            c.fillStyle = "#e8d3a8"; c.fillRect(9, -5, 3, 8.5); c.strokeRect(9, -5, 3, 8.5);
        }
        else if (type == "cornrifle")
        {
            box(-7, -3, 12, 7, 2, "#8a5a2b"); box(4, -2.6, 29, 4.6, 1.5, "#6b4a2b");
            c.fillStyle = "#f6c42d"; c.beginPath(); c.ellipse(13, -6.5, 6, 2.6, 0, 0, TAU); c.fill(); c.stroke();
            c.fillStyle = "#e0a91f"; foreach (var x in new[] { 9, 12, 15, 18 }) { c.beginPath(); c.arc(x, -6.5, .8, 0, TAU); c.fill(); }
        }
        else if (type == "crossbow")
        {
            box(-4, -3, 24, 6, 2, "#8a5a2b");
            c.strokeStyle = INK; c.lineWidth = 3.2; c.beginPath(); c.moveTo(16, -12); c.quadraticCurveTo(24, 0, 16, 12); c.stroke();
            c.strokeStyle = "#c9973f"; c.lineWidth = 1.8; c.stroke();
            c.strokeStyle = "#fff4dc"; c.lineWidth = 1; seg(c, 16, -12, 8, 0); seg(c, 8, 0, 16, 12);
            c.strokeStyle = INK; c.lineWidth = 1.6; c.fillStyle = "#e6e3d6"; c.beginPath(); c.moveTo(8, 0); c.lineTo(30, 0); c.stroke(); c.beginPath(); c.moveTo(30, -2.5); c.lineTo(34, 0); c.lineTo(30, 2.5); c.closePath(); c.fill(); c.stroke();
        }
        else if (type == "syringe")
        {
            box(-6, -1.5, 6, 3, 1, "#c9d0d8"); box(-1, -4.5, 4, 9, 1, "#c9d0d8");
            box(3, -4, 18, 8, 3, "#e6f6ff"); c.fillStyle = "#5fcf3f"; c.fillRect(9, -2.6, 11, 5.2);
            c.strokeStyle = INK; c.lineWidth = 1; foreach (var x in new[] { 8, 12, 16 }) seg(c, x, -4, x, -1.5);
            c.lineWidth = 2; seg(c, 21, 0, 29, 0);
        }
        else if (type == "vitamin")
        {
            box(0, -5, 21, 10, 3, "#ffffff"); box(20, -2.5, 7, 5, 1.5, "#3d3f4a");
            c.fillStyle = "#e8322a"; c.fillRect(2, -5, 9, 10); c.strokeStyle = INK; c.lineWidth = 2; rr(c, 0, -5, 21, 10, 3); c.stroke();
            circ(c, 8, 8, 5, "#9cf27a"); c.fillStyle = "#ffc93a"; c.beginPath(); c.ellipse(8, 8, 2.4, 1.3, .5, 0, TAU); c.fill();
        }
        else if (type == "shotgun")
        {
            box(-6, -3, 11, 7, 2, "#8a5a2b"); box(4, -3.5, 24, 5, 1.5, "#3d3f4a"); box(6, 1, 16, 3.5, 1, "#55585f");
        }
        else if (type == "smg")
        {
            box(2, -4.5, 19, 8, 2, "#3d3f4a"); box(10, 2, 4.5, 10, 1, "#2a2b33"); box(20, -2.5, 6, 4, 1, "#2a2b33");
        }
        else if (type == "stick")
        { // деревянная палка с сучком и листиком
            c.lineCap = "round"; c.strokeStyle = INK; c.lineWidth = 7.5; seg(c, -2, 0, 31, 0); seg(c, 17, 0, 22, -7);
            c.strokeStyle = "#a8743d"; c.lineWidth = 4.6; seg(c, -2, 0, 31, 0); seg(c, 17, 0, 22, -7);
            c.strokeStyle = "#c9935a"; c.lineWidth = 1.4; seg(c, 2, -1.2, 27, -1.2);
            c.fillStyle = "#7a4a24"; c.beginPath(); c.arc(9, .5, 1.3, 0, TAU); c.arc(25, .4, 1.1, 0, TAU); c.fill();
            c.fillStyle = "#6db343"; c.strokeStyle = INK; c.lineWidth = 1.2; c.beginPath(); c.ellipse(24.5, -9.5, 4, 2.2, -.6, 0, TAU); c.fill(); c.stroke();
            c.lineCap = "butt";
        }
        else if (type == "blunder")
        {
            box(-6, -3, 12, 7, 2, "#7a4a24");
            c.fillStyle = "#c8a04a"; c.beginPath(); c.moveTo(4, -3.5); c.lineTo(22, -4); c.lineTo(28, -8); c.lineTo(28, 6); c.lineTo(22, 3); c.lineTo(4, 3.5); c.closePath(); c.fill(); c.stroke();
        }
    }

    sealed class Spec { public double s, hx, hy, hr, ex, ey, gy; }
    static readonly Dictionary<string, Spec> SPEC = new()
    {
        ["hen"] = new() { s = 1, hx = 9, hy = -33, hr = 8.5, ex = 11.5, ey = -33.5, gy = -19 },
        ["rooster"] = new() { s = 1.08, hx = 9, hy = -34, hr = 9, ex = 11.8, ey = -34.5, gy = -20 },
        ["chick"] = new() { s = .88, hx = 7, hy = -28, hr = 9, ex = 9.8, ey = -28.5, gy = -15 },
        ["duck"] = new() { s = 1, hx = 10, hy = -33, hr = 8, ex = 12.4, ey = -34.5, gy = -18 },
        ["goose"] = new() { s = 1.05, hx = 14, hy = -48, hr = 7.4, ex = 16, ey = -49, gy = -19 },
    };

    static void drawHat(Ctx c, string hat, double hx, double hy, double hr, double ex, double ey, string layer)
    {
        if (hat == null || hat == "none") return;
        c.strokeStyle = INK; c.lineWidth = 2.2; c.lineJoin = "round";
        if (layer == "over")
        {
            if (hat == "pirate") { c.lineWidth = 1.4; seg(c, hx - hr + 1, hy - 4.5, ex + 3, ey - 3.5); circ(c, ex, ey, 3.8, "#1d1d22"); }
            return;
        }
        if (hat == "bandana")
        {
            c.fillStyle = "#d8302a"; c.beginPath(); c.moveTo(hx - hr + .5, hy - 4); c.quadraticCurveTo(hx, hy - hr - 1.5, hx + hr - .5, hy - 5.5); c.lineTo(hx + hr - .3, hy - 2.5); c.quadraticCurveTo(hx, hy - hr + 1.5, hx - hr + .5, hy - 1); c.closePath(); c.fill(); c.stroke();
            c.beginPath(); c.moveTo(hx - hr + 1, hy - 3); c.lineTo(hx - hr - 8, hy - 8); c.lineTo(hx - hr - 6, hy - 3.5); c.lineTo(hx - hr - 10, hy); c.closePath(); c.fill(); c.stroke();
            c.fillStyle = "#fff"; foreach (var (x, y) in new[] { (hx - 3, hy - 5.5), (hx + 2, hy - 6.8) }) { c.beginPath(); c.arc(x, y, .9, 0, TAU); c.fill(); }
        }
        else if (hat == "helmet")
        {
            c.fillStyle = "#5c7a3a"; c.beginPath(); c.arc(hx, hy - 1.5, hr + 2.2, Math.PI * 1.02, Math.PI * 1.98); c.closePath(); c.fill(); c.stroke();
            c.fillStyle = "#4a6430"; rr(c, hx - hr - 3, hy - 3.2, 2 * hr + 6, 3.2, 1.5); c.fill(); c.stroke();
            c.fillStyle = "#7d9b55"; c.beginPath(); c.ellipse(hx - 2, hy - hr - .5, 2.6, 1.4, -.3, 0, TAU); c.fill();
        }
        else if (hat == "cowboy")
        {
            c.fillStyle = "#a8743d"; c.beginPath(); c.moveTo(hx - hr + 2, hy - hr + 2); c.lineTo(hx - hr + 3, hy - hr - 8); c.quadraticCurveTo(hx, hy - hr - 5, hx + hr - 3, hy - hr - 8); c.lineTo(hx + hr - 2, hy - hr + 2); c.closePath(); c.fill(); c.stroke();
            c.fillStyle = "#4a2a14"; c.fillRect(hx - hr + 2.4, hy - hr - 1.6, 2 * hr - 4.8, 2.6);
            c.fillStyle = "#8a5a2b"; c.beginPath(); c.ellipse(hx, hy - hr + 2.2, hr + 8, 2.8, 0, 0, TAU); c.fill(); c.stroke();
        }
        else if (hat == "crown")
        {
            c.fillStyle = "#ffc93a"; c.beginPath(); c.moveTo(hx - hr + 1.5, hy - hr + 3); c.lineTo(hx - hr + 1.5, hy - hr - 7); c.lineTo(hx - hr / 2, hy - hr - 2); c.lineTo(hx, hy - hr - 9.5); c.lineTo(hx + hr / 2, hy - hr - 2); c.lineTo(hx + hr - 1.5, hy - hr - 7); c.lineTo(hx + hr - 1.5, hy - hr + 3); c.closePath(); c.fill(); c.stroke();
            c.lineWidth = 1.2; circ(c, hx, hy - hr - .3, 1.8, "#e8322a"); circ(c, hx - hr / 2 - 1, hy - hr + .6, 1.3, "#3fa7d6"); circ(c, hx + hr / 2 + 1, hy - hr + .6, 1.3, "#3fa7d6");
        }
        else if (hat == "pirate")
        {
            c.fillStyle = "#1d1d22"; c.beginPath(); c.arc(hx, hy - 1, hr + .9, Math.PI * 1.04, Math.PI * 1.96); c.closePath(); c.fill(); c.stroke();
            c.beginPath(); c.moveTo(hx - hr + 1, hy - 3); c.lineTo(hx - hr - 7, hy - 7); c.lineTo(hx - hr - 6, hy - 1); c.closePath(); c.fill(); c.stroke();
            circ(c, hx + 1, hy - hr + 3, 1.8, "#fff", false);
        }
        else if (hat == "halo")
        {
            c.save(); c.shadowColor = "rgba(255,220,90,.9)"; c.shadowBlur = 8; c.strokeStyle = INK; c.lineWidth = 5; c.beginPath(); c.ellipse(hx, hy - hr - 8, hr * .95, 2.8, 0, 0, TAU); c.stroke();
            c.strokeStyle = "#ffd23a"; c.lineWidth = 2.8; c.stroke(); c.restore();
        }
        else if (hat == "propeller")
        { // кепка с пропеллером — Цыпа
            c.fillStyle = "#3fa7d6"; c.beginPath(); c.arc(hx, hy - 1, hr + .6, Math.PI * 1.05, Math.PI * 1.95); c.closePath(); c.fill(); c.stroke();
            c.fillStyle = "#e8322a"; c.beginPath(); c.moveTo(hx + 2, hy - 4); c.lineTo(hx + hr + 7, hy - 2.5); c.lineTo(hx + hr, hy - 6); c.closePath(); c.fill(); c.stroke();
            c.lineWidth = 1.6; seg(c, hx, hy - hr - 1, hx, hy - hr - 5); var sp = Math.Sin(S.t * 30) * 7; c.fillStyle = "#ffd23a"; c.beginPath(); c.ellipse(hx, hy - hr - 6, Math.Abs(sp) + 1.5, 1.8, 0, 0, TAU); c.fill(); c.stroke();
        }
        else if (hat == "sailor")
        { // бескозырка — Утка
            c.fillStyle = "#fff"; c.beginPath(); c.ellipse(hx, hy - hr + 1, hr + 2.5, 3.6, 0, 0, TAU); c.fill(); c.stroke();
            c.fillStyle = "#2b4a8a"; c.fillRect(hx - hr + .5, hy - hr + .5, 2 * hr - 1, 2.6); c.strokeRect(hx - hr + .5, hy - hr + .5, 2 * hr - 1, 2.6);
            c.strokeStyle = "#2b4a8a"; c.lineWidth = 2; seg(c, hx - hr, hy - hr + 2, hx - hr - 6, hy - hr + 8); seg(c, hx - hr, hy - hr + 2, hx - hr - 3, hy - hr + 10);
        }
        else if (hat == "visor")
        { // рыцарский шлем с забралом — Бронебой
            c.fillStyle = "#b9c2cc"; c.beginPath(); c.arc(hx, hy, hr + 1.8, Math.PI * .95, Math.PI * 2.05); c.closePath(); c.fill(); c.stroke();
            c.fillStyle = "#8a939e"; rr(c, hx - 1, ey - 2.6, hr + 3, 5.2, 1.5); c.fill(); c.stroke(); c.fillStyle = "#2b1d14"; c.fillRect(hx + 2, ey - .8, hr - 1, 1.6);
            c.fillStyle = "#e8322a"; c.beginPath(); c.moveTo(hx - 3, hy - hr - 1); c.quadraticCurveTo(hx - 9, hy - hr - 11, hx - 15, hy - hr - 4); c.quadraticCurveTo(hx - 8, hy - hr - 5, hx - 3, hy - hr + 2); c.closePath(); c.fill(); c.stroke();
        }
        else if (hat == "goggles")
        { // лётные очки — Цесарка
            c.fillStyle = "#6b4428"; c.beginPath(); c.arc(hx, hy - 1, hr + .8, Math.PI * 1.05, Math.PI * 1.95); c.closePath(); c.fill(); c.stroke();
            c.lineWidth = 1.8; circ(c, hx - 1, hy - hr + 1.5, 3.4, "#9fe3ff"); circ(c, hx + 5, hy - hr + 2.5, 3.4, "#9fe3ff");
            c.fillStyle = "rgba(255,255,255,.8)"; c.fillRect(hx - 2.5, hy - hr, 1.4, 1.4); c.fillRect(hx + 3.5, hy - hr + 1, 1.4, 1.4);
        }
        else if (hat == "ninja")
        {
            c.save(); c.beginPath(); c.arc(hx, hy, hr - 1.1, 0, TAU); c.clip(); c.fillStyle = "#1d1d22"; c.fillRect(hx - hr, ey - 3.6, 2 * hr, 7.2); c.restore();
            c.fillStyle = "#1d1d22"; c.beginPath(); c.moveTo(hx - hr + 1, ey - 1); c.lineTo(hx - hr - 9, ey - 6); c.lineTo(hx - hr - 7, ey); c.lineTo(hx - hr - 10, ey + 4); c.closePath(); c.fill(); c.stroke();
        }
    }

    static readonly HashSet<string> HAT_COVERS = new() { "helmet", "cowboy", "crown", "pirate", "propeller", "sailor", "visor", "goggles" };
    public static void drawBird(Ctx c, Sprite o)
    {
        var sp = SPEC.TryGetValue(o.sp ?? "hen", out var sp0) ? sp0 : SPEC["hen"]; double s = sp.s, hx = sp.hx, hy = sp.hy, hr = sp.hr, ex = sp.ex, ey = sp.ey;
        bool adren = o.special == "adren";
        c.save(); c.translate(o.x, o.y);
        if (o.blink && Odd(S.t * 20)) c.globalAlpha = .55;
        shadow(c, 14 * s, 5 * s);
        if (o.ghost) c.globalAlpha = .35 + Math.Sin(S.t * 12) * .08;
        if (o.swim) { c.save(); var k = (S.t * 1.4) % 1; c.strokeStyle = "rgba(255,255,255," + Inv(.8 - k * .6) + ")"; c.lineWidth = 2.2; c.beginPath(); c.ellipse(0, 0, 18 + k * 10, 6 + k * 3, 0, 0, TAU); c.stroke(); c.restore(); c.translate(0, 8); }
        if (o.buff == "cry") { c.save(); c.globalAlpha = .4 + Math.Sin(S.t * 14) * .15; c.strokeStyle = "#ff6b4a"; c.lineWidth = 3; c.beginPath(); c.ellipse(0, -20, 30, 36, 0, 0, TAU); c.stroke(); c.restore(); }
        if (o.buff == "fort") { c.save(); c.globalAlpha = .35 + Math.Sin(S.t * 10) * .1; c.fillStyle = "#cfe8ff"; c.strokeStyle = "#ffffff"; c.lineWidth = 3; c.beginPath(); c.ellipse(0, -22, 34, 38, 0, 0, TAU); c.fill(); c.stroke(); c.restore(); }
        if (o.buff == "storm")
        {
            c.save(); c.strokeStyle = "rgba(255,255,255,.75)"; c.lineWidth = 3; for (int k = 0; k < 3; k++) { var a0 = S.t * 9 + k * TAU / 3; c.beginPath(); c.ellipse(0, -20, 104, 44, 0, a0, a0 + 1.4); c.stroke(); }
            c.fillStyle = "rgba(134,188,220,.18)"; c.beginPath(); c.ellipse(0, -10, 110, 46, 0, 0, TAU); c.fill(); c.restore();
        }
        if (o.buff == "adren") { c.save(); c.globalAlpha = .5 + Math.Sin(S.t * 18) * .15; var g = c.createRadialGradient(0, -22, 4, 0, -22, 42); g.addColorStop(0, "rgba(255,210,58,.7)"); g.addColorStop(1, "rgba(255,60,30,0)"); c.fillStyle = g; c.beginPath(); c.arc(0, -22, 42, 0, TAU); c.fill(); c.restore(); }
        double bob = o.moving ? Math.Abs(Math.Sin(o.phase)) * 3 : 0, lg = o.moving ? Math.Sin(o.phase) * 5 : 0;
        c.save(); c.scale(o.face * s, s); c.lineCap = "round"; c.lineJoin = "round";
        double legTop = o.sp == "chick" ? -8 : -11; var legCol = (o.sp == "duck" || o.sp == "goose") ? "#f08a1f" : "#f0a02a";
        if (!o.swim)
        {
            c.strokeStyle = INK; c.lineWidth = 5; seg(c, -4, legTop, -4 + lg, 0); seg(c, 4, legTop, 4 - lg, 0);
            c.strokeStyle = legCol; c.lineWidth = 2.6; seg(c, -4, legTop, -4 + lg, 0); seg(c, 4, legTop, 4 - lg, 0);
        }
        c.translate(0, -bob);
        bool fl = o.flash > 0; string body = fl ? "#fff" : o.body, wing = fl ? "#fff" : o.wing, tail = fl ? "#fff" : (o.tail ?? o.wing), head = fl ? "#fff" : (o.head ?? o.body);
        c.strokeStyle = INK; c.lineWidth = 2.4;
        c.fillStyle = tail; c.beginPath();
        if (o.sp == "rooster")
        {
            var cols = adren ? new[] { "#e8322a", "#ffd23a", "#ff7a1a" } : new[] { tail, "#f2a33a", tail };
            for (int k = 0; k < 3; k++)
            {
                double fl2 = adren ? Math.Sin(S.t * 14 + k) * 2 : 0;
                c.beginPath(); c.moveTo(-10, -22 + k * 3); c.quadraticCurveTo(-32 - k * 3 + fl2, -46 + k * 7, -28 + k * 3, -15 + k * 3); c.quadraticCurveTo(-22, -27 + k * 4, -12, -17 + k * 3); c.closePath();
                c.fillStyle = fl ? "#fff" : cols[k]; c.fill(); c.stroke();
            }
        }
        else if (o.sp == "chick") { c.moveTo(-10, -16); c.lineTo(-17, -21); c.lineTo(-12, -10); c.closePath(); c.fill(); c.stroke(); }
        else if (o.sp == "duck" || o.sp == "goose") { c.moveTo(-12, -20); c.lineTo(-23, -28); c.lineTo(-17, -14); c.closePath(); c.fill(); c.stroke(); }
        else { c.moveTo(-10, -22); c.lineTo(-23, -35); c.lineTo(-19, -25); c.lineTo(-26, -27); c.lineTo(-15, -15); c.closePath(); c.fill(); c.stroke(); }
        c.fillStyle = body; c.beginPath();
        if (o.sp == "chick") c.ellipse(0, -16, 13.5, 12, 0, 0, TAU);
        else if (o.sp == "goose") c.ellipse(-1, -20, 16.5, 12.5, 0, 0, TAU);
        else if (o.sp == "duck") c.ellipse(0, -19, 16, 11.5, 0, 0, TAU);
        else if (o.sp == "rooster") c.ellipse(0, -21, 15.5, 13, 0, 0, TAU);
        else c.ellipse(0, -20, 15, 12.5, 0, 0, TAU);
        c.fill(); c.stroke();
        if (o.sp == "goose")
        { // Бронебой — короткая толстая тёмная шея и стальной нагрудник; Медсестра — тонкая белая шея
            bool arm = o.special == "armor"; double nw = arm ? 15 : o.special == "nurse" ? 10 : 12;
            if (arm)
            {
                c.fillStyle = fl ? "#fff" : "#c3ccd6"; c.beginPath(); c.moveTo(1, -31); c.quadraticCurveTo(15, -30, 16, -20); c.quadraticCurveTo(14, -10, 3, -9); c.closePath(); c.fill(); c.stroke();
                c.fillStyle = "#6f7782"; foreach (var (x, y) in new[] { (6.0, -26.0), (11.0, -21.0), (7.0, -14.0) }) { c.beginPath(); c.arc(x, y, 1.3, 0, TAU); c.fill(); }
                c.strokeStyle = "rgba(255,255,255,.7)"; c.lineWidth = 1.6; seg(c, 4, -28, 10, -27); c.strokeStyle = INK; c.lineWidth = 2.4;
            }
            c.strokeStyle = INK; c.lineWidth = nw; seg(c, 6, -24, hx - 2, hy + 3); c.strokeStyle = fl ? "#fff" : (arm ? head : o.body); c.lineWidth = nw - 4.6; seg(c, 6, -24, hx - 2, hy + 3); c.strokeStyle = INK; c.lineWidth = 2.4;
        }
        if (o.special == "quail" || o.special == "guinea")
        {
            c.save(); c.beginPath(); if (o.sp == "chick") c.ellipse(0, -16, 13.5, 12, 0, 0, TAU); else c.ellipse(0, -20, 15, 12.5, 0, 0, TAU); c.clip();
            c.fillStyle = o.special == "guinea" ? "rgba(255,255,255,.85)" : "rgba(255,240,210,.7)";
            for (int i = 0; i < 14; i++) { double px = -13 + (i * 7.3) % 26, py = (o.sp == "chick" ? -26 : -31) + ((i * 5.1) % 22); c.beginPath(); c.arc(px, py, o.special == "guinea" ? 1.5 : 1.2, 0, TAU); c.fill(); }
            c.restore();
        }
        bool hatCoversComb = (o.hat != null && HAT_COVERS.Contains(o.hat)) || o.special == "guinea" || o.special == "quail";
        if (!hatCoversComb)
        {
            var comb = o.robo ? "#6f7782" : adren ? "#ff3b1f" : "#e8322a";
            if (o.sp == "hen") foreach (var (x, y, r) in new[] { (-4.0, -8.5, 3.2), (.5, -10.0, 3.6), (4.5, -7.5, 3.0) }) circ(c, hx + x, hy + y, r, comb);
            else if (o.sp == "rooster") foreach (var (x, y, r) in new[] { (-6.0, -7.5, 3.6), (-1.8, -10.5, 4.4), (3.0, -11.0, 4.4), (7.0, -7.5, 3.6) }) circ(c, hx + x, hy + y, r, comb);
        }
        circ(c, hx, hy, hr, head);
        if (o.sp == "chick" && !hatCoversComb) { c.lineWidth = 2; seg(c, hx - 1, hy - hr + .5, hx - 3, hy - hr - 5); seg(c, hx + 2, hy - hr + .5, hx + 3.5, hy - hr - 5); c.lineWidth = 2.4; }
        if (o.special == "armor") { c.fillStyle = fl ? "#fff" : "#f4f1e6"; c.beginPath(); c.ellipse(hx - 1, hy + 3, 4.6, 3, -.4, 0, TAU); c.fill(); } // белая щека, как у канадской казарки
        if (o.special == "nurse") { c.fillStyle = "rgba(255,120,150,.55)"; c.beginPath(); c.ellipse(hx + 1, hy + 3.5, 3, 1.8, 0, 0, TAU); c.fill(); }
        if (o.sp == "duck" && o.head != o.body) { c.fillStyle = "#fff"; c.beginPath(); c.ellipse(hx - 2, hy + hr - 1.2, 5.5, 1.8, -.2, 0, TAU); c.fill(); }
        if (o.sp == "hen") { c.beginPath(); c.fillStyle = "#e8322a"; c.ellipse(hx + 6, hy + 6.5, 2.6, 3.8, 0, 0, TAU); c.fill(); c.stroke(); }
        if (o.sp == "rooster") { c.beginPath(); c.fillStyle = "#e8322a"; c.ellipse(hx + 6.5, hy + 7.5, 3.2, 5, 0, 0, TAU); c.fill(); c.stroke(); }
        if (o.sp == "duck") { c.fillStyle = "#f39a2b"; rr(c, hx + hr - 3.5, hy - .5, 11, 5.5, 2.7); c.fill(); c.stroke(); }
        else if (o.sp == "goose") { c.fillStyle = "#f08a1f"; c.beginPath(); c.moveTo(hx + hr - 2.5, hy - 3); c.lineTo(hx + hr + 8.5, hy + .5); c.lineTo(hx + hr - 2.5, hy + 3.5); c.closePath(); c.fill(); c.stroke(); circ(c, hx + hr - 1.6, hy - 3, 2.2, INK, false); }
        else if (o.sp == "chick") { c.fillStyle = "#f6a723"; c.beginPath(); c.moveTo(hx + 7.5, hy - 1.5); c.lineTo(hx + 13, hy + .8); c.lineTo(hx + 7.5, hy + 3); c.closePath(); c.fill(); c.stroke(); }
        else { c.fillStyle = "#f6a723"; c.beginPath(); c.moveTo(hx + 6.5, hy - 3); c.lineTo(hx + 14.5, hy); c.lineTo(hx + 6.5, hy + 3); c.closePath(); c.fill(); c.stroke(); }
        if (o.special == "quail")
        {
            c.fillStyle = fl ? "#fff" : "#2b1d14"; c.beginPath(); c.moveTo(hx - 1, hy - hr + 1); c.quadraticCurveTo(hx - 2, hy - hr - 11, hx + 6, hy - hr - 9); c.quadraticCurveTo(hx + 2, hy - hr - 6, hx + 2, hy - hr + 1); c.closePath(); c.fill();
            c.strokeStyle = "#fff4dc"; c.lineWidth = 1.2; seg(c, hx + 2, hy - 1, hx + hr - 1, hy + 2); c.strokeStyle = INK; c.lineWidth = 2.4;
        }
        if (o.special == "guinea")
        {
            c.fillStyle = "#c98a43"; c.beginPath(); c.moveTo(hx - 3, hy - hr + 1); c.lineTo(hx, hy - hr - 7); c.lineTo(hx + 3, hy - hr + 1); c.closePath(); c.fill(); c.stroke();
            c.fillStyle = "#e8322a"; c.beginPath(); c.ellipse(hx + 5, hy + 6, 1.8, 2.8, 0, 0, TAU); c.fill(); c.stroke();
        }
        if (o.special == "nurse" && (o.hat == null || o.hat == "none" || o.hat == "bandana"))
        {
            c.fillStyle = "#ffffff"; c.beginPath(); c.moveTo(hx - hr + 1, hy - 3); c.lineTo(hx - hr + 3, hy - hr - 4); c.lineTo(hx + hr - 2, hy - hr - 4); c.lineTo(hx + hr - 1, hy - 3); c.closePath(); c.fill(); c.stroke();
            c.fillStyle = "#e8322a"; c.fillRect(hx - 1.2, hy - hr - 2.5, 2.4, 6.5); c.fillRect(hx - 3.2, hy - hr - .4, 6.4, 2.4);
        }
        drawHat(c, o.special == "nurse" && o.hat == "bandana" ? "none" : o.hat, hx, hy, hr, ex, ey, "under");
        c.lineWidth = 1.5; circ(c, ex, ey, 3.3, "#fff"); circ(c, ex + 1.2, ey + .2, 1.7, adren ? "#e8322a" : INK, false);
        if (o.kind != "player" && !o.robo) { c.lineWidth = 2.6; seg(c, ex - 4, ey - 5, ex + 3.5, ey - 2); }
        if (o.special == "nurse") { c.lineWidth = 1.3; seg(c, ex - 1, ey - 3.2, ex - 2.5, ey - 5.6); seg(c, ex + 1.2, ey - 3.4, ex + 1.4, ey - 6); seg(c, ex + 3, ey - 2.6, ex + 4.6, ey - 4.8); }
        if (o.special == "armor") { c.lineWidth = 2.6; seg(c, ex - 4, ey - 4.6, ex + 3.5, ey - 3.4); }
        if (o.robo)
        {
            c.strokeStyle = INK; c.lineWidth = 1.6; c.fillStyle = "#e8322a"; rr(c, ex - 5, ey - 2.4, 9.5, 4.8, 2); c.fill(); c.stroke(); c.fillStyle = "rgba(255,220,200,.8)"; c.fillRect(ex - 3.5, ey - 1.5, 2, 1.4);
            seg(c, hx - 3, hy - hr + 1, hx - 6, hy - hr - 9); circ(c, hx - 6, hy - hr - 10, 2.2, "#e8322a"); c.fillStyle = "#6f7782";
            foreach (var (x, y) in new[] { (-6.0, 3.0), (4.0, 6.0) }) { c.beginPath(); c.arc(x, -20 + y * .5, 1.2, 0, TAU); c.fill(); }
        }
        if (adren && o.hat != "pirate" && o.hat != "ninja")
        { // shades
            c.fillStyle = "#111"; c.strokeStyle = INK; c.lineWidth = 1.5; rr(c, ex - 4.5, ey - 3, 9.5, 5.5, 2); c.fill(); c.stroke(); seg(c, ex - 4.5, ey - 1.5, hx - hr + 1, ey - 2.5);
            c.fillStyle = "rgba(255,255,255,.7)"; c.fillRect(ex - 2.5, ey - 2, 2.2, 1.4);
        }
        drawHat(c, o.hat, hx, hy, hr, ex, ey, "over");
        c.strokeStyle = INK; c.lineWidth = 2.4; c.fillStyle = wing; c.beginPath();
        if (o.sp == "chick") c.ellipse(-2, -15, 7, 5.5, -.25, 0, TAU); else c.ellipse(-2, -19, 8, 6, -.25, 0, TAU);
        c.fill(); c.stroke();
        if (o.special == "armor")
        {
            c.fillStyle = fl ? "#fff" : "#9aa4ae"; c.beginPath(); c.ellipse(-2, -22, 8.5, 4.6, -.25, Math.PI, TAU); c.closePath(); c.fill(); c.stroke(); // стальной наплечник
            c.fillStyle = "#55585f"; foreach (var (x, y) in new[] { (-7.0, -23.0), (-2.0, -25.5), (3.0, -24.0) }) { c.beginPath(); c.arc(x, y, 1.1, 0, TAU); c.fill(); }
        }
        if (o.special == "nurse" && !fl)
        {
            c.fillStyle = "#fff"; c.lineWidth = 1.4; c.beginPath(); c.arc(-2, -19, 4.2, 0, TAU); c.fill(); c.stroke(); // красный крест на крыле
            c.fillStyle = "#e8322a"; c.fillRect(-3, -22.2, 2, 6.4); c.fillRect(-5.2, -20, 6.4, 2); c.lineWidth = 2.4;
        }
        if (adren && !fl) { c.fillStyle = "#ffd23a"; c.lineWidth = 1.2; c.beginPath(); c.moveTo(0, -24); c.lineTo(-4, -18.5); c.lineTo(-1.5, -18.5); c.lineTo(-4, -14); c.lineTo(2, -20); c.lineTo(-.5, -20); c.closePath(); c.fill(); c.stroke(); }
        c.restore();
        c.translate(o.face * 3 * s, sp.gy * s - bob); c.rotate(o.ang); if (Math.Cos(o.ang) < 0) c.scale(1, -1);
        if (o.gun == "stick") c.rotate(o.swing > 0 ? -1.5 + 3 * (1 - o.swing) : -.75); // палка поднята, при ударе — размах сверху вниз
        drawGun(c, o.gun);
        c.strokeStyle = INK; c.lineWidth = 2; circ(c, 6, 1, 4.2, fl ? "#fff" : o.wing);
        c.restore();
    }
    static string Inv(double v) => v.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
}
