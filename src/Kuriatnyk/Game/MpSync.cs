using System;
using System.Collections.Generic;
using System.Linq;
using static Kuriatnyk.D;
using static Kuriatnyk.Store;

namespace Kuriatnyk;

/// <summary>Список частиц, который в сетевой игре запоминает новые — хост рассылает их клиентам.</summary>
public sealed class PartList : List<Part>
{
    public static bool Rec; public static readonly List<Part> Fresh = new();
    public new void Add(Part p) { base.Add(p); if (Rec && Fresh.Count < 400) Fresh.Add(p); }
}

// Сетевая игра: снимок мира (хост → клиенты), ввод (клиент → хост), запуск и конец боя на клиенте.
public static partial class G
{
    /// <summary>Мир сменился (новая карта или уровень подземелья) — хост пошлёт его клиентам заново.</summary>
    public static int worldVer;
    /// <summary>Клиент сетевой игры показывает мир хоста (сам бой не считает).</summary>
    public static bool IsClientWorld;
    static readonly List<FText> freshTexts = new();
    static readonly List<(string big, string small)> freshBanners = new();
    static double MpScale(int n) => Mp.Scale(n);

    /// <summary>Герои боя: в сетевой игре у хоста — все из лобби, иначе — один свой.</summary>
    static void spawnPlayers(string heroId)
    {
        if (Net.IsHost && Mp.roster.Count > 0)
        {
            players = new();
            foreach (var r in Mp.roster)
            {
                var pl = r.host ? newPlayer(heroId ?? SAVE.hero) : newPlayer(r.hero, r.lv, r.gun);
                pl.pid = r.pid; pl.remote = !r.host; pl.pname = r.name; pl.avatar = r.avatar; pl.tp = 1;
                if (pl.remote) pl.ri = new RemoteIn { x = pl.x, y = pl.y };
                players.Add(pl);
            }
            Me = players.First(p => !p.remote); P = Me;
            for (int i = 0; i < players.Count; i++) { var pl = players[i]; pl.x += (i % 2 == 0 ? -1 : 1) * (i + 1) / 2 * 46; pl._px = pl.x; pl._py = pl.y; if (pl.ri != null) { pl.ri.x = pl.x; pl.ri.y = pl.y; } }
        }
        else { P = newPlayer(heroId); solo(); }
        PartList.Rec = Net.IsHost; PartList.Fresh.Clear(); freshTexts.Clear(); freshBanners.Clear(); Sfx.Rec = Net.IsHost; Sfx.Fresh.Clear();
    }
    public static void recText(FText t) { if (PartList.Rec && freshTexts.Count < 60) freshTexts.Add(t); }
    public static void recBanner(string big, string small) { if (PartList.Rec) freshBanners.Add((big, small)); }

