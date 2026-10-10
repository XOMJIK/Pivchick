using System;
using Kuriatnyk.Render;

namespace Kuriatnyk;

// Атмосфера кадра: тени облаков по полю, летающая мелочь (пыльца, листья, снег, светлячки), свет и виньетка, искры от попаданий.
// Всё считается от времени и номера частицы — без объектов в памяти, поэтому не тормозит и одинаково у всех в сетевой игре.
public static partial class G
{
    /// <summary>Тени облаков, медленно ползущие по полю (только на улице и днём).</summary>
    static void drawCloudShadows(Ctx c)
    {
        if (DUN != null || S.night) return;
        double t = S.t + clock * .2;
        for (int i = 0; i < 4; i++)
        {
            double span = WW + 900, x = (fr(i * 7 + 3) * span + t * (14 + i * 3)) % span - 450, y = fr(i * 13 + 5) * WH, r = 260 + fr(i * 3 + 1) * 220;
            c.save(); c.translate(x, y); c.scale(1.6, .8);
            var g = c.createRadialGradient(0, 0, r * .2, 0, 0, r); g.addColorStop(0, "rgba(20,40,10,.10)"); g.addColorStop(1, "rgba(20,40,10,0)");
            c.fillStyle = g; c.beginPath(); c.arc(0, 0, r, 0, TAU); c.fill(); c.restore();
        }
    }

    /// <summary>Летающая мелочь в мировых координатах: плитка 520×520 повторяется по видимой области.</summary>
    static void drawAmbient(Ctx c, double vx0, double vy0, double vx1, double vy1)
    {
        if (REDUCED) return;
        string kind = S.night ? "fly" : DUN != null ? (DUN.hub ? "dust" : S.skLv > SK_WORLD ? "ember" : "spore") : MAP switch { "forest" => "leaf", "factory" => "soot", "mountain" => "snow", _ => "pollen" };
        const double TILE = 520; int per = kind is "snow" ? 16 : kind is "fly" ? 7 : kind == "leaf" ? 5 : 9;
        double t = clock;
        int tx0 = (int)Math.Floor(vx0 / TILE), tx1 = (int)Math.Floor(vx1 / TILE), ty0 = (int)Math.Floor(vy0 / TILE), ty1 = (int)Math.Floor(vy1 / TILE);
        c.save(); c.lineCap = "round";
        for (int ty = ty0; ty <= ty1; ty++)
            for (int tx = tx0; tx <= tx1; tx++)
                for (int i = 0; i < per; i++)
                {
                    double a = fr(i * 31 + 7), b = fr(i * 17 + 11), sp = .5 + fr(i * 5 + 2);
                    double fall = kind switch { "snow" => 26, "leaf" => 18, "ember" => -22, "soot" => -8, _ => 0 } * sp, drift = kind switch { "snow" => 10, "leaf" => 22, "pollen" => 9, _ => 5 } * sp;
                    double px = (a * TILE + t * drift + Math.Sin(t * (.6 + b) + i) * 18) % TILE, py = (b * TILE + t * fall + Math.Cos(t * (.5 + a) + i * 2) * 14) % TILE;
                    if (px < 0) px += TILE; if (py < 0) py += TILE;
                    double x = tx * TILE + px, y = ty * TILE + py; if (x < 0 || y < 0 || x > WW || y > WH) continue;
                    double tw = .5 + .5 * Math.Sin(t * (2 + b * 3) + i * 1.7);
                    switch (kind)
                    {
                        case "pollen": c.globalAlpha = .35 + tw * .45; c.fillStyle = i % 3 == 0 ? "#fff6c8" : "#ffffff"; c.beginPath(); c.arc(x, y, 1.6 + b, 0, TAU); c.fill(); break;
                        case "snow": c.globalAlpha = .55 + tw * .4; c.fillStyle = "#ffffff"; c.beginPath(); c.arc(x, y, 1.4 + b * 1.6, 0, TAU); c.fill(); break;
                        case "leaf":
                            c.globalAlpha = .85; c.save(); c.translate(x, y); c.rotate(t * (1 + b) + i); c.scale(1, .45 + .55 * Math.Abs(Math.Sin(t * 2 + i)));
                            c.fillStyle = i % 2 == 0 ? "#d98a2b" : "#7fb24a"; c.beginPath(); c.ellipse(0, 0, 5, 2.6, 0, 0, TAU); c.fill(); c.restore(); break;
                        case "soot": c.globalAlpha = .18 + tw * .12; c.fillStyle = "#6b6f78"; c.beginPath(); c.arc(x, y, 3 + b * 3, 0, TAU); c.fill(); break;
                        case "spore": c.globalAlpha = .25 + tw * .45; c.fillStyle = "#c9f2b0"; c.beginPath(); c.arc(x, y, 1.8, 0, TAU); c.fill(); break;
                        case "ember": c.globalAlpha = .3 + tw * .6; c.fillStyle = tw > .5 ? "#ffd23a" : "#ff7a1a"; c.beginPath(); c.arc(x, y, 1.6, 0, TAU); c.fill(); break;
                        case "dust": c.globalAlpha = .2 + tw * .3; c.fillStyle = "#fff4dc"; c.beginPath(); c.arc(x, y, 1.3, 0, TAU); c.fill(); break;
                        case "fly": // светлячки ночью
                            {
                                var g = c.createRadialGradient(x, y, 0, x, y, 9); g.addColorStop(0, "rgba(220,255,140," + Inv(.55 * tw) + ")"); g.addColorStop(1, "rgba(220,255,140,0)");
                                c.globalAlpha = 1; c.fillStyle = g; c.beginPath(); c.arc(x, y, 9, 0, TAU); c.fill();
                                c.fillStyle = "#f4ffc0"; c.globalAlpha = .4 + tw * .6; c.beginPath(); c.arc(x, y, 1.6, 0, TAU); c.fill(); break;
                            }
                    }
                }
        c.restore();
    }

