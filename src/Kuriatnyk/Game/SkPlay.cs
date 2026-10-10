using System;
using System.Collections.Generic;
using System.Linq;
using static Kuriatnyk.D;
using static Kuriatnyk.Store;

namespace Kuriatnyk;

// Soul Knight: стрельба своим оружием, эффекты снарядов, лужи, предметы на земле, сундуки, торговец, пивчики и пассивки.
public static partial class G
{
    /* ---------- выстрел ---------- */
    static void skFire(Weap W, double ang)
    {
        double dmg = W.dmg * P.dmgMul, cx = Math.Cos(ang), sy = Math.Sin(ang);
        if (W.special == "beam") { fireBeam(P.x, P.y, ang, dmg, true, null, "rgba(255,60,60,.6)", .45, 1); return; }
        if (W.special == "chain") { chainBolt(P.x, P.y, ang, dmg, W.chain); return; }
        if (W.special == "lob")
        {
            double tx, ty; var t = nearestTarget();
            if (input.mouse && input.firing) { tx = camX + input.mx / SC; ty = camY + input.my / SC; }
            else if (t != null) { tx = t.x; ty = t.y; }
            else { tx = P.x + cx * 260; ty = P.y + sy * 260; }
            var d = hypot(tx - P.x, ty - P.y); if (d > 400) { tx = P.x + (tx - P.x) / d * 400; ty = P.y + (ty - P.y) / d * 400; }
            bombs.Add(new Bomb { x = tx, y = ty, t = .6, max = .6, r = W.boomR, dmg = dmg, fromP = true, lob = true, sx = P.x, sy = P.y, kind = W.id });
            return;
        }
        if (W.ring > 0)
        {
            for (int i = 0; i < W.ring; i++) { var a = ang + i * TAU / W.ring; skBullet(W, P.x + Math.Cos(a) * 18, P.y + Math.Sin(a) * 18, a, W.spd, dmg, i); }
            return;
        }
        for (int i = 0; i < W.pellets; i++)
        {
            double a = ang + (W.pellets > 1 ? (i - (W.pellets - 1) / 2.0) / Math.Max(1, W.pellets - 1) * W.spread + (rnd() - .5) * .1 : (rnd() - .5) * W.spread);
            if (W.pellets > 3) a = ang + (rnd() - .5) * W.spread; // дробь — случайным веером
            double sp = W.spd * (W.pellets > 3 || W.shape == "steam" ? rand(.85, 1.12) : 1);
            skBullet(W, P.x + cx * 22, P.y + sy * 22, a, sp, dmg, i);
        }
    }
    static Bullet skBullet(Weap W, double x, double y, double a, double sp, double dmg, int i)
    {
        double life = W.life * P.rangeMul * (W.pellets > 3 || W.shape == "steam" ? rand(.8, 1.1) : 1);
        var b = new Bullet
        {
            x = x, y = y, vx = Math.Cos(a) * sp, vy = Math.Sin(a) * sp, r = W.size > 0 ? W.size : W.pellets > 3 ? 3.6 : 5, dmg = dmg, fromP = true,
            life = life, max = life, kind = W.shape ?? W.id, pierce = W.pierce, hits = W.pierce > 0 ? new() : null, w = W, shape = i % 4, spin = rand(0, TAU)
        };
        bullets.Add(b); return b;
    }