    /* ---------- мир ---------- */
    public static byte[] WorldMsg()
    {
        var p = new Pk(Mp.T_WORLD).S(S.kind).I(S.lvIdx).I(S.skLv).B(S.night).F(WW).F(WH).S(MAP).B(DUN != null);
        if (DUN != null)
        {
            var D = DUN; p.B(D.hub).F(D.doorX).F(D.doorY).I(D.rooms.Count);
            foreach (var R in D.rooms) p.F(R.x).F(R.y).F(R.w).F(R.h).S(R.type).B(R.cleared).B(R.seen).I(R.waves);
            p.I(D.cors.Count); foreach (var c in D.cors) p.F(c.x).F(c.y).F(c.w).F(c.h).B(c.horiz).I(D.rooms.IndexOf(c.a)).I(D.rooms.IndexOf(c.b));
            p.I(D.portals.Count); foreach (var o in D.portals) p.F(o.x).F(o.y).B(o.on).S(o.kind);
        }
        p.I(RIVERS.Count);
        foreach (var R in RIVERS)
        {
            p.S(R.axis.ToString()).F(R.@base).F(R.amp).F(R.freq).F(R.phase).F(R.hw).F(R.lo).F(R.hi).S(R.end).I(RIVERS.IndexOf(R.parent)).I(R.pside).I(DUN != null && R.room != null ? DUN.rooms.IndexOf(R.room) : -1);
            p.I(R.bridges.Count); foreach (var b in R.bridges) p.F(b.u).F(b.hw).B(b.rotten).B(b.broken).I(b.passes);
        }
        return p.Bytes();
    }
    public static void ReadWorld(Rd d)
    {
        S.kind = d.S(); S.lvIdx = d.I(); S.skLv = d.I(); S.night = d.B(); WW = d.F(); WH = d.F(); var map = d.S(); bool dun = d.B();
        S.L = S.kind == "story" && S.lvIdx >= 0 && S.lvIdx < STORY.Count ? STORY[S.lvIdx] : null;
        DUN = null;
        if (dun)
        {
            var D = new Dungeon { hub = d.B(), doorX = d.F(), doorY = d.F() }; int n = d.I();
            for (int i = 0; i < n; i++) D.rooms.Add(new Room { x = d.F(), y = d.F(), w = d.F(), h = d.F(), type = d.S(), cleared = d.B(), seen = d.B(), waves = d.I() });
            n = d.I(); for (int i = 0; i < n; i++) { var c = new Corr { x = d.F(), y = d.F(), w = d.F(), h = d.F(), horiz = d.B() }; int a = d.I(), b = d.I(); c.a = D.rooms[a]; c.b = D.rooms[b]; c.a.cors.Add(c); c.b.cors.Add(c); D.cors.Add(c); }
            n = d.I(); for (int i = 0; i < n; i++) D.portals.Add(new Portal { x = d.F(), y = d.F(), on = d.B(), kind = d.S() });
            DUN = D;
        }
        RIVERS = new(); int nr = d.I(); var parents = new List<int>();
        for (int i = 0; i < nr; i++)
        {
            var R = new River { axis = (d.S() ?? "h")[0], @base = d.F(), amp = d.F(), freq = d.F(), phase = d.F(), hw = d.F(), lo = d.F(), hi = d.F(), end = d.S() };
            parents.Add(d.I()); R.pside = d.I(); int room = d.I(); if (room >= 0 && DUN != null) R.room = DUN.rooms[room];
            int nb = d.I(); R.bridges = new();
            for (int k = 0; k < nb; k++) R.bridges.Add(new Bridge { u = d.F(), hw = d.F(), rotten = d.B(), broken = d.B(), passes = d.I(), R = R });
            RIVERS.Add(R);
        }
        for (int i = 0; i < nr; i++) RIVERS[i].parent = parents[i] >= 0 ? RIVERS[parents[i]] : null;
        RIVER = RIVERS.FirstOrDefault();
        foreach (var b in allBridges()) b.ends = bridgeEnds(b);
        MAP = MAPS.ContainsKey(map ?? "") ? map : "yard";
        if (DUN != null) renderDungeon(); else { ground?.Dispose(); ground = RenderGround(MAPS[MAP]); }
        fish = new();
        if (Me != null) { Me.lastBridge = null; snapCamera(); }
    }