    /// <summary>Свет: тёплый солнечный отсвет сверху и мягкая виньетка по краям экрана (в подземелье — гуще).</summary>
    static void drawLighting(Ctx c)
    {
        if (!S.night && DUN == null)
        {
            var sun = c.createLinearGradient(0, 0, VW * .6, VH * .7); sun.addColorStop(0, "rgba(255,236,170,.13)"); sun.addColorStop(1, "rgba(255,236,170,0)");
            c.fillStyle = sun; c.fillRect(0, 0, VW, VH);
        }
        double k = S.mode == "menu" ? .6 : 1, a = (DUN != null ? .5 : .26) * k;
        var v = c.createRadialGradient(VW / 2, VH / 2, Math.Min(VW, VH) * .42, VW / 2, VH / 2, Math.Max(VW, VH) * .78);
        v.addColorStop(0, "rgba(16,22,8,0)"); v.addColorStop(1, "rgba(16,22,8," + Inv(a) + ")");
        c.fillStyle = v; c.fillRect(0, 0, VW, VH);
    }

    /// <summary>Искры от попадания: короткие лучи в сторону удара.</summary>
    static void sparks(double x, double y, double z, double vx, double vy)
    {
        if (parts.Count > 460) return;
        parts.Add(new Part { k = "spark", x = x, y = y, z = z, a = Math.Atan2(vy, vx), t = 0, max = .16, seed = rnd() });
    }
    static void drawSpark(Ctx c, Part p)
    {
        double q = p.t / p.max, len = 6 + 10 * q; c.save(); c.translate(p.x, p.y - p.z); c.lineCap = "round"; c.globalAlpha = 1 - q;
        for (int i = 0; i < 5; i++)
        {
            double an = p.a + (i - 2) * .45 + (fr((int)(p.seed * 1000) + i) - .5) * .4, r0 = 3 + 5 * q;
            c.strokeStyle = i % 2 == 0 ? "#ffffff" : "#ffe066"; c.lineWidth = 2.4 * (1 - q) + .6;
            seg(c, Math.Cos(an) * r0, Math.Sin(an) * r0 * .8, Math.Cos(an) * (r0 + len), Math.Sin(an) * (r0 + len) * .8);
        }
        c.restore();
    }
}
