using System;
using System.Collections.Generic;
using System.Linq;
using static Kuriatnyk.D;
using static Kuriatnyk.Store;

namespace Kuriatnyk;

/// <summary>Что игра просит у интерфейса (экраны, тосты). Реализует слой Avalonia.</summary>
public interface IGameUi
{
    void Toast(string msg);
    void ToastClear();
    void OnGameStart();
    void ShowPause(bool show);
    void ShowOver(OverInfo info);
    void ShowWin(WinInfo info);
    void OnMenu();
    void RefreshMenu();
    void ShowSkPick(List<string> passives); // Soul Knight: выбор пассивки в портале
    void ShowMpLobby(); // сетевая игра: окно лобби
}
public sealed class NullUi : IGameUi
{
    public void Toast(string msg) { } public void ToastClear() { } public void OnGameStart() { } public void ShowPause(bool show) { }
    public void ShowOver(OverInfo info) { } public void ShowWin(WinInfo info) { } public void OnMenu() { } public void RefreshMenu() { }
    public void ShowSkPick(List<string> passives) => G.skTakePassive(passives[0]); // без интерфейса (Snap) — первая же
    public void ShowMpLobby() { }
}
public sealed class OverInfo { public string lead, earnCoins, earnTokens, waveLabel, waveText; public long score, pts, xp; public int kills, wave; public bool record; }
public sealed class WinRow { public string icon, text, sub; }
public sealed class WinInfo { public string eyebrow, lead, nextLabel; public bool hasNext; public List<WinRow> rows = new(); }

// Интерфейс боя: баннер, пауза, победа и поражение (порт web/js/hud.js).
public static partial class G
{
    public static IGameUi Ui = new NullUi();

    // баннер «Волна N» — рисуется поверх поля (анимация 2.1 с, как в CSS)
    public static string banBig = "", banSmall = ""; public static double banT = -99;
    public static double Clock => clock;
    public static void showBanner(string big, string small) { banBig = big ?? ""; banSmall = small ?? ""; banT = clock; recBanner(banBig, banSmall); }
    public static void clearBanner() { banBig = ""; banSmall = ""; banT = -99; }

    /// <summary>Уровень сюжета длится не меньше трёх минут; босс выходит через 1:15 после начала.</summary>
    public const double STORY_MIN = 180, BOSS_AT = 75;
    /// <summary>Секунды в виде «3:05».</summary>
    public static string mmss(double sec) { int t = (int)Math.Ceiling(Math.Max(0, sec)); return t / 60 + ":" + (t % 60).ToString("00"); }

    public static string goalLabel(bool lng)
    {
        if (S.kind == "endless") return T("Волна ") + Math.Max(1, S.wave);
        if (S.kind == "sk") return skGoal();
        var g = S.L.goal;
        if (S.goalDone && !lng) return T("Держись ") + mmss(STORY_MIN - S.t); // цель выполнена — дотянуть до трёх минут
        if (g.type == "waves") return T("Волна ") + Math.Max(1, S.wave) + "/" + g.n;
        if (g.type == "kill") { var w = killN(g.what) ?? ETYPE[g.what].name; return lng ? T("Победи: ") + w.ToLower(L.Culture) + " ×" + g.n : w + " " + S.goalKills + "/" + g.n; }
        if (g.type == "survive") return (lng ? T("Продержись ") : T("Держись ")) + mmss(S.survT);
        if (g.type == "boss") return lng ? T("Победи босса") : S.boss == null && S.bossT > 0 ? T("Босс через ") + mmss(S.bossT) : T("Босс: ") + ETYPE[g.boss].name;
        return "";
    }
    /// <summary>Цель уровня сюжета выполнена. Если трёх минут ещё нет — держимся до конца, враги продолжают идти.</summary>
    public static void storyGoal()
    {
        if (S.mode != "play") return;
        if (S.t >= STORY_MIN) { victory(); return; }
        if (S.goalDone) return;
        S.goalDone = true; Sfx.wave();
        showBanner(T("Цель выполнена!"), T("Продержись до конца уровня: ещё ") + mmss(STORY_MIN - S.t));
    }

