using System;
using System.Collections.Generic;
using System.Linq;
using Kuriatnyk.Render;
using static Kuriatnyk.D;
using static Kuriatnyk.Store;

namespace Kuriatnyk;

// Прогресс аккаунта вне боя: награды, статистика, уровень за PTS, аватарки, достижения, задания и боевой пропуск.
// Всё хранится в сохранении текущего аккаунта (SaveData), сервера пока нет.

/// <summary>Награда: валюты, сундуки, аватарка, PTS (опыт аккаунта) и опыт боевого пропуска.</summary>
public sealed class Reward
{
    public long coins, tokens, pts, xp; public int btok, eggs, sch, lch; public string avatar;
    public void Grant()
    {
        SAVE.coins += coins; SAVE.tokens += tokens; SAVE.btokens += btok; SAVE.eggs += eggs; SAVE.schests += sch; SAVE.lchests += lch;
        if (avatar != null && !SAVE.avatars.Contains(avatar)) SAVE.avatars.Add(avatar);
        if (xp > 0) Pass.AddXp(xp);
        if (pts > 0) Account.AddPts(pts);
    }
    /// <summary>Строки награды: значок (coin, tok, btok, gegg, sch, lch, pts, xp, ava) и текст.</summary>
    public List<(string icon, string text)> Items()
    {
        var r = new List<(string, string)>();
        if (coins > 0) r.Add(("coin", "+" + L.Fmt(coins)));
        if (tokens > 0) r.Add(("tok", "+" + tokens));
        if (btok > 0) r.Add(("btok", "+" + btok));
        if (eggs > 0) r.Add(("gegg", "+" + eggs));
        if (sch > 0) r.Add(("sch", "+" + sch));
        if (lch > 0) r.Add(("lch", "+" + lch));
        if (avatar != null) r.Add(("ava", Avatars.Get(avatar)?.name ?? avatar));
        if (pts > 0) r.Add(("pts", "+" + pts + " PTS"));
        if (xp > 0) r.Add(("xp", "+" + xp + L.T(" опыта пропуска")));
        return r;
    }
    /// <summary>Коротко для тоста: «+150 зёрен · +3 золотых яйца».</summary>
    public string Text()
    {
        var p = new List<string>();
        if (coins > 0) p.Add("+" + L.Fmt(coins) + L.T(" зёрен"));
        if (tokens > 0) p.Add("+" + tokens + L.T(" жетонов"));
        if (btok > 0) p.Add("+" + btok + L.T(" больш. жетон."));
        if (eggs > 0) p.Add("+" + eggs + L.T(" золотых яиц"));
        if (sch > 0) p.Add(L.T("сверхбольшой сундук"));
        if (lch > 0) p.Add(L.T("легендарный сундук"));
        if (avatar != null) p.Add(L.T("аватарка «") + (Avatars.Get(avatar)?.name ?? avatar) + "»");
        if (pts > 0) p.Add("+" + pts + " PTS");
        return string.Join(" · ", p);
    }
}

/// <summary>Статистика аккаунта за всё время. Считается только то, что сделал сам игрок (в сетевой игре — свой герой).</summary>
public static class Stats
{
    /// <summary>Сетевая игра: удар или убийство сделал не локальный игрок — передать ему. true — событие ушло другому игроку.</summary>
    public static Func<string, long, bool, bool> Route;
    public static long Get(string key) => SAVE?.stats != null && SAVE.stats.TryGetValue(key, out var v) ? v : 0;
    public static void Add(string key, long n = 1)
    {
        if (n <= 0 || SAVE == null) return;
        if (Route != null && Route(key, n, false)) return;
        AddLocal(key, n);
    }
    public static void AddLocal(string key, long n)
    {
        SAVE.stats[key] = Get(key) + n;
        Quests.OnStat(key, n, false);
        Ach.Check();
    }
    /// <summary>Рекорд: запомнить, если больше прежнего (лучшая волна, глубина подземелья). Задания «дойди до…» считают максимум.</summary>
    public static void Max(string key, long v)
    {
        if (SAVE == null) return;
        if (Route != null && Route(key, v, true)) return;
        MaxLocal(key, v);
    }
    public static void MaxLocal(string key, long v)
    {
        if (v > Get(key)) SAVE.stats[key] = v;
        Quests.OnStat(key, v, true);
        Ach.Check();
    }
}

