using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using static Kuriatnyk.D;
using static Kuriatnyk.Store;
using static Kuriatnyk.UI.K;

namespace Kuriatnyk.UI;

/// <summary>Окно многопользовательской игры: локальный сервер (одна сеть или Radmin VPN) и закрытый пока сетевой сервер.</summary>
public static partial class Overlays
{
    static bool mpHooked; static string mpView = "choose"; static List<Net.Found> mpFound; static bool mpSearching;
    public static void OpenMultiplayer(MainView mv, string view)
    {
        mpView = view; OpenMultiplayer(mv);
    }
    public static void OpenMultiplayer(MainView mv)
    {
        Audio.Init();
        if (!mpHooked) { mpHooked = true; Mp.Changed += () => { if (mv.Shown("mpOv")) mv.Show("mpOv", Multiplayer(mv), 8); }; }
        if (Mp.state != "idle") mpView = "lobby"; else if (mpView == "lobby") mpView = "choose";
        mv.Show("mpOv", Multiplayer(mv), 8);
    }
    static void MpRefresh(MainView mv) { if (mv.Shown("mpOv")) mv.Show("mpOv", Multiplayer(mv), 8); }

    static Control Multiplayer(MainView mv)
    {
        var kids = new List<Control> { Eyebrow(T("Многопользовательская игра")) };
        if (Mp.state == "connecting") { kids.Add(Logo(T("Подключение"), LogoSize(mv))); kids.Add(Lead(Mp.status)); kids.Add(Btn(T("Отмена"), () => { Mp.Leave(); mpView = "local"; MpRefresh(mv); }, "alt")); }
        else if (Mp.state is "lobby" or "game") kids.AddRange(MpLobby(mv));
        else if (mpView == "local") kids.AddRange(MpLocal(mv));
        else kids.AddRange(MpChoose(mv));
        var card = Box(Col(12, kids.ToArray()), PaperB, 4, 24, new Thickness(18, 18, 18, 16), 8); card.MaxWidth = 560;
        return Ov(mv, card, true);
    }

    /// <summary>Первый экран: локальный сервер или сетевой (наш) — он пока закрыт.</summary>
    static IEnumerable<Control> MpChoose(MainView mv)
    {
        yield return Logo(T("Где играем?"), LogoSize(mv));
        Press big(string title, string sub, string bg, Action click, bool locked)
        {
            var col = Col(3, Txt(title, 18, FontWeight.Black, locked ? B("#6b5a48") : InkB), Txt(sub, 12.5, FontWeight.SemiBold, B("#b32b1d14")));
            Control body = col;
            if (locked)
            {
                var soon = Box(Txt(T("Скоро").ToUpper(L.Culture), 10.5, FontWeight.ExtraBold, B("#f6e3b8"), wrap: false), B("#6b5a48"), 0, 6, new Thickness(7, 2)); soon.HorizontalAlignment = HorizontalAlignment.Left;
                col.Children.Add(soon);
                var ic = Icon("lock", 20); ic.Width = 20; ic.Height = 22; ic.Margin = new Thickness(0, 0, 12, 0);
                var d = new DockPanel(); DockPanel.SetDock(ic, Dock.Left); d.Children.Add(ic); d.Children.Add(col); body = d;
            }
            var p = new Press(body, locked ? B(Poor) : B(bg), 3, 16, new Thickness(16, 12), 4, locked ? B("#6b5a48") : null) { MinHeight = 64 };
            p.Click += click; return p;
        }
        yield return big(T("Локальный сервер"), T("Одна сеть: Wi-Fi, кабель или Radmin VPN / Hamachi. Один игрок создаёт игру, остальные подключаются"), "#d7f0ff", () => { mpView = "local"; MpRefresh(mv); }, false);
        yield return big(T("Сетевой сервер"), T("Игра через наши серверы — их пока нет"), "#e9dcff", () => { mv.Toast(T("Наших серверов пока нет — играй по локальной сети")); Sfx.hurt(); }, true);
        yield return Btn(T("Назад"), () => mv.Hide("mpOv"), "alt");
    }