    /* ---------- снимок (30 раз в секунду) ---------- */
    static void wFighterLook(Pk p, Fighter f) { p.F(f.x).F(f.y).F(f.hp).F(f.max).F(f.ang).I(f.face).F(f.phase).B(f.moving).F(f.flash).S(f.buff).F(f.buffT).F(f.ult); }
    public static byte[] SnapMsg()
    {
        var p = new Pk(Mp.T_SNAP);
        p.F(S.t).S(S.mode).I(S.wave).I(S.toSpawn).L(S.score).I(S.kills).I(S.goalKills).F(S.survT).F(S.bossT).F(S.between).B(S.running).L(S.coins).L(S.tokens).L(S.piv)
         .I(S.combo).B(S.goalDone).B(S.won).F(S.shake).F(S.diff).I(S.boss != null ? enemies.IndexOf(S.boss) : -1).B(bossBarOn).S(Music.Want).F(MpHp);
        p.U8(S.skPass.Count); foreach (var s in S.skPass) p.S(s);
        // герои
        p.U8(players.Count);
        foreach (var pl in players)
        {
            p.I(pl.pid).S(pl.hid).I(pl.hlv).S(pl.pname).S(pl.avatar); wFighterLook(p, pl);
            p.B(pl.alive).F(pl.downT).F(pl.inv).S(pl.w).F(double.IsInfinity(pl.ammo) ? -1 : pl.ammo).I(pl.slot).I(pl.bagMax).F(pl.stam).F(pl.stamMax).I(pl.nades).S(pl.ultId).F(pl.bowT).F(pl.swing).F(pl.swingA).F(pl.spin).I(pl.tp).F(pl.armor).F(pl.dmgMul);
            p.U8(pl.bag?.Count ?? 0); if (pl.bag != null) foreach (var w in pl.bag) p.S(w);
        }
        p.U8(allies.Count);
        foreach (var a in allies) { p.S(a.id).S(a.gun).F(a.down).B(a.summon).F(a.summonT).S(a.cls); wFighterLook(p, a); }
        // враги
        p.I(enemies.Count);
        foreach (var e in enemies)
        {
            p.S(e.type).F(e.x).F(e.y).F(e.r).F(e.hp).F(e.max).I(e.face).F(e.ang).F(e.phase).F(e.flash).S(e.st).F(e.dx).F(e.dy).F(e.dashLen).B(e.moving).F(e.emerge)
             .B(e.kami).F(e.kamiT).S(e.buff).F(e.frozen).F(e.chill).F(e.honey).F(e.bubble).F(e.stun).F(e.s).S(e.body).S(e.wing).B(e.hop).F(e.quakeT).F(e.quakeA).F(e.tellT).F(e.burn);
        }
        p.I(bullets.Count);
        foreach (var b in bullets) p.F(b.x).F(b.y).F(b.vx).F(b.vy).F(b.r).S(b.kind).B(b.fromP).F(b.spin).I(b.shape).F(b.life).F(b.max).S(b.w?.id).B(b.ally);
        p.I(pickups.Count);
        foreach (var q in pickups) p.F(q.x).F(q.y).S(q.kind).S(q.wid).I(q.n).I(q.price).B(q.egg).F(q.t).F(q.ph).F(q.z);
        p.I(bombs.Count);
        foreach (var b in bombs) p.F(b.x).F(b.y).F(b.t).F(b.max).F(b.r).B(b.fromP).B(b.lob).B(b.quiet).F(b.sx).F(b.sy).S(b.kind);
        p.I(nades.Count); foreach (var n in nades) p.F(n.x).F(n.y).F(n.z).F(n.rot).F(n.t);
        p.I(zones.Count); foreach (var z in zones) p.F(z.x).F(z.y).F(z.t).F(z.r).F(z.max).B(z.mini);
        p.I(strikes.Count); foreach (var z in strikes) p.F(z.x).F(z.y).F(z.t).F(z.r).F(z.max);
        p.I(puddles.Count); foreach (var z in puddles) p.F(z.x).F(z.y).F(z.t).F(z.r).F(z.max);
        p.I(goo.Count); foreach (var g in goo) p.S(g.kind).F(g.x).F(g.y).F(g.t).F(g.max).F(g.r);
        p.I(quakes.Count); foreach (var q in quakes) p.F(q.x).F(q.y).F(q.r).F(q.max).F(q.delay);
        p.I(holes.Count); foreach (var h in holes) p.F(h.x).F(h.y).F(h.t).F(h.ct).S(h.state);
        p.I(OBS.Count); foreach (var o in OBS) p.F(o.x).F(o.y).F(o.r).S(o.type).S(o.kind).F(o.hp).F(o.hit).B(o.open).I(o.st);
        p.I(RIVERS.Sum(r => r.bridges.Count)); foreach (var b in allBridges()) p.B(b.broken).I(b.passes).F(b.wob);
        p.I(fish.Count); foreach (var f in fish) p.I(RIVERS.IndexOf(f.R)).F(f.u).F(f.dir).F(f.ph).S(f.st).F(f.t).F(f.sx).F(f.sy).F(f.tx).F(f.ty);
        if (DUN != null)
        {
            p.B(true).I(DUN.active != null ? DUN.rooms.IndexOf(DUN.active) : -1).I(DUN.rooms.Count);
            foreach (var R in DUN.rooms) p.B(R.cleared).B(R.seen).I(R.wave).I(R.waves);
            p.I(DUN.portals.Count); foreach (var o in DUN.portals) p.B(o.on);
        }
        else p.B(false);
        // что появилось с прошлого снимка: частицы, всплывающий текст, звуки, баннеры
        var fp = PartList.Fresh; p.I(fp.Count);
        foreach (var q in fp) p.S(q.k).S(q.col).F(q.x).F(q.y).F(q.z).F(q.vx).F(q.vy).F(q.vz).F(q.rot).F(q.vr).F(q.t).F(q.max).F(q.r).F(q.seed).F(q.a).F(q.s).F(q.x2).F(q.y2).F(q.sx).F(q.sy).F(q.R).B(q.dirt);
        fp.Clear();
        p.I(freshTexts.Count); foreach (var t in freshTexts) p.F(t.x).F(t.y).F(t.size).F(t.max).F(t.vx).S(t.txt).S(t.col); freshTexts.Clear();
        p.I(Sfx.Fresh.Count); foreach (var a in Sfx.Fresh) { p.U8(a.Length); foreach (var v in a) p.F(v); } Sfx.Fresh.Clear();
        p.I(freshBanners.Count); foreach (var (b1, b2) in freshBanners) p.S(b1).S(b2); freshBanners.Clear();
        return p.Bytes();
    }

