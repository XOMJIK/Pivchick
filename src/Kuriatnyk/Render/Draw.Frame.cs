using System;
using System.Collections.Generic;
using Kuriatnyk.Render;
using SkiaSharp;
using static Kuriatnyk.D;
using static Kuriatnyk.Store;

namespace Kuriatnyk;

// Кадр игры, частицы, стики, сундуки (порт последней трети web/js/draw.js).
public static partial class G
{
    static void drawParts(Ctx c, bool groundLayer)
    {
        foreach (var p in parts)
        {
            double a = 1 - p.t / p.max;
            if (groundLayer != (p.k == "splat")) continue;
            if (p.k == "splat")
            {
                c.save(); c.globalAlpha = Math.Min(1, a * 2); c.translate(p.x, p.y);
                c.fillStyle = "#fffaf0"; c.beginPath(); for (int i = 0; i < 9; i++) { double an = i / 9.0 * TAU, q = p.r * (.75 + ((p.seed * 97 * (i + 1)) % 1) * .5); c.lineTo(Math.Cos(an) * q, Math.Sin(an) * q * .5); } c.closePath(); c.fill();
                c.fillStyle = "#ffb21f"; c.beginPath(); c.ellipse(1, 0, p.r * .42, p.r * .24, 0, 0, TAU); c.fill(); c.restore();
            }
            else if (p.k == "feather")
            {
                c.save(); c.globalAlpha = Math.Min(1, a * 3); c.translate(p.x, p.y - p.z); c.rotate(p.rot);
                c.fillStyle = p.col; c.strokeStyle = INK; c.lineWidth = 1; c.beginPath(); c.ellipse(0, 0, 5.5, 2.2, 0, 0, TAU); c.fill(); c.stroke(); c.restore();
            }
            else if (p.k == "puff") { c.globalAlpha = a * .85; c.fillStyle = p.col; c.beginPath(); c.arc(p.x, p.y - p.z, p.r * (1 + p.t / p.max), 0, TAU); c.fill(); c.globalAlpha = 1; }
            else if (p.k == "shell")
            {
                c.save(); c.translate(p.x, p.y - p.z); c.rotate(p.rot); c.globalAlpha = Math.Min(1, a * 3); c.fillStyle = p.dirt ? "#8a5a2b" : "#fffaf0"; c.strokeStyle = INK; c.lineWidth = 1;
                c.beginPath(); c.moveTo(-4, -2); c.lineTo(4, -3); c.lineTo(2, 3); c.closePath(); c.fill(); c.stroke(); c.restore();
            }
            else if (p.k == "flash")
            {
                c.save(); c.translate(p.x, p.y - p.z); c.rotate(p.a); c.scale(p.s, p.s); c.fillStyle = "#fff3a0"; c.beginPath();
                for (int i = 0; i < 8; i++) { double an = i / 8.0 * TAU, q = i % 2 == 1 ? 4 : 11; c.lineTo(Math.Cos(an) * q * 1.3, Math.Sin(an) * q * .8); }
                c.closePath(); c.fill(); c.fillStyle = "#fff"; c.beginPath(); c.arc(0, 0, 3.5, 0, TAU); c.fill(); c.restore();
            }
            else if (p.k == "plus")
            {
                c.save(); c.globalAlpha = Math.Min(1, a * 2); c.translate(p.x, p.y - p.z); c.fillStyle = "#5fcf3f"; c.strokeStyle = INK; c.lineWidth = 1.4;
                c.beginPath(); c.rect(-2, -6, 4, 12); c.rect(-6, -2, 12, 4); c.fill(); c.fillStyle = "#9cf27a"; c.fillRect(-1.2, -5, 2.4, 10); c.fillRect(-5, -1.2, 10, 2.4); c.restore();
            }
            else if (p.k == "beam")
            {
                double wk = p.s > 0 ? p.s : 1;
                c.save(); c.lineCap = "round"; c.globalAlpha = a; c.strokeStyle = p.col ?? "rgba(120,230,255,.55)"; c.lineWidth = (16 * a + 2) * wk; seg(c, p.x, p.y - 19, p.x2, p.y2 - 19);
                c.strokeStyle = "#ffffff"; c.lineWidth = (4.5 * a + 1) * Math.Max(.5, wk); seg(c, p.x, p.y - 19, p.x2, p.y2 - 19); c.restore();
            }
            else if (p.k == "bolt")
            { // молния: ломаная линия от точки к точке
                c.save(); c.lineCap = "round"; c.lineJoin = "round"; c.globalAlpha = a;
                double dx = p.x2 - p.x, dy = p.y2 - p.y, l = Math.Max(1, hypot(dx, dy)), nx = -dy / l, ny = dx / l; int n = Math.Max(3, (int)(l / 22));
                for (int pass = 0; pass < 2; pass++)
                {
                    c.strokeStyle = pass == 0 ? "rgba(180,140,255,.6)" : "#ffffff"; c.lineWidth = pass == 0 ? 7 : 2.4; c.beginPath(); c.moveTo(p.x, p.y);
                    for (int i = 1; i < n; i++) { double k = (double)i / n, j = (fr((int)(p.seed * 1000) + i + (int)(p.t * 30)) - .5) * 18; c.lineTo(p.x + dx * k + nx * j, p.y + dy * k + ny * j); }
                    c.lineTo(p.x2, p.y2); c.stroke();
                }
                c.restore();
            }
            else if (p.k == "kit")
            {
                double q = Math.Min(1, p.t / p.max), x = p.sx + (p.x - p.sx) * q, y = p.sy + (p.y - p.sy) * q - Math.Sin(q * Math.PI) * 70 - 20; c.save(); c.translate(x, y); c.rotate(q * 6);
                c.fillStyle = "#fff"; c.strokeStyle = INK; c.lineWidth = 1.8; rr(c, -7, -6, 14, 12, 2); c.fill(); c.stroke(); c.fillStyle = "#e8322a"; c.fillRect(-1.5, -4, 3, 8); c.fillRect(-4, -1.5, 8, 3); c.restore();
            }
            else if (p.k == "swing")
            { // след взмаха ближним оружием: светлая дуга перед героем (ширина — по сектору удара)
                double q = p.t / p.max, R = p.R > 0 ? p.R : 80, w = p.s > 0 ? p.s : 1.1; c.save(); c.translate(p.x, p.y - 19); c.scale(1, .75); c.lineCap = "round";
                c.globalAlpha = Math.Max(0, 1 - q); c.strokeStyle = p.col ?? "rgba(255,244,220,.9)"; c.lineWidth = 10 * (1 - q) + 2;
                c.beginPath(); c.arc(0, 0, R * .8, p.a - w + q * .3, p.a + w * (q * 2 - 1) + .3); c.stroke();
                c.strokeStyle = "rgba(255,201,58,.8)"; c.lineWidth = 3; c.beginPath(); c.arc(0, 0, R * .95, p.a - w * .9, p.a + w * .9 * (q * 2 - 1)); c.stroke(); c.restore();
            }
            else if (p.k == "ring")
            {
                double R = p.R > 0 ? p.R : 130, f = 1 - a * a * .6; c.globalAlpha = a; c.strokeStyle = p.col ?? "#fff6c8"; c.lineWidth = 6 * a + 1;
                c.beginPath(); c.ellipse(p.x, p.y, R * f, R * .42 * f, 0, 0, TAU); c.stroke(); c.globalAlpha = 1;
            }
        }
    }

