using System;
using System.Collections.Generic;
using System.Linq;
using static Kuriatnyk.D;
using static Kuriatnyk.Store;

namespace Kuriatnyk;

// Игровой цикл: поток уровня, игрок, ИИ врагов и боссов, пули (порт web/js/update.js).
public static partial class G
{
    public const double MENU_ZOOM = 1.55;
    /// <summary>Точка на экране, где в меню стоит герой (над табличкой с именем); задаёт интерфейс лобби.</summary>
    public static (double x, double y)? MenuAnchor;
    static (double x, double y) menuAnchor() => MenuAnchor ?? (VW / 2, VH * .45);

    public static void update(double dt)
    {
        S.t += dt;
        if (S.mode == "play" || S.mode == "dying" || S.mode == "won")
        {
            if (S.mode == "play") { updatePlayer(dt); updateBuff(dt); updateAllies(dt); updateZones(dt); }
            if (S.mode != "won") updateEnemies(dt);
            updateBullets(dt); updateNades(dt); updateBombs(dt);
            if (S.cleanup) { S.cleanup = false; enemies = enemies.Where(e => !e.dead).ToList(); bullets = bullets.Where(b => b.fromP).ToList(); bombs = bombs.Where(b => b.fromP).ToList(); }
            if (S.mode == "play") updateFlow(dt);
            if (S.mode == "dying") { S.deadT -= dt; if (S.deadT <= 0) gameOver(); }
            if (S.mode == "won")
            {
                S.winT -= dt;
                if (rnd() < dt * 20) feathers(P.x + rand(-200, 200), P.y + rand(-150, 100), new[] { "#ffc93a", "#fff", "#e8322a", "#7fd4ff" }[(int)Math.Floor(rnd() * 4)], 1, .4);
                if (S.winT <= 0) showWin();
            }
        }
        if (fish.Count > 0) updateFish(dt);
        foreach (var o in OBS) if (o.hit > 0) o.hit -= dt;
        updateParts(dt);
        S.shake *= Math.Pow(.004, dt); if (S.shake < .1) S.shake = 0;
        if (S.mode == "menu")
        { // showcase: hero stands on the lobby stage
            double Z = SC * MENU_ZOOM; var a = menuAnchor();
            camX = P.x - a.x / Z; camY = P.y - a.y / Z; return;
        }
        double vw = VW / SC, vh = VH / SC, tx = P.x - vw / 2, ty = P.y - 28 - vh / 2;
        double k = Math.Min(1, dt * 7);
        camX += (tx - camX) * k; camY += (ty - camY) * k;
        camX = vw < WW + 80 ? clamp(camX, -40, WW - vw + 40) : (WW - vw) / 2;
        camY = vh < WH + 80 ? clamp(camY, -60, WH - vh + 40) : (WH - vh) / 2;
    }

    static void updateFlow(double dt)
    {
        if (S.kind == "sk") updateSK(dt);
        else
        {
            updateHoles(dt);
            if (S.kind == "story" && S.goalDone && S.t >= STORY_MIN) { victory(); return; } // цель выполнена и три минуты прошли
            if (isWaves())
            {
                if (S.bossT > 0) { S.bossT -= dt; if (S.bossT <= 0) spawnEndlessBoss(); }
                if (S.between > 0) { S.between -= dt; if (S.between <= 0) startWave(); }
                else
                {
                    S.spawnT -= dt;
                    if (S.spawnT <= 0 && S.toSpawn > 0 && enemies.Count(e => e.type != "mole") < Math.Min(6 + S.diff, 16)) { spawnEnemy(pickType()); S.toSpawn--; S.spawnT = Math.Max(.35, 1.3 - S.diff * .08); }
                    if (S.toSpawn == 0 && enemies.Count == 0 && S.wave > 0 && !(S.bossT > 0))
                    {
                        if (S.kind == "story" && S.wave >= S.L.goal.n) { storyGoal(); if (S.mode != "play") return; } // раньше трёх минут — следующая волна
                        S.between = 3; var h = Math.Min(10, P.max - P.hp); P.hp += h;
                        bool en = S.kind == "endless"; int bonus = en ? 10 + S.wave * 5 : 5 + S.wave * 3; S.coins += bonus; S.tokens += en ? 5 : 3;
                        if (en && S.wave % 5 == 0) { SAVE.btokens++; persist(); Later(.9, () => Ui.Toast(T("+1 большой жетон за 5 волн"))); }
                        showBanner(T("Двор чист!"), "+" + bonus + (en ? T(" зёрен · +5 жетонов") : T(" зёрен · +3 жетона")) + (h > 0 ? " · +" + Math.Round(h, MidpointRounding.AwayFromZero) + T(" здоровья") : "")); Sfx.pick();
                    }
                }
            }
            else
            {
                var g = S.L.goal;
                if (S.between > 0) { S.between -= dt; if (S.between <= 0) { S.running = true; showBanner(S.L.name, goalLabel(true)); Sfx.wave(); if (g.type == "boss") S.bossT = BOSS_AT; } }
                else if (S.running)
                {
                    if (S.bossT > 0) { S.bossT -= dt; if (S.bossT <= 0) spawnBoss(g.boss); } // сначала армия, потом босс
                    S.spawnT -= dt;
                    bool bossOn = g.type == "boss" && S.boss != null;
                    double cap = bossOn ? Math.Min(3 + Math.Floor(S.diff / 2), 7) : Math.Min(Math.Round(5 + S.diff, MidpointRounding.AwayFromZero), 15);
                    int alive = enemies.Count(e => !e.T.boss && e.type != "mole");
                    if (S.spawnT <= 0 && alive < cap) { spawnEnemy(pickType()); S.spawnT = Math.Max(.45, (bossOn ? 2.6 : 1.5) - S.diff * .08); }
                    if (g.type == "survive") { S.survT -= dt; if (S.survT <= 0) { S.survT = 0; storyGoal(); return; } }
                }
            }
        }
        if (S.mode != "play") return;
        if (S.comboT > 0) { S.comboT -= dt; if (S.comboT <= 0) S.combo = 0; }
        for (int i = pickups.Count - 1; i >= 0; i--)
        {
            var p = pickups[i]; p.t -= dt; p.ph += dt;
            if (S.kind == "sk") updateSkPickup(p, dt);
            if (p.kind == "skw" || p.price > 0) continue; // оружие и товары — кнопкой действия
            if (p.z <= 0 && hypot(p.x - P.x, p.y - P.y) < P.r + P.magnet) { collect(p); pickups.RemoveAt(i); continue; }
            if (p.t <= 0) pickups.RemoveAt(i);
        }
    }