    public static void toMenu()
    {
        if (S.mode is "play" or "paused" or "dying" or "pick") bankCoins();
        IsClientWorld = false; PartList.Rec = false; Sfx.Rec = false;
        Mp.BackToLobby(); // хост вышел из боя — все в лобби
        S.mode = "menu"; S.night = false; Music.Play("menu"); Music.Duck(1); clearBanner();
        bool wasDun = DUN != null; DUN = null;
        if (MAP != menuMap() || ground == null || wasDun || WW != MAP_SIZE) buildWorld(menuMap());
        OBS.RemoveAll(o => o.type == "crate");
        P = newPlayer(); solo(); makeAllies(); enemies = new(); bullets = new(); nades = new(); bombs = new(); pickups = new(); texts = new(); parts = new(); holes = new(); zones = new(); strikes = new(); puddles = new(); goo = new(); quakes = new(); MpHp = 1;
        bossBarOn = false; S.boss = null;
        Ui.OnMenu();
        if (Net.role != Net.Role.Off && Mp.state == "lobby") Ui.ShowMpLobby();
    }
    /// <summary>После покупки/смены героя в меню — пересобрать героя и отряд на сцене.</summary>
    public static void RebuildMenuHero()
    {
        if (S.mode != "menu" || P == null) return;
        double x = P.x, y = P.y; P = newPlayer(); solo(); P.x = x; P.y = y; makeAllies();
    }
    public static void pauseGame()
    {
        if (S.mode != "play") return;
        if (Mp.InMatch) { Ui.ShowPause(true); sticks.move = sticks.aim = null; input.firing = false; return; } // в сетевой игре бой не останавливается
        S.mode = "paused"; Ui.ShowPause(true); Music.Duck(.35); sticks.move = sticks.aim = null; input.firing = false;
    }
    public static void resumeGame()
    {
        if (Mp.InMatch) { Ui.ShowPause(false); return; }
        if (S.mode != "paused") return; S.mode = "play"; Ui.ShowPause(false); Music.Duck(1);
    }
    static void gameOver()
    {
        S.mode = "over";
        if (S.kind == "endless" && S.wave > 1) { S.coins += S.wave * 15; S.tokens += S.wave * 2; } // бонус за достигнутую волну
        bankCoins();
        bool rec = false;
        if (S.kind == "endless") { rec = S.score > SAVE.best.score; if (rec) { SAVE.best = new Best { score = S.score, wave = S.wave }; persist(); } }
        var info = new OverInfo { score = S.score, kills = S.kills, record = rec, earnCoins = S.coins.ToString(), earnTokens = S.tokens.ToString(), pts = S.lastPts, xp = S.lastXp };
        if (S.kind == "sk") { info.waveText = skLvName(S.skLv); info.waveLabel = T("Уровень"); info.lead = T("Забег окончен на уровне ") + skLvName(S.skLv) + T(". Попробуй ещё раз!"); }
        else if (S.kind == "story") { info.wave = S.lvIdx + 1; info.waveLabel = T("Уровень"); info.lead = T("Уровень «") + S.L.name + T("» не пройден. Попробуй ещё раз!"); }
        else { info.wave = S.wave; info.waveLabel = T("Волна"); info.lead = T("Курятник пал на волне ") + S.wave + "."; }
        Mp.SendEnd(false, info.lead, info.waveLabel, info.waveText ?? info.wave.ToString());
        Ui.ShowOver(info);
    }
    public static void victory()
    {
        if (S.mode != "play") return;
        S.mode = "won"; S.winT = 1.4; Sfx.win(); foreach (var pl in players) { pl.buff = null; pl.inv = 99; } S.won = true; Stats.AddTeam("story_wins");
        foreach (var e in enemies) if (!e.dead) { puff(e.x, e.y, "#ffffff", 6, 1); e.dead = true; }
        S.cleanup = true;
        foreach (var h in holes) if (h.state != "close") { h.state = "close"; h.ct = 0; }
        bossBarOn = false; S.boss = null;
        showBanner(T("Победа!"), S.L.name); sticks.move = sticks.aim = null; input.firing = false;
    }
    public static int storyCh = -1;
    static void showWin()
    {
        S.mode = "result";
        int i = S.lvIdx; var Lv = S.L; bool first = !SAVE.story.done.Contains(i);
        long lc = first ? Lv.coins : (long)Math.Round(Lv.coins * .4, MidpointRounding.AwayFromZero), lt = first ? Lv.tokens : (long)Math.Round(Lv.tokens * .5, MidpointRounding.AwayFromZero);
        long sc = S.coins, st = S.tokens;
        bankCoins();
        SAVE.coins += lc; SAVE.tokens += lt; int le = first ? Lv.eggs : 0; SAVE.eggs += le;
        bool big = first; if (big) SAVE.btokens += 1; // большой жетон — за каждое первое прохождение уровня
        bool sup = first && Lv.i == 19; if (sup) SAVE.schests++;
        bool lch = sup && Lv.ch == CHAPTERS.Count - 1; if (lch) SAVE.lchests++; // за Императора Ворона — ещё и легендарный сундук
        if (first) SAVE.story.done.Add(i);
        persist();
        var w = new WinInfo
        {
            eyebrow = T("Глава ") + (Lv.ch + 1) + T(" · уровень ") + (Lv.i + 1) + T(" из 20"),
            lead = Lv.i == 19 && first ? CHAPTERS[Lv.ch].outro : L.Qo + Lv.name + T("» пройден."),
            hasNext = i < STORY.Count - 1,
            nextLabel = Lv.i == 19 ? T("Глава ") + (Lv.ch + 2) : T("Уровень ") + (Lv.i + 2)
        };
        w.rows.Add(new WinRow { icon = "coin", text = "+" + (lc + sc) + T(" зёрен"), sub = first ? T("награда за первое прохождение") : T("повторное прохождение") });
        w.rows.Add(new WinRow { icon = "tok", text = "+" + (lt + st) + T(" жетонов"), sub = T("на обычный сундук: ") + L.Fmt(SAVE.tokens) + "/100" });
        if (le > 0) w.rows.Add(new WinRow { icon = "gegg", text = "+" + le + T(" золотых яиц"), sub = T("только за первое прохождение") });
        if (lch) w.rows.Add(new WinRow { icon = "lch", text = T("Легендарный сундук!"), sub = T("за победу над Императором Вороном · открой в «Сундуках»") });
        if (sup) w.rows.Add(new WinRow { icon = "sch", text = StripIcon(T("<i class=\"sch\"></i>Сверхбольшой сундук!")), sub = T("за финального босса главы · открой в «Сундуках»") });
        if (big) w.rows.Add(new WinRow { icon = "btok", text = StripIcon(T("<i class=\"btok\"></i>+1 большой жетон")), sub = T("на большой сундук: ") + SAVE.btokens + "/10" });
        else w.rows.Add(new WinRow { icon = "btok", text = StripIcon(T("<i class=\"btok\"></i>Большой жетон — за первое прохождение")), sub = T("или каждые 5 волн в бесконечном бою") });
        if (Lv.i == 19 && first) storyCh = -1;
        AddMatchRow(w);
        Mp.SendEnd(true, w.lead, null, null);
        Ui.ShowWin(w);
    }
    /// <summary>Строка «+PTS · опыт пропуска» в окне победы.</summary>
    public static void AddMatchRow(WinInfo w)
    {
        if (S.lastPts <= 0 && S.lastXp <= 0) return;
        w.rows.Add(new WinRow { icon = "pts", text = "+" + S.lastPts + " PTS · +" + S.lastXp + T(" опыта пропуска"), sub = T("уровень аккаунта ") + Account.Level + T(" · пропуск: ступень ") + Pass.Tier + "/" + Pass.TIERS });
    }
    /// <summary>Убрать HTML-иконки вида &lt;i class="coin"&gt;&lt;/i&gt; из переведённых строк — иконки рисует интерфейс.</summary>
    public static string StripIcon(string s) => System.Text.RegularExpressions.Regex.Replace(s ?? "", "<[^>]+>", "");
}
