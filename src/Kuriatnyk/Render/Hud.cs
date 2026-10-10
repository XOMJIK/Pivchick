using System;
using System.Collections.Generic;
using System.Linq;
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
            case "nade": act("nade"); break;
            case "ult": act("ult"); break;
            case "pause": Sfx.click(); pauseGame(); break;
            case "swap": act("swap"); break;
            case "act": act("act"); break;
            case "drop": act("drop"); break;
            case "slot0": act("slot0"); break; case "slot1": act("slot1"); break; case "slot2": act("slot2"); break;
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
    /// <summary>Пивчик — янтарная монетка с пенной шапкой.</summary>
    public static void Piv(Ctx c, double x, double y, double s)
    {
        c.fillStyle = "#e89a1c"; c.strokeStyle = INK; c.lineWidth = Math.Max(1.4, s * .14); c.beginPath(); c.arc(x, y, s / 2 - 1, 0, TAU); c.fill(); c.stroke();
        c.fillStyle = "#ffd25a"; c.beginPath(); c.arc(x, y + s * .06, s * .3, 0, TAU); c.fill();
        c.fillStyle = "#fffaf0"; c.beginPath(); c.arc(x - s * .14, y - s * .14, s * .14, 0, TAU); c.arc(x + s * .02, y - s * .2, s * .15, 0, TAU); c.arc(x + s * .17, y - s * .13, s * .12, 0, TAU); c.fill();
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
        else if (icon == "piv") Piv(c, x + padX + 6, y + h / 2, 13.4);
        c.fillStyle = fg; c.textAlign = "left"; c.fillText(text, x + padX + iw, y + h / 2 + 4.6);
        return w;
    }

    /// <summary>Подсказка клавиши на кнопке (ПК): маленькая «клавиша» с буквой.</summary>
    static void KeyCap(Ctx c, double x, double y, string key)
    {
        c.font = "900 12px Rubik"; double w = Math.Max(22, c.measureText(key) + 10);
        RRect(c, x, y, w, 22, 6, INK, 0, 0);
        RRect(c, x + 2, y + 2, w - 4, 16, 4, PAPER, 0, 0);
        c.textAlign = "center"; c.fillStyle = INK; c.fillText(key, x + w / 2, y + 14.5);
    }
    /// <summary>Цвет редкости оружия Soul Knight.</summary>
    public static string Rc(string r) => r switch { "common" => "#e3e8ec", "rare" => "#7cc8f2", "epic" => "#cfa8ff", "legendary" => "#ffb547", _ => PAPER }; // светлее, чем RAR — на тёмной плашке

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
        // --- стамина (только в Soul Knight): тонкая голубая полоска под здоровьем ---
        double sy = Tp + hh + 6, shh = 12, cy = Tp + hh + 9;
        if (S.kind == "sk")
        {
            bool low = P.stam < shotCost(WEAP[P.w], WEAP[P.w].rate);
            RRect(c, Lf, sy, hw, shh, 7, "#1d3a4a", 0, 2);
            c.save(); rr(c, Lf, sy, hw, shh, 7); c.clip();
            c.fillStyle = low && (long)(Clock * 6) % 2 == 1 ? "#ff8a6b" : "#4cc3ff"; c.fillRect(Lf, sy, hw * Math.Clamp(P.stam / Math.Max(1, P.stamMax), 0, 1), shh); c.restore();
            c.strokeStyle = INK; c.lineWidth = 2; rr(c, Lf + 1, sy + 1, hw - 2, shh - 2, 6); c.stroke();
            Outlined(c, Math.Floor(P.stam) + " / " + P.stamMax, Lf + hw / 2, sy + shh / 2 + 3.6, "800 10px Rubik", "#fff", 1);
            c.font = "800 10px Rubik"; c.textAlign = "left"; c.fillStyle = PAPER; c.lineWidth = 2; c.strokeStyle = INK; c.strokeText(T("стамина"), Lf + hw + 6, sy + 10); c.fillText(T("стамина"), Lf + hw + 6, sy + 10);
            cy = sy + shh + 3 + 6;
        }
        // --- фишки ---
        double cx = Lf, maxX = Math.Max(Lf + 150, VW - 150);
        void chip(string t, string bg, string fg, string icon = null, bool brd = false)
        {
            c.font = "700 13px Rubik"; double need = c.measureText(t) + (icon != null ? 16.4 : 0) + 18;
            if (cx + need > maxX && cx > Lf) { cx = Lf; cy += 31; }
            cx += Chip(c, cx, cy, t, bg, fg, icon, brd) + 6;
        }
        chip(goalLabel(false), PAPER, INK, null, true);
        chip(S.score.ToString("#,0", L.Culture) + T(" очк."), INK, PAPER);
        if (S.kind == "sk") chip(S.piv.ToString(), "#5a3410", "#ffd25a", "piv");
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
            double bw = Math.Min(320, VW * .62), bx = VW / 2 - bw / 2, by = sh ? SafeT + 12 : SafeT + 96;
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
            var W = WEAP[P.w]; var name = W.name; var ammo = double.IsInfinity(P.ammo) ? "∞" : ((int)P.ammo).ToString();
            double cost = shotCost(W, W.rate); string cs = cost > 0 ? (cost < 1 ? cost.ToString("0.#", L.Culture) : Math.Round(cost, 1).ToString("0.#", L.Culture)) : null;
            c.font = "800 15px Rubik"; double nw = c.measureText(name); c.font = "900 15px Rubik"; double aw = c.measureText(ammo);
            if (ammo == "∞") aw = 14;
            c.font = "900 12px Rubik"; double cw = (cs != null ? c.measureText(cs) + 22 : 0) + (W.coin ? 26 : 0);
            double w = nw + 10 + aw + 28 + cw, h = 31, x = VW / 2 - w / 2, y = B - h;
            bool bag = P.bag != null; double slotW = 46, slotsW = bag ? P.bagMax * (slotW + 5) : 0, swapW = bag && P.bag.Count > 1 ? 46 : 0;
            if (bag) x = VW / 2 - (slotsW + w + swapW) / 2 + slotsW;
            RRect(c, x, y, w, h, 14, INK, 0);
            c.font = "800 15px Rubik"; c.textAlign = "left"; c.fillStyle = W.skr != null ? Rc(W.skr) : PAPER; c.fillText(name, x + 14, y + 21);
            double ax = x + 14 + nw + 10;
            if (ammo == "∞") { c.strokeStyle = YOLK; c.lineWidth = 2.6; double ix = ax + 7, iy = y + 16; c.beginPath(); c.ellipse(ix - 3.4, iy, 3.4, 3, 0, 0, TAU); c.stroke(); c.beginPath(); c.ellipse(ix + 3.4, iy, 3.4, 3, 0, 0, TAU); c.stroke(); }
            else { c.font = "900 15px Rubik"; c.fillStyle = YOLK; c.fillText(ammo, ax, y + 21); }
            if (cs != null)
            { // цена выстрела в стамине: синяя капля
                double dx = ax + aw + 12, dy = y + 15.5; c.fillStyle = "#4cc3ff"; c.beginPath(); c.moveTo(dx, dy - 7); c.quadraticCurveTo(dx + 6, dy, dx, dy + 6); c.quadraticCurveTo(dx - 6, dy, dx, dy - 7); c.fill();
                c.font = "900 12px Rubik"; c.fillStyle = "#bfe9ff"; c.fillText(cs, dx + 7, y + 20);
            }
            if (W.coin) { double px2 = ax + aw + 20 + (cs != null ? c.measureText(cs) + 22 : 0); Piv(c, px2, y + 15.5, 14); if (S.piv < 1) { c.strokeStyle = BARN; c.lineWidth = 2.4; seg(c, px2 - 7, y + 9, px2 + 7, y + 22); } }
            if (bag)
            { // Soul Knight: слоты рюкзака слева, кнопка смены справа
                for (int i = 0; i < P.bagMax; i++)
                {
                    double bx = x - slotsW + i * (slotW + 5), by = y - 4; bool cur = i == P.slot && i < P.bag.Count;
                    RRect(c, bx, by, slotW, h + 4, 10, i < P.bag.Count ? (cur ? YOLK : PAPER) : "rgba(255,244,220,.35)", cur ? 3 : 2, cur ? 0 : 2);
                    if (i < P.bag.Count)
                    {
                        c.save(); c.translate(bx + slotW / 2 - 13, by + (h + 4) / 2 + 1); c.scale(.85, .85); drawGun(c, P.bag[i]); c.restore();
                        c.fillStyle = Rc(WEAP[P.bag[i]].skr); c.fillRect(bx + 6, by + h - 1, slotW - 12, 3);
                        if (!COARSE) { c.font = "900 10px Rubik"; c.textAlign = "left"; c.fillStyle = INK; c.fillText((i + 1).ToString(), bx + 5, by + 12); }
                        btns["slot" + i] = (bx, by, slotW, h + 4);
                    }
                }
                if (P.bag.Count > 1)
                { // выбросить оружие: стрелка вниз к земле — слева от слотов
                    double dw = 38, dx2 = x - slotsW - 6 - dw, dy2 = y - 4;
                    RRect(c, dx2, dy2, dw, h + 4, 12, PAPER, 3, 3);
                    c.strokeStyle = INK; c.lineWidth = 2.6; c.lineCap = "round"; double mx3 = dx2 + dw / 2, my3 = dy2 + (h + 4) / 2;
                    seg(c, mx3, my3 - 9, mx3, my3 + 3); seg(c, mx3 - 5, my3 - 2, mx3, my3 + 3); seg(c, mx3 + 5, my3 - 2, mx3, my3 + 3); seg(c, mx3 - 9, my3 + 8, mx3 + 9, my3 + 8); c.lineCap = "butt";
                    if (!COARSE) KeyCap(c, dx2 - 8, y - 14, keyHint("drop"));
                    btns["drop"] = (dx2, dy2, dw, h + 4);
                }
                if (swapW > 0)
                {
                    double sx = x + w + 6, sw = 40;
                    RRect(c, sx, y - 4, sw, h + 4, 12, YOLK, 3, 3);
                    c.strokeStyle = INK; c.lineWidth = 2.4; c.lineCap = "round"; double mx2 = sx + sw / 2, my2 = y + h / 2 - 2;
                    seg(c, mx2 - 9, my2 - 4, mx2 + 8, my2 - 4); seg(c, mx2 + 4, my2 - 8, mx2 + 8, my2 - 4); seg(c, mx2 + 4, my2, mx2 + 8, my2 - 4);
                    seg(c, mx2 + 9, my2 + 5, mx2 - 8, my2 + 5); seg(c, mx2 - 4, my2 + 1, mx2 - 8, my2 + 5); seg(c, mx2 - 4, my2 + 9, mx2 - 8, my2 + 5); c.lineCap = "butt";
                    if (!COARSE) KeyCap(c, sx + sw - 12, y - 14, keyHint("swap"));
                    btns["swap"] = (x, y - 4, w + 6 + sw, h + 4);
                }
            }
            // кнопка действия над оружием: взять, заменить или купить
            var lbl = actLabel();
            if (lbl != null)
            {
                int price = actPrice(); bool egg = actEgg(); c.font = "900 15px Rubik"; double lw = c.measureText(lbl), pw = price > 0 ? c.measureText(price.ToString()) + 30 : 0;
                double bw = lw + pw + 30 + (COARSE ? 0 : 26), bh = 38, bx = VW / 2 - bw / 2, by = y - 16 - bh;
                bool poor = price > 0 && (egg ? SAVE.eggs < price : S.piv < price);
                RRect(c, bx, by, bw, bh, 14, poor ? "#d9cdb4" : price > 0 ? "#3fa535" : YOLK, 3, 3);
                double tx = bx + 15; if (!COARSE) { KeyCap(c, tx - 2, by + 8, keyHint("act")); tx += 26; }
                c.textAlign = "left"; c.fillStyle = price > 0 && !poor ? "#fff" : INK;
                var wn = actPick?.kind == "skw" ? WEAP[actPick.wid].name : null;
                if (wn != null && lbl.EndsWith(wn))
                { // название оружия — цветом его редкости, с обводкой, чтобы читалось на любой кнопке
                    var pre = lbl[..^wn.Length]; c.fillText(pre, tx, by + 24.5); double nx = tx + c.measureText(pre);
                    c.lineJoin = "round"; c.strokeStyle = INK; c.lineWidth = 3.4; c.strokeText(wn, nx, by + 24.5); c.fillStyle = Rc(WEAP[actPick.wid].skr); c.fillText(wn, nx, by + 24.5);
                    c.fillStyle = price > 0 && !poor ? "#fff" : INK;
                }
                else c.fillText(lbl, tx, by + 24.5);
                if (price > 0) { if (egg) { c.save(); c.translate(tx + lw + 9, by + bh / 2 - 8); UI.K.DrawIcon(c, "gegg", 13, 16); c.restore(); } else Piv(c, tx + lw + 16, by + bh / 2, 16); c.fillText(price.ToString(), tx + lw + 27, by + 24.5); }
                btns["act"] = (bx, by, bw, bh);
                if (actPick?.kind == "skw") WeaponCompare(c, WEAP[actPick.wid], WEAP[P.w], by - 8);
                else if (actStatue != null) UltInfo(c, actStatue.kind, by - 8);
            }
        }
        // --- кнопка «Бум» (яйцо-граната) ---
        // на телефоне — над правым стиком; на ПК стиков нет, поэтому «Бум» и ульта стоят в правом нижнем углу с подсказками клавиш
        // в Soul Knight гранат нет: ульта встаёт на место «Бума»
        bool pc = !COARSE, sk = S.kind == "sk";
        double nadeX = VW - SafeR - (pc ? 20 : sh ? 22 : 18) + 22;
        if (!sk)
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
            if (pc) KeyCap(c, x - 6, y + h - 22, keyHint("nade"));
            double bx = x + w + 8 - 28, by = y - 8; RRect(c, bx, by, 28, 28, 14, YOLK, 3);
            c.font = "900 14px Rubik"; c.fillStyle = INK; c.fillText(P.nades.ToString(), bx + 14, by + 19);
            c.restore();
            btns["nade"] = (x, y, w, h);
        }
        // --- ульта ---
        {
            double d = pc ? 66 : sh ? 68 : 78, x = pc || sk ? nadeX - 22 - d : VW - SafeR - 100 - d, y = pc ? B - d - 4 : sk ? VH - SafeB - (sh ? 150 : 200) - d : VH - SafeB - (sh ? 178 : 212) - d, ucx = x + d / 2, ucy = y + d / 2, r = d / 2;
            int p = (int)Math.Floor(P.ult); string st = P.buff != null ? "live" : p >= 100 ? "ready" : "charge";
            if (st == "ready") { double k = (Clock % 1.0); double gl = Math.Sin(k * Math.PI) * 9; c.save(); c.globalAlpha = .9 * (1 - k); c.fillStyle = YOLK; c.beginPath(); c.arc(ucx, ucy + 4, r + gl, 0, TAU); c.fill(); c.restore(); }
            c.fillStyle = INK; c.beginPath(); c.arc(ucx, ucy + 4, r, 0, TAU); c.fill();
            c.fillStyle = "#6b5a48"; c.beginPath(); c.arc(ucx, ucy, r, 0, TAU); c.fill();
            if (p > 0) { c.fillStyle = YOLK; c.beginPath(); c.moveTo(ucx, ucy); c.arc(ucx, ucy, r, -Math.PI / 2, -Math.PI / 2 + TAU * Math.Min(1, p / 100.0)); c.closePath(); c.fill(); }
            c.strokeStyle = INK; c.lineWidth = 3; c.beginPath(); c.arc(ucx, ucy, r - 1.5, 0, TAU); c.stroke();
            double ir = r - 6;
            c.fillStyle = st == "ready" ? BARN : st == "live" ? YOLK : PAPER; c.beginPath(); c.arc(ucx, ucy, ir, 0, TAU); c.fill();
            c.strokeStyle = INK; c.lineWidth = 2; c.beginPath(); c.arc(ucx, ucy, ir - 1, 0, TAU); c.stroke();
            var lbl = st == "live" ? Math.Ceiling(P.buffT) + T(" сек") : st == "ready" ? T("УЛЬТА") : p + "%";
            c.font = "900 12.5px Rubik"; c.textAlign = "center"; c.fillStyle = st == "ready" ? "#fff" : INK; c.fillText(lbl, ucx, ucy + 4.5);
            if (pc) KeyCap(c, x - 4, y + d - 20, keyHint("ult"));
            btns["ult"] = (x, y, d, d);
            if (sk)
            { // чья ульта сейчас: своя или от статуи (золотом)
                var un = SKU.Name(P.ultId ?? P.hid); c.font = "800 11px Rubik"; double uw = c.measureText(un);
                Outlined(c, un, Math.Min(ucx, VW - SafeR - 8 - uw / 2), y - 7, "800 11px Rubik", P.ultId != null ? YOLK : "#fff", 1.2);
            }
        }
        if (sk && DUN != null && !DUN.hub) { double my = Tp + bs + 10; my = Minimap(c, R, my, sh ? 104 : 128); Passives(c, R, my + 8); }
        // сетевая игра: выбит — ждём возвращения; хост выбирает пассивку
        if (!P.alive && P.downT > 0) Outlined(c, T("Ты выбит — вернёшься через ") + Math.Ceiling(P.downT) + T(" сек"), VW / 2, VH * .42, "900 20px Rubik", "#ffb3a3", 2.5);
        if (IsClientWorld && S.mode == "pick") Outlined(c, T("Хост выбирает пассивку для команды…"), VW / 2, VH * .42, "900 20px Rubik", "#ffe066", 2.5);
        DrawBanner(c);
    }

    /// <summary>Мини-карта подземелья в правом верхнем углу: где был, какие комнаты рядом, где ты сейчас. Возвращает нижний край.</summary>
    static double Minimap(Ctx c, double right, double top, double size)
    {
        var D = DUN; double x0 = 1e9, y0 = 1e9, x1 = -1e9, y1 = -1e9;
        foreach (var R in D.rooms) { x0 = Math.Min(x0, R.x); y0 = Math.Min(y0, R.y); x1 = Math.Max(x1, R.x + R.w); y1 = Math.Max(y1, R.y + R.h); }
        double pad = 8, k = (size - pad * 2) / Math.Max(x1 - x0, y1 - y0), w = (x1 - x0) * k + pad * 2, h = (y1 - y0) * k + pad * 2, left = right - w;
        double mx(double v) => left + pad + (v - x0) * k; double my(double v) => top + pad + (v - y0) * k;
        RRect(c, left, top, w, h, 10, "rgba(27,32,24,.78)", 2.5);
        // комната видна, если герой в ней был или она за дверью от такой комнаты
        bool known(Room R) => R.seen || R.cors.Any(co => (co.a == R ? co.b : co.a).seen);
        foreach (var co in D.cors)
        {
            if (!co.a.seen && !co.b.seen) continue;
            c.fillStyle = "rgba(233,220,192,.55)";
            if (co.horiz) c.fillRect(mx(co.x), my(co.y + co.h / 2) - 2, co.w * k, 4); else c.fillRect(mx(co.x + co.w / 2) - 2, my(co.y), 4, co.h * k);
        }
        foreach (var R in D.rooms)
        {
            if (!known(R)) continue;
            bool here = inRoom(R, P.x, P.y, 0), act = D.active == R;
            string fill = !R.seen ? "rgba(233,220,192,.12)" : act ? "#c0392b" : R.type is "fight" or "boss" or "bossopt" && !R.cleared ? "#8a7a55" : "#d9cdb4";
            c.fillStyle = fill; rr(c, mx(R.x), my(R.y), R.w * k, R.h * k, 3); c.fill();
            c.strokeStyle = here ? YOLK : !R.seen ? "rgba(233,220,192,.5)" : INK; c.lineWidth = here ? 2.2 : 1.4; c.stroke();
            double cx = mx(R.cx), cy = my(R.cy), s = Math.Min(R.w, R.h) * k * .5;
            switch (R.type)
            { // значки особых комнат
                case "shop": Piv(c, cx, cy, Math.Min(12, s)); break;
                case "chest": c.fillStyle = "#c9a227"; c.strokeStyle = INK; c.lineWidth = 1.4; rr(c, cx - 5, cy - 3.5, 10, 7, 1.5); c.fill(); c.stroke(); seg(c, cx - 5, cy - .5, cx + 5, cy - .5); break;
                case "shrine": c.fillStyle = OBS.Any(o => o.type == "statue" && o.st == 0) ? YOLK : "#9a958c"; c.strokeStyle = INK; c.lineWidth = 1.4; c.beginPath(); for (int i = 0; i < 10; i++) { double a = -Math.PI / 2 + i * Math.PI / 5, q = i % 2 == 1 ? 2.6 : 6; c.lineTo(cx + Math.Cos(a) * q, cy + Math.Sin(a) * q); } c.closePath(); c.fill(); c.stroke(); break;
                case "boss": case "bossopt": if (!R.cleared) { c.fillStyle = R.type == "boss" ? "#e8322a" : "#ff9a1f"; c.strokeStyle = INK; c.lineWidth = 1.4; c.beginPath(); c.arc(cx, cy, 5, 0, TAU); c.fill(); c.stroke(); c.fillStyle = INK; c.fillRect(cx - 2.6, cy - 1.6, 1.8, 1.8); c.fillRect(cx + .8, cy - 1.6, 1.8, 1.8); } break;
                case "start": c.fillStyle = "#5fcf3f"; c.beginPath(); c.arc(cx, cy, 2.6, 0, TAU); c.fill(); break;
            }
        }
        foreach (var p in D.portals)
        { // портал на следующий уровень
            var R = D.rooms.FirstOrDefault(r => inRoom(r, p.x, p.y, 0)); if (R == null || !known(R)) continue;
            c.fillStyle = p.on ? "#8f5cff" : "#4a4458"; c.strokeStyle = p.on ? "#e8dcff" : INK; c.lineWidth = 1.4; c.beginPath(); c.ellipse(mx(p.x), my(p.y), 5, 3, 0, 0, TAU); c.fill(); c.stroke();
        }
        // герой
        double hx = mx(P.x), hy = my(P.y);
        c.fillStyle = "#fff"; c.strokeStyle = INK; c.lineWidth = 2; c.beginPath(); c.arc(hx, hy, 3.6, 0, TAU); c.fill(); c.stroke();
        return top + h;
    }

    /// <summary>Взятые пассивки забега — значки под мини-картой (повторы — с числом).</summary>
    static void Passives(Ctx c, double right, double top)
    {
        if (S.skPass.Count == 0) return;
        var groups = S.skPass.GroupBy(id => id).ToList(); double s = 24, gap = 4, x = right - s;
        int perRow = Math.Max(1, (int)((Math.Min(200, VW * .3) + gap) / (s + gap)));
        for (int i = 0; i < groups.Count; i++)
        {
            var p = SKP.Get(groups[i].Key); if (p == null) continue;
            double px = right - s - (i % perRow) * (s + gap), py = top + (i / perRow) * (s + gap);
            c.save(); c.translate(px, py); UI.Overlays.DrawPassIcon(c, p.icon, s, s); c.restore();
            int n = groups[i].Count();
            if (n > 1) { c.font = "900 10px Rubik"; c.textAlign = "center"; c.fillStyle = YOLK; c.strokeStyle = INK; c.lineWidth = 3; c.strokeText("×" + n, px + s - 3, py + s + 1); c.fillText("×" + n, px + s - 3, py + s + 1); }
        }
    }

    /// <summary>Что даст поклон статуе: название ульты, описание и какая ульта у героя сейчас.</summary>
    static void UltInfo(Ctx c, string id, double bottom)
    {
        double w = 280, pad = 10;
        c.font = "600 11.5px Rubik"; var lines = new List<string>(); var cur = "";
        foreach (var word in SKU.Desc(id).Split(' '))
        {
            var t = cur.Length == 0 ? word : cur + " " + word;
            if (c.measureText(t) > w - pad * 2 && cur.Length > 0) { lines.Add(cur); cur = word; } else cur = t;
        }
        if (cur.Length > 0) lines.Add(cur);
        if (lines.Count > 4) { lines = lines.GetRange(0, 4); lines[3] += "…"; }
        double h = pad * 2 + 22 + lines.Count * 14.5 + 18, x = VW / 2 - w / 2, y = bottom - h;
        RRect(c, x, y, w, h, 12, "rgba(43,29,20,.9)", 0);
        c.strokeStyle = SKU.Gem(id); c.lineWidth = 2.5; rr(c, x + 1.25, y + 1.25, w - 2.5, h - 2.5, 11); c.stroke();
        c.textAlign = "left"; c.font = "900 15px Rubik"; c.fillStyle = YOLK; c.fillText(SKU.Name(id), x + pad, y + pad + 14);
        c.font = "600 11.5px Rubik"; c.fillStyle = "rgba(255,244,220,.8)";
        for (int i = 0; i < lines.Count; i++) c.fillText(lines[i], x + pad, y + pad + 34 + i * 14.5);
        c.font = "700 11px Rubik"; c.fillStyle = "rgba(255,244,220,.55)"; c.fillText(T("Сейчас: ") + SKU.Name(P.ultId ?? P.hid), x + pad, y + h - pad);
    }

    /// <summary>Характеристики оружия на земле и сравнение с тем, что в руках (как в Soul Knight): зелёный треугольник — лучше, красный — хуже.</summary>
    static void WeaponCompare(Ctx c, Weap W, Weap cur, double bottom)
    {
        bool same = W.id == cur.id;
        (string label, double now, double was, bool higherBetter, string fmt)[] rows =
        {
            (T("Урон"), Dmg(W), Dmg(cur), true, "0"),
            (W.special == "melee" ? T("Ударов в секунду") : T("Выстрелов в секунду"), 1 / W.rate, 1 / cur.rate, true, "0.#"),
            (T("Урон в секунду"), SKW.Power(W), SKW.Power(cur), true, "0"),
            (T("Стамина за выстрел"), W.stam, cur.stam, false, "0.##"),
        };
        double w = 280, rh = 19, pad = 10;
        // описание — до двух строк по ширине окошка
        c.font = "600 11px Rubik"; var lines = new List<string>(); var cur2 = "";
        foreach (var word in W.desc.Split(' '))
        {
            var t = cur2.Length == 0 ? word : cur2 + " " + word;
            if (c.measureText(t) > w - pad * 2 && cur2.Length > 0) { lines.Add(cur2); cur2 = word; } else cur2 = t;
        }
        if (cur2.Length > 0) lines.Add(cur2);
        if (lines.Count > 2) { lines = lines.GetRange(0, 2); lines[1] += "…"; }
        double h = pad * 2 + 22 + rows.Length * rh + 6 + lines.Count * 14, x = VW / 2 - w / 2, y = bottom - h;
        RRect(c, x, y, w, h, 12, "rgba(43,29,20,.9)", 0);
        c.strokeStyle = Rc(W.skr); c.lineWidth = 2.5; rr(c, x + 1.25, y + 1.25, w - 2.5, h - 2.5, 11); c.stroke();
        c.textAlign = "left"; c.font = "900 15px Rubik"; c.fillStyle = Rc(W.skr); c.fillText(W.name, x + pad, y + pad + 14);
        c.font = "800 11px Rubik"; c.textAlign = "right"; c.fillStyle = "rgba(255,244,220,.6)"; c.fillText(RAR[W.skr].name, x + w - pad, y + pad + 13);
        double ry = y + pad + 22;
        foreach (var (label, now, was, hb, fmt) in rows)
        {
            ry += rh;
            c.textAlign = "left"; c.font = "700 12px Rubik"; c.fillStyle = "rgba(255,244,220,.75)"; c.fillText(label, x + pad, ry);
            c.textAlign = "right"; c.font = "900 13px Rubik"; c.fillStyle = PAPER; double vx = x + w - pad - 58;
            string ns = now.ToString(fmt, L.Culture), ws = was.ToString(fmt, L.Culture);
            c.fillText(ns, vx, ry);
            if (same || ns == ws) continue; // одинаковые на экране числа — без стрелки «лучше/хуже»
            bool better = hb ? now > was : now < was; double tx = vx + 10, ty = ry - 5;
            c.fillStyle = better ? "#5fcf3f" : "#ff6b5a"; c.beginPath();
            if (now > was) { c.moveTo(tx, ty + 4); c.lineTo(tx + 5, ty - 4); c.lineTo(tx + 10, ty + 4); } else { c.moveTo(tx, ty - 4); c.lineTo(tx + 5, ty + 4); c.lineTo(tx + 10, ty - 4); }
            c.closePath(); c.fill();
            c.font = "700 11px Rubik"; c.fillStyle = "rgba(255,244,220,.5)"; c.fillText(was.ToString(fmt, L.Culture), x + w - pad, ry);
        }
        c.textAlign = "left"; c.font = "600 11px Rubik"; c.fillStyle = "rgba(255,244,220,.7)";
        for (int i = 0; i < lines.Count; i++) c.fillText(lines[i], x + pad, ry + 18 + i * 14);
    }
    static double Dmg(Weap W) => W.special == "chain" ? W.dmg * W.chain : W.dmg * Math.Max(1, W.pellets) * (W.ring > 0 ? W.ring : 1);

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