    static Sprite playerSprite() => new Sprite
    {
        x = P.x, y = P.y, face = P.face, ang = P.ang, phase = P.phase, moving = P.moving, flash = P.flash, kind = "player", gun = P.w, blink = P.inv > 0, buff = P.buff,
        swim = P.hop && inWater(P.x, P.y), ghost = P.buff == "shadow", swing = P.swing > 0 ? P.swing / .22 : 0
    }.With(P.look);

    static readonly Ctx frameCtx = new();
    // список отрисовки по глубине: без лямбд на каждый объект — меньше мусора для сборщика в каждом кадре
    struct Item { public double y; public int i; public byte kind; public object o; }
    static readonly List<Item> drawList = new();
    static readonly Comparison<Item> byDepth = (p, q) => p.y != q.y ? p.y.CompareTo(q.y) : p.i.CompareTo(q.i);

    /// <summary>Нарисовать кадр игры на полотне размером VW×VH логических пикселей.</summary>
    public static void render(SKCanvas canvas)
    {
        var c = frameCtx; c.Begin(canvas);
        c.fillStyle = DUN != null ? DUN_VOID : MAPS[MAP].@out; c.fillRect(0, 0, VW, VH);
        if (P == null || ground == null) return;
        double Z = S.mode == "menu" ? SC * MENU_ZOOM : SC;
        c.save(); c.scale(Z, Z);
        double sx = 0, sy = 0; if (S.shake > 0 && !REDUCED && SET.shake) { sx = (rnd() - .5) * S.shake; sy = (rnd() - .5) * S.shake; }
        c.translate(-camX + sx, -camY + sy);
        // земля целиком: всё, что вне кадра, Skia и так отсекает, а с целой картинкой работают mip-уровни (кусок рисуется медленнее)
        c.drawImage(ground, 0, 0, WW, WH);
        drawRiverDyn(c); drawDungeonDyn(c);
        drawParts(c, true);
        foreach (var h in holes) drawHole(c, h);
        foreach (var z in zones)
        {
            double k = Math.Min(1, z.t / .4), a = .22 + Math.Sin(S.t * 6) * .06; c.save(); c.globalAlpha = a * k; c.fillStyle = "#9cf27a"; c.beginPath(); c.ellipse(z.x, z.y, z.r, z.r * .45, 0, 0, TAU); c.fill();
            c.globalAlpha = .85 * k; c.strokeStyle = "#5fcf3f"; c.lineWidth = 3; c.setLineDash(new double[] { 12, 8 }); c.lineDashOffset = -S.t * 30; c.stroke(); c.setLineDash(null);
            c.fillStyle = "#ffffff"; c.strokeStyle = INK; c.lineWidth = 1.5;
            for (int i = 0; i < 4; i++) { double an = S.t * .8 + i * TAU / 4, px = z.x + Math.Cos(an) * z.r * .55, py = z.y + Math.Sin(an) * z.r * .25 - 8 - Math.Sin(S.t * 3 + i) * 4; c.fillRect(px - 2, py - 6, 4, 12); c.fillRect(px - 6, py - 2, 12, 4); }
            c.restore();
        }
        foreach (var p in pickups) drawPickup(c, p);
        foreach (var pd in puddles)
        {
            double k = Math.Min(Math.Min(1, pd.t / .6), (pd.max - pd.t) / .3 + .2); c.save(); c.globalAlpha = .55 * k; c.fillStyle = "#4aa8d8"; c.beginPath(); c.ellipse(pd.x, pd.y, pd.r, pd.r * .45, 0, 0, TAU); c.fill();
            c.globalAlpha = .7 * k; c.strokeStyle = "#bfe6ff"; c.lineWidth = 2;
            for (int i = 0; i < 3; i++) { var q = ((S.t * .6 + i / 3.0) % 1); c.beginPath(); c.ellipse(pd.x + Math.Sin(i * 2.1) * pd.r * .4, pd.y + Math.Cos(i * 1.7) * pd.r * .15, 14 + q * 30, (14 + q * 30) * .4, 0, 0, TAU); c.stroke(); }
            c.restore();
        }
        if (goo.Count > 0) drawGoo(c);
        foreach (var z in strikes)
        {
            double k = 1 - z.t / z.max, pl = .5 + .5 * Math.Sin(S.t * 14); c.save(); c.translate(z.x, z.y);
            c.globalAlpha = .16 + k * .14; c.fillStyle = "#ffc93a"; c.beginPath(); c.ellipse(0, 0, z.r, z.r * .45, 0, 0, TAU); c.fill();
            c.globalAlpha = .75 + pl * .25; c.strokeStyle = "#fff3b0"; c.lineWidth = 3; c.setLineDash(new double[] { 14, 9 }); c.lineDashOffset = -S.t * 40; c.beginPath(); c.ellipse(0, 0, z.r, z.r * .45, 0, 0, TAU); c.stroke(); c.setLineDash(null);
            c.lineWidth = 2.4; c.strokeStyle = "#ff9a1f"; seg(c, -16, 0, 16, 0); seg(c, 0, -8, 0, 8); c.restore();
        }
        foreach (var b in bombs)
        {
            double k = 1 - b.t / b.max; c.save(); c.translate(b.x, b.y);
            if (b.quiet)
            {
                c.globalAlpha = 1; double zq = (1 - k) * 190 + 8; c.fillStyle = "rgba(25,50,10,.25)"; c.beginPath(); c.ellipse(0, 0, 6 * k + 2, 2.5 * k + 1, 0, 0, TAU); c.fill(); c.translate(0, -zq);
                c.fillStyle = "#fffaf0"; c.strokeStyle = INK; c.lineWidth = 2; c.beginPath(); c.ellipse(0, 0, 6, 7.5, 0, 0, TAU); c.fill(); c.stroke(); c.restore(); continue;
            }
            c.globalAlpha = .25 + k * .35; c.fillStyle = b.fromP ? "#ffc93a" : "#e8322a"; c.beginPath(); c.ellipse(0, 0, b.r * k, b.r * .42 * k, 0, 0, TAU); c.fill();
            c.globalAlpha = .9; c.strokeStyle = b.fromP ? "#fff3b0" : "#ff5a3c"; c.lineWidth = 2.5; c.beginPath(); c.ellipse(0, 0, b.r, b.r * .42, 0, 0, TAU); c.stroke();
            c.globalAlpha = 1;
            if (b.lob) { c.translate((b.sx - b.x) * (1 - k), (b.sy - b.y) * (1 - k)); c.translate(0, -(Math.Sin(k * Math.PI) * 130 + 14)); }
            else { double zz = (1 - k) * 190 + 8; c.translate(0, -zz); }
            c.strokeStyle = INK; c.lineWidth = 2;
            if (b.kind == "pumpkin")
            { // тыква с хвостиком
                c.rotate(k * 9); c.fillStyle = "#f28c28"; c.beginPath(); c.ellipse(0, 0, 10, 8, 0, 0, TAU); c.fill(); c.stroke();
                c.strokeStyle = "#c9661d"; c.lineWidth = 1.3; seg(c, -4, -7, -4, 7); seg(c, 4, -7, 4, 7); c.fillStyle = "#6db343"; c.fillRect(-1.2, -11, 2.4, 4); c.restore(); continue;
            }
            c.fillStyle = "#fffaf0"; c.beginPath(); c.ellipse(0, 0, 6, 7.5, 0, 0, TAU); c.fill(); c.stroke(); c.restore();
        }
        foreach (var n in nades) { c.fillStyle = "rgba(25,50,10,.3)"; c.beginPath(); c.ellipse(n.x, n.y, 7, 3, 0, 0, TAU); c.fill(); }
        drawList.Clear(); int idx = 0;
        void add(double y, byte kind, object o) => drawList.Add(new Item { y = y, i = idx++, kind = kind, o = o });
        double vx0 = camX - 120, vy0 = camY - 160, vx1 = camX + VW / Z + 120, vy1 = camY + VH / Z + 200; // что далеко за кадром — не рисуем
        bool vis(double x, double y) => x > vx0 && x < vx1 && y > vy0 && y < vy1;
        foreach (var o in OBS) if (vis(o.x, o.y)) add(o.y, 0, o);
        foreach (var e in enemies) if (vis(e.x, e.y)) add(e.y + (e.T.fly ? 60 : 0), 1, e);
        if (P.alive) add(P.y, 2, P);
        foreach (var a in allies) if (a.down <= 0) add(a.y, 3, a);
        foreach (var n in nades) add(n.y, 4, n);
        drawList.Sort(byDepth);
        foreach (var it in drawList)
            switch (it.kind)
            {
                case 0: drawObstacle(c, (Obs)it.o); break;
                case 1: drawEnemy(c, (Enemy)it.o); break;
                case 2:
                    if (P.bowT > 0)
                    { // поклон статуе: наклон вперёд и обратно
                        double bk = Math.Sin(clamp(1 - P.bowT / .9, 0, 1) * Math.PI) * .5;
                        c.save(); c.translate(P.x, P.y); c.rotate(bk * P.face); c.translate(-P.x, -P.y); drawBird(c, playerSprite()); c.restore();
                    }
                    else drawBird(c, playerSprite());
                    break;
                case 3:
                    {
                        var aa = (Ally)it.o;
                        if (aa.cls == "medic" && S.mode != "menu") { c.save(); c.globalAlpha = .18 + Math.Sin(S.t * 3) * .05; c.strokeStyle = "#5fcf3f"; c.lineWidth = 2; c.setLineDash(DASH8); c.beginPath(); c.ellipse(aa.x, aa.y, 240, 100, 0, 0, TAU); c.stroke(); c.restore(); }
                        c.save(); c.strokeStyle = "rgba(95,207,63,.9)"; c.lineWidth = 2.5; c.beginPath(); c.ellipse(aa.x, aa.y + 1, 16, 6, 0, 0, TAU); c.stroke(); c.restore();
                        drawBird(c, new Sprite { x = aa.x, y = aa.y, face = aa.face, ang = aa.ang, phase = aa.phase, moving = aa.moving, flash = aa.flash, kind = "player", gun = aa.gun, swim = aa.hop && inWater(aa.x, aa.y), buff = aa.buff }.With(aa.look));
                        break;
                    }
                case 4:
                    {
                        var nn = (Nade)it.o;
                        c.save(); c.translate(nn.x, nn.y - nn.z - 8); c.rotate(nn.rot); c.fillStyle = nn.t < .35 && Odd(S.t * 20) ? "#ffd34d" : "#fffaf0"; c.strokeStyle = INK; c.lineWidth = 2;
                        c.beginPath(); c.ellipse(0, 0, 6, 7.5, 0, 0, TAU); c.fill(); c.stroke(); c.fillStyle = "#c0392b"; c.fillRect(-5.6, -1, 11.2, 3); c.restore();
                        break;
                    }
            }
        foreach (var b in bullets) drawBullet(c, b);
        if (fish.Count > 0) drawFishAir(c);
        drawParts(c, false);
        foreach (var t in texts)
        {
            double a = 1 - t.t / t.max; c.globalAlpha = Math.Min(1, a * 2.5); c.font = textFont(t.size); c.textAlign = "center"; c.lineJoin = "round";
            c.strokeStyle = INK; c.lineWidth = 4; c.strokeText(t.txt, t.x, t.y); c.fillStyle = t.col; c.fillText(t.txt, t.x, t.y); c.globalAlpha = 1;
        }
        foreach (var a in allies)
        {
            if (a.down > 0 || S.mode == "menu") continue; double w = 28, y = a.y - 56;
            c.fillStyle = INK; c.fillRect(a.x - w / 2 - 1.5, y - 1.5, w + 3, 11); c.fillStyle = "#8e2a21"; c.fillRect(a.x - w / 2, y, w, 5); c.fillStyle = "#5fcf3f"; c.fillRect(a.x - w / 2, y, w * Math.Max(0, a.hp / a.max), 5);
            c.fillStyle = "#4a3a2a"; c.fillRect(a.x - w / 2, y + 6, w, 2.5);
            if (a.summon) { c.fillStyle = "#9cf27a"; c.fillRect(a.x - w / 2, y + 6, w * clamp(a.summonT / SKU.SUMMON_T, 0, 1), 2.5); continue; } // вызванному бойцу — сколько ему осталось
            c.fillStyle = a.ult >= 100 ? (Odd(S.t * 8) ? "#fff" : "#ffc93a") : "#ffc93a"; c.fillRect(a.x - w / 2, y + 6, w * Math.Min(1, a.ult / 100), 2.5);
        }
        foreach (var e in enemies)
        {
            if (e.hp < e.max && !e.T.boss)
            {
                double w = e.type == "rat" ? 20 : 30, h = 5, y = e.y + (e.type == "crow" ? -66 : e.type == "rat" ? -26 : e.type == "bigrat" ? -38 : e.type == "turkey" ? -62 : -52);
                c.fillStyle = INK; c.fillRect(e.x - w / 2 - 1.5, y - 1.5, w + 3, h + 3); c.fillStyle = "#8e2a21"; c.fillRect(e.x - w / 2, y, w, h); c.fillStyle = "#ffc93a"; c.fillRect(e.x - w / 2, y, w * Math.Max(0, e.hp / e.max), h);
            }
        }
        c.restore();
        if (S.night)
        {
            double px = (P.x - camX) * SC, py = (P.y - 24 - camY) * SC, fl = P.buff == "adren" ? 1.25 : 1;
            var g = c.createRadialGradient(px, py, 70 * SC * fl, px, py, NIGHT_R * SC * fl); g.addColorStop(0, "rgba(8,10,30,0)"); g.addColorStop(.55, "rgba(8,10,30,.55)"); g.addColorStop(1, "rgba(8,10,30,.97)");
            c.fillStyle = g; c.fillRect(0, 0, VW, VH);
        }
        c.save(); c.scale(Z, Z); c.translate(-camX + sx, -camY + sy); drawWarnings(c); c.restore();
        if (S.mode == "play")
        {
            if (!S.night)
                foreach (var e in enemies)
                {
                    double sx2 = (e.x - camX) * SC, sy2 = (e.y - 20 - camY) * SC; if (sx2 > -10 && sx2 < VW + 10 && sy2 > -10 && sy2 < VH + 10) continue;
                    double cx = VW / 2, cy = VH / 2, a = Math.Atan2(sy2 - cy, sx2 - cx), m = 26; double px = clamp(sx2, m, VW - m), py = clamp(sy2, m + 60, VH - m - 60);
                    var Tt = e.T;
                    c.save(); c.translate(px, py); c.rotate(a); c.fillStyle = Tt.boss ? "#ffc93a" : e.type == "fox" ? "#e8742a" : e.type == "turkey" ? "#8a5a2b" : e.type == "crow" ? "#2e2a33" : "#c0392b"; c.strokeStyle = INK; c.lineWidth = 2.5;
                    double s2 = Tt.boss ? 1.6 : 1; c.beginPath(); c.moveTo(11 * s2, 0); c.lineTo(-7 * s2, -8 * s2); c.lineTo(-7 * s2, 8 * s2); c.closePath(); c.fill(); c.stroke(); c.restore();
                }
            drawSticks(c);
        }
        if (P.flash > 0 || (S.mode == "play" && P.hp < P.max * .3))
        {
            double k = P.flash > 0 ? .45 : .18 + Math.Sin(S.t * 6) * .08;
            var g = c.createRadialGradient(VW / 2, VH / 2, Math.Min(VW, VH) * .35, VW / 2, VH / 2, Math.Max(VW, VH) * .75);
            g.addColorStop(0, "rgba(192,57,43,0)"); g.addColorStop(1, "rgba(192,57,43," + Inv(k) + ")"); c.fillStyle = g; c.fillRect(0, 0, VW, VH);
        }
        if (S.mode is "play" or "dying" or "won" or "paused" or "pick") Hud.Draw(c);
    }

