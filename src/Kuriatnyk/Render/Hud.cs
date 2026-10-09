using System;
using System.Collections.Generic;
using Kuriatnyk.Render;
using static Kuriatnyk.D;
using static Kuriatnyk.G;
using static Kuriatnyk.Store;

namespace Kuriatnyk;

/// <summary>
/// Интерфейс боя поверх поля: здоровье, цели, кнопки паузы и звука, «Бум», ульта, оружие, баннер, полоса босса.
/// В веб-версии это был HTML (#hud); здесь всё рисуется на полотне, размеры и цвета — из style.css.
/// </summary>
public static class Hud
{
    public static double SafeL, SafeT, SafeR, SafeB; // вырезы экрана (safe-area-inset)
    const string PAPER = "#fff4dc", YOLK = "#ffc93a", BARN = "#c0392b", HP = "#5fcf3f";
    static readonly Dictionary<string, (double x, double y, double w, double h)> btns = new();

    static bool Short => VH <= 500 && VW > VH; // @media (max-height:500px) and (orientation:landscape)

    public static bool HitButton(double x, double y, out string name)
    {
        foreach (var kv in btns)
        {
            var (bx, by, bw, bh) = kv.Value;
            if (x >= bx - 6 && x <= bx + bw + 6 && y >= by - 6 && y <= by + bh + 6) { name = kv.Key; return true; }
        }
        name = null; return false;
    }
    public static void Press(string name)
    {
        switch (name)
        {
            case "nade": throwNade(); break;
            case "ult": useUlt(); break;
            case "pause": Sfx.click(); pauseGame(); break;
            case "swap": swapWeapon(); break;
            case "mute": SET.muted = !SET.muted; saveSet(); Audio.ApplyVolume(); Sfx.click(); break;
        }
    }

    static void RRect(Ctx c, double x, double y, double w, double h, double r, string fill, double border, double shadowY = 0)
    {
        if (shadowY > 0) { c.fillStyle = INK; rr(c, x, y + shadowY, w, h, r); c.fill(); }
        c.fillStyle = fill; rr(c, x, y, w, h, r); c.fill();
        if (border > 0) { c.strokeStyle = INK; c.lineWidth = border; rr(c, x + border / 2, y + border / 2, w - border, h - border, Math.Max(0, r - border / 2)); c.stroke(); }
    }
    /// <summary>Текст с чернильной обводкой (text-shadow во все стороны в CSS).</summary>
    static void Outlined(Ctx c, string t, double x, double y, string font, string col, double ow, string align = "center")
    {
        c.font = font; c.textAlign = align; c.lineJoin = "round";
        c.strokeStyle = INK; c.lineWidth = ow * 2; c.strokeText(t, x, y);
        c.fillStyle = col; c.fillText(t, x, y);
    }
    public static void Coin(Ctx c, double x, double y, double s)
    {
        c.fillStyle = YOLK; c.strokeStyle = INK; c.lineWidth = 2; c.beginPath(); c.arc(x, y, s / 2 - 1, 0, TAU); c.fill(); c.stroke();
        c.fillStyle = "#fff3b8"; c.beginPath(); c.arc(x - s * .15, y - s * .15, s * .2, 0, TAU); c.fill();
    }
    public static void Tok(Ctx c, double x, double y, double s, string col = "#4cc3ff", string dot = "#e3f7ff")
    {
        c.save(); c.translate(x, y); c.rotate(Math.PI / 4); var h = s * .82 / 2;
        c.fillStyle = col; c.strokeStyle = INK; c.lineWidth = 2; rr(c, -h, -h, h * 2, h * 2, 3); c.fill(); c.stroke();
        c.fillStyle = dot; c.beginPath(); c.arc(0, 0, h * .45, 0, TAU); c.fill(); c.restore();
    }

    static double Chip(Ctx c, double x, double y, string text, string bg, string fg, string icon = null, bool bordered = false)
    {
        c.font = "700 13px Rubik"; double tw = c.measureText(text), iw = icon != null ? 13 * .95 + 4 : 0;
        double padX = bordered ? 8 : 9, w = tw + iw + padX * 2, h = 25;
        RRect(c, x, y, w, h, 9, bg, bordered ? 2 : 0);
        if (icon == "coin") Coin(c, x + padX + 6, y + h / 2, 12.4);
        else if (icon == "tok") Tok(c, x + padX + 6, y + h / 2, 12.4);
        c.fillStyle = fg; c.textAlign = "left"; c.fillText(text, x + padX + iw, y + h / 2 + 4.6);
        return w;
    }