/// <summary>Уровень аккаунта: растёт от PTS (за бои, задания и достижения). За каждый уровень — награда.</summary>
public static class Account
{
    public const int MAX_LV = 50;
    /// <summary>Сколько всего PTS нужно, чтобы дойти до уровня lv: 100, 300, 600, 1000…</summary>
    public static long Total(int lv) => 50L * lv * (lv - 1);
    public static int LevelOf(long pts) { int lv = 1; while (lv < MAX_LV && pts >= Total(lv + 1)) lv++; return lv; }
    public static int Level => LevelOf(SAVE.pts);
    /// <summary>Прогресс внутри текущего уровня: (набрано, нужно).</summary>
    public static (long have, long need) Progress()
    {
        int lv = Level; if (lv >= MAX_LV) return (1, 1);
        return (SAVE.pts - Total(lv), Total(lv + 1) - Total(lv));
    }
    /// <summary>Награда за новый уровень: зёрна всегда, каждые 5 уровней — золотые яйца и жетоны, на некоторых — аватарки.</summary>
    public static Reward RewardFor(int lv)
    {
        var r = new Reward { coins = 100 + 25 * lv };
        if (lv % 5 == 0) { r.eggs = 5 + lv / 5 * 2; r.tokens = 40; }
        if (lv % 10 == 0) r.btok = 2;
        r.avatar = lv switch { 3 => "goldegg", 8 => "amethyst", 15 => "fireegg", 25 => "crown", 40 => "rainbow", _ => null };
        return r;
    }
    public static void AddPts(long n)
    {
        if (n <= 0) return;
        SAVE.pts += n;
        int lv = Level;
        while (SAVE.accLv < lv)
        {
            SAVE.accLv++; var r = RewardFor(SAVE.accLv); r.Grant();
            int l = SAVE.accLv; G.Ui.Toast(L.T("Уровень аккаунта ") + l + "! " + r.Text());
            Sfx.win();
        }
    }
    /// <summary>Конец боя: PTS и опыт пропуска за то, что успел сделать (засчитывается и проигрыш).</summary>
    public static (long pts, long xp) MatchEnd(bool won)
    {
        var S = G.S; long k = S.kills;
        long pts = 5 + k / 4 + (S.kind == "endless" ? S.wave * 2 : 0) + (S.kind == "sk" ? S.skLv * 6 : 0) + (won ? 20 : 0);
        long xp = 10 + k / 2 + (S.kind == "endless" ? S.wave * 4 : 0) + (S.kind == "sk" ? S.skLv * 12 : 0) + (won ? 40 : 0);
        pts = Math.Min(pts, 250); xp = Math.Min(xp, 400);
        Stats.AddLocal("games", 1);
        AddPts(pts); Pass.AddXp(xp);
        return (pts, xp);
    }
}

/// <summary>Аватарки профиля: рисуются кодом. Одни открываются героями, другие — уровнем аккаунта, достижениями и пропуском.</summary>
public static class Avatars
{
    public sealed class Av { public string id, nameRu, howRu; public string name => L.T(nameRu); public string how => L.T(howRu); }
    public static readonly Av[] LIST =
    {
        new() { id = "egg", nameRu = "Яйцо", howRu = "Есть с самого начала" },
        new() { id = "hen", nameRu = "Пеструшка", howRu = "Открой героя" },
        new() { id = "chick", nameRu = "Цыпа Шустрик", howRu = "Открой героя" },
        new() { id = "rooster", nameRu = "Петя Шпора", howRu = "Открой героя" },
        new() { id = "duck", nameRu = "Утка Кря", howRu = "Открой героя" },
        new() { id = "goose", nameRu = "Гусь Бронебой", howRu = "Открой героя" },
        new() { id = "quail", nameRu = "Перепёлка Тень", howRu = "Открой героя" },
        new() { id = "guinea", nameRu = "Цесарка Буря", howRu = "Открой героя" },
        new() { id = "nurse", nameRu = "Гусыня Медсестра", howRu = "Открой героя" },
        new() { id = "adren", nameRu = "Боевой Петушок Adrenaline", howRu = "Открой героя" },
        new() { id = "goldegg", nameRu = "Золотое яйцо", howRu = "3-й уровень аккаунта" },
        new() { id = "amethyst", nameRu = "Аметистовое яйцо", howRu = "8-й уровень аккаунта" },
        new() { id = "fireegg", nameRu = "Огненное яйцо", howRu = "15-й уровень аккаунта" },
        new() { id = "crown", nameRu = "Корона", howRu = "25-й уровень аккаунта" },
        new() { id = "rainbow", nameRu = "Радужное яйцо", howRu = "40-й уровень аккаунта" },
        new() { id = "fox", nameRu = "Лис-атаман", howRu = "Достижение «Гроза банды»" },
        new() { id = "piv", nameRu = "Пивчик", howRu = "Достижение «Покоритель подземелий»" },
        new() { id = "skull", nameRu = "Череп босса", howRu = "Достижение «Охотник на боссов»" },
        new() { id = "hamster", nameRu = "Хомяк-Трясун", howRu = "Достижение «Землетрясение»" },
        new() { id = "bp_star", nameRu = "Звезда сезона", howRu = "Бесплатный пропуск, ступень 30" },
        new() { id = "bp_gold", nameRu = "Золотой петух", howRu = "Премиум-пропуск, ступень 10" },
        new() { id = "bp_crown", nameRu = "Королевский пропуск", howRu = "Премиум-пропуск, ступень 30" },
    };
    public static Av Get(string id) => LIST.FirstOrDefault(a => a.id == id);
    public static bool Has(string id) => Get(id) != null;
    /// <summary>Доступна ли аватарка: герои — когда герой открыт, остальные — из списка открытых.</summary>
    public static bool Unlocked(string id) => id == "egg" || (HEROES.ContainsKey(id) ? SAVE.heroes.ContainsKey(id) : SAVE.avatars.Contains(id));