    static readonly double[] DASH8 = { 8, 8 };
    static readonly Dictionary<double, string> fontBySize = new();
    static string textFont(double size) { if (!fontBySize.TryGetValue(size, out var f)) fontBySize[size] = f = "900 " + size.ToString(System.Globalization.CultureInfo.InvariantCulture) + "px Rubik"; return f; }
    public static void drawBulletPublic(Ctx c, Bullet b) => drawBullet(c, b); // для листа спрайтов в Snap
    static void drawBullet(Ctx c, Bullet b)
    {
        if (b.w != null) { drawSkBullet(c, b); return; }
        if (b.fromP && b.kind == "flame")
        {
            double q = 1 - b.life / (b.max > 0 ? b.max : .36); c.globalAlpha = Math.Max(0, .9 - q * .8); c.fillStyle = q < .3 ? "#fff2a0" : q < .65 ? "#ff9a1f" : "#e8322a";
            c.beginPath(); c.arc(b.x, b.y - 19, 4 + q * 17, 0, TAU); c.fill(); c.globalAlpha = 1; return;
        }
        if (b.fromP && b.kind == "ice") { c.save(); c.translate(b.x, b.y - 19); c.rotate(Math.Atan2(b.vy, b.vx)); c.fillStyle = "#e6f8ff"; c.strokeStyle = "#3b8fd1"; c.lineWidth = 1.4; c.beginPath(); c.moveTo(9, 0); c.lineTo(-6, -3.2); c.lineTo(-3, 0); c.lineTo(-6, 3.2); c.closePath(); c.fill(); c.stroke(); c.restore(); return; }
        if (!b.fromP && b.kind == "bolt") { c.save(); c.translate(b.x, b.y - 19); c.rotate(Math.Atan2(b.vy, b.vx)); c.fillStyle = "rgba(255,60,40,.35)"; c.beginPath(); c.ellipse(0, 0, 13, 6, 0, 0, TAU); c.fill(); c.fillStyle = "#ff4d2e"; c.strokeStyle = INK; c.lineWidth = 1.4; c.beginPath(); c.ellipse(0, 0, 8, 3, 0, 0, TAU); c.fill(); c.stroke(); c.restore(); return; }
        if (!b.fromP && b.kind == "feather") { c.save(); c.translate(b.x, b.y - 19); c.rotate(Math.Atan2(b.vy, b.vx)); c.fillStyle = "#f4e9d2"; c.strokeStyle = INK; c.lineWidth = 1.3; c.beginPath(); c.ellipse(0, 0, 10, 2.8, 0, 0, TAU); c.fill(); c.stroke(); c.fillStyle = "#8a6a45"; c.fillRect(-10, -1, 5, 2); c.restore(); return; }
        if (!b.fromP && (b.kind == "rock" || b.kind == "pellet" || b.kind == "dirt"))
        {
            c.fillStyle = b.kind == "rock" ? "#8d8a85" : b.kind == "dirt" ? "#8a5a2b" : "#c9c2b5"; c.strokeStyle = INK; c.lineWidth = 1.6; c.beginPath(); c.arc(b.x, b.y - 19, b.r, 0, TAU); c.fill(); c.stroke();
            c.fillStyle = "rgba(25,50,10,.2)"; c.beginPath(); c.ellipse(b.x, b.y, b.r * .8, b.r * .3, 0, 0, TAU); c.fill(); return;
        }
        if (b.fromP && b.kind == "pfeather") { c.save(); c.translate(b.x, b.y - 19); c.rotate(Math.Atan2(b.vy, b.vx)); c.fillStyle = "#e8f4ff"; c.strokeStyle = INK; c.lineWidth = 1.3; c.beginPath(); c.ellipse(0, 0, 10, 3, 0, 0, TAU); c.fill(); c.stroke(); c.strokeStyle = "#86bcdc"; c.lineWidth = 1.2; seg(c, -9, 0, 8, 0); c.restore(); return; }
        if (b.fromP && b.kind == "fireball") { c.save(); c.translate(b.x, b.y - 19); c.fillStyle = "rgba(255,90,30,.35)"; c.beginPath(); c.arc(0, 0, b.r * 2, 0, TAU); c.fill(); c.fillStyle = "#ff7a1a"; c.strokeStyle = INK; c.lineWidth = 1.8; c.beginPath(); c.arc(0, 0, b.r, 0, TAU); c.fill(); c.stroke(); c.fillStyle = "#ffe066"; c.beginPath(); c.arc(-1, -1, b.r * .45, 0, TAU); c.fill(); c.restore(); return; }
        if (b.fromP && b.kind == "yarn")
        {
            c.save(); c.translate(b.x, b.y - 19);
            c.strokeStyle = "rgba(255,111,168,.55)"; c.lineWidth = 2; c.beginPath(); var a = Math.Atan2(b.vy, b.vx); c.moveTo(-Math.Cos(a) * 8, -Math.Sin(a) * 8); c.quadraticCurveTo(-Math.Cos(a) * 18 + Math.Sin(b.spin) * 5, -Math.Sin(a) * 18, -Math.Cos(a) * 28, -Math.Sin(a) * 28 + Math.Cos(b.spin) * 4); c.stroke();
            c.rotate(b.spin); c.strokeStyle = INK; c.lineWidth = 2; circ(c, 0, 0, b.r, "#ff7fb6");
            c.strokeStyle = "#c2407e"; c.lineWidth = 1.3; for (int i = -1; i <= 1; i++) { c.beginPath(); c.ellipse(0, i * 2.6, b.r * .85, 1.8, 0, 0, TAU); c.stroke(); }
            c.restore(); return;
        }
        if (b.fromP && b.kind == "figure") { c.save(); c.translate(b.x, b.y - 19); c.rotate(S.t * 9 + b.shape); c.fillStyle = FIGCOL[b.shape]; c.strokeStyle = INK; c.lineWidth = 1.4; figShape(c, b.shape, 7.5); c.fill(); c.stroke(); c.restore(); return; }
        if (b.fromP)
        {
            var k = b.kind;
            var col = k == "turbocat" ? "rgba(255,170,90,.65)" : k is "syringe" or "vitamin" ? "rgba(156,242,122,.65)" : k is "crossbow" or "cornrifle" ? "rgba(255,255,255,.75)" : k == "fire" ? "rgba(255,122,26,.65)" : k == "popcorn" ? "rgba(255,255,255,.6)" : k == "millet" ? "rgba(255,224,102,.6)" : k == "refl" ? "rgba(207,232,255,.7)" : "rgba(255,243,170,.55)";
            c.strokeStyle = col; c.lineWidth = b.r * 1.2; c.lineCap = "round"; seg(c, b.x - b.vx * .025, b.y - 19 - b.vy * .025, b.x, b.y - 19);
            c.fillStyle = k == "turbocat" ? "#ffb35c" : k == "syringe" ? "#e6f6ff" : k == "vitamin" ? ((long)Math.Floor(b.x) % 2 != 0 ? "#e8322a" : "#9cf27a") : k == "fire" ? "#ffd23a" : k == "popcorn" ? "#fffbe8" : k == "millet" ? "#ffe066" : k == "refl" ? "#cfe8ff" : "#fff8c8";
            c.strokeStyle = INK; c.lineWidth = 1.5; c.beginPath(); c.arc(b.x, b.y - 19, b.r * (k == "popcorn" ? 1.1 : .8), 0, TAU); c.fill(); c.stroke();
        }
        else if (b.kind == "fire")
        {
            c.fillStyle = "rgba(255,90,40,.35)"; c.beginPath(); c.arc(b.x, b.y - 19, b.r * 1.8, 0, TAU); c.fill(); c.fillStyle = "#ff7a1a"; c.strokeStyle = INK; c.lineWidth = 1.8; c.beginPath(); c.arc(b.x, b.y - 19, b.r, 0, TAU); c.fill(); c.stroke();
            c.fillStyle = "#ffe066"; c.beginPath(); c.arc(b.x - 1, b.y - 20, b.r * .45, 0, TAU); c.fill();
        }
        else
        {
            c.save(); c.translate(b.x, b.y - 19); c.rotate(Math.Atan2(b.vy, b.vx)); c.fillStyle = "#fffaf0"; c.strokeStyle = INK; c.lineWidth = 1.8; c.beginPath(); c.ellipse(0, 0, 6.5, 5, 0, 0, TAU); c.fill(); c.stroke(); c.restore();
            c.fillStyle = "rgba(25,50,10,.2)"; c.beginPath(); c.ellipse(b.x, b.y, 4, 1.6, 0, 0, TAU); c.fill();
        }
    }