    /// <summary>Подсказка клавиши на кнопке (ПК): маленькая «клавиша» с буквой.</summary>
    static void KeyCap(Ctx c, double x, double y, string key)
    {
        RRect(c, x, y, 22, 22, 6, INK, 0, 0);
        RRect(c, x + 2, y + 2, 18, 16, 4, PAPER, 0, 0);
        c.font = "900 12px Rubik"; c.textAlign = "center"; c.fillStyle = INK; c.fillText(key, x + 11, y + 14.5);
    }

    static void EggPath(Ctx c, double cx, double top, double w, double h)
    {   // border-radius: 50% 50% 46% 46% / 58% 58% 42% 42%
        double mid = top + h * .58;
        c.beginPath(); c.ellipse(cx, mid, w / 2, h * .58, 0, Math.PI, TAU); c.ellipse(cx, mid, w / 2, h * .42, 0, 0, Math.PI); c.closePath();
    }

    public static void Draw(Ctx c)
    {
        btns.Clear();
        if (P == null) return;
        double Lf = SafeL + 16, R = VW - SafeR - 16, Tp = SafeT + 10, B = VH - SafeB - 12;
        bool sh = Short;
        // --- здоровье ---
        double hw = sh ? 130 : 150, hh = sh ? 20 : 22;
        RRect(c, Lf, Tp, hw, hh, 12, "#8e2a21", 0, 3);
        c.save(); rr(c, Lf, Tp, hw, hh, 12); c.clip();
        c.fillStyle = P.hp < P.max * .3 ? "#ff5a3c" : HP; c.fillRect(Lf, Tp, hw * Math.Max(0, P.hp / P.max), hh); c.restore();
        c.strokeStyle = INK; c.lineWidth = 3; rr(c, Lf + 1.5, Tp + 1.5, hw - 3, hh - 3, 10.5); c.stroke();
        Outlined(c, Math.Ceiling(P.hp) + " / " + P.max, Lf + hw / 2, Tp + hh / 2 + 4.5, "800 13px Rubik", "#fff", 1);
        // --- фишки ---
        double cx = Lf, cy = Tp + hh + 3 + 6, maxX = Math.Max(Lf + 150, VW - 150);
        void chip(string t, string bg, string fg, string icon = null, bool brd = false)
        {
            c.font = "700 13px Rubik"; double need = c.measureText(t) + (icon != null ? 16.4 : 0) + 18;
            if (cx + need > maxX && cx > Lf) { cx = Lf; cy += 31; }
            cx += Chip(c, cx, cy, t, bg, fg, icon, brd) + 6;
        }
        chip(goalLabel(false), PAPER, INK, null, true);
        chip(S.score.ToString("#,0", L.Culture) + T(" очк."), INK, PAPER);
        chip(S.coins.ToString(), INK, PAPER, "coin");
        chip(S.tokens.ToString(), INK, PAPER, "tok");
        if (S.combo >= 4) chip(T("Комбо ×") + Math.Min(4, 1 + (S.combo - 1) / 3), YOLK, INK);
        // --- кнопки справа: звук и пауза ---
        double bs = sh ? 46 : 48;
        double px = R - bs, mx = px - 8 - bs;
        RRect(c, px, Tp, bs, bs, 15, PAPER, 3, 3);
        c.fillStyle = INK; c.fillRect(px + bs / 2 - 6, Tp + bs / 2 - 8, 4.5, 16); c.fillRect(px + bs / 2 + 1.5, Tp + bs / 2 - 8, 4.5, 16);
        btns["pause"] = (px, Tp, bs, bs);
        c.save(); if (SET.muted) c.globalAlpha = .55;
        RRect(c, mx, Tp, bs, bs, 15, PAPER, 3, 3);
        { // нота ♪
            double nx = mx + bs / 2 - 3, ny = Tp + bs / 2 + 6;
            c.fillStyle = INK; c.beginPath(); c.ellipse(nx, ny, 5, 3.8, -.4, 0, TAU); c.fill();
            c.fillRect(nx + 3.2, ny - 16, 2.4, 16);
            c.beginPath(); c.moveTo(nx + 5.6, ny - 16); c.quadraticCurveTo(nx + 13, ny - 12, nx + 10, ny - 6); c.quadraticCurveTo(nx + 10, ny - 10, nx + 5.6, ny - 11); c.closePath(); c.fill();
            if (SET.muted) { c.strokeStyle = BARN; c.lineWidth = 3; c.lineCap = "round"; seg(c, mx + 10, Tp + 10, mx + bs - 10, Tp + bs - 10); }
        }
        c.restore();
        btns["mute"] = (mx, Tp, bs, bs);
        // --- полоса босса ---
        if (bossBarOn && S.boss != null && !S.boss.dead)
        {
            double bw = Math.Min(320, VW * .62), bx = VW / 2 - bw / 2, by = sh ? SafeT + 12 : SafeT + 78;
            c.save(); Outlined(c, S.boss.T.name.ToUpper(L.Culture), VW / 2, by + 11, "900 13px Rubik", "#fff", 1.2); c.restore();
            double y2 = by + 17;
            RRect(c, bx, y2, bw, 14, 9, "#8e2a21", 0, 3);
            c.save(); rr(c, bx, y2, bw, 14, 9); c.clip();
            var g = c.createLinearGradient(0, y2, 0, y2 + 14); g.addColorStop(0, "#ff7a4d"); g.addColorStop(1, "#e8322a");
            c.fillStyle = g; c.fillRect(bx, y2, bw * Math.Max(0, S.boss.hp / S.boss.max), 14); c.restore();
            c.strokeStyle = INK; c.lineWidth = 3; rr(c, bx + 1.5, y2 + 1.5, bw - 3, 11, 7.5); c.stroke();
        }
        // --- оружие внизу ---
        {
            var name = WEAP[P.w].name; var ammo = double.IsInfinity(P.ammo) ? "∞" : ((int)P.ammo).ToString();
            c.font = "800 15px Rubik"; double nw = c.measureText(name); c.font = "900 15px Rubik"; double aw = c.measureText(ammo);
            if (ammo == "∞") aw = 14;
            double w = nw + 10 + aw + 28, h = 31, x = VW / 2 - w / 2, y = B - h;
            RRect(c, x, y, w, h, 14, INK, 0);
            c.font = "800 15px Rubik"; c.textAlign = "left"; c.fillStyle = PAPER; c.fillText(name, x + 14, y + 21);
            if (ammo == "∞") { c.strokeStyle = YOLK; c.lineWidth = 2.6; double ix = x + 14 + nw + 10 + 7, iy = y + 16; c.beginPath(); c.ellipse(ix - 3.4, iy, 3.4, 3, 0, 0, TAU); c.stroke(); c.beginPath(); c.ellipse(ix + 3.4, iy, 3.4, 3, 0, 0, TAU); c.stroke(); }
            else { c.font = "900 15px Rubik"; c.fillStyle = YOLK; c.fillText(ammo, x + 14 + nw + 10, y + 21); }
            if (P.sec != null)
            { // Soul Knight: кнопка смены оружия справа от названия (на ПК — клавиша F)
                double sx = x + w + 6, sw = 40;
                RRect(c, sx, y - 4, sw, h + 4, 12, YOLK, 3, 3);
                c.strokeStyle = INK; c.lineWidth = 2.4; c.lineCap = "round"; double mx2 = sx + sw / 2, my2 = y + h / 2 - 2;
                seg(c, mx2 - 9, my2 - 4, mx2 + 8, my2 - 4); seg(c, mx2 + 4, my2 - 8, mx2 + 8, my2 - 4); seg(c, mx2 + 4, my2, mx2 + 8, my2 - 4);
                seg(c, mx2 + 9, my2 + 5, mx2 - 8, my2 + 5); seg(c, mx2 - 4, my2 + 1, mx2 - 8, my2 + 5); seg(c, mx2 - 4, my2 + 9, mx2 - 8, my2 + 5); c.lineCap = "butt";
                if (!COARSE) KeyCap(c, sx + sw - 12, y - 14, "F");
                btns["swap"] = (x, y - 4, w + 6 + sw, h + 4);
            }
        }
        // --- кнопка «Бум» (яйцо-граната) ---
        // на телефоне — над правым стиком; на ПК стиков нет, поэтому «Бум» и ульта стоят в правом нижнем углу с подсказками клавиш
        bool pc = !COARSE;
        double nadeX;
        {
            double w = pc ? 60 : sh ? 62 : 68, h = pc ? 70 : sh ? 72 : 80, x = VW - SafeR - (pc ? 20 : sh ? 22 : 18) - w, y = pc ? B - h : VH - SafeB - (sh ? 150 : 200) - h, cxn = x + w / 2;
            nadeX = x;
            c.save(); if (P.nades <= 0) c.globalAlpha = .45;
            c.fillStyle = INK; c.save(); c.translate(0, 4); EggPath(c, cxn, y, w, h); c.fill(); c.restore();
            EggPath(c, cxn, y, w, h); c.fillStyle = PAPER; c.fill();
            c.save(); EggPath(c, cxn, y, w, h); c.clip(); double band = y + (pc || sh ? 30 : 34);
            c.fillStyle = INK; c.fillRect(x + 6, band, w - 12, 11); c.fillStyle = BARN; c.fillRect(x + 6, band + 2, w - 12, 7); c.restore();
            c.strokeStyle = INK; c.lineWidth = 3; EggPath(c, cxn, y + 1.5, w - 3, h - 3); c.stroke();
            c.font = "900 12px Rubik"; c.textAlign = "center"; c.fillStyle = INK; c.fillText(T("Бум"), cxn, band + 26);
            if (pc) KeyCap(c, x - 6, y + h - 22, "Q");
            double bx = x + w + 8 - 28, by = y - 8; RRect(c, bx, by, 28, 28, 14, YOLK, 3);
            c.font = "900 14px Rubik"; c.fillStyle = INK; c.fillText(P.nades.ToString(), bx + 14, by + 19);
            c.restore();
            btns["nade"] = (x, y, w, h);
        }
        // --- ульта ---
        {
            double d = pc ? 66 : sh ? 68 : 78, x = pc ? nadeX - 22 - d : VW - SafeR - 100 - d, y = pc ? B - d - 4 : VH - SafeB - (sh ? 178 : 212) - d, ucx = x + d / 2, ucy = y + d / 2, r = d / 2;
            int p = (int)Math.Floor(P.ult); string st = P.buff != null ? "live" : p >= 100 ? "ready" : "charge";
            if (st == "ready") { double k = (Clock % 1.0); double gl = Math.Sin(k * Math.PI) * 9; c.save(); c.globalAlpha = .9 * (1 - k); c.fillStyle = YOLK; c.beginPath(); c.arc(ucx, ucy + 4, r + gl, 0, TAU); c.fill(); c.restore(); }
            c.fillStyle = INK; c.beginPath(); c.arc(ucx, ucy + 4, r, 0, TAU); c.fill();
            c.fillStyle = "#6b5a48"; c.beginPath(); c.arc(ucx, ucy, r, 0, TAU); c.fill();
            if (p > 0) { c.fillStyle = YOLK; c.beginPath(); c.moveTo(ucx, ucy); c.arc(ucx, ucy, r, -Math.PI / 2, -Math.PI / 2 + TAU * Math.Min(1, p / 100.0)); c.closePath(); c.fill(); }
            c.strokeStyle = INK; c.lineWidth = 3; c.beginPath(); c.arc(ucx, ucy, r - 1.5, 0, TAU); c.stroke();
            double ir = r - 6;
            c.fillStyle = st == "ready" ? BARN : st == "live" ? YOLK : PAPER; c.beginPath(); c.arc(ucx, ucy, ir, 0, TAU); c.fill();
            c.strokeStyle = INK; c.lineWidth = 2; c.beginPath(); c.arc(ucx, ucy, ir - 1, 0, TAU); c.stroke();
            var lbl = st == "live" ? Math.Ceiling(P.buffT) + T(" с") : st == "ready" ? T("УЛЬТА") : p + "%";
            c.font = "900 12.5px Rubik"; c.textAlign = "center"; c.fillStyle = st == "ready" ? "#fff" : INK; c.fillText(lbl, ucx, ucy + 4.5);
            if (pc) KeyCap(c, x - 4, y + d - 20, "R");
            btns["ult"] = (x, y, d, d);
        }
        DrawBanner(c);
    }