    /// <summary>Каждый кадр: самонаведение, бумеранг, смерч.</summary>
    static void skBulletTick(Bullet b, double dt)
    {
        var W = b.w; b.spin += dt * 10; b.tick += dt;
        if (W.hom > 0)
        {
            Enemy best = null; double bd = 340 * 340;
            foreach (var e in enemies) { if (e.dead || e.emerge > 0) continue; double dx = e.x - b.x, dy = e.y - b.y, d2 = dx * dx + dy * dy; if (d2 < bd) { bd = d2; best = e; } }
            if (best != null)
            {
                double sp = hypot(b.vx, b.vy), a = Math.Atan2(b.vy, b.vx), want = Math.Atan2(best.y - b.y, best.x - b.x);
                double df = ((want - a + Math.PI * 3) % TAU) - Math.PI, step = W.hom * dt;
                a += Math.Abs(df) < step ? df : Math.Sign(df) * step;
                b.vx = Math.Cos(a) * sp; b.vy = Math.Sin(a) * sp;
            }
        }
        if (W.boomer)
        {
            if (!b.back && b.life < b.max * .5) { b.back = true; b.hits?.Clear(); }
            if (b.back)
            {
                double dx = P.x - b.x, dy = P.y - 10 - b.y, d = hypot(dx, dy), sp = Math.Max(500, hypot(b.vx, b.vy));
                if (d < 22 || b.tick > 3) b.life = 0; else { b.vx = dx / d * sp; b.vy = dy / d * sp; b.life = Math.Max(b.life, .05); }
            }
        }
        if (b.kind == "tornado")
        {
            foreach (var e in enemies)
            {
                if (e.dead || e.T.boss) continue; double dx = b.x - e.x, dy = b.y - e.y, d = hypot(dx, dy);
                if (d < 120 && d > 6) { e.kx += dx / d * 900 * dt; e.ky += dy / d * 900 * dt; }
            }
            if (b.tick > .35) { b.tick = 0; b.hits?.Clear(); } // кружит и бьёт снова
            if (rnd() < dt * 20) parts.Add(new Part { k = "puff", x = b.x + rand(-14, 14), y = b.y, z = rand(4, 34), vx = rand(-40, 40), vy = 0, vz = 50, t = 0, max = .35, r = rand(3, 6), col = "#e3f2f7" });
        }
        if (b.kind == "rocket" && rnd() < dt * 40) parts.Add(new Part { k = "puff", x = b.x, y = b.y, z = 19, vx = 0, vy = 0, vz = 10, t = 0, max = .3, r = rand(2, 4), col = rnd() < .5 ? "#ffd23a" : "#c9c4bb" });
    }

    /// <summary>Снаряд Soul Knight попал во врага.</summary>
    static void skHit(Bullet b, Enemy e)
    {
        var W = b.w;
        if (W.chill > 0)
        {
            e.chill = Math.Max(e.chill, W.chill); e.iceN = (e.iceT > 0 ? e.iceN : 0) + 1; e.iceT = 1.5;
            if (W.id == "snow" && e.iceN >= 4 && !e.T.boss) { e.iceN = 0; e.stun = Math.Max(e.stun, 1.2); e.frozen = 1.2; puff(e.x, e.y, "#cfefff", 6, 1); Sfx.ice(); }
        }
        if (W.honey > 0) e.honey = Math.Max(e.honey, W.honey);
        if (W.stun > 0 && !e.T.boss) { e.stun = Math.Max(e.stun, W.stun); e.bubble = W.stun; }
        if (W.burnT > 0) { e.burn = W.burnT; e.burnDps = 10 * P.dmgMul; }
        if (W.heal > 0 && P.alive && P.hp < P.max) { P.hp = Math.Min(P.max, P.hp + W.heal); healFx(P); }
        if (W.split > 0 && !b.frag)
            for (int q = 0; q < W.split; q++)
            {
                var an = Math.Atan2(b.vy, b.vx) + (q - 1) * .7 + rand(-.15, .15);
                bullets.Add(new Bullet { x = e.x, y = e.y, vx = Math.Cos(an) * 520, vy = Math.Sin(an) * 520, r = 3.4, dmg = b.dmg * .4, fromP = true, life = .35, max = .35, kind = "shard", frag = true, hits = new() { e }, w = W });
            }
    }
    /// <summary>Снаряд исчез (попал, врезался или долетел): взрыв или лужа, если оружие так умеет.</summary>
    static void skExpire(Bullet b)
    {
        var W = b.w; if (b.frag) return;
        if (W.boomR > 0) { blast(b.x, b.y, W.boomR, (W.boomDmg > 0 ? W.boomDmg : W.dmg) * P.dmgMul, true, false); if (W.burnT > 0) foreach (var e in enemies) if (!e.dead && hypot(e.x - b.x, e.y - b.y) < W.boomR) { e.burn = W.burnT; e.burnDps = 10 * P.dmgMul; } }
        if (W.zone != null) addGoo(W.zone, b.x, b.y, W.zoneR, W.zoneT, W.zoneDps * P.dmgMul);
    }