    static void drawSticks(Ctx c)
    {
        void show(Stick s, double dx, double dy, string label)
        {
            double ox, oy, kx, ky; bool act = s != null;
            if (s != null) { ox = s.ox; oy = s.oy; var v = stickVal(s); kx = ox + v.x * STICK_R; ky = oy + v.y * STICK_R; }
            else { if (!COARSE) return; ox = dx; oy = dy; kx = ox; ky = oy; }
            bool fire = label == T("огонь");
            c.globalAlpha = act ? 1 : .55;
            c.fillStyle = "rgba(43,29,20,.18)"; c.strokeStyle = "rgba(255,255,255,.7)"; c.lineWidth = 3; c.beginPath(); c.arc(ox, oy, STICK_R, 0, TAU); c.fill(); c.stroke();
            c.fillStyle = fire ? "rgba(192,57,43,.85)" : "rgba(255,255,255,.85)"; c.strokeStyle = INK; c.lineWidth = 3; c.beginPath(); c.arc(kx, ky, 24, 0, TAU); c.fill(); c.stroke();
            if (!act) { c.font = "800 12px Rubik"; c.textAlign = "center"; c.fillStyle = fire ? "#fff" : INK; c.fillText(label, ox, oy + 4); }
            c.globalAlpha = 1;
        }
        bool land = VW > VH; double by = VH - (land ? Math.Max(100, VH * .28) : Math.Max(130, VH * .17));
        show(sticks.move, land ? Math.Max(130, VW * .16) : Math.Max(86, VW * .22), by, T("бег"));
        show(sticks.aim, VW - (land ? Math.Max(150, VW * .17) : Math.Max(96, VW * .25)), by, T("огонь"));
    }