    /// <summary>Нарисовать аватарку в круге диаметром s с левым верхним углом в (x, y).</summary>
    public static void Draw(Ctx c, string id, double x, double y, double s, bool locked = false)
    {
        double r = s / 2, cx = x + r, cy = y + r;
        string bg = id switch
        {
            "goldegg" => "#ffe9a8", "amethyst" => "#e9dcff", "fireegg" => "#ffd2a8", "crown" => "#c0392b", "rainbow" => "#bfe9ff", "fox" => "#5f9e3c", "piv" => "#5a3410",
            "skull" => "#3b3550", "hamster" => "#c9a06a", "bp_star" => "#2f95d0", "bp_gold" => "#8e2a21", "bp_crown" => "#6a3fb5", "egg" => "#9ed46f", _ => "#7cc451",
        };
        c.save();
        c.beginPath(); c.arc(cx, cy, r, 0, Ctx.TAU); c.fillStyle = bg; c.fill();
        c.save(); c.beginPath(); c.arc(cx, cy, r - 1, 0, Ctx.TAU); c.clip();
        var gl = c.createRadialGradient(cx - r * .3, cy - r * .4, 0, cx, cy, r * 1.2); gl.addColorStop(0, "rgba(255,255,255,.35)"); gl.addColorStop(1, "rgba(255,255,255,0)"); c.fillStyle = gl; c.fillRect(x, y, s, s);
        double k = s / 64;
        c.translate(cx, cy); c.scale(k, k);
        if (HEROES.ContainsKey(id))
        { // голова и плечи героя
            var look = G.lookFor(id); var (hx, hy) = G.headOf(look.sp); double hk = 2.1;
            c.translate(-hx * hk - 2, -hy * hk + 2); c.scale(hk, hk);
            G.drawBird(c, new Sprite { face = 1, ang = .4, kind = "player", gun = null }.With(look));
        }
        else DrawIcon(c, id);
        c.restore();
        c.strokeStyle = G.INK; c.lineWidth = Math.Max(2, s * .05); c.beginPath(); c.arc(cx, cy, r - c.lineWidth / 2, 0, Ctx.TAU); c.stroke();
        if (locked)
        {
            c.globalAlpha = .62; c.fillStyle = "#2b1d14"; c.beginPath(); c.arc(cx, cy, r - 1, 0, Ctx.TAU); c.fill(); c.globalAlpha = 1;
            c.save(); c.translate(cx - s * .16, cy - s * .2); UI.K.DrawIcon(c, "lock", s * .32, s * .38); c.restore();
        }
        c.restore();
    }
    static void Egg(Ctx c, string a, string b, string d)
    {
        var g = c.createLinearGradient(-16, -22, 16, 22); g.addColorStop(0, a); g.addColorStop(.5, b); g.addColorStop(1, d);
        c.fillStyle = g; c.strokeStyle = G.INK; c.lineWidth = 3; c.beginPath(); c.ellipse(0, 3, 18, 23, 0, 0, Ctx.TAU); c.fill(); c.stroke();
        c.fillStyle = "rgba(255,255,255,.75)"; c.beginPath(); c.ellipse(-7, -7, 4, 8, -.4, 0, Ctx.TAU); c.fill();
    }
    static void Star(Ctx c, double R, string fill)
    {
        c.fillStyle = fill; c.strokeStyle = G.INK; c.lineWidth = 3; c.beginPath();
        for (int i = 0; i < 10; i++) { double a = -Math.PI / 2 + i * Math.PI / 5, q = i % 2 == 1 ? R * .45 : R; c.lineTo(Math.Cos(a) * q, Math.Sin(a) * q); }
        c.closePath(); c.fill(); c.stroke();
    }
    static void Crown(Ctx c, double y, string fill)
    {
        c.fillStyle = fill; c.strokeStyle = G.INK; c.lineWidth = 3; c.beginPath();
        c.moveTo(-20, y + 10); c.lineTo(-22, y - 10); c.lineTo(-10, y); c.lineTo(0, y - 16); c.lineTo(10, y); c.lineTo(22, y - 10); c.lineTo(20, y + 10); c.closePath(); c.fill(); c.stroke();
        G.circ(c, 0, y - 1, 3.4, "#e8322a"); G.circ(c, -12, y + 3, 2.6, "#4cc3ff"); G.circ(c, 12, y + 3, 2.6, "#4cc3ff");
    }
    static void DrawIcon(Ctx c, string id)
    {
        c.lineJoin = "round"; c.lineCap = "round";
        switch (id)
        {
            case "egg": Egg(c, "#ffffff", "#fffaf0", "#e6dcc6"); c.fillStyle = "#c0392b"; c.fillRect(-17, 0, 34, 6); break;
            case "goldegg": Egg(c, "#fff3b0", "#ffc93a", "#d9861a"); break;
            case "amethyst": Egg(c, "#f6e6ff", "#b46cff", "#5a2aa8"); Crown(c, -22, "#ffc93a"); break;
            case "fireegg":
                foreach (var (col, sc) in new[] { ("#e8322a", 1.0), ("#ff8a1a", .75), ("#ffd23a", .5) })
                { c.fillStyle = col; c.beginPath(); c.moveTo(-22 * sc, 18); c.quadraticCurveTo(-26 * sc, -10, -8 * sc, -30 * sc); c.quadraticCurveTo(-6 * sc, -12, 0, -34 * sc); c.quadraticCurveTo(6 * sc, -12, 8 * sc, -30 * sc); c.quadraticCurveTo(26 * sc, -10, 22 * sc, 18); c.closePath(); c.fill(); }
                Egg(c, "#fff1c2", "#ff8a1a", "#b3261e"); break;
            case "crown": Crown(c, 4, "#ffd23a"); break;
            case "rainbow":
                {
                    var g = c.createLinearGradient(-18, -22, 18, 24);
                    foreach (var (col, at) in new[] { ("#ff5a5a", 0.0), ("#ffc93a", .25), ("#7df05a", .5), ("#4cc3ff", .75), ("#b46cff", 1.0) }) g.addColorStop(at, col);
                    c.fillStyle = g; c.strokeStyle = G.INK; c.lineWidth = 3; c.beginPath(); c.ellipse(0, 3, 18, 23, 0, 0, Ctx.TAU); c.fill(); c.stroke();
                    c.fillStyle = "rgba(255,255,255,.7)"; c.beginPath(); c.ellipse(-7, -7, 4, 8, -.4, 0, Ctx.TAU); c.fill(); break;
                }
            case "fox":
                c.fillStyle = "#e8742a"; c.strokeStyle = G.INK; c.lineWidth = 3;
                c.beginPath(); c.moveTo(-20, -8); c.lineTo(-16, -30); c.lineTo(-4, -16); c.lineTo(4, -16); c.lineTo(16, -30); c.lineTo(20, -8); c.quadraticCurveTo(22, 12, 0, 24); c.quadraticCurveTo(-22, 12, -20, -8); c.closePath(); c.fill(); c.stroke();
                c.fillStyle = "#fff4dc"; c.beginPath(); c.moveTo(-14, 4); c.quadraticCurveTo(0, 2, 14, 4); c.quadraticCurveTo(8, 22, 0, 24); c.quadraticCurveTo(-8, 22, -14, 4); c.fill();
                G.circ(c, -8, -4, 3, G.INK, false); G.circ(c, 8, -4, 3, G.INK, false); G.circ(c, 0, 18, 3.2, G.INK, false);
                c.fillStyle = "#3b3550"; c.fillRect(-14, -14, 28, 5); break; // повязка атамана
            case "piv": Hud.Piv(c, 0, 2, 44); break;
            case "skull":
                c.fillStyle = "#fffaf0"; c.strokeStyle = G.INK; c.lineWidth = 3; c.beginPath(); c.arc(0, -4, 18, Math.PI * .85, Math.PI * 2.15); c.lineTo(10, 16); c.lineTo(-10, 16); c.closePath(); c.fill(); c.stroke();
                G.circ(c, -7, -3, 5, G.INK, false); G.circ(c, 7, -3, 5, G.INK, false); c.fillStyle = G.INK; c.beginPath(); c.moveTo(0, 4); c.lineTo(-3, 9); c.lineTo(3, 9); c.closePath(); c.fill();
                c.strokeStyle = G.INK; c.lineWidth = 2; for (int i = -1; i <= 1; i++) G.seg(c, i * 5, 12, i * 5, 17); break;
            case "hamster": G.drawHamsterFace(c); break;
            case "bp_star": Star(c, 24, "#ffd23a"); break;
            case "bp_gold":
                { var (hx, hy) = G.headOf("rooster"); c.translate(-hx * 2.1 - 2, -hy * 2.1 + 2); c.scale(2.1, 2.1); G.drawBird(c, new Sprite { face = 1, ang = .4, kind = "player" }.With(new Look { sp = "rooster", body = "#f6cd4c", wing = "#d9a21c", tail = "#ff7a1a", head = "#f6cd4c", hat = "crown" })); break; }
            case "bp_crown": Crown(c, -2, "#ffd23a"); Star(c, 9, "#fff4dc"); break;
        }
    }
}