    static T At<T>(List<T> list, int i) where T : new() { while (list.Count <= i) list.Add(new T()); return list[i]; }
    static void Trim<T>(List<T> list, int n) { if (list.Count > n) list.RemoveRange(n, list.Count - n); }
    static double lastHx, lastHy; // где герой стоит у хоста (для телепорта своего героя)
    static void rFighterLook(Rd d, Fighter f, bool keepPos)
    {
        double x = d.F(), y = d.F(); lastHx = x; lastHy = y; f.hp = d.F(); f.max = d.F(); double ang = d.F(); int face = d.I(); double ph = d.F(); bool mv = d.B(); f.flash = d.F(); f.buff = d.S(); f.buffT = d.F(); f.ult = d.F();
        if (!keepPos) { f.x = x; f.y = y; f.ang = ang; f.face = face; f.phase = ph; f.moving = mv; }
    }
    public static void ReadSnap(Rd d)
    {
        S.t = d.F(); var mode = d.S(); S.wave = d.I(); S.toSpawn = d.I(); S.score = d.L(); S.kills = d.I(); S.goalKills = d.I(); S.survT = d.F(); S.bossT = d.F(); S.between = d.F(); S.running = d.B();
        S.coins = d.L(); S.tokens = d.L(); S.piv = d.L(); S.combo = d.I(); S.goalDone = d.B(); S.won = d.B(); S.shake = Math.Max(S.shake, d.F()); S.diff = d.F(); int bossI = d.I(); bossBarOn = d.B(); var mus = d.S(); MpHp = d.F();
        if (mus != null && mus != Music.Want) Music.Play(mus);
        int nps = d.U8(); S.skPass = new(); for (int i = 0; i < nps; i++) S.skPass.Add(d.S());
        // режим: «pick» — хост выбирает пассивку, «over»/«result» — конец придёт отдельным сообщением
        if (mode is "play" or "dying" or "won" or "pick") S.mode = mode;
        int np = d.U8(); var keep = new List<Player>();
        for (int i = 0; i < np; i++)
        {
            int pid = d.I(); var hid = d.S(); int hlv = d.I();
            var pl = players.FirstOrDefault(q => q.pid == pid);
            if (pl == null || pl.hid != hid) { var old = pl; pl = newPlayer(hid, hlv); pl.pid = pid; if (old != null) { pl.x = old.x; pl.y = old.y; pl.tp = old.tp; } if (pid == Mp.myPid) { pl.remote = false; Me = pl; P = pl; } else pl.remote = true; }
            keep.Add(pl);
            pl.hlv = hlv; pl.pname = d.S(); pl.avatar = d.S();
            bool mine = pl == Me;
            rFighterLook(d, pl, mine); double hx = lastHx, hy = lastHy;
            pl.alive = d.B(); pl.downT = d.F(); pl.inv = d.F(); var w = d.S(); double ammo = d.F(); pl.slot = d.I(); pl.bagMax = d.I(); pl.stam = d.F(); pl.stamMax = d.F(); pl.nades = d.I(); pl.ultId = d.S(); pl.bowT = d.F(); pl.swing = d.F(); pl.swingA = d.F(); pl.spin = d.F();
            int tp = d.I(); pl.armor = d.F(); pl.dmgMul = d.F();
            pl.w = w ?? pl.w; pl.prim = pl.w; pl.ammo = ammo < 0 ? double.PositiveInfinity : ammo;
            int nb = d.U8(); if (nb > 0) { pl.bag = new(); for (int k = 0; k < nb; k++) pl.bag.Add(d.S()); } else pl.bag = S.kind == "sk" ? new() : null;
            if (mine && tp != pl.tp)
            { // хост переставил героя (портал, комната, возвращение) — прыгаем туда же
                pl.tp = tp; pl.x = hx; pl.y = hy; pl._px = pl.x; pl._py = pl.y; pl.lastBridge = null; snapCamera();
            }
            if (!mine) pl.tp = tp;
        }
        players = keep; if (Me == null || !players.Contains(Me)) Me = players.FirstOrDefault(q => q.pid == Mp.myPid) ?? players.FirstOrDefault(); P = Me;
        int na = d.U8(); allies = allies ?? new();
        for (int i = 0; i < na; i++) { var a = At(allies, i); a.id = d.S(); a.gun = d.S(); a.down = d.F(); a.summon = d.B(); a.summonT = d.F(); a.cls = d.S(); rFighterLook(d, a, false); a.look ??= lookFor(a.id); if (a.r <= 1) a.r = HEROES[a.id].r; }
        Trim(allies, na); foreach (var a in allies) if (a.look == null || a.look.sp != HEROES[a.id].sp) a.look = lookFor(a.id);
        int ne = d.I();
        for (int i = 0; i < ne; i++)
        {
            var e = At(enemies, i); var type = d.S(); if (e.type != type || e.T == null) { e.type = type; e.T = ETYPE[type]; }
            e.x = d.F(); e.y = d.F(); e.r = d.F(); e.hp = d.F(); e.max = d.F(); e.face = d.I(); e.ang = d.F(); e.phase = d.F(); e.flash = d.F(); e.st = d.S() ?? "walk"; e.dx = d.F(); e.dy = d.F(); e.dashLen = d.F(); e.moving = d.B(); e.emerge = d.F();
            e.kami = d.B(); e.kamiT = d.F(); e.buff = d.S(); e.frozen = d.F(); e.chill = d.F(); e.honey = d.F(); e.bubble = d.F(); e.stun = d.F(); e.s = d.F(); e.body = d.S(); e.wing = d.S(); e.hop = d.B(); e.quakeT = d.F(); e.quakeA = d.F(); e.tellT = d.F(); e.burn = d.F(); e.dead = false;
        }
        Trim(enemies, ne); S.boss = bossI >= 0 && bossI < enemies.Count ? enemies[bossI] : null;
        int nbu = d.I();
        for (int i = 0; i < nbu; i++)
        {
            var b = At(bullets, i); b.x = d.F(); b.y = d.F(); b.vx = d.F(); b.vy = d.F(); b.r = d.F(); b.kind = d.S(); b.fromP = d.B(); b.spin = d.F(); b.shape = d.I(); b.life = d.F(); b.max = d.F();
            var wid = d.S(); b.w = wid != null && WEAP.TryGetValue(wid, out var ww) ? ww : null; b.ally = d.B();
        }
        Trim(bullets, nbu);
        int npk = d.I();
        for (int i = 0; i < npk; i++) { var q = At(pickups, i); q.x = d.F(); q.y = d.F(); q.kind = d.S(); q.wid = d.S(); q.n = d.I(); q.price = d.I(); q.egg = d.B(); q.t = d.F(); q.ph = d.F(); q.z = d.F(); }
        Trim(pickups, npk);
        int nbo = d.I();
        for (int i = 0; i < nbo; i++) { var b = At(bombs, i); b.x = d.F(); b.y = d.F(); b.t = d.F(); b.max = d.F(); b.r = d.F(); b.fromP = d.B(); b.lob = d.B(); b.quiet = d.B(); b.sx = d.F(); b.sy = d.F(); b.kind = d.S(); }
        Trim(bombs, nbo);
        int nn = d.I(); for (int i = 0; i < nn; i++) { var n = At(nades, i); n.x = d.F(); n.y = d.F(); n.z = d.F(); n.rot = d.F(); n.t = d.F(); }
        Trim(nades, nn);
        int nz = d.I(); for (int i = 0; i < nz; i++) { var z = At(zones, i); z.x = d.F(); z.y = d.F(); z.t = d.F(); z.r = d.F(); z.max = d.F(); z.mini = d.B(); }
        Trim(zones, nz);
        nz = d.I(); for (int i = 0; i < nz; i++) { var z = At(strikes, i); z.x = d.F(); z.y = d.F(); z.t = d.F(); z.r = d.F(); z.max = d.F(); }
        Trim(strikes, nz);
        nz = d.I(); for (int i = 0; i < nz; i++) { var z = At(puddles, i); z.x = d.F(); z.y = d.F(); z.t = d.F(); z.r = d.F(); z.max = d.F(); }
        Trim(puddles, nz);
        nz = d.I(); for (int i = 0; i < nz; i++) { var g = At(goo, i); g.kind = d.S(); g.x = d.F(); g.y = d.F(); g.t = d.F(); g.max = d.F(); g.r = d.F(); }
        Trim(goo, nz);
        nz = d.I(); for (int i = 0; i < nz; i++) { var q = At(quakes, i); q.x = d.F(); q.y = d.F(); q.r = d.F(); q.max = d.F(); q.delay = d.F(); }
        Trim(quakes, nz);
        nz = d.I(); for (int i = 0; i < nz; i++) { var h = At(holes, i); h.x = d.F(); h.y = d.F(); h.t = d.F(); h.ct = d.F(); h.state = d.S(); }
        Trim(holes, nz);
        int no = d.I();
        for (int i = 0; i < no; i++) { var o = At(OBS, i); o.x = d.F(); o.y = d.F(); o.r = d.F(); o.type = d.S(); o.kind = d.S(); o.hp = d.F(); o.hit = d.F(); o.open = d.B(); o.st = d.I(); }
        Trim(OBS, no);
        int nbr = d.I(); var brs = allBridges().ToList();
        for (int i = 0; i < nbr; i++) { bool br = d.B(); int pas = d.I(); double wob = d.F(); if (i < brs.Count) { brs[i].broken = br; brs[i].passes = pas; brs[i].wob = wob; } }
        int nf = d.I();
        for (int i = 0; i < nf; i++)
        {
            var f = At(fish, i); int ri = d.I(); f.R = ri >= 0 && ri < RIVERS.Count ? RIVERS[ri] : RIVERS.FirstOrDefault(); f.u = d.F(); f.dir = d.F(); f.ph = d.F(); f.st = d.S() ?? "swim"; f.t = d.F(); f.sx = d.F(); f.sy = d.F(); f.tx = d.F(); f.ty = d.F();
        }
        Trim(fish, nf); fish.RemoveAll(f => f.R == null);
        if (d.B())
        {
            int act = d.I(), nr = d.I();
            for (int i = 0; i < nr; i++) { bool cl = d.B(), seen = d.B(); int wv = d.I(), wvs = d.I(); if (DUN != null && i < DUN.rooms.Count) { var R = DUN.rooms[i]; R.cleared = cl; R.seen = seen; R.wave = wv; R.waves = wvs; } }
            int npo = d.I(); for (int i = 0; i < npo; i++) { bool on = d.B(); if (DUN != null && i < DUN.portals.Count) DUN.portals[i].on = on; }
            if (DUN != null) DUN.active = act >= 0 && act < DUN.rooms.Count ? DUN.rooms[act] : null;
        }
        int nfp = d.I();
        for (int i = 0; i < nfp; i++)
            parts.Add(new Part { k = d.S(), col = d.S(), x = d.F(), y = d.F(), z = d.F(), vx = d.F(), vy = d.F(), vz = d.F(), rot = d.F(), vr = d.F(), t = d.F(), max = d.F(), r = d.F(), seed = d.F(), a = d.F(), s = d.F(), x2 = d.F(), y2 = d.F(), sx = d.F(), sy = d.F(), R = d.F(), dirt = d.B() });
        int nt = d.I(); for (int i = 0; i < nt; i++) texts.Add(new FText { x = d.F(), y = d.F(), size = d.F(), max = d.F(), vx = d.F(), txt = d.S() ?? "", col = d.S() ?? "#fff" });
        int ns = d.I(); for (int i = 0; i < ns; i++) { int k = d.U8(); var a = new float[k]; for (int j = 0; j < k; j++) a[j] = (float)d.F(); Sfx.PlayRaw(a); }
        int nbn = d.I(); for (int i = 0; i < nbn; i++) showBanner(d.S() ?? "", d.S() ?? "");
    }