    /// <summary>Цепная молния: ближайший враг перед героем, потом перескоки на соседей.</summary>
    static void chainBolt(double x, double y, double ang, double dmg, int n)
    {
        var hit = new List<Enemy>(); Enemy cur = null; double bs = 1e9;
        foreach (var e in enemies)
        {
            if (e.dead || e.emerge > 0) continue; double dx = e.x - x, dy = e.y - y, d = hypot(dx, dy); if (d > 420) continue;
            var df = Math.Abs(((Math.Atan2(dy, dx) - ang + Math.PI * 3) % TAU) - Math.PI); double sc = d + df * 260;
            if (sc < bs && (e.T.fly || los(x, y, e.x, e.y))) { bs = sc; cur = e; }
        }
        double fx = x, fy = y - 19;
        if (cur == null) { parts.Add(new Part { k = "bolt", x = fx, y = fy, x2 = x + Math.Cos(ang) * 200, y2 = y + Math.Sin(ang) * 200 - 19, t = 0, max = .18, seed = rnd() }); Sfx.zap(); return; }
        for (int k = 0; k < n && cur != null; k++)
        {
            parts.Add(new Part { k = "bolt", x = fx, y = fy, x2 = cur.x, y2 = cur.y - 19, t = 0, max = .22, seed = rnd() });
            hitEnemy(cur, dmg * (1 - k * .08), cur.x - fx, cur.y - fy, true, .5); hit.Add(cur); puff(cur.x, cur.y, "#d9c4ff", 2, .6);
            fx = cur.x; fy = cur.y - 19; Enemy next = null; double nd = 175;
            foreach (var e in enemies) { if (e.dead || hit.Contains(e) || e.emerge > 0) continue; var d = hypot(e.x - cur.x, e.y - cur.y); if (d < nd) { nd = d; next = e; } }
            cur = next;
        }
        Sfx.zap();
    }

    /* ---------- лужи и облака ---------- */
    static void addGoo(string kind, double x, double y, double r, double t, double dps)
    {
        goo.Add(new Goo { kind = kind, x = x, y = y, r = r, t = t, max = t, dps = dps, held = kind == "roots" ? new() : null });
        if (kind == "roots") { puff(x, y, "#6db343", 8, 1); Sfx.chest(); }
        else if (kind == "poison") puff(x, y, "#9be05a", 8, 1.2);
        else splat(x, y, 1.2);
    }
    static void updateGoo(double dt)
    {
        for (int i = goo.Count - 1; i >= 0; i--)
        {
            var g = goo[i]; g.t -= dt; g.tick += dt;
            bool tick = g.tick >= .33; if (tick) g.tick = 0;
            foreach (var e in enemies)
            {
                if (e.dead || e.emerge > 0 || (e.T.fly && g.kind != "poison")) continue;
                if (hypot(e.x - g.x, (e.y - g.y) * 1.6) > g.r + e.r * .5) continue;
                if (g.kind == "poop") e.honey = Math.Max(e.honey, .3); // в луже вязнут
                if (g.kind == "roots" && !g.held.Contains(e)) { g.held.Add(e); if (!e.T.boss) e.stun = Math.Max(e.stun, 1.3); }
                if (tick) hitEnemy(e, g.dps * .33, 0, 0, true, 0, true);
            }
            if (g.kind == "poison" && rnd() < dt * 14) parts.Add(new Part { k = "puff", x = g.x + rand(-g.r, g.r) * .8, y = g.y + rand(-g.r, g.r) * .35, z = rand(2, 14), vx = 0, vy = 0, vz = 18, t = 0, max = .8, r = rand(5, 10), col = "rgba(140,220,80,.5)" });
            if (g.t <= 0) goo.RemoveAt(i);
        }
    }