    /// <summary>Локальная сеть: создать игру, найти игры рядом или подключиться по адресу.</summary>
    static IEnumerable<Control> MpLocal(MainView mv)
    {
        yield return Logo(T("Локальный сервер"), LogoSize(mv));
        if (!string.IsNullOrEmpty(Mp.status)) yield return Lead(Mp.status);
        yield return Btn(T("Создать игру"), () => { if (Mp.Host()) { mpView = "lobby"; Sfx.buy(); } MpRefresh(mv); });
        // поиск в сети
        var found = Col(6);
        if (mpSearching) found.Children.Add(Txt(T("Ищем игры в сети…"), 13, FontWeight.Bold, B("#b32b1d14")));
        else if (mpFound != null && mpFound.Count == 0) found.Children.Add(Txt(T("Игр в сети не найдено. Попроси друга создать игру или введи его адрес ниже."), 12.5, FontWeight.SemiBold, B("#b32b1d14")));
        if (mpFound != null)
            foreach (var f in mpFound)
            {
                var ff = f; bool ok = f.open && f.proto == Mp.PROTO;
                var row = new DockPanel();
                var join = Panels.PBtn(ok ? T("Войти") : f.proto != Mp.PROTO ? T("Другая версия") : T("Занято"), ok ? "go" : "poor", () => { if (ok) { Mp.Join(ff.ip); MpRefresh(mv); } }, true); join.MinWidth = 110;
                DockPanel.SetDock(join, Dock.Right); row.Children.Add(join);
                row.Children.Add(Col(1, Txt(f.name, 15, FontWeight.Black), Txt(f.ip + " · " + T("игроков: ") + f.count + "/" + Net.MAX_PLAYERS, 12, FontWeight.Bold, B("#b32b1d14"))));
                found.Children.Add(Box(row, StrawB, 0, 12, new Thickness(10, 8)));
            }
        var search = Btn(mpSearching ? T("Ищем…") : T("Найти игры в сети"), () =>
        {
            if (mpSearching) return; mpSearching = true; mpFound = null; MpRefresh(mv);
            Net.Discover(list => { mpSearching = false; mpFound = list; MpRefresh(mv); });
        }, "gold");
        yield return Box(Col(8, search, found), StrawB, 0, 14, new Thickness(10));
        // по адресу
        var ipBox = new TextBox { Text = SET.lastIp ?? "", FontFamily = Body, FontSize = 16, FontWeight = FontWeight.Bold, Watermark = "192.168.1.5 / 26.10.20.30", MinWidth = 180 };
        var go = Panels.PBtn(T("Подключиться"), "go", () =>
        {
            var ip = (ipBox.Text ?? "").Trim();
            if (ip.Length < 3) { mv.Toast(T("Введи адрес компьютера, который создал игру")); Sfx.hurt(); return; }
            Mp.Join(ip); MpRefresh(mv);
        }, true);
        var addr = new DockPanel(); DockPanel.SetDock(go, Dock.Right); go.Margin = new Thickness(8, 0, 0, 0); addr.Children.Add(go); addr.Children.Add(ipBox);
        yield return Box(Col(6, Txt(T("Подключиться по адресу"), 14, FontWeight.ExtraBold), addr,
            Txt(T("Адрес показывает тот, кто создал игру. Для Radmin VPN — адрес вида 26.x.x.x. Порт ") + Net.PORT + T(" должен быть открыт в брандмауэре."), 11.5, FontWeight.SemiBold, B("#b32b1d14"))), StrawB, 0, 14, new Thickness(10));
        yield return Btn(T("Назад"), () => { Mp.status = ""; mpView = "choose"; MpRefresh(mv); }, "alt");
    }