/// <summary>Достижения: открываются сами, награда выдаётся сразу.</summary>
public static class Ach
{
    public sealed class Def { public string id, nameRu, descRu, icon; public Func<long> val; public long goal; public Reward rw; public string name => L.T(nameRu); public string desc => L.T(descRu); }
    static long St(string k) => Stats.Get(k);
    public static readonly Def[] LIST =
    {
        new() { id = "kill1", nameRu = "Первая кровь", descRu = "Победи первого врага", icon = "beak", val = () => St("kills"), goal = 1, rw = new() { coins = 50, pts = 20 } },
        new() { id = "kill500", nameRu = "Гроза двора", descRu = "Победи 500 врагов", icon = "beak", val = () => St("kills"), goal = 500, rw = new() { coins = 400, pts = 60 } },
        new() { id = "kill3000", nameRu = "Гроза банды", descRu = "Победи 3000 врагов", icon = "beak", val = () => St("kills"), goal = 3000, rw = new() { eggs = 15, pts = 150, avatar = "fox" } },
        new() { id = "kill15000", nameRu = "Легенда курятника", descRu = "Победи 15 000 врагов", icon = "beak", val = () => St("kills"), goal = 15000, rw = new() { eggs = 50, pts = 400 } },
        new() { id = "boss1", nameRu = "Сбил фуражку", descRu = "Победи босса", icon = "skull", val = () => St("bosses"), goal = 1, rw = new() { coins = 200, pts = 40 } },
        new() { id = "boss25", nameRu = "Охотник на боссов", descRu = "Победи 25 боссов", icon = "skull", val = () => St("bosses"), goal = 25, rw = new() { eggs = 20, pts = 200, avatar = "skull" } },
        new() { id = "hamster", nameRu = "Землетрясение", descRu = "Победи Хомяка-Трясуна", icon = "skull", val = () => St("kill_hamster"), goal = 1, rw = new() { eggs = 10, pts = 100, avatar = "hamster" } },
        new() { id = "wave10", nameRu = "Держим оборону", descRu = "Дойди до 10-й волны в бесконечном бою", icon = "shield", val = () => St("wave"), goal = 10, rw = new() { coins = 300, pts = 50 } },
        new() { id = "wave25", nameRu = "Стальной забор", descRu = "Дойди до 25-й волны в бесконечном бою", icon = "shield", val = () => St("wave"), goal = 25, rw = new() { eggs = 15, pts = 150 } },
        new() { id = "wave50", nameRu = "Неприступный курятник", descRu = "Дойди до 50-й волны в бесконечном бою", icon = "shield", val = () => St("wave"), goal = 50, rw = new() { eggs = 50, pts = 400 } },
        new() { id = "story1", nameRu = "Начало пути", descRu = "Пройди первый уровень сюжета", icon = "star", val = () => SAVE.story.done.Count, goal = 1, rw = new() { coins = 100, pts = 20 } },
        new() { id = "story20", nameRu = "Курятник спасён", descRu = "Пройди первую главу сюжета", icon = "star", val = () => SAVE.story.done.Count(i => i < 20), goal = 20, rw = new() { eggs = 10, pts = 120 } },
        new() { id = "story80", nameRu = "Конец Императора", descRu = "Пройди все 80 уровней сюжета", icon = "star", val = () => SAVE.story.done.Count, goal = 80, rw = new() { eggs = 60, pts = 500 } },
        new() { id = "sk5", nameRu = "Лесной следопыт", descRu = "Дойди до 0-5 в режиме Soul Knight", icon = "sword", val = () => St("sk_depth"), goal = 5, rw = new() { coins = 400, pts = 80 } },
        new() { id = "skrun", nameRu = "Покоритель подземелий", descRu = "Пройди оба мира Soul Knight", icon = "sword", val = () => St("sk_runs"), goal = 1, rw = new() { eggs = 20, pts = 250, avatar = "piv" } },
        new() { id = "skrooms", nameRu = "Чистильщик", descRu = "Зачисти 100 комнат в Soul Knight", icon = "sword", val = () => St("sk_rooms"), goal = 100, rw = new() { eggs = 10, pts = 120 } },
        new() { id = "piv", nameRu = "Копилка", descRu = "Собери 1000 пивчиков", icon = "piv", val = () => St("piv"), goal = 1000, rw = new() { eggs = 10, pts = 100 } },
        new() { id = "chest10", nameRu = "Кладоискатель", descRu = "Открой 10 сундуков", icon = "bag", val = () => St("chests"), goal = 10, rw = new() { coins = 300, pts = 50 } },
        new() { id = "chest100", nameRu = "Сундучных дел мастер", descRu = "Открой 100 сундуков", icon = "bag", val = () => St("chests"), goal = 100, rw = new() { eggs = 25, pts = 200 } },
        new() { id = "ult50", nameRu = "Суперсила", descRu = "Примени ульту 50 раз", icon = "star", val = () => St("ults"), goal = 50, rw = new() { coins = 300, pts = 60 } },
        new() { id = "ult500", nameRu = "Мастер ульт", descRu = "Примени ульту 500 раз", icon = "star", val = () => St("ults"), goal = 500, rw = new() { eggs = 20, pts = 200 } },
        new() { id = "crates", nameRu = "Щепки летят", descRu = "Разбей 100 ящиков", icon = "bag", val = () => St("crates"), goal = 100, rw = new() { coins = 250, pts = 50 } },
        new() { id = "heroes", nameRu = "Весь курятник в сборе", descRu = "Открой всех героев", icon = "heart", val = () => SAVE.heroes.Count, goal = HERO_IDS.Length, rw = new() { eggs = 40, pts = 300 } },
        new() { id = "hero10", nameRu = "Прокачанный", descRu = "Подними любого героя до 10-го уровня", icon = "heart", val = () => SAVE.heroes.Values.Max(h => h.lv), goal = 10, rw = new() { eggs = 15, pts = 150 } },
        new() { id = "quests", nameRu = "Исполнительный", descRu = "Выполни 20 заданий", icon = "clover", val = () => St("quests"), goal = 20, rw = new() { eggs = 15, pts = 150 } },
        new() { id = "pass", nameRu = "Сезонный боец", descRu = "Дойди до 15-й ступени боевого пропуска", icon = "star", val = () => Pass.Tier, goal = 15, rw = new() { eggs = 10, pts = 100 } },
        new() { id = "mp", nameRu = "Вместе веселее", descRu = "Сыграй в многопользовательскую игру", icon = "heart", val = () => St("mp_games"), goal = 1, rw = new() { coins = 300, pts = 50 } },
        new() { id = "lv10", nameRu = "Опытный", descRu = "Дойди до 10-го уровня аккаунта", icon = "star", val = () => Account.Level, goal = 10, rw = new() { eggs = 10 } },
    };
    public static bool Done(string id) => SAVE.ach.ContainsKey(id);
    static bool checking;
    /// <summary>Проверить все достижения: новые открываются, награда выдаётся сразу, всплывает тост.</summary>
    public static void Check()
    {
        if (SAVE == null || checking) return; checking = true;
        try
        {
            foreach (var a in LIST)
            {
                if (Done(a.id)) continue;
                long v; try { v = a.val(); } catch { continue; }
                if (v < a.goal) continue;
                SAVE.ach[a.id] = 2; a.rw.Grant(); Sfx.win();
                G.Ui.Toast(L.T("Достижение: ") + a.name + " · " + a.rw.Text());
            }
        }
        finally { checking = false; }
    }
}