    /* ---------- оружие на земле, торговец, сундук ---------- */
    /// <summary>Случайное оружие для сундука и торговца: чем глубже уровень и больше удачи, тем чаще редкое.</summary>
    public static string rollSkWeapon(int lv, int luck, IEnumerable<string> except = null)
    {
        var ex = new HashSet<string>(except ?? Array.Empty<string>()); foreach (var s in SKW.START) ex.Add(s);
        var w = new Dictionary<string, double>
        {
            ["common"] = Math.Max(4, 52 - 7 * (lv - 1) - 8 * luck), ["rare"] = 32 + 2 * (lv - 1),
            ["epic"] = 11 + 3 * (lv - 1) + 5 * luck, ["legendary"] = 3 + 1.5 * (lv - 1) + 3 * luck,
        };
        for (int tries = 0; tries < 20; tries++)
        {
            var r = pickFrom(w); var pool = SKW.LIST.Where(x => x.skr == r && !ex.Contains(x.id)).ToList();
            if (pool.Count > 0) return pool[(int)(rnd() * pool.Count)].id;
        }
        var all = SKW.LIST.Where(x => !ex.Contains(x.id)).ToList();
        return all.Count > 0 ? all[(int)(rnd() * all.Count)].id : "axe";
    }
    static IEnumerable<string> skTaken() => (P?.bag ?? new List<string>()).Concat(pickups.Where(p => p.kind == "skw").Select(p => p.wid));
    static Pickup dropWeapon(string id, double x, double y, int price = 0)
    {
        var p = new Pickup { x = x, y = y, kind = "skw", wid = id, t = 1e9, ph = rand(0, 6), price = price, vz = price > 0 ? 0 : 160 };
        pickups.Add(p); return p;
    }
    static void dropPiv(double x, double y, int n)
    {
        n = (int)Math.Round(n * (P?.pivMul ?? 1)); if (n <= 0) return;
        int coins = Math.Min(n, 8);
        for (int i = 0; i < coins; i++)
        {
            int v = n / coins + (i < n % coins ? 1 : 0); var a = rand(0, TAU); double sp = rand(40, 140);
            pickups.Add(new Pickup { x = x, y = y, kind = "piv", n = v, t = 40, ph = rand(0, 6), vx = Math.Cos(a) * sp, vy = Math.Sin(a) * sp * .7, vz = rand(120, 220) });
        }
    }
    /// <summary>Ближайший предмет, который берут кнопкой действия (оружие на земле, товар у торговца), автомат или статуя в святилище.</summary>
    public static Pickup actPick; public static Obs actVend, actStatue;
    static void findAct()
    {
        actPick = null; actVend = null; actStatue = null; if (S.kind != "sk" || P == null || !P.alive) return; double bd = 46;
        foreach (var p in pickups) { if (p.kind != "skw" && p.price <= 0) continue; var d = hypot(p.x - P.x, p.y - P.y); if (d < bd) { bd = d; actPick = p; } }
        if (actPick != null) return;
        foreach (var o in OBS) if (o.type == "vend" && !o.open && hypot(o.x - P.x, o.y + 30 - P.y) < 62) actVend = o;
        if (actVend != null) return;
        bd = 70; foreach (var o in OBS) { if (o.type != "statue" || o.st != 0) continue; var d = hypot(o.x - P.x, o.y + 34 - P.y); if (d < bd) { bd = d; actStatue = o; } }
    }
    static string itemName(Pickup p) => p.kind == "skw" ? WEAP[p.wid].name : p.kind == "corn" ? T("Кукуруза") : p.kind == "hpp" ? T("Зелье здоровья") : T("Зелье стамины");
    /// <summary>Подпись кнопки действия: «Взять: Топор», «Заменить на …», «Купить …», «Автомат: случайный предмет», «Поклониться: …».</summary>
    public static string actLabel()
    {
        if (actStatue != null) return T("Поклониться: ") + SKU.Name(actStatue.kind);
        if (actVend != null) return T("Автомат: случайный предмет");
        var p = actPick; if (p == null) return null;
        var nm = itemName(p);
        if (p.price > 0) return T("Купить: ") + nm;
        return (P.bag.Count < P.bagMax ? T("Взять: ") : T("Заменить на: ")) + nm;
    }
    /// <summary>Цена на кнопке действия (0 — бесплатно).</summary>
    public static int actPrice() => actVend != null ? vendPrice() : actPick?.price ?? 0;
    static int vendPrice() => SKW.Cost(SKW.VEND_PRICE, S.skLv);
    /// <summary>Автомат: один раз за уровень за пивчики выдаёт одно зелье наугад — здоровья или стамины.</summary>
    static void useVend(Obs o)
    {
        int price = vendPrice();
        if (S.piv < price) { ftext(P.x, P.y - 58, T("Не хватает пивчиков"), "#ffb3a3", 13); Sfx.hurt(); return; }
        S.piv -= price; o.open = true; Sfx.buy(); Sfx.chest(); puff(o.x, o.y - 20, "#ffe066", 8, 1);
        var p = new Pickup { x = o.x, y = o.y + 44, kind = rnd() < .5 ? "hpp" : "stamp", t = 1e9, ph = rand(0, 6), vz = 150 };
        pickups.Add(p);
        ftext(o.x, o.y - 70, itemName(p), p.kind == "hpp" ? "#ff8a7a" : "#7fd4ff", 15);
    }
    /// <summary>Поклон статуе: её ульта заменяет твою до конца забега, остальные статуи святилища гаснут.</summary>
    static void bowStatue(Obs o)
    {
        P.ultId = o.kind == P.hid ? null : o.kind; o.st = 1; P.bowT = .9;
        P.ang = Math.Atan2(o.y - P.y, o.x - P.x);
        foreach (var s in OBS) if (s.type == "statue" && s != o && s.st == 0 && hypot(s.x - o.x, s.y - o.y) < 400) s.st = 2;
        if (P.buff == null) P.ult = Math.Max(P.ult, 50); // благословение сразу заряжает ульту наполовину
        Sfx.ult(); Sfx.chest(); S.shake += 4; Platform.Buzz(30);
        ring(o.x, o.y, "#ffe066", 120, .6); ring(P.x, P.y, "#fff6c8", 80, .5); puff(o.x, o.y - 50, "#ffe066", 12, 1.2);
        showBanner(T("Благословение!"), SKU.Name(o.kind) + T(" — новая ульта"));
    }
    /// <summary>Ульта «Подкрепление»: на подмогу приходит случайный боец, через 20 с он уходит.</summary>
    static void summonAlly()
    {
        var pool = HEROES.Keys.Where(k => k != P.hid).ToList(); var id = pool[(int)(rnd() * pool.Count)];
        double a = P.ang + Math.PI + rand(-.6, .6);
        var al = newAlly(id, Math.Max(1, P.hlv), P.x + Math.Cos(a) * 50, P.y + Math.Sin(a) * 40);
        double k = 1 + .15 * (Math.Max(1, S.skLv) - 1); al.max = al.hp = Math.Round(al.max * k); al.dmgMul *= k; // глубже — крепче, иначе его сразу выбьют
        al.summon = true; al.summonT = SKU.SUMMON_T; al.ult = 60; al._px = P.x; al._py = P.y;
        collideWorld(al); allies.Add(al);
        ring(al.x, al.y, "#9cf27a", 90, .5); puff(al.x, al.y, "#ffffff", 12, 1.3); feathers(al.x, al.y, al.look.body, 8, 1);
        ftext(al.x, al.y - 62, HEROES[id].name.Split(' ')[0] + T(" на подмогу!"), "#9cf27a", 15);
    }
    public static void interact()
    {
        if (S.mode != "play" || P == null || !P.alive) return;
        findAct();
        if (actStatue != null) { bowStatue(actStatue); actStatue = null; return; }
        if (actVend != null) { useVend(actVend); actVend = null; return; }
        var p = actPick; if (p == null) return;
        if (p.price > 0)
        {
            if (S.piv < p.price) { ftext(P.x, P.y - 58, T("Не хватает пивчиков"), "#ffb3a3", 13); Sfx.hurt(); return; }
            S.piv -= p.price; p.price = 0; Sfx.buy(); puff(p.x, p.y, "#ffe066", 6, 1);
            if (p.kind != "skw") { pickups.Remove(p); collect(p); actPick = null; return; }
        }
        if (p.kind != "skw") return;
        string got = p.wid;
        if (P.bag.Count < P.bagMax) { P.bag.Add(got); pickups.Remove(p); P.slot = P.bag.Count - 1; }
        else { var old = P.bag[P.slot]; P.bag[P.slot] = got; p.wid = old; p.vz = 140; p.z = 0; } // старое оружие остаётся на земле
        P.w = P.prim = got; P.spin = 0; P.cd = Math.Max(P.cd, .15); actPick = null;
        ftext(P.x, P.y - 58, WEAP[got].name, RAR[WEAP[got].skr].c, 15); Sfx.pick();
    }
    /// <summary>Сундук в своей комнате: открывается, когда подходишь, внутри — случайное оружие и пивчики.</summary>
    static void openChest(Obs o)
    {
        o.open = true; Sfx.chest(); Sfx.win(); S.shake += 4; puff(o.x, o.y - 10, "#ffe066", 10, 1.2); ring(o.x, o.y, "#ffe066", 90, .5);
        var id = rollSkWeapon(S.skLv, P.luck + 1, skTaken()); dropWeapon(id, o.x, o.y + 46);
        dropPiv(o.x, o.y + 10, 3 + S.skLv);
        ftext(o.x, o.y - 50, WEAP[id].name, RAR[WEAP[id].skr].c, 16);
    }
    static void stockShop(Room R)
    {
        // два ствола (цена по силе ствола) и два зелья — стамины и здоровья; глубже — дороже
        var taken = new List<string>(P?.bag ?? new List<string>());
        for (int i = 0; i < 2; i++)
        {
            var id = rollSkWeapon(S.skLv, P?.luck ?? 0, taken); taken.Add(id);
            dropWeapon(id, R.cx - 120 + i * 80, R.cy + 44, SKW.Price(WEAP[id], S.skLv));
        }
        pickups.Add(new Pickup { x = R.cx + 40, y = R.cy + 44, kind = "stamp", t = 1e9, ph = rand(0, 6), price = SKW.Cost(SKW.STAM_PRICE, S.skLv) });
        pickups.Add(new Pickup { x = R.cx + 120, y = R.cy + 44, kind = "hpp", t = 1e9, ph = rand(0, 6), price = SKW.Cost(SKW.HEAL_PRICE, S.skLv) });
    }