    /// <summary>Лобби: кто подключился, адреса для друзей, режим, сложность и запуск.</summary>
    static IEnumerable<Control> MpLobby(MainView mv)
    {
        bool host = Net.IsHost; int n = Math.Max(1, Mp.roster.Count);
        yield return Logo(host ? T("Твоя игра") : T("Лобби"), LogoSize(mv));
        if (!string.IsNullOrEmpty(Mp.status) && !host) yield return Lead(Mp.status);
        if (host)
        {
            var ips = Net.LocalIPs();
            var t = ips.Count == 0 ? T("Не найдено ни одной сети — подключи Wi-Fi, кабель или Radmin VPN")
                : string.Join("\n", ips.Select(a => a.ip + "  (" + a.name + ")"));
            yield return Box(Col(4, Txt(T("Адрес для друзей"), 13, FontWeight.ExtraBold, BarnB), Txt(t, 15, FontWeight.Black), Txt(T("Друзья находят игру кнопкой «Найти игры в сети» или вводят этот адрес. Порт ") + Net.PORT + ".", 11.5, FontWeight.SemiBold, B("#b32b1d14"))), StrawB, 0, 14, new Thickness(12, 10));
        }
        // игроки
        var list = Col(6);
        foreach (var r in Mp.roster)
        {
            var ava = MetaUi.Avatar(r.avatar ?? "egg", 40);
            var col = Col(1, Txt(r.name + (r.pid == Mp.myPid ? T(" (ты)") : ""), 15, FontWeight.Black), Txt(HEROES[HEROES.ContainsKey(r.hero ?? "") ? r.hero : "hen"].name + " · " + T("ур. ") + r.lv, 12, FontWeight.Bold, B("#b32b1d14")));
            var d = new DockPanel(); DockPanel.SetDock(ava, Dock.Left); ava.Margin = new Thickness(0, 0, 10, 0); d.Children.Add(ava);
            if (r.host) { var hb = Rar(T("Хост"), "#c0392b", 9); hb.VerticalAlignment = VerticalAlignment.Center; DockPanel.SetDock(hb, Dock.Right); d.Children.Add(hb); }
            d.Children.Add(col);
            list.Children.Add(Box(d, PaperB, 2.5, 14, new Thickness(10, 8), 2));
        }
        for (int i = Mp.roster.Count; i < Net.MAX_PLAYERS; i++) list.Children.Add(Box(Txt(T("Свободное место"), 13, FontWeight.Bold, B("#992b1d14")), B("#f2e6c8"), 0, 14, new Thickness(12, 12)));
        yield return Box(Col(6, Txt(T("Игроки") + " · " + Mp.roster.Count + "/" + Net.MAX_PLAYERS, 13, FontWeight.ExtraBold, BarnB), list), StrawB, 0, 14, new Thickness(10));
        // сложность
        var diff = Row(6); string[] labels = { "1", "2", "3", "4" };
        for (int k = 1; k <= 4; k++)
        {
            bool on = k == n; var chip = Box(Txt(k + T(" игр.") + " — " + Math.Round(Mp.Scale(k) * 100) + "%", 12, FontWeight.ExtraBold, on ? White : InkB, wrap: false), on ? BarnB : PaperB, 2, 9, new Thickness(7, 4));
            diff.Children.Add(chip);
        }
        var diffSv = new ScrollViewer { Content = diff, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto, VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
        yield return Box(Col(6, Txt(T("Сложность: враги крепче, чем больше игроков"), 13, FontWeight.ExtraBold), diffSv), StrawB, 0, 14, new Thickness(10));
        // режим
        string modeName = Mp.mode == "sk" ? T("Режим Soul Knight") : Mp.mode == "story" ? T("Сюжет: ") + T("Глава ") + (STORY[Math.Clamp(Mp.modeIdx, 0, STORY.Count - 1)].ch + 1) + " · " + (STORY[Math.Clamp(Mp.modeIdx, 0, STORY.Count - 1)].i + 1) + " — " + STORY[Math.Clamp(Mp.modeIdx, 0, STORY.Count - 1)].name : T("Бесконечный бой");
        if (host)
        {
            var seg = new Avalonia.Controls.Primitives.UniformGrid { Rows = 1 };
            foreach (var (id, nm) in new[] { ("endless", T("Бесконечный бой")), ("story", T("Сюжет")), ("sk", "Soul Knight") })
            {
                bool on = Mp.mode == id; var tx = Txt(nm, 13, FontWeight.ExtraBold, InkB, TextAlignment.Center); tx.VerticalAlignment = VerticalAlignment.Center;
                var p = new Press(tx, on ? YolkB : PaperB, 0, 0, new Thickness(6, 9), 0) { MinHeight = 38 };
                if (seg.Children.Count > 0) { p.BorderBrush = InkB; p.BorderThickness = new Thickness(2.5, 0, 0, 0); }
                var mid = id;
                p.Click += () => { int idx = mid == "story" ? Math.Min(MaxStory(), Mp.modeIdx) : 0; Mp.SetMode(mid, idx); };
                seg.Children.Add(p);
            }
            var segB = Box(seg, PaperB, 2.5, 12); segB.ClipToBounds = true;
            var modeCol = Col(8, Txt(T("Режим"), 13, FontWeight.ExtraBold), segB);
            if (Mp.mode == "story")
            { // выбор уровня: только открытые у хоста
                var prev = Panels.PBtn("‹", "eq", () => Mp.SetMode("story", Math.Max(0, Mp.modeIdx - 1)), true); prev.MinWidth = 46;
                var next = Panels.PBtn("›", "eq", () => Mp.SetMode("story", Math.Min(MaxStory(), Mp.modeIdx + 1)), true); next.MinWidth = 46;
                var Lv = STORY[Math.Clamp(Mp.modeIdx, 0, STORY.Count - 1)];
                var mid2 = Col(1, Txt(T("Глава ") + (Lv.ch + 1) + " · " + (Lv.i + 1) + "/20", 12, FontWeight.Bold, B("#b32b1d14"), TextAlignment.Center), Txt(Lv.name, 14, FontWeight.Black, InkB, TextAlignment.Center));
                var g = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"), ColumnSpacing = 8 }; Grid.SetColumn(mid2, 1); Grid.SetColumn(next, 2); mid2.VerticalAlignment = VerticalAlignment.Center;
                g.Children.Add(prev); g.Children.Add(mid2); g.Children.Add(next); modeCol.Children.Add(g);
            }
            if (Mp.mode == "sk") modeCol.Children.Add(Txt(T("Каждый идёт своим героем; пивчики общие, пассивку в портале выбирает хост — она достаётся всем."), 12, FontWeight.SemiBold, B("#b32b1d14")));
            yield return Box(modeCol, StrawB, 0, 14, new Thickness(10));
            yield return Btn(Mp.state == "game" ? T("Бой идёт…") : T("Начать бой!"), () =>
            {
                if (Mp.state == "game") return;
                Audio.Init(); mv.Hide("mpOv"); Mp.StartMatch(SAVE.hero);
            });
            yield return Btn(T("Закрыть сервер"), () => { Mp.Leave(); mpView = "local"; MpRefresh(mv); }, "alt");
        }
        else
        {
            yield return Box(Col(4, Txt(T("Режим"), 13, FontWeight.ExtraBold), Txt(modeName, 15, FontWeight.Black), Txt(T("Режим выбирает хост. Твой герой — тот, что выбран в меню."), 12, FontWeight.SemiBold, B("#b32b1d14"))), StrawB, 0, 14, new Thickness(10));
            yield return Btn(T("Выйти из лобби"), () => { Mp.Leave(); Mp.status = ""; mpView = "local"; MpRefresh(mv); }, "alt");
        }
        yield return Btn(T("Свернуть"), () => mv.Hide("mpOv"), "alt");
    }
    /// <summary>Последний открытый у хоста уровень сюжета.</summary>
    static int MaxStory() { int m = 0; for (int i = 0; i < STORY.Count; i++) if (i == 0 || SAVE.story.done.Contains(i - 1) || SAVE.story.done.Contains(i)) m = i; return m; }
}