/// <summary>Задания: три ежедневных (новые каждый день в 00:00 UTC) и одно недельное (в понедельник в 00:00 UTC).</summary>
public static class Quests
{
    public sealed class Def { public string id, textRu, stat, icon; public long[] goals; public bool max, weekly; public string Text(long n) => L.F(textRu, n); }
    public static readonly Def[] LIST =
    {
        new() { id = "kills", textRu = "Победи {0} врагов", stat = "kills", goals = new long[] { 120, 150, 200 }, icon = "beak" },
        new() { id = "bosses", textRu = "Победи боссов: {0}", stat = "bosses", goals = new long[] { 2, 3 }, icon = "skull" },
        new() { id = "story", textRu = "Пройди уровни сюжета: {0}", stat = "story_wins", goals = new long[] { 2, 3 }, icon = "star" },
        new() { id = "wave", textRu = "Дойди до {0}-й волны в бесконечном бою", stat = "wave", goals = new long[] { 8, 10, 12 }, max = true, icon = "shield" },
        new() { id = "skrooms", textRu = "Зачисти комнаты в Soul Knight: {0}", stat = "sk_rooms", goals = new long[] { 6, 8, 10 }, icon = "sword" },
        new() { id = "ults", textRu = "Примени ульту {0} раз", stat = "ults", goals = new long[] { 8, 10, 12 }, icon = "star" },
        new() { id = "chests", textRu = "Открой сундуки: {0}", stat = "chests", goals = new long[] { 2, 3 }, icon = "bag" },
        new() { id = "piv", textRu = "Собери {0} пивчиков", stat = "piv", goals = new long[] { 40, 60, 80 }, icon = "piv" },
        new() { id = "crates", textRu = "Разбей {0} ящиков", stat = "crates", goals = new long[] { 12, 15, 20 }, icon = "bag" },
        new() { id = "games", textRu = "Сыграй боёв: {0}", stat = "games", goals = new long[] { 3, 4 }, icon = "heart" },
        new() { id = "fox", textRu = "Победи {0} лис", stat = "kill_fox", goals = new long[] { 30, 40 }, icon = "beak" },
        new() { id = "crow", textRu = "Сбей {0} ворон", stat = "kill_crow", goals = new long[] { 20, 30 }, icon = "beak" },
        new() { id = "rat", textRu = "Победи {0} мышей и крыс", stat = "kill_rat", goals = new long[] { 40, 60 }, icon = "beak" },
        // недельные
        new() { id = "w_kills", textRu = "Победи {0} врагов за неделю", stat = "kills", goals = new long[] { 1200, 1500 }, weekly = true, icon = "beak" },
        new() { id = "w_story", textRu = "Пройди уровни сюжета: {0}", stat = "story_wins", goals = new long[] { 10, 12 }, weekly = true, icon = "star" },
        new() { id = "w_sk", textRu = "Пройди уровни Soul Knight: {0}", stat = "sk_levels", goals = new long[] { 8, 10 }, weekly = true, icon = "sword" },
        new() { id = "w_bosses", textRu = "Победи боссов: {0}", stat = "bosses", goals = new long[] { 12, 15 }, weekly = true, icon = "skull" },
        new() { id = "w_wave", textRu = "Дойди до {0}-й волны в бесконечном бою", stat = "wave", goals = new long[] { 20, 25 }, max = true, weekly = true, icon = "shield" },
        new() { id = "w_ults", textRu = "Примени ульту {0} раз", stat = "ults", goals = new long[] { 60, 80 }, weekly = true, icon = "star" },
    };
    public static Def Find(string id) => LIST.FirstOrDefault(q => q.id == id);
    public static Reward DailyReward => new() { coins = 120, tokens = 20, xp = 60, pts = 25 };
    public static Reward WeeklyReward => new() { coins = 600, eggs = 10, btok = 1, xp = 300, pts = 120 };
    public static string Day => DateTime.UtcNow.ToString("yyyy-MM-dd");
    /// <summary>Понедельник текущей недели (UTC) — по нему меняется недельное задание.</summary>
    public static string Week { get { var d = DateTime.UtcNow.Date; int back = ((int)d.DayOfWeek + 6) % 7; return d.AddDays(-back).ToString("yyyy-MM-dd"); } }
    public static TimeSpan DayLeft => DateTime.UtcNow.Date.AddDays(1) - DateTime.UtcNow;
    public static TimeSpan WeekLeft { get { var d = DateTime.UtcNow.Date; int back = ((int)d.DayOfWeek + 6) % 7; return d.AddDays(7 - back) - DateTime.UtcNow; } }
    static QuestRec Make(Def d) => new() { id = d.id, goal = d.goals[(int)(G.rnd() * d.goals.Length)] };
    /// <summary>Новый день или неделя по UTC — выдать новые задания.</summary>
    public static void Ensure()
    {
        if (SAVE == null) return; var Q = SAVE.quests;
        if (Q.day != Day)
        {
            Q.day = Day; var pool = LIST.Where(d => !d.weekly).ToList(); Q.daily = new();
            while (Q.daily.Count < 3 && pool.Count > 0) { int i = (int)(G.rnd() * pool.Count); Q.daily.Add(Make(pool[i])); pool.RemoveAt(i); }
            persist();
        }
        if (Q.week != Week || Q.weekly == null)
        {
            Q.week = Week; var pool = LIST.Where(d => d.weekly).ToList(); Q.weekly = Make(pool[(int)(G.rnd() * pool.Count)]); persist();
        }
    }
    public static IEnumerable<QuestRec> All() { Ensure(); foreach (var q in SAVE.quests.daily) yield return q; if (SAVE.quests.weekly != null) yield return SAVE.quests.weekly; }
    public static bool Ready(QuestRec q) => !q.claimed && q.prog >= q.goal;
    public static int ReadyCount() => SAVE == null ? 0 : All().Count(Ready);
    public static void OnStat(string key, long n, bool max)
    {
        if (SAVE?.quests == null) return;
        foreach (var q in SAVE.quests.daily.Append(SAVE.quests.weekly))
        {
            if (q == null || q.claimed || q.prog >= q.goal) continue; var d = Find(q.id); if (d == null || d.stat != key) continue;
            q.prog = max ? Math.Max(q.prog, n) : q.prog + n;
            if (q.prog >= q.goal) { q.prog = q.goal; G.Ui.Toast(L.T("Задание выполнено: ") + d.Text(q.goal) + L.T(" · забери награду в «Заданиях»")); Sfx.pick(); }
        }
    }
    public static Reward RewardOf(QuestRec q) => Find(q.id)?.weekly == true ? WeeklyReward : DailyReward;
    public static bool Claim(QuestRec q)
    {
        if (!Ready(q)) return false;
        q.claimed = true; RewardOf(q).Grant(); Stats.AddLocal("quests", 1); persist(); return true;
    }
}

