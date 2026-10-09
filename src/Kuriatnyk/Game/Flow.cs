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
}
public sealed class NullUi : IGameUi
{
    public void Toast(string msg) { } public void ToastClear() { } public void OnGameStart() { } public void ShowPause(bool show) { }
    public void ShowOver(OverInfo info) { } public void ShowWin(WinInfo info) { } public void OnMenu() { } public void RefreshMenu() { }
}
public sealed class OverInfo { public string lead, earnCoins, earnTokens, waveLabel; public long score; public int kills, wave; public bool record; }
public sealed class WinRow { public string icon, text, sub; }
public sealed class WinInfo { public string eyebrow, lead, nextLabel; public bool hasNext; public List<WinRow> rows = new(); }

// Интерфейс боя: баннер, пауза, победа и поражение (порт web/js/hud.js).
public static partial class G
{
    public static IGameUi Ui = new NullUi();

    // баннер «Волна N» — рисуется поверх поля (анимация 2.1 с, как в CSS)
    public static string banBig = "", banSmall = ""; public static double banT = -99;
    public static double Clock => clock;
    public static void showBanner(string big, string small) { banBig = big ?? ""; banSmall = small ?? ""; banT = clock; }
    public static void clearBanner() { banBig = ""; banSmall = ""; banT = -99; }

    public static string goalLabel(bool lng)
    {
        if (S.kind == "endless") return T("Волна ") + Math.Max(1, S.wave);
        var g = S.L.goal;
        if (g.type == "waves") return T("Волна ") + Math.Max(1, S.wave) + "/" + g.n;
        if (g.type == "kill") { var w = killN(g.what) ?? ETYPE[g.what].name; return lng ? T("Победи: ") + w.ToLower(L.Culture) + " ×" + g.n : w + " " + S.goalKills + "/" + g.n; }
        if (g.type == "survive") { int t = (int)Math.Ceiling(S.survT); return (lng ? T("Продержись ") : T("Держись ")) + t / 60 + ":" + (t % 60).ToString("00"); }
        if (g.type == "boss") return lng ? T("Победи босса") : T("Босс: ") + ETYPE[g.boss].name;
        return "";
    }

    public static void toMenu()
    {
        if (S.mode is "play" or "paused" or "dying") bankCoins();
        S.mode = "menu"; S.night = false; Music.Play("menu"); Music.Duck(1); clearBanner();
        if (MAP != menuMap() || ground == null) buildWorld(menuMap());
        P = newPlayer(); makeAllies(); enemies = new(); bullets = new(); nades = new(); bombs = new(); pickups = new(); texts = new(); parts = new(); holes = new(); zones = new(); strikes = new(); puddles = new();
        bossBarOn = false; S.boss = null;
        Ui.OnMenu();
    }
    /// <summary>После покупки/смены героя в меню — пересобрать героя и отряд на сцене.</summary>
    public static void RebuildMenuHero()
    {
        if (S.mode != "menu" || P == null) return;
        double x = P.x, y = P.y; P = newPlayer(); P.x = x; P.y = y; makeAllies();
    }
    public static void pauseGame() { if (S.mode != "play") return; S.mode = "paused"; Ui.ShowPause(true); Music.Duck(.35); sticks.move = sticks.aim = null; input.firing = false; }
    public static void resumeGame() { if (S.mode != "paused") return; S.mode = "play"; Ui.ShowPause(false); Music.Duck(1); }
    static void gameOver()
    {
        S.mode = "over";
        bankCoins();
        bool rec = false;
        if (S.kind == "endless") { rec = S.score > SAVE.best.score; if (rec) { SAVE.best = new Best { score = S.score, wave = S.wave }; persist(); } }
        var info = new OverInfo { score = S.score, kills = S.kills, record = rec, earnCoins = S.coins.ToString(), earnTokens = S.tokens.ToString() };
        if (S.kind == "story") { info.wave = S.lvIdx + 1; info.waveLabel = T("Уровень"); info.lead = T("Уровень «") + S.L.name + T("» не пройден. Попробуй ещё раз!"); }
        else { info.wave = S.wave; info.waveLabel = T("Волна"); info.lead = T("Курятник пал на волне ") + S.wave + "."; }
        Ui.ShowOver(info);
    }
    public static void victory()
    {
        if (S.mode != "play") return;
        S.mode = "won"; S.winT = 1.4; Sfx.win(); P.buff = null; P.inv = 99;
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
        if (first) SAVE.story.done.Add(i);
        persist();
        var w = new WinInfo
        {
            eyebrow = T("Глава ") + (Lv.ch + 1) + T(" · уровень ") + (Lv.i + 1) + T(" из 20"),
            lead = Lv.i == 19 && first ? CHAPTERS[Lv.ch].outro : (L.En ? "“" : "«") + Lv.name + T("» пройден."),
            hasNext = i < STORY.Count - 1,
            nextLabel = Lv.i == 19 ? T("Глава ") + (Lv.ch + 2) : T("Уровень ") + (Lv.i + 2)
        };
        w.rows.Add(new WinRow { icon = "coin", text = "+" + (lc + sc) + T(" зёрен"), sub = first ? T("награда за первое прохождение") : T("повторное прохождение") });
        w.rows.Add(new WinRow { icon = "tok", text = "+" + (lt + st) + T(" жетонов"), sub = T("на обычный сундук: ") + Math.Min(SAVE.tokens, 100) + "/100" });
        if (le > 0) w.rows.Add(new WinRow { icon = "gegg", text = "+" + le + T(" золотых яиц"), sub = T("только за первое прохождение") });
        if (sup) w.rows.Add(new WinRow { icon = "sch", text = StripIcon(T("<i class=\"sch\"></i>Сверхбольшой сундук!")), sub = T("за финального босса главы · открой в «Сундуках»") });
        if (big) w.rows.Add(new WinRow { icon = "btok", text = StripIcon(T("<i class=\"btok\"></i>+1 большой жетон")), sub = T("на большой сундук: ") + SAVE.btokens + "/10" });
        else w.rows.Add(new WinRow { icon = "btok", text = StripIcon(T("<i class=\"btok\"></i>Большой жетон — за первое прохождение")), sub = T("или каждые 10 волн в бесконечном бою") });
        if (Lv.i == 19 && first) storyCh = -1;
        Ui.ShowWin(w);
    }
    /// <summary>Убрать HTML-иконки вида &lt;i class="coin"&gt;&lt;/i&gt; из переведённых строк — иконки рисует интерфейс.</summary>
    public static string StripIcon(string s) => System.Text.RegularExpressions.Regex.Replace(s ?? "", "<[^>]+>", "");
}