    static double regenTick;
    static void updatePlayer(double dt)
    {
        double mx = 0, my = 0;
        if (sticks.move != null) { var v = stickVal(sticks.move); mx = v.x; my = v.y; }
        if (held("left")) mx -= 1; if (held("right")) mx += 1;
        if (held("up")) my -= 1; if (held("down")) my += 1;
        double ml = hypot(mx, my); if (ml > 1) { mx /= ml; my /= ml; ml = 1; }
        if (ml < .12) { mx = my = 0; ml = 0; }
        P._px = P.x; P._py = P.y;
        var PW = WEAP[P.w]; bool spinGun = PW.special == "mini" || PW.spinUp;
        double ox = P.x, oy = P.y, spd = P.spd * (P.buff == "turbo" ? 2 : P.buff == "adren" ? 1.4 : 1) * (spinGun && P.wasFiring ? .72 : 1) * (P.hop && inWater(P.x, P.y) ? .65 : 1); // утка плывёт медленнее
        P.x += mx * spd * dt; P.y += my * spd * dt; P.moving = ml > 0; if (P.moving) P.phase += dt * 15 * Math.Max(.5, ml);
        bool firing = false;
        if (sticks.aim != null)
        {
            var a = stickVal(sticks.aim); var m = hypot(a.x, a.y);
            if (m > .2) { P.ang = Math.Atan2(a.y, a.x); firing = m > .38; }
            else { var t = nearestTarget(); if (t != null) { P.ang = Math.Atan2(t.y - P.y, t.x - P.x); firing = true; } }
        }
        else if (input.mouse && input.firing) { double wx = camX + input.mx / SC, wy = camY + input.my / SC; P.ang = Math.Atan2(wy - (P.y - 19), wx - P.x); firing = true; }
        else if (pcAuto())
        {
            var t = nearestTarget();
            if (t != null) { P.ang = Math.Atan2(t.y - P.y, t.x - P.x); firing = true; }
            else if (P.moving) P.ang = Math.Atan2(my, mx);
            else if (input.mouse) { double wx = camX + input.mx / SC, wy = camY + input.my / SC; P.ang = Math.Atan2(wy - (P.y - 19), wx - P.x); }
        }
        else if (input.mouse) { double wx = camX + input.mx / SC, wy = camY + input.my / SC; P.ang = Math.Atan2(wy - (P.y - 19), wx - P.x); }
        else if (P.moving) P.ang = Math.Atan2(my, mx);
        if (firing && PW.special == "melee" && !(input.mouse && input.firing) && !(sticks.aim != null && hypot(stickVal(sticks.aim).x, stickVal(sticks.aim).y) > .38))
        { // ближним оружием сам машет только когда враг рядом, а не по всем на экране
            var t = nearestTarget();
            if (t == null || hypot(t.x - P.x, t.y - P.y) - t.r > (PW.meleeR > 0 ? PW.meleeR : MELEE_R) + 20) firing = false; else P.ang = Math.Atan2(t.y - P.y, t.x - P.x);
        }
        if (P.noStamT > 0) P.noStamT -= dt;
        P.stamT += dt;
        if (P.stam < P.stamMax && (S.kind != "sk" || P.stamT > SK_STAM_PAUSE)) P.stam = Math.Min(P.stamMax, P.stam + P.stamRegen * dt);
        if (P.swing > 0) P.swing -= dt;
        P.face = Math.Cos(P.ang) >= 0 ? 1 : -1;
        foreach (var e in enemies) { if (e.T.fly) continue; double dx = P.x - e.x, dy = P.y - e.y, d = hypot(dx, dy); if (d == 0) d = 1; double m = P.r + e.r; if (d < m) { P.x += dx / d * (m - d) * .5; P.y += dy / d * (m - d) * .5; } }
        collideWorld(P);
        P.vx = (P.x - ox) / Math.Max(dt, .001); P.vy = (P.y - oy) / Math.Max(dt, .001);
        { var k = Math.Min(1, dt * 3); P.svx += (P.vx - P.svx) * k; P.svy += (P.vy - P.svy) * k; }
        P.cd -= dt; if (P.inv > 0) P.inv -= dt; if (P.flash > 0) P.flash -= dt;
        if (P.medic) { if (P.hp < P.max) { P.hp = Math.Min(P.max, P.hp + 1.2 * dt * P.medMul); healFx(P); } healTeam(P.x, P.y, 2.5 * dt * P.medMul, 240, false); }
        if (P.regen > 0 && S.t - P.lastHit > 4 && P.hp < P.max)
        {
            P.hp = Math.Min(P.max, P.hp + P.regen * dt); regenTick += dt;
            if (regenTick > .5) { regenTick = 0; if (rnd() < .5) parts.Add(new Part { k = "puff", x = P.x + rand(-10, 10), y = P.y, z = rand(20, 40), vx = 0, vy = 0, vz = 30, t = 0, max = .5, r = 3, col = "#9cf27a" }); }
        }
        if (firing && P.cd <= 0) shoot();
        if (!firing || !spinGun) P.spin = Math.Max(0, P.spin - dt * 1.4);
        P.wasFiring = firing;
        if (!P.hop) waterBlock(P, P._px, P._py);
        updateBridges(dt);
        if (P.moving && rnd() < dt * 8) puff(P.x - mx * 8, P.y, "#e9dcc0", 1, .5);
    }