    /// <summary>Подбор пивчиков: катятся, подпрыгивают и летят к герою.</summary>
    static void updateSkPickup(Pickup p, double dt)
    {
        if (p.vz != 0 || p.z > 0) { p.z += p.vz * dt; p.vz -= 600 * dt; if (p.z <= 0) { p.z = 0; p.vz = Math.Abs(p.vz) > 60 ? -p.vz * .4 : 0; } }
        p.x += p.vx * dt; p.y += p.vy * dt; p.vx *= Math.Pow(.05, dt); p.vy *= Math.Pow(.05, dt);
        if (p.kind is "piv" or "stam" && P.alive)
        {
            double dx = P.x - p.x, dy = P.y - p.y, d = hypot(dx, dy);
            double mr = p.kind == "piv" ? (DUN != null && DUN.active == null && !DUN.hub ? 900 : P.magnetFar ? 240 : 80) : 60; // зачистил комнату — всё летит к тебе
            if (d < mr && p.z <= 0 && p.t < 39.6) { double sp = 420 + (mr - d) * 2; p.x += dx / d * sp * dt; p.y += dy / d * sp * dt; }
        }
    }

    /* ---------- пассивки ---------- */
    public static List<string> skOffer;
    static List<string> rollPassives(int n)
    {
        var pool = SKP.LIST.Where(p => !(p.once && S.skPass.Contains(p.id))).Select(p => p.id).ToList();
        var res = new List<string>();
        while (res.Count < n && pool.Count > 0) { var i = (int)(rnd() * pool.Count); res.Add(pool[i]); pool.RemoveAt(i); }
        return res;
    }
    /// <summary>Игрок выбрал пассивку в портале — применяем и идём на следующий уровень.</summary>
    public static void skTakePassive(string id)
    {
        if (S.mode != "pick") return;
        S.skPass.Add(id); Sfx.buy();
        switch (id)
        {
            case "bag": P.bagMax++; break;
            case "hp": P.max += 25; P.hp = Math.Min(P.max, P.hp + 25); break;
            case "dmg": P.dmgMul *= 1.15; break;
            case "rate": P.rateMul *= 1.12; break;
            case "spd": P.spd *= 1.1; break;
            case "stam": P.stamMax += 30; P.stam += 30; break;
            case "sregen": P.stamGain *= 2; break;
            case "thrift": P.stamMul *= .75; break;
            case "armor": P.armor = 1 - (1 - P.armor) * .88; break;
            case "vamp": P.vamp += 2; break;
            case "magnet": P.magnetFar = true; P.pivMul += .3; break;
            case "crit": P.crit += .12; break;
            case "melee": P.meleeMul *= 1.4; break;
            case "regen": P.regen += 1.5; break;
            case "ult": P.ultRate *= 1.3; break;
            case "luck": P.luck++; break;
        }
        S.mode = "play"; skOffer = null; Music.Duck(1);
        enterLevel(S.skLv + 1);
    }
}