    static double fr(int i) { var v = Math.Sin(i * 12.9898 + 78.233) * 43758.5453; return v - Math.Floor(v); }
    static void chestRays(Ctx c, double cx, double cy, double open)
    {
        if (open <= 0) return; c.save(); c.translate(cx, cy); c.globalAlpha = open;
        for (int i = 0; i < 12; i++)
        {
            var a = -Math.PI / 2 + (i - 5.5) * .24 + Math.Sin(S.t * 2) * .05; c.fillStyle = i % 2 == 1 ? "rgba(255,230,140,.65)" : "rgba(255,255,255,.45)";
            c.beginPath(); c.moveTo(0, 0); c.lineTo(Math.Cos(a - .07) * 190, Math.Sin(a - .07) * 190); c.lineTo(Math.Cos(a + .07) * 190, Math.Sin(a + .07) * 190); c.closePath(); c.fill();
        }
        c.restore();
    }
    public static void drawChest(Ctx c, int tier, double open, double shake, double crack = 0)
    {
        bool big = tier >= 1, sup = tier == 2, leg = tier == 3;
        c.save(); c.rotate(shake); c.lineJoin = "round"; c.lineCap = "round";
        c.fillStyle = "rgba(60,30,0,.22)"; c.beginPath(); c.ellipse(0, 3, big ? 82 : 64, 10, 0, 0, TAU); c.fill();
        if (!big)
        {
            // wicker basket of a laying hen, covered with a checkered cloth
            double w = 104, h = 54;
            chestRays(c, 0, -h - 6, open);
            c.strokeStyle = INK; c.lineWidth = 8; c.beginPath(); c.moveTo(-w * .4, -h - 2); c.bezierCurveTo(-w * .4, -h - 54, w * .4, -h - 54, w * .4, -h - 2); c.stroke();
            c.strokeStyle = "#c98a43"; c.lineWidth = 4.5; c.stroke();
            c.strokeStyle = "#8a5a2b"; c.lineWidth = 1.2; c.setLineDash(new double[] { 4, 4 }); c.stroke(); c.setLineDash(null);
            if (open > 0)
            {
                c.strokeStyle = "#e8c26a"; c.lineWidth = 2; for (int i = 0; i < 14; i++) { var x = -w * .42 + fr(i) * w * .84; seg(c, x, -h - 2, x + (fr(i + 7) - .5) * 14, -h - 10 - fr(i + 3) * 8); }
                foreach (var (x, y, col) in new[] { (-24.0, -h - 6, "#fffaf0"), (0.0, -h - 11, "#f1d3a8"), (24.0, -h - 6, "#fffaf0") })
                {
                    c.fillStyle = col; c.strokeStyle = INK; c.lineWidth = 2.4; c.beginPath(); c.ellipse(x, y, 11, 14, 0, 0, TAU); c.fill(); c.stroke();
                    c.fillStyle = "rgba(255,255,255,.8)"; c.beginPath(); c.ellipse(x - 4, y - 5, 2.5, 4, -.3, 0, TAU); c.fill();
                }
            }
            void body() { c.beginPath(); c.moveTo(-w / 2, -h); c.lineTo(w / 2, -h); c.lineTo(w * .39, -5); c.quadraticCurveTo(w * .37, 0, w * .31, 0); c.lineTo(-w * .31, 0); c.quadraticCurveTo(-w * .37, 0, -w * .39, -5); c.closePath(); }
            body(); c.fillStyle = "#d39a4f"; c.fill();
            c.save(); body(); c.clip();
            int r = 0; for (double y = -h + 6; y < 6; y += 9, r++) { int k = 0; for (double x = -w / 2 - 7 + (r % 2) * 7; x < w / 2 + 8; x += 14, k++) { c.fillStyle = (r + k) % 2 == 1 ? "#e6b465" : "#b97a35"; c.beginPath(); c.ellipse(x, y, 6.6, 3.7, 0, 0, TAU); c.fill(); } }
            c.strokeStyle = "rgba(110,65,25,.6)"; c.lineWidth = 1.4; for (double x = -w / 2; x <= w / 2; x += 14) seg(c, x, -h, x * .8, 0);
            c.restore();
            body(); c.strokeStyle = INK; c.lineWidth = 3; c.stroke();
            c.fillStyle = "#b97a35"; rr(c, -w / 2 - 5, -h - 6, w + 10, 11, 5.5); c.fill(); c.stroke();
            c.strokeStyle = "#8a5a2b"; c.lineWidth = 1.5; for (double x = -w / 2; x < w / 2 + 2; x += 8) seg(c, x, -h - 4, x + 5, -h + 3);
            c.save(); c.translate(open * 40, -open * 78); c.rotate(open * .75); c.globalAlpha = 1 - open * .55;
            void cloth() { c.beginPath(); c.moveTo(-w / 2 - 7, -h - 2); c.quadraticCurveTo(-w * .3, -h - 31, 0, -h - 29); c.quadraticCurveTo(w * .3, -h - 31, w / 2 + 7, -h - 2); c.lineTo(w / 2 + 2, -h + 17); c.lineTo(w * .34, -h + 3); c.lineTo(-w * .34, -h + 3); c.lineTo(-w / 2 - 2, -h + 17); c.closePath(); }
            cloth(); c.fillStyle = "#fff4dc"; c.fill();
            c.save(); cloth(); c.clip(); c.fillStyle = "#e8322a";
            for (int iy = 0; iy < 8; iy++) for (int ix = 0; ix < 15; ix++) if ((ix + iy) % 2 == 0) c.fillRect(-w / 2 - 12 + ix * 9, -h - 36 + iy * 9, 9, 9);
            c.fillStyle = "rgba(232,50,42,.35)"; for (int iy = 0; iy < 8; iy++) for (int ix = 0; ix < 15; ix++) if ((ix + iy) % 2 == 1 && ix % 2 == 0) c.fillRect(-w / 2 - 12 + ix * 9, -h - 36 + iy * 9, 9, 9);
            c.restore();
            cloth(); c.strokeStyle = INK; c.lineWidth = 3; c.stroke();
            c.strokeStyle = "rgba(43,29,20,.35)"; c.lineWidth = 1.6; seg(c, -w * .2, -h - 25, -w * .27, -h + 3); seg(c, w * .18, -h - 26, w * .25, -h + 3);
            if (open < .5) { c.save(); c.translate(w * .3, -h - 17); c.rotate(-.9); c.fillStyle = "#fff"; c.strokeStyle = INK; c.lineWidth = 1.8; c.beginPath(); c.ellipse(0, -10, 5, 12, 0, 0, TAU); c.fill(); c.stroke(); seg(c, 0, 3, 0, -21); c.restore(); }
            c.restore();
        }
        else
        {
            // большой — золотое яйцо в гнезде, сверхбольшой — аметистовое яйцо с короной, легендарный — огненное яйцо в языках пламени
            const double EY = -62, RX = 38, RY = 48;
            chestRays(c, 0, EY, open);
            if (leg)
                for (int i = 0; i < 7; i++)
                { // пламя за яйцом
                    double a = -Math.PI / 2 + (i - 3) * .42, fl = 1 + .18 * Math.Sin(S.t * 9 + i * 1.7), r0 = RX * .7, r1 = (RX + 30) * fl;
                    double bx = Math.Cos(a) * r0, by = EY + Math.Sin(a) * r0 * 1.15, tx = Math.Cos(a) * r1, ty = EY + Math.Sin(a) * r1 * 1.15 - 6;
                    foreach (var (col, k) in new[] { ("#e8322a", 1.0), ("#ff8a1a", .72), ("#ffd23a", .45) })
                    {
                        c.fillStyle = col; c.beginPath(); c.moveTo(bx - Math.Sin(a) * 13 * k, by + Math.Cos(a) * 13 * k);
                        c.quadraticCurveTo(bx + (tx - bx) * .5 - Math.Sin(a) * 10 * k, by + (ty - by) * .5 + Math.Cos(a) * 10 * k, bx + (tx - bx) * k, by + (ty - by) * k);
                        c.quadraticCurveTo(bx + (tx - bx) * .5 + Math.Sin(a) * 10 * k, by + (ty - by) * .5 - Math.Cos(a) * 10 * k, bx + Math.Sin(a) * 13 * k, by - Math.Cos(a) * 13 * k);
                        c.closePath(); c.fill();
                    }
                }
            c.fillStyle = "#a87430"; c.strokeStyle = INK; c.lineWidth = 3; c.beginPath(); c.ellipse(0, -20, 76, 24, 0, Math.PI, TAU); c.closePath(); c.fill(); c.stroke();
            c.lineWidth = 2; for (int i = 0; i < 26; i++) { double a = Math.PI + .15 + fr(i) * (Math.PI - .3), x = Math.Cos(a) * 68, y = -20 + Math.Sin(a) * 20; c.strokeStyle = new[] { "#e8c26a", "#c9973f", "#f2d27a" }[i % 3]; seg(c, x - 7, y + 2, x + 7, y - 3); }
            void egg() { c.beginPath(); c.ellipse(0, EY, RX, RY, 0, 0, TAU); }
            void paintEgg()
            {
                c.save(); egg(); c.clip();
                var g = c.createLinearGradient(-RX, EY - RY, RX, EY + RY); var pal = leg ? new[] { "#fff1c2", "#ff8a1a", "#b3261e" } : sup ? new[] { "#f6e6ff", "#b46cff", "#5a2aa8" } : new[] { "#fff3b0", "#ffc93a", "#d9861a" };
                g.addColorStop(0, pal[0]); g.addColorStop(.45, pal[1]); g.addColorStop(1, pal[2]); c.fillStyle = g; c.fillRect(-RX - 2, EY - RY - 2, RX * 2 + 4, RY * 2 + 4);
                c.fillStyle = leg ? "rgba(255,230,120,.5)" : sup ? "rgba(255,255,255,.3)" : "rgba(205,120,20,.45)";
                foreach (var (x, y, r) in new[] { (-15.0, -86.0, 5.0), (17.0, -70.0, 6.0), (-9.0, -42.0, 4.5), (21.0, -40.0, 3.5), (-24.0, -62.0, 3.2), (6.0, -96.0, 3.0) }) { c.beginPath(); c.arc(x, y, r, 0, TAU); c.fill(); }
                c.fillStyle = sup || leg ? "#ffc93a" : "#e8322a"; c.fillRect(-6, EY - RY, 12, RY * 2); c.fillStyle = "rgba(255,255,255,.3)"; c.fillRect(-6, EY - RY, 3, RY * 2);
                c.fillStyle = "rgba(255,255,255,.78)"; c.beginPath(); c.ellipse(-17, EY - 22, 6.5, 13, -.4, 0, TAU); c.fill();
                c.restore(); egg(); c.strokeStyle = INK; c.lineWidth = 3; c.stroke();
                c.lineWidth = 2; seg(c, -6, EY - RY + 1, -6, EY + RY - 1); seg(c, 6, EY - RY + 1, 6, EY + RY - 1);
            }
            void bow()
            {
                c.strokeStyle = INK; c.lineWidth = 2.4;
                if (sup || leg)
                {
                    double y = EY - RY + 4; c.fillStyle = leg ? "#ffd23a" : "#ffc93a"; c.beginPath(); c.moveTo(-15, y); c.lineTo(-17, y - 16); c.lineTo(-8, y - 8); c.lineTo(0, y - 20); c.lineTo(8, y - 8); c.lineTo(17, y - 16); c.lineTo(15, y); c.closePath(); c.fill(); c.stroke();
                    c.lineWidth = 1.4; circ(c, 0, y - 6, 2.6, leg ? "#ff8a1a" : "#e8322a"); circ(c, -9, y - 4, 1.9, leg ? "#e8322a" : "#4cc3ff"); circ(c, 9, y - 4, 1.9, leg ? "#e8322a" : "#4cc3ff"); return;
                }
                c.fillStyle = "#e8322a";
                c.beginPath(); c.ellipse(-11, EY - RY - 6, 11, 6.5, .35, 0, TAU); c.fill(); c.stroke(); c.beginPath(); c.ellipse(11, EY - RY - 6, 11, 6.5, -.35, 0, TAU); c.fill(); c.stroke();
                c.fillStyle = "#c0392b"; c.beginPath(); c.arc(0, EY - RY - 3, 5, 0, TAU); c.fill(); c.stroke();
            }
            var Zg = new List<(double x, double y)>(); for (int i = 0; i <= 8; i++) Zg.Add((-RX - 6 + i * (2 * RX + 12) / 8, EY - 2 + (i % 2 == 1 ? -7 : 6)));
            void zig(string close)
            {
                c.beginPath(); c.moveTo(Zg[0].x, Zg[0].y); foreach (var p in Zg) c.lineTo(p.x, p.y);
                if (close == "top") { c.lineTo(RX + 12, EY - RY - 40); c.lineTo(-RX - 12, EY - RY - 40); c.closePath(); }
                else if (close == "bot") { c.lineTo(RX + 12, EY + RY + 12); c.lineTo(-RX - 12, EY + RY + 12); c.closePath(); }
            }
            if (open <= 0)
            {
                paintEgg(); bow();
                if (crack > .35) { int n = (int)Math.Floor((crack - .35) / .65 * 8) + 1; c.strokeStyle = INK; c.lineWidth = 2.2; c.beginPath(); c.moveTo(Zg[0].x + 8, Zg[0].y); for (int i = 1; i <= Math.Min(8, n); i++) c.lineTo(Zg[i].x * .92, Zg[i].y); c.stroke(); }
            }
            else
            {
                c.save(); zig("bot"); c.clip(); paintEgg();
                c.globalAlpha = open; c.fillStyle = "#ffe066"; c.beginPath(); c.ellipse(0, EY + 2, RX * .82, 9, 0, 0, TAU); c.fill(); c.globalAlpha = 1; c.restore();
                c.save(); c.translate(-open * 20, -open * 60); c.rotate(-open * .55);
                c.save(); zig("top"); c.clip(); paintEgg(); c.restore(); bow(); c.restore();
            }
            c.fillStyle = "#c9973f"; c.strokeStyle = INK; c.lineWidth = 3; c.beginPath(); c.ellipse(0, -16, 78, 25, 0, 0, Math.PI); c.ellipse(0, -23, 60, 11, 0, Math.PI, 0, true); c.closePath(); c.fill(); c.stroke();
            c.lineWidth = 2.2;
            for (int i = 0; i < 34; i++) { double a = .12 + fr(i + 40) * (Math.PI - .24), x = Math.Cos(a) * 68, y = -17 + Math.Sin(a) * 18; c.strokeStyle = new[] { "#f2d27a", "#a87430", "#e8c26a" }[i % 3]; c.beginPath(); c.moveTo(x - 8, y - 2); c.quadraticCurveTo(x, y + 3, x + 8, y - 1); c.stroke(); }
            c.strokeStyle = "#e8c26a"; c.lineWidth = 2.2; seg(c, -74, -16, -88, -26); seg(c, 72, -14, 90, -22); seg(c, -60, 4, -70, 10);
            if (leg)
                for (int i = 0; i < 9; i++)
                { // искры поднимаются от огненного яйца
                    double q = (S.t * .55 + i * .113) % 1, x = Math.Sin(i * 2.3 + S.t) * (30 + i * 4), y = -40 - q * 110;
                    c.globalAlpha = 1 - q; c.fillStyle = i % 2 == 0 ? "#ffd23a" : "#ff8a1a"; c.beginPath(); c.arc(x, y, 2.4 + (1 - q) * 2, 0, TAU); c.fill();
                }
            c.globalAlpha = 1;
            if (sup)
                foreach (var (x, y, ph) in new[] { (-58.0, -92.0, 0.0), (56.0, -104.0, 1.7), (-44.0, -128.0, 3.1), (62.0, -52.0, 4.4), (-66.0, -46.0, 5.2) })
                {
                    double k = .5 + .5 * Math.Sin(S.t * 4 + ph), r = 4 + k * 5;
                    c.save(); c.translate(x, y); c.globalAlpha = .4 + k * .6; c.fillStyle = "#fff"; c.strokeStyle = "#b46cff"; c.lineWidth = 1.3; c.beginPath();
                    for (int i = 0; i < 8; i++) { double a = i * Math.PI / 4, q = i % 2 == 1 ? r * .3 : r; c.lineTo(Math.Cos(a) * q, Math.Sin(a) * q); }
                    c.closePath(); c.fill(); c.stroke(); c.restore();
                }
        }
        c.restore();
    }