    /// <summary>Баннер «Волна N» с анимацией как @keyframes ban (2,1 с).</summary>
    static void DrawBanner(Ctx c)
    {
        double t = (Clock - banT) / 2.1; if (t < 0 || t > 1 || banBig.Length + banSmall.Length == 0) return;
        double op, sc, dy = 0;
        if (t < .12) { var k = t / .12; op = k; sc = .6 + (1.08 - .6) * k; }
        else if (t < .2) { var k = (t - .12) / .08; op = 1; sc = 1.08 - .08 * k; }
        else if (t < .8) { op = 1; sc = 1; }
        else { var k = (t - .8) / .2; op = 1 - k; sc = 1; dy = -14 * k; }
        bool sh = Short;
        double size = sh ? 42 : Math.Clamp(VW * .12, 36, 62), y0 = VH * (sh ? .24 : .30) + dy;
        c.save(); c.globalAlpha = Math.Clamp(op, 0, 1);
        c.translate(VW / 2, y0 + size / 2); c.scale(sc, sc); c.translate(-VW / 2, -(y0 + size / 2));
        if (banBig.Length > 0)
        {
            c.font = "400 " + size + "px Rubik Bubbles"; c.textAlign = "center"; c.lineJoin = "round";
            double by = y0 + size * .85;
            c.fillStyle = INK; c.fillText(banBig, VW / 2, by + 5);
            c.strokeStyle = INK; c.lineWidth = 4; c.strokeText(banBig, VW / 2, by);
            c.fillStyle = YOLK; c.fillText(banBig, VW / 2, by);
        }
        if (banSmall.Length > 0) Outlined(c, banSmall, VW / 2, y0 + size + 8 + 16, "800 16px Rubik", "#fff", 2);
        c.restore();
    }
}