/// <summary>Боевой пропуск: сезон — календарный месяц по UTC, 30 ступеней. Бесплатная линейка — всем, премиум — за 299 золотых яиц.</summary>
public static class Pass
{
    public const int TIERS = 30, XP_PER = 250, PRICE = 299;
    public static string Season => DateTime.UtcNow.ToString("yyyy-MM");
    public static TimeSpan Left { get { var n = DateTime.UtcNow; return new DateTime(n.Year, n.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1) - n; } }
    /// <summary>Номер сезона для подписи: «Сезон 10 · октябрь».</summary>
    public static int SeasonNo { get { var n = DateTime.UtcNow; return (n.Year - 2026) * 12 + n.Month - 9; } }
    public static void Ensure()
    {
        if (SAVE?.pass == null) return;
        if (SAVE.pass.season != Season) { SAVE.pass = new PassRec { season = Season }; persist(); }
    }
    public static int Tier { get { if (SAVE?.pass == null) return 0; Ensure(); return (int)Math.Min(TIERS, SAVE.pass.xp / XP_PER); } }
    public static void AddXp(long n)
    {
        if (n <= 0 || SAVE?.pass == null) return; Ensure();
        int before = Tier; SAVE.pass.xp += n; int after = Tier;
        if (after > before) G.Ui.Toast(L.T("Боевой пропуск: ступень ") + after + "!");
    }
    public static Reward Free(int t)
    {
        if (t == TIERS) return new() { avatar = "bp_star", eggs = 10 };
        if (t == 15) return new() { sch = 1 };
        if (t % 5 == 0) return new() { btok = 1, eggs = 3 };
        if (t % 2 == 0) return new() { tokens = 40 };
        return new() { coins = 150 + 10 * t };
    }
    public static Reward Prem(int t)
    {
        if (t == TIERS) return new() { avatar = "bp_crown", lch = 1 };
        if (t == 20) return new() { lch = 1 };
        if (t == 10) return new() { avatar = "bp_gold", eggs = 15 };
        if (t % 5 == 0) return new() { eggs = 15 };
        if (t % 3 == 0) return new() { btok = 2 };
        return new() { coins = 300 + 20 * t };
    }
    public static bool CanClaim(int t, bool prem) => t <= Tier && (prem ? SAVE.pass.premium && !SAVE.pass.prem.Contains(t) : !SAVE.pass.free.Contains(t));
    public static int ReadyCount() { if (SAVE?.pass == null) return 0; int n = 0; for (int t = 1; t <= Tier; t++) { if (CanClaim(t, false)) n++; if (CanClaim(t, true)) n++; } return n; }
    public static bool Claim(int t, bool prem)
    {
        if (!CanClaim(t, prem)) return false;
        (prem ? SAVE.pass.prem : SAVE.pass.free).Add(t); (prem ? Prem(t) : Free(t)).Grant(); persist(); return true;
    }
    public static bool BuyPremium()
    {
        Ensure(); if (SAVE.pass.premium || SAVE.eggs < PRICE) return false;
        SAVE.eggs -= PRICE; SAVE.pass.premium = true; persist(); return true;
    }
}