    static void bite(Enemy e, double dmg, double cd) { if (e.bite > 0) return; e.bite = cd; hitTarget(curTg, eDmgE(e, dmg), e); }
    static void startDash(Enemy e, double tell, double dur) { var tg = curTg ?? P; e.st = "tell"; e.tellT = tell; double dx = tg.x - e.x, dy = tg.y - e.y, d = hypot(dx, dy); if (d == 0) d = 1; e.dx = dx / d; e.dy = dy / d; e.dashLen = dur; e.hitThis = false; }
    static void startAim(Enemy e, double t) { var tg = curTg ?? P; e.st = "aim"; e.tellT = t; double dx = tg.x - e.x, dy = tg.y - e.y, d = hypot(dx, dy); if (d == 0) d = 1; e.dx = dx / d; e.dy = dy / d; }

    static void updateEnemies(double dt)
    {
        bool alive = P.alive;
        bool gooseSlow = P.hid == "goose" && hasP(5) && alive;
        for (int ei = 0; ei < enemies.Count; ei++) // по индексу: босс может призвать новых врагов прямо в цикле (как в JS)
        {
            var e = enemies[ei];
            if (e.dead) continue;
            e._px = e.x; e._py = e.y;
            var T = e.T;
            double rk = e.buff == "rate" ? 1.7 : 1; // бафф скорострельности: перезарядка идёт быстрее
            Unit tg = P;
            if (alive && allies.Count > 0)
            {
                e.tgT -= dt;
                if (e.tgT <= 0)
                {
                    e.tgT = .6; Ally best = null; double bd = hypot(P.x - e.x, P.y - e.y) * .5;
                    foreach (var a in allies) { if (a.down > 0) continue; var da = hypot(a.x - e.x, a.y - e.y); if (da < bd) { bd = da; best = a; } }
                    e.tgt = best;
                }
                if (e.tgt is Ally ta && ta.down <= 0) tg = ta; else e.tgt = null;
            }
            if (tg == P && P.buff == "shadow" && !T.boss)
            { // Перепёлка в тени: враг её не видит и бродит
                e.cd = Math.Max(e.cd, .5); e.bite = Math.Max(e.bite, .3); if (e.kami) e.kamiT = Math.Max(e.kamiT, .5);
                tg = new Unit { x = e.x + Math.Cos(e.phase * .25) * 160, y = e.y + Math.Sin(e.phase * .25) * 160, r = 1 };
            }
            curTg = tg;
            if (e.iceT > 0) e.iceT -= dt; if (e.frozen > 0) e.frozen -= dt;
            double dx = tg.x - e.x, dy = tg.y - e.y, d = hypot(dx, dy); if (d == 0) d = 1;
            if (e.bite > 0) e.bite -= dt * rk;
            if (e.flash > 0) e.flash -= dt;
            if (e.burn > 0)
            {
                e.burn -= dt; e.burnTick -= dt;
                if (rnd() < dt * 14) parts.Add(new Part { k = "puff", x = e.x + rand(-8, 8), y = e.y, z = rand(10, 30) * (e.s > 0 ? e.s : 1), vx = 0, vy = 0, vz = 50, t = 0, max = .35, r = rand(2, 4), col = rnd() < .5 ? "#ff7a1a" : "#ffd23a" });
                if (e.burnTick <= 0) { e.burnTick = .33; hitEnemy(e, e.burnDps * .33, 0, 0, true, 0, true); if (e.dead) continue; }
            }
            double spdMul = 1; bool noSep = false;
            if (e.chill > 0) { e.chill -= dt; spdMul *= .5; }
            if (e.honey > 0) { e.honey -= dt; spdMul *= .4; }
            if (e.bubble > 0) e.bubble -= dt;
            foreach (var pd in puddles) if (!T.fly && hypot(e.x - pd.x, (e.y - pd.y) * 1.6) < pd.r) { spdMul *= .45; break; }
            if (e.emerge > 0) { e.emerge -= dt; e.moving = false; continue; }
            if (e.stun > 0)
            {
                e.stun -= dt; e.moving = false; e.x += e.kx * dt; e.y += e.ky * dt; e.kx *= Math.Pow(.002, dt); e.ky *= Math.Pow(.002, dt); collideWorld(e, T.fly); continue;
            }
            double vx = 0, vy = 0;
            bool see = alive && (T.fly || los(e.x, e.y, tg.x, tg.y));
            if (!alive) { vx = -dx / d * .4; vy = -dy / d * .4; }
            else if (e.kami)
            { // стриж-камикадзе: летит быстро, но поворачивает плавно — в последний момент можно отскочить в сторону
                e.kamiT -= dt;
                double want = Math.Atan2(dy, dx), df = ((want - e.ang + Math.PI * 3) % TAU) - Math.PI, turn = (d < 120 ? 1.7 : 2.8) * dt;
                e.ang += Math.Abs(df) < turn ? df : Math.Sign(df) * turn;
                double sp = T.spd * (1 + (S.diff - 1) * .015) * spdMul;
                e.x += (Math.Cos(e.ang) * sp + e.kx) * dt; e.y += (Math.Sin(e.ang) * sp + e.ky) * dt; e.kx *= Math.Pow(.002, dt); e.ky *= Math.Pow(.002, dt);
                e.phase += dt * 26; e.moving = true; e.face = Math.Cos(e.ang) >= 0 ? 1 : -1;
                if (rnd() < dt * 25) parts.Add(new Part { k = "puff", x = e.x - Math.Cos(e.ang) * 12, y = e.y, z = rand(34, 44) * (e.s > 0 ? e.s : 1), vx = rand(-20, 20), vy = 0, vz = 40, t = 0, max = .25, r = 2, col = rnd() < .5 ? "#ffd23a" : "#ff7a1a" });
                double fq = e.kamiT < 1.2 ? 8 : 4;
                if (Math.Floor(e.kamiT * fq) != Math.Floor((e.kamiT + dt) * fq) && onScreen(e, 0)) Sfx.beep();
                if (d < e.r + tg.r + 8 || e.kamiT <= 0) { e.dead = true; kamiBoom(e, true); killEnemy(e, true); continue; }
                collideWorld(e, true); continue;
            }
            else if (e.st == "tell") { e.tellT -= dt * (e.chill > 0 ? .6 : 1); spdMul = 0; if (e.tellT <= 0) { e.st = "dash"; e.dashT = e.dashLen; } }
            else if (e.st == "aim")
            {
                spdMul = 0; e.tellT -= dt;
                if (e.tellT > .18) { e.dx = dx / d; e.dy = dy / d; }
                if (e.tellT <= 0)
                {
                    e.st = "walk"; var a = Math.Atan2(e.dy, e.dx); eBullet(e.x + e.dx * 20, e.y + e.dy * 20, a, T.bspd, eDmgE(e, T.dmg), T.bul); e.ang = a; e.cd = rand(T.cd[0], T.cd[1]);
                    if (e.type == "owl") Sfx.hoot(); else Sfx.zap();
                }
            }
            else if (e.st == "dash")
            {
                noSep = true; double sp = (T.dash > 0 ? T.dash : 520) * (e.chill > 0 ? .6 : 1) * (e.buff == "spd" ? 1.2 : 1);
                e.x += e.dx * sp * dt; e.y += e.dy * sp * dt; e.dashT -= dt; e.phase += dt * 24; e.moving = true;
                if (!T.fly && rnd() < dt * 30) puff(e.x, e.y, "#e9dcc0", 1, .7);
                if (!e.hitThis && d < e.r + tg.r + 6) { e.hitThis = true; hitTarget(tg, eDmgE(e, T.melee > 0 ? T.melee : 15), e); }
                if (e.dashT <= 0) { e.st = "walk"; e.cd = T.cd != null ? rand(T.cd[0], T.cd[1]) : rand(2, 3); if (T.boss && e.dashes > 0) { e.dashes--; startDash(e, .45, .55); } }
                collideWorld(e, T.fly); e.face = e.dx >= 0 ? 1 : -1; continue;
            }
            else if (e.type is "fox" or "rat" or "wolf" or "bigrat")
            {
                vx = dx / d; vy = dy / d; var z = Math.Sin(S.t * 5 + e.phase) * (e.type is "rat" or "bigrat" ? .25 : .45); vx += -dy / d * z; vy += dx / d * z;
                if (e.type == "wolf") { e.cd -= dt * rk; if (d < 170 && d > 50 && e.cd <= 0 && see) startDash(e, .32, .3); }
                if (e.type == "bigrat") { e.cd -= dt * rk; if (d < 160 && d > 40 && e.cd <= 0 && see) { startDash(e, .22, .22); Sfx.squeak(); } } // крыса прыгает и кусает
                if (d < e.r + tg.r + 5) bite(e, T.melee, e.type == "rat" ? .6 : e.type == "bigrat" ? .5 : .75);
            }
            else if (e.type is "hen" or "ferret" or "robohen" or "owl" or "mole")
            {
                if (e.type == "mole" && d < e.r + tg.r + 5) bite(e, T.melee, .8);
                double keep = (enemies.Count <= 3 && S.toSpawn == 0) || (S.night && d > NIGHT_R) ? Math.Min(T.keep, S.night ? NIGHT_R - 60 : 150) : T.keep; // stragglers come to you
                if (!see || d > keep + 50) { vx = dx / d; vy = dy / d; } else if (d < keep - 60) { vx = -dx / d; vy = -dy / d; }
                vx += -dy / d * e.strafe * .75; vy += dx / d * e.strafe * .75;
                e.strafeT -= dt; if (e.strafeT <= 0) { e.strafe *= -1; e.strafeT = rand(1, 2.8); }
                if (e.burst > 0) { e.burstT -= dt * rk; if (e.burstT <= 0) { e.burstT = .12; e.burst--; enemyShoot(e, T, 1, 0, T.bul); } }
                e.cd -= dt * rk;
                if (e.cd <= 0)
                {
                    if (see && d < T.range && onScreen(e))
                    {
                        if (e.type is "hen" or "mole") { enemyShoot(e, T, 1); e.cd = rand(T.cd[0], T.cd[1]) * Math.Max(.6, 1 - (S.diff - 1) * .04); }
                        else if (e.type == "ferret") { e.burst = 3; e.burstT = 0; e.cd = rand(T.cd[0], T.cd[1]); }
                        else { startAim(e, e.type == "owl" ? .75 : .55); e.cd = 9; }
                    }
                    else e.cd = .3;
                }
            }
            else if (e.type == "turkey")
            {
                if (d > 110 || !see) { vx = dx / d; vy = dy / d; } // не видит цель из-за укрытия — обходит, а не стоит
                if (d < e.r + tg.r + 5) bite(e, T.melee, .9);
                e.cd -= dt * rk; if (e.cd <= 0) { if (see && d < T.range && onScreen(e)) { enemyShoot(e, T, 3); e.cd = rand(T.cd[0], T.cd[1]); } else e.cd = .4; }
            }
            else if (e.type is "crow" or "eagle")
            {
                double want = e.type == "eagle" ? 210 : 175; vx = dx / d * (d - want) / 55 + (-dy / d) * e.strafe; vy = dy / d * (d - want) / 55 + (dx / d) * e.strafe;
                e.strafeT -= dt; if (e.strafeT <= 0) { e.strafe *= -1; e.strafeT = rand(2, 4); }
                e.cd -= dt * rk;
                if (e.cd <= 0)
                {
                    if (!onScreen(e, 0)) e.cd = .4;
                    else if (e.type == "crow") { dropBombs(T, 1.1, 58, eDmgE(e, T.dmg)); Sfx.caw(); e.cd = rand(T.cd[0], T.cd[1]) * Math.Max(.6, 1 - (S.diff - 1) * .03); }
                    else { startDash(e, .55, .5); Sfx.caw(); }
                }
                noSep = true;
            }
            else if (e.type == "raccoon")
            {
                vx = dx / d; vy = dy / d; e.cd -= dt * rk;
                if (d < 240 && e.cd <= 0 && see) startDash(e, .6, .42);
                if (d < e.r + tg.r + 5) bite(e, 10, .8);
            }
            else if (T.boss)
            {
                double want = T.fly ? 220 : (e.type is "gturkey" or "steelturkey") ? 170 : 200;
                if (d > want + 40) { vx = dx / d; vy = dy / d; } else if (d < want - 60) { vx = -dx / d * .6; vy = -dy / d * .6; }
                vx += -dy / d * e.strafe * .5; vy += dx / d * e.strafe * .5; e.strafeT -= dt; if (e.strafeT <= 0) { e.strafe *= -1; e.strafeT = rand(2, 4); }
                if (d < e.r + tg.r + 5) bite(e, T.melee, 1);
                bossAI(e, T, dt, d);
                if (T.fly) noSep = true;
            }
            bool straggle = enemies.Count <= 3 && S.toSpawn == 0;
            if (!T.fly && !e.hop && alive && RIVER != null && e.st != "dash" && e.st != "tell" && e.st != "aim" && (straggle || !(T.bul != null && !T.boss && d < (T.range > 0 ? T.range : 400) * .85)))
            {
                var nt = navTarget(e, tg.x, tg.y, e.navAlt);
                if (nt != null)
                {
                    double nx = nt.Value.x - e.x, ny = nt.Value.y - e.y, nd = hypot(nx, ny); if (nd == 0) nd = 1; vx = nx / nd; vy = ny / nd;
                    // защита от застревания: другой мост, а потом обход на нужный берег
                    e.navT += dt; if (e.navBest == null || d < e.navBest - 25) { e.navBest = d; e.navT = 0; }
                    if (e.navT > 7)
                    {
                        e.navT = 0; e.navBest = null; e.navAlt++;
                        if (e.navAlt > 2) { var p = DUN != null ? null : sidePoint(regionOf(tg.x, tg.y)); if (p != null) { puff(e.x, e.y, "#e9dcc0", 6, 1); e.x = p.Value.x; e.y = p.Value.y; e._px = e.x; e._py = e.y; } e.navAlt = 0; } // в подземелье враг заперт в комнате — не переносим
                    }
                }
                else { e.navT = 0; e.navBest = null; }
            }
            if (gooseSlow && d < 110) spdMul *= .7;
            if (!T.fly) foreach (var o in OBS) { double ox = e.x - o.x, oy = e.y - o.y, od = hypot(ox, oy); if (od < o.r + e.r + 26) { vx += ox / od * .9; vy += oy / od * .9; } }
            curTg = null;
            if (!noSep) foreach (var f in enemies) { if (f == e || f.dead || f.T.fly) continue; double fx = e.x - f.x, fy = e.y - f.y, fd = hypot(fx, fy); if (fd == 0) fd = 1; if (fd < e.r + f.r + 6) { vx += fx / fd * .8; vy += fy / fd * .8; } }
            double l = hypot(vx, vy); double spd = T.spd * (1 + (S.diff - 1) * .025) * spdMul * (e.buff == "spd" ? 1.4 : 1); // бафф скорости
            if (l > .05 && spd > 0)
            {
                double m = Math.Min(1, l); vx = vx / l * spd * (T.fly ? Math.Max(.5, m) : 1); vy = vy / l * spd * (T.fly ? Math.Max(.5, m) : 1); e.moving = true;
                e.phase += dt * (e.type is "fox" or "rat" or "wolf" or "bigrat" ? 18 : 13);
            }
            else { vx = vy = 0; e.moving = false; }
            if (T.fly) e.phase += dt * 10;
            e.x += (vx + e.kx) * dt; e.y += (vy + e.ky) * dt; e.kx *= Math.Pow(.002, dt); e.ky *= Math.Pow(.002, dt);
            if (!T.fly && alive) { double ox = e.x - tg.x, oy = e.y - tg.y, od = hypot(ox, oy); if (od == 0) od = 1; double m = e.r + tg.r - 2; if (od < m) { e.x = tg.x + ox / od * m; e.y = tg.y + oy / od * m; } } // не залезать внутрь бойца
            if (e.st != "tell" && e.st != "aim") e.face = dx >= 0 ? 1 : -1; else if (e.st == "aim") e.face = e.dx >= 0 ? 1 : -1;
            if (e.type != "fox" && e.st != "aim" && e.cd > .25) e.ang = Math.Atan2(dy, dx);
            if (e.st == "aim") e.ang = Math.Atan2(e.dy, e.dx);
            collideWorld(e, T.fly);
        }
        enemies.RemoveAll(x => x.dead);
        curTg = null;
    }
    static (double x, double y) predictP(double t) // куда игрок придёт через t секунд: сглаженная скорость, не дальше 150, только по суше
    {
        double px = P.svx * t * .8, py = P.svy * t * .8; var l = hypot(px, py); if (l > 150) { px *= 150 / l; py *= 150 / l; }
        double x = clamp(P.x + px, FENCE + 10, WW - FENCE - 10), y = clamp(P.y + py, FENCE + 10, WH - FENCE - 10);
        if (inWater(x, y)) { x = P.x; y = P.y; }
        return (x, y);
    }
    static void addBomb(double x, double y, double t, double r, double dmg) => bombs.Add(new Bomb { x = clamp(x, FENCE + 10, WW - FENCE - 10), y = clamp(y, FENCE + 10, WH - FENCE - 10), t = t, max = t, r = r, dmg = dmg, fromP = false });
    static void dropBombs(ET Tt, double t, double r, double dmg)
    {
        double mv = hypot(P.svx, P.svy), m = rnd();
        if (mv < 40 || m < .35) addBomb(P.x + rand(-18, 18), P.y + rand(-18, 18), t, r, dmg);            // стоишь — бьют по месту
        else if (m < .7) { var (x, y) = predictP(t); addBomb(x, y, t, r, dmg); }                      // бежишь — на упреждение
        else { var (x, y) = predictP(t); for (int i = 0; i < 3; i++) { double k = i / 2.0; addBomb(P.x + (x - P.x) * k, P.y + (y - P.y) * k, t + i * .15, r * .85, dmg); } } // ковровая цепочка
    }
    static void bossRain(ET Tt)
    {
        double t0 = .9, dmg = eDmg(Tt.dmg * 1.5); var (px, py) = predictP(t0 + .2);
        addBomb(P.x, P.y, t0, 62, dmg); addBomb(px, py, t0 + .2, 62, dmg);
        double side = Math.Atan2(P.svy, P.svx == 0 ? 1 : P.svx) + Math.PI / 2;
        foreach (var k in new[] { -1, 1 }) addBomb(px + Math.Cos(side) * k * 95, py + Math.Sin(side) * k * 95, t0 + .4, 58, dmg);
        addBomb(P.x + rand(-140, 140), P.y + rand(-140, 140), t0 + .6, 58, dmg);
    }
    static void bossAI(Enemy e, ET Tt, double dt, double d)
    {
        double enr = e.hp < e.max * .5 ? .72 : 1; var bul = Tt.bul ?? "egg";
        if (e.spin > 0)
        {
            e.spin -= dt; e.burstT -= dt;
            if (e.burstT <= 0) { e.burstT = .07; e.spinA += .38; for (int k = 0; k < 2; k++) { var a = e.spinA + k * Math.PI; eBullet(e.x + Math.Cos(a) * 30, e.y + Math.Sin(a) * 30, a, Tt.bspd * .85, eDmg(Tt.dmg), bul); } }
            return;
        }
        if (e.burst > 0) { e.burstT -= dt; if (e.burstT <= 0) { e.burstT = .3; e.burst--; enemyShoot(e, Tt, 3, .16, bul); } return; }
        e.atkT -= dt; if (e.atkT > 0) return;
        e.pat = (e.pat + 1) % Tt.pats.Length; e.atkT = 2.3 * enr;
        var p = Tt.pats[e.pat];
        if (p == "fan") { var bas = Math.Atan2(P.y - e.y, P.x - e.x); for (int i = 0; i < 9; i++) { var a = bas + (i - 4) * .13; eBullet(e.x + Math.Cos(a) * 34, e.y + Math.Sin(a) * 34, a, Tt.bspd, eDmg(Tt.dmg), bul); } Sfx.egg(); }
        else if (p == "burst") { e.burst = 3; e.burstT = 0; }
        else if (p == "summon")
        {
            int n = e.type == "emperor" ? 3 : 2;
            for (int i = 0; i < n; i++) { var k = Tt.summon[(int)Math.Floor(rnd() * Tt.summon.Length)]; makeEnemy(k, clamp(e.x + rand(-80, 80), FENCE + 30, WW - FENCE - 30), clamp(e.y + rand(-60, 60), FENCE + 30, WH - FENCE - 30)); }
            ftext(e.x, e.y - 90, Tt.taunt ?? T("В атаку!"), "#fff", 13); Sfx.gobble();
        }
        else if (p == "dash") { e.dashes = 1; startDash(e, .75, .55); }
        else if (p == "spiral") { e.spin = 1.5; e.burstT = 0; e.spinA = rand(0, TAU); }
        else if (p == "ring") { var off = rand(0, TAU); for (int i = 0; i < 18; i++) { var a = off + i / 18.0 * TAU; eBullet(e.x + Math.Cos(a) * 30, e.y + Math.Sin(a) * 30, a, Tt.bspd * .9, eDmg(Tt.dmg), bul); } Sfx.boom(); }
        else if (p == "rain") { bossRain(Tt); ftext(e.x, e.y - 90, T("Берегись неба!"), "#ffc93a", 13); }
    }
    static void updateBullets(double dt)
    {
        for (int i = bullets.Count - 1; i >= 0; i--)
        {
            if (i >= bullets.Count) continue;
            var b = bullets[i];
            if (b.w != null) skBulletTick(b, dt);
            b.x += b.vx * dt; b.y += b.vy * dt; b.life -= dt;
            if (b.bounce > 0 && b.life > 0)
            { // фигурки рикошетят от забора и укрытий
                bool hit = false;
                if (b.x < FENCE || b.x > WW - FENCE) { b.vx *= -1; b.x = clamp(b.x, FENCE + 1, WW - FENCE - 1); hit = true; }
                if (b.y < FENCE || b.y > WH - FENCE) { b.vy *= -1; b.y = clamp(b.y, FENCE + 1, WH - FENCE - 1); hit = true; }
                if (!hit)
                    foreach (var o in OBS)
                    {
                        double dx = b.x - o.x, dy = b.y - o.y, d2 = dx * dx + dy * dy;
                        if (d2 < o.r * o.r)
                        {
                            double d = Math.Sqrt(d2); if (d == 0) d = 1; double nx = dx / d, ny = dy / d, dot = b.vx * nx + b.vy * ny;
                            if (dot < 0) { b.vx -= 2 * dot * nx; b.vy -= 2 * dot * ny; }
                            b.x = o.x + nx * (o.r + 1); b.y = o.y + ny * (o.r + 1); hit = true; break;
                        }
                    }
                if (hit) { b.bounce--; b.life = Math.Max(b.life, .22); puff(b.x, b.y, "#ffe066", 3, .5); }
            }
            if (b.kind == "yarn") b.spin += dt * 14;
            bool dead = b.life <= 0 || b.x < FENCE || b.x > WW - FENCE || b.y < FENCE || b.y > WH - FENCE || (DUN != null && !dunWalk(b.x, b.y, -2)); // стены подземелья
            if (!dead && !(b.w != null && b.w.ghost)) // гаусс-снаряд пролетает сквозь укрытия
            {
                Obs hitO = null;
                foreach (var o in OBS)
                {
                    if ((b.x - o.x) * (b.x - o.x) + (b.y - o.y) * (b.y - o.y) < o.r * o.r)
                    {
                        dead = true; hitO = o;
                        if (!b.fromP) splat(b.x, b.y, .7); else puff(b.x, b.y, o.type == "hay" ? "#f3d36e" : o.type == "bush" ? "#5f9e35" : o.type == "rock" ? "#b8b2a6" : "#a87547", 3, .6);
                        break;
                    }
                }
                if (hitO?.type == "crate") hitCrate(hitO, b.fromP ? b.dmg : b.dmg * .5); // ящик ломается от выстрелов (вражеских тоже)
            }
            if (!dead)
            {
                if (b.fromP)
                {
                    for (int ei = 0; ei < enemies.Count; ei++)
                    {
                        var e = enemies[ei];
                        if (e.dead || (b.hits != null && b.hits.Contains(e))) continue;
                        double er = e.r;
                        if ((b.x - e.x) * (b.x - e.x) + (b.y - e.y) * (b.y - e.y) < (er + b.r) * (er + b.r))
                        {
                            var k = b.kind;
                            if (b.w != null)
                            {
                                hitEnemy(e, b.dmg, b.vx, b.vy, true, b.w.knock * (k == "steam" ? .5 : 1), k is "steam" or "tornado");
                                skHit(b, e);
                                if (b.pierce > 0) { b.pierce--; b.hits?.Add(e); } else dead = true;
                                break;
                            }
                            hitEnemy(e, b.dmg, b.vx, b.vy, !b.ally, k == "flame" ? .06 : k == "ice" ? .4 : 1, k == "flame", b.owner);
                            if (k == "flame") { e.burn = 2.2; e.burnDps = 9 * P.dmgMul; }
                            else if (k == "ice")
                            {
                                e.chill = 1.6; e.iceN = (e.iceT > 0 ? e.iceN : 0) + 1; e.iceT = 1.5;
                                if (e.iceN >= 4 && !e.T.boss) { e.iceN = 0; e.stun = Math.Max(e.stun, 1.2); e.frozen = 1.2; puff(e.x, e.y, "#cfefff", 6, 1); Sfx.ice(); }
                            }
                            if (k == "crossbow" && !e.T.boss) e.stun = Math.Max(e.stun, .5);
                            if (k == "popcorn" && !b.frag)
                                for (int q = 0; q < 3; q++) { var an = rand(0, TAU); bullets.Add(new Bullet { x = e.x, y = e.y, vx = Math.Cos(an) * 420, vy = Math.Sin(an) * 420, r = 2.6, dmg = b.dmg * .3, fromP = true, life = .28, max = .28, kind = "popcorn", frag = true, pierce = 0, hits = new() { e }, ally = b.ally, owner = b.owner }); }
                            if (k == "fireball")
                            {
                                e.burn = 2.5; e.burnDps = 10 * P.dmgMul; ring(b.x, b.y, "#ff7a1a", 70, .3); puff(b.x, b.y, "#ff7a1a", 6, 1); Sfx.pop();
                                for (int fi = 0; fi < enemies.Count; fi++) { var f = enemies[fi]; if (!f.dead && f != e && hypot(f.x - b.x, f.y - b.y) < 70) { hitEnemy(f, b.dmg * .6, f.x - b.x, f.y - b.y, true, 1, true); f.burn = 2; f.burnDps = 8 * P.dmgMul; } }
                            }
                            WEAP.TryGetValue(k ?? "", out var Wk);
                            if (!b.ally && Wk != null && Wk.heal > 0) healHit(Wk.heal);
                            if (b.owner != null) { b.owner.ult = Math.Min(100, b.owner.ult + b.dmg * .14); if (Wk != null && Wk.heal > 0) healTeam(b.owner.x, b.owner.y, Wk.heal, 240, true); }
                            if (b.pierce > 0) { b.pierce--; b.hits?.Add(e); } else dead = true;
                            break;
                        }
                    }
                }
                else if (P.alive)
                {
                    double d2 = (b.x - P.x) * (b.x - P.x) + (b.y - P.y) * (b.y - P.y);
                    if (P.buff == "fort" && d2 < (P.r + 34) * (P.r + 34)) { b.fromP = true; b.vx *= -1.15; b.vy *= -1.15; b.dmg = 30 * P.dmgMul; b.life = 1.2; b.kind = "refl"; b.pierce = 0; b.hits = null; puff(b.x, b.y, "#cfe8ff", 2, .6); continue; }
                    if (d2 < (P.r + b.r - 2) * (P.r + b.r - 2)) { hurtPlayer(b.dmg, "egg"); splat(b.x, b.y, .8); dead = true; }
                }
                if (!dead && !b.fromP)
                    foreach (var a in allies)
                    {
                        if (a.down > 0) continue; double dd = (b.x - a.x) * (b.x - a.x) + (b.y - a.y) * (b.y - a.y);
                        if (a.buff == "fort" && dd < (a.r + 30) * (a.r + 30)) { b.fromP = true; b.vx *= -1.15; b.vy *= -1.15; b.dmg = 24 * a.dmgMul; b.life = 1.2; b.kind = "refl"; b.pierce = 0; b.hits = null; b.ally = true; break; }
                        if (dd < (a.r + b.r - 2) * (a.r + b.r - 2)) { hurtAlly(a, b.dmg); splat(b.x, b.y, .6); dead = true; break; }
                    }
            }
            if (dead)
            {
                if (b.w != null) skExpire(b);
                var idx = i < bullets.Count && bullets[i] == b ? i : bullets.IndexOf(b); if (idx >= 0) bullets.RemoveAt(idx);
            }
        }
    }
    static void updateNades(double dt)
    {
        for (int i = nades.Count - 1; i >= 0; i--)
        {
            var n = nades[i];
            n.x += n.vx * dt; n.y += n.vy * dt; n.z += n.vz * dt; n.vz -= 720 * dt; n.rot += dt * (hypot(n.vx, n.vy) / 20);
            if (n.z < 0) { n.z = 0; n.vz *= -.35; n.vx *= .55; n.vy *= .55; }
            foreach (var o in OBS)
            {
                double dx = n.x - o.x, dy = n.y - o.y, d = hypot(dx, dy); if (d == 0) d = 1;
                if (d < o.r + 5) { n.x = o.x + dx / d * (o.r + 5); n.y = o.y + dy / d * (o.r + 5); double dot = (n.vx * dx + n.vy * dy) / d; n.vx -= 1.6 * dot * dx / d; n.vy -= 1.6 * dot * dy / d; }
            }
            n.x = clamp(n.x, FENCE + 6, WW - FENCE - 6); n.y = clamp(n.y, FENCE + 6, WH - FENCE - 6);
            n.t -= dt; if (n.t <= 0) { nades.RemoveAt(i); blast(n.x, n.y, 130, 110 * P.dmgMul, true, true); }
        }
    }
    static void updateBombs(double dt)
    {
        for (int i = bombs.Count - 1; i >= 0; i--)
        {
            if (i >= bombs.Count) continue;
            var b = bombs[i]; b.t -= dt;
            if (b.t <= 0) { bombs.RemoveAt(i); if (!b.fromP && S.mode != "play") continue; blast(b.x, b.y, b.r, b.dmg, b.fromP, false, b.heal); }
        }
    }
    static void updateParts(double dt)
    {
        int keep = 0; // отжившие частицы убираем одним проходом, без сдвига списка на каждой
        for (int i = 0; i < parts.Count; i++)
        {
            var p = parts[i]; p.t += dt;
            if (p.k == "feather")
            {
                p.x += p.vx * dt; p.y += p.vy * dt; p.z += p.vz * dt; p.vz = Math.Max(p.vz - 320 * dt, -34); p.vx *= Math.Pow(.3, dt); p.vy *= Math.Pow(.3, dt);
                p.x += Math.Sin(p.t * 7 + p.rot) * 14 * dt; p.rot += p.vr * dt; if (p.z < 0) { p.z = 0; p.vz = 0; p.vr = 0; }
            }
            else if (p.k == "puff") { p.x += p.vx * dt; p.y += p.vy * dt; p.z += p.vz * dt; }
            else if (p.k == "plus") p.z += p.vz * dt;
            else if (p.k == "shell") { p.x += p.vx * dt; p.y += p.vy * dt; p.z += p.vz * dt; p.vz -= 700 * dt; p.rot += p.vr * dt; if (p.z < 0) { p.z = 0; p.vz *= -.3; p.vx *= .5; p.vy *= .5; p.vr *= .5; } }
            if (p.t < p.max) parts[keep++] = p;
        }
        if (keep < parts.Count) parts.RemoveRange(keep, parts.Count - keep);
        if (parts.Count > 520) parts.RemoveRange(0, parts.Count - 520);
        keep = 0;
        for (int i = 0; i < texts.Count; i++) { var t = texts[i]; t.t += dt; t.y -= 40 * dt; t.x += t.vx * dt; if (t.t < t.max) texts[keep++] = t; }
        if (keep < texts.Count) texts.RemoveRange(keep, texts.Count - keep);
        if (texts.Count > 80) texts.RemoveRange(0, texts.Count - 80); // лавина цифр урона не тормозит кадр
    }
}