    /* ---------- ввод клиента ---------- */
    static readonly List<string> pendingActs = new();
    /// <summary>Кнопка героя: в одиночной игре и у хоста — сразу, у клиента — уходит хосту со следующим вводом.</summary>
    public static void act(string a)
    {
        if (IsClientWorld) { if (S.mode == "play" && Me != null && Me.alive && pendingActs.Count < 16) pendingActs.Add(a); return; }
        runAct(a);
    }
    static void runAct(string a)
    {
        switch (a)
        {
            case "nade": throwNade(); break;
            case "ult": useUlt(); break;
            case "swap": swapWeapon(); break;
            case "act": interact(); break;
            case "drop": dropCurrent(); break;
            case "slot0": selectSlot(0); break; case "slot1": selectSlot(1); break; case "slot2": selectSlot(2); break;
        }
    }
    public static byte[] InputMsg()
    {
        var m = Me; var p = new Pk(Mp.T_INPUT);
        p.F(m.x).F(m.y).F(m.ang).F(m.phase).B(clientFiring).B(m.moving).I(m.face).B(m.aimMouse).B(m.aimHeld).B(m.manualNade).F(m.tx).F(m.ty).I(m.tp);
        p.U8(pendingActs.Count); foreach (var a in pendingActs) p.S(a); pendingActs.Clear();
        return p.Bytes();
    }
    public static void ReadInput(Rd d, Player pl)
    {
        var ri = pl.ri ??= new RemoteIn();
        ri.x = d.F(); ri.y = d.F(); ri.ang = d.F(); ri.phase = d.F(); ri.firing = d.B(); ri.moving = d.B(); ri.face = d.I(); ri.aimMouse = d.B(); ri.aimHeld = d.B(); ri.manualNade = d.B(); ri.tx = d.F(); ri.ty = d.F(); ri.tp = d.I();
        int n = d.U8();
        for (int i = 0; i < n; i++)
        {
            var a = d.S(); if (a == null) continue;
            var old = P; P = pl; try { if (S.mode == "play") runAct(a); } finally { P = old; }
        }
    }