    /* ---------- картинки для магазина ---------- */
    public static void drawEggPile(Ctx c, int n)
    {
        var pos = new List<(double x, double y)> { (0, 0), (-14, 2), (14, 2), (-7, -12), (7, -12), (-24, 6), (24, 6), (0, -24), (-20, -8), (20, -8), (-30, -2), (30, -2) };
        c.fillStyle = "#c9973f"; c.strokeStyle = INK; c.lineWidth = 2.5; c.beginPath(); c.ellipse(0, 14, 44, 12, 0, 0, Math.PI); c.ellipse(0, 8, 36, 7, 0, Math.PI, 0, true); c.closePath(); c.fill(); c.stroke();
        var list = pos.GetRange(0, Math.Min(n, pos.Count)); list.Sort((a, b) => a.y.CompareTo(b.y));
        foreach (var (x, y) in list)
        {
            var g = c.createLinearGradient(x - 8, y - 14, x + 8, y + 6); g.addColorStop(0, "#fff3b0"); g.addColorStop(.5, "#ffc93a"); g.addColorStop(1, "#d9861a");
            c.fillStyle = g; c.lineWidth = 2; c.beginPath(); c.ellipse(x, y, 8.5, 11, 0, 0, TAU); c.fill(); c.stroke(); c.fillStyle = "rgba(255,255,255,.75)"; c.beginPath(); c.ellipse(x - 3, y - 4, 2, 3.5, -.4, 0, TAU); c.fill();
        }
    }
    public static void drawSack(Ctx c, int k)
    {
        double s = .8 + k * .18; c.scale(s, s); c.strokeStyle = INK; c.lineWidth = 2.5;
        c.fillStyle = "#d9b77a"; c.beginPath(); c.moveTo(-22, 12); c.quadraticCurveTo(-30, -14, -12, -22); c.lineTo(12, -22); c.quadraticCurveTo(30, -14, 22, 12); c.quadraticCurveTo(0, 20, -22, 12); c.closePath(); c.fill(); c.stroke();
        c.fillStyle = "#b8925a"; c.beginPath(); c.moveTo(-12, -22); c.lineTo(-16, -30); c.lineTo(16, -30); c.lineTo(12, -22); c.closePath(); c.fill(); c.stroke();
        c.fillStyle = "#ffe066"; foreach (var (x, y) in new[] { (-8.0, -30.0), (0.0, -33.0), (8.0, -30.0), (4.0, -36.0), (-4.0, -35.0) }) { c.beginPath(); c.ellipse(x, y, 2.6, 1.8, .4, 0, TAU); c.fill(); }
        // цветок вместо символа ✿ — рисуем пятью лепестками, чтобы не зависеть от шрифта
        c.fillStyle = "#8a5a2b"; for (int i = 0; i < 5; i++) { var a = i * TAU / 5 - Math.PI / 2; c.beginPath(); c.arc(Math.Cos(a) * 3.2, -3 + Math.Sin(a) * 3.2, 2.4, 0, TAU); c.fill(); }
    }
    public static void drawMapPreview(Ctx c, string key, double w, double h)
    {
        var M = MAPS[key]; var R = seeded(M.seed); c.fillStyle = M.grass; c.fillRect(0, 0, w, h);
        for (int i = 0; i < 5; i++) { c.fillStyle = M.patch; c.beginPath(); c.ellipse(R() * w, R() * h, 10 + R() * 18, 6 + R() * 10, R() * 3, 0, TAU); c.fill(); }
        for (int i = 0; i < 40; i++) { c.fillStyle = M.tuft[i % 2]; c.fillRect(R() * w, R() * h, 2, 3); }
        bool vert = M.river.axis == 'v'; double Ln = vert ? h : w, bas = (vert ? w : h) * M.river.@base / WW;
        (double x, double y) pt(double t) { var v = bas + Math.Sin(t / Ln * TAU * 1.2 + M.river.phase) * (vert ? w : h) * .07; return vert ? (v, t) : (t, v); }
        void path() { c.beginPath(); for (double t = -4; t <= Ln + 4; t += 4) { var (x, y) = pt(t); if (t < 0) c.moveTo(x, y); else c.lineTo(x, y); } }
        var B = M.branch;
        void bpath()
        {
            if (B == null) return; bool bv = B.axis == 'v'; double bL = bv ? h : w, bb = (bv ? w : h) * B.@base / WW, j = bv ? pt(bb * w / w).y : pt(bb).x; c.beginPath();
            for (double t = 0; t <= 1.0001; t += .05)
            {
                double u = B.end == "hi" ? j + (bL + 4 - j) * t : j * (1 - t) - 4 * t, v = bb + Math.Sin(u / bL * TAU + B.phase) * (bv ? w : h) * .04;
                var (x, y) = bv ? (v, u) : (u, v); if (t > 0) c.lineTo(x, y); else c.moveTo(x, y);
            }
        }
        c.lineCap = "round";
        path(); c.strokeStyle = M.bank; c.lineWidth = 20; c.stroke(); bpath(); c.lineWidth = 16; c.stroke();
        bpath(); c.strokeStyle = M.water; c.lineWidth = 10; c.stroke(); path(); c.lineWidth = 13; c.stroke();
        bpath(); c.strokeStyle = M.deep; c.lineWidth = 3.5; c.stroke(); path(); c.lineWidth = 5; c.stroke();
        var (bx, by) = pt(Ln * .42); c.save(); c.translate(bx, by); if (!vert) c.rotate(Math.PI / 2); c.fillStyle = "#a8743d"; c.strokeStyle = INK; c.lineWidth = 1.5; rr(c, -14, -6, 28, 12, 2); c.fill(); c.stroke();
        c.strokeStyle = "rgba(43,29,20,.5)"; c.lineWidth = 1; for (int x = -10; x <= 10; x += 5) seg(c, x, -6, x, 6); c.restore();
        var spots = vert ? new[] { (w * .2, h * .3), (w * .38, h * .75), (w * .9, h * .2) } : new[] { (w * .2, h * .78), (w * .62, h * .72), (w * .86, h * .12) };
        for (int i = 0; i < spots.Length; i++) { var (x, y) = spots[i]; c.save(); c.translate(x, y); c.scale(.42, .42); drawObstacle(c, new Obs { x = 0, y = 0, r = 30, type = M.obs[i * 2 % M.obs.Length] }); c.restore(); }
    }
}