    /* ---------- клиент: начало, кадр, конец ---------- */
    static bool clientFiring;
    public static void ClientStart(string mode, int idx)
    {
        IsClientWorld = true; DUN = null;
        enemies = new(); bullets = new(); parts = new(); pickups = new(); nades = new(); bombs = new(); texts = new(); holes = new(); zones = new(); strikes = new(); puddles = new(); goo = new(); quakes = new(); allies = new(); fish = new();
        var hero = mode == "sk" ? SAVE.hero : SAVE.hero;
        P = newPlayer(hero); P.pid = Mp.myPid; P.tp = 0; Me = P; players = new() { P };
        S.mode = "play"; S.kind = mode; S.lvIdx = idx; S.L = mode == "story" ? STORY[Math.Clamp(idx, 0, STORY.Count - 1)] : null; S.t = 0; S.score = 0; S.kills = 0; S.coins = 0; S.tokens = 0; S.piv = 0; S.banked = false; S.won = false; S.skPass = new();
        S.boss = null; bossBarOn = false; S.shake = 0; pendingActs.Clear();
        sticks.move = sticks.aim = null; clearBanner(); Ui.ToastClear(); Music.Play("battle"); Music.Duck(1);
        Ui.OnGameStart();
    }
    /// <summary>Кадр клиента: двигаем своего героя, остальное — из снимков хоста (между ними пули летят по инерции).</summary>
    static void clientUpdate(double dt)
    {
        P = Me;
        if (Me != null && Me.alive && S.mode == "play") { clientFiring = localControls(dt); findAct(); if (Me.swing > 0) Me.swing -= dt; }
        else clientFiring = false;
        foreach (var b in bullets) { b.x += b.vx * dt; b.y += b.vy * dt; if (b.kind == "yarn") b.spin += dt * 14; }
        foreach (var o in OBS) if (o.hit > 0) o.hit -= dt;
        foreach (var pl in players) if (pl != Me && pl.flash > 0) pl.flash -= dt;
        foreach (var f in fish) f.ph += dt * 1.3;
        foreach (var p in pickups) p.ph += dt;
        updateParts(dt);
        S.shake *= Math.Pow(.004, dt); if (S.shake < .1) S.shake = 0;
        if (Me != null) updateCamera(dt);
    }
    /// <summary>Хост прислал итог боя: зачислить свои зёрна и показать окно.</summary>
    public static void ClientEnd(bool won, string lead, string wl, string wt, long score, int kills, long coins, long tokens)
    {
        S.coins = coins; S.tokens = tokens; S.score = score; S.kills = kills; S.won = won; S.mode = won ? "result" : "over";
        sticks.move = sticks.aim = null; input.firing = false;
        bankCoins();
        if (won)
        {
            var w = new WinInfo { eyebrow = T("Сетевая игра"), lead = lead ?? "", hasNext = false };
            w.rows.Add(new WinRow { icon = "coin", text = "+" + coins + T(" зёрен"), sub = T("за бой вместе с командой") });
            w.rows.Add(new WinRow { icon = "tok", text = "+" + tokens + T(" жетонов"), sub = T("на обычный сундук: ") + L.Fmt(SAVE.tokens) + "/100" });
            AddMatchRow(w); Ui.ShowWin(w);
        }
        else Ui.ShowOver(new OverInfo { score = score, kills = kills, earnCoins = coins.ToString(), earnTokens = tokens.ToString(), waveLabel = wl ?? T("Волна"), waveText = wt, lead = lead ?? "", pts = S.lastPts, xp = S.lastXp });
    }
}
