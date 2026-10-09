using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Kuriatnyk.Render;
using static Kuriatnyk.D;
using static Kuriatnyk.Store;
using static Kuriatnyk.UI.K;

namespace Kuriatnyk.UI;

/// <summary>Окна поверх игры (.ov и .card из style.css).</summary>
public static class Overlays
{
    // ---------- строители ----------
    public static OutlineText Logo(string text, double size = 0)
        => new() { Text = text, Display = true, FontSize = size > 0 ? size : 40, Stroke = 2, ShadowY = 4, Align = TextAlignment.Center, HorizontalAlignment = HorizontalAlignment.Stretch };
    public static TextBlock Eyebrow(string text)
    {
        var t = Txt(text.ToUpper(L.Culture), 12, FontWeight.ExtraBold, BarnB, TextAlignment.Center); t.LetterSpacing = 1.4; return t;
    }
    public static TextBlock Lead(string text) => Txt(text, 15, FontWeight.Medium, InkB, TextAlignment.Center);
    public static TextBlock Best(string text) { var t = Txt(text, 13, FontWeight.SemiBold, B("#cc2b1d14"), TextAlignment.Center); return t; }

    public static Press Btn(string text, Action click, string kind = "main")
    {
        bool alt = kind == "alt", gold = kind == "gold";
        var t = Txt(text, alt || gold ? 16 : 20, FontWeight.ExtraBold, alt || gold ? InkB : White, TextAlignment.Center);
        var p = new Press(t, alt ? White : gold ? YolkB : BarnB, 3, 16, alt || gold ? new Thickness(10, 13) : new Thickness(18, 16), 5);
        p.Click += click; return p;
    }
    public static Border CardBox(double width, params Control[] kids)
    {
        var col = Col(14, kids);
        var b = Box(col, PaperB, 4, 24, new Thickness(18, 20, 18, 18), 8);
        b.MaxWidth = width; b.HorizontalAlignment = HorizontalAlignment.Stretch; return b;
    }
    /// <summary>Затемнение на весь экран с карточкой по центру и прокруткой, если не влезает.</summary>
    public static Control Ov(MainView mv, Control card, bool top)
    {
        card.HorizontalAlignment = HorizontalAlignment.Center; card.VerticalAlignment = VerticalAlignment.Center;
        var holder = new Grid { Margin = new Thickness(16) }; holder.Children.Add(card);
        var sv = new ScrollViewer { Content = holder, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        holder.MinHeight = 0;
        sv.SizeChanged += (_, e) => holder.MinHeight = Math.Max(0, e.NewSize.Height - 32);
        var g = new Border { Background = B(top ? "#992b1d14" : "#6b2b1d14"), Child = sv, Padding = mv.Safe };
        return g;
    }
    static bool ShortLand(MainView mv) => mv.View.Height <= 500 && mv.View.Width > mv.View.Height;
    static double LogoSize(MainView mv) => Math.Clamp(mv.View.Width * .11, 34, 48);

    // ---------- пауза ----------
    public static Control Pause(MainView mv)
    {
        var card = CardBox(380, Logo(T("Пауза"), LogoSize(mv)),
            Col(10, Btn(T("Продолжить"), G.resumeGame), Btn(T("Заново"), () => { Audio.Init(); G.bankCoins(); G.startGame(G.S.kind, G.S.lvIdx); }, "alt"),
                Btn(T("Настройки"), () => OpenSettings(mv), "alt"), Btn(T("В меню"), G.toMenu, "alt")),
            Best(T("Версия ") + GAME_VERSION));
        return Ov(mv, card, false);
    }

    // ---------- поражение ----------
    public static Control Over(MainView mv, OverInfo o)
    {
        Control tile(string v, string l)
        {
            var lt = Txt(l.ToUpper(L.Culture), 11, FontWeight.Bold, B("#b32b1d14"), TextAlignment.Center); lt.LetterSpacing = .9;
            return Box(Col(4, Txt(v, 22, FontWeight.Black, InkB, TextAlignment.Center), lt), StrawB, 0, 14, new Thickness(4, 10));
        }
        var tiles = new UniformGrid { Columns = 3, ColumnSpacing = 8 };
        tiles.Children.Add(tile(L.Fmt(o.score), T("Очки"))); tiles.Children.Add(tile(o.waveText ?? o.wave.ToString(), o.waveLabel)); tiles.Children.Add(tile(o.kills.ToString(), T("Враги")));
        var earn = Rich(T("Заработано: <i class=\"coin\"></i>") + o.earnCoins + T(" зёрен · <i class=\"tok\"></i>") + o.earnTokens + T(" жетонов"), 13, FontWeight.SemiBold, B("#cc2b1d14"), TextAlignment.Center);
        earn.HorizontalAlignment = HorizontalAlignment.Center;
        var rec = Txt(T("Новый рекорд!"), 15, FontWeight.ExtraBold, BarnB, TextAlignment.Center); rec.IsVisible = o.record;
        var again = Btn(T("Ещё раз"), () => { Audio.Init(); G.startGame(G.S.kind, G.S.lvIdx); });
        var menu = Btn(T("В меню"), G.toMenu, "alt");
        Control card;
        if (ShortLand(mv)) card = TwoCol(mv, 640, new Control[] { Logo(T("Ощипали!"), 34), Lead(o.lead), earn, rec }, new Control[] { tiles }, again, menu);
        else card = CardBox(380, Logo(T("Ощипали!"), LogoSize(mv)), Lead(o.lead), earn, tiles, rec, Col(10, again, menu));
        return Ov(mv, card, false);
    }
    static Control TwoCol(MainView mv, double width, Control[] left, Control[] right, params Press[] buttons)
    {
        var g = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*"), ColumnSpacing = 16 };
        var l = Col(10, left); var r = Col(10, right); r.VerticalAlignment = VerticalAlignment.Center; Grid.SetColumn(r, 1);
        g.Children.Add(l); g.Children.Add(r);
        var bg = new UniformGrid { Rows = 1, ColumnSpacing = 10 }; foreach (var b in buttons) bg.Children.Add(b);
        var b2 = Box(Col(10, g, bg), PaperB, 4, 24, new Thickness(16, 14), 8); b2.MaxWidth = width; return b2;
    }

    // ---------- победа ----------
    public static Control Win(MainView mv, WinInfo w)
    {
        var rows = Col(6);
        foreach (var r in w.rows)
        {
            var ic = Icon(r.icon, 15 * .95); ic.Margin = new Thickness(0, 0, 6, 0);
            var line = new DockPanel(); DockPanel.SetDock(ic, Dock.Left); line.Children.Add(ic);
            line.Children.Add(Col(1, Txt(r.text, 15, FontWeight.ExtraBold), Txt(r.sub ?? "", 12, FontWeight.SemiBold, B("#bf2b1d14"))));
            rows.Children.Add(Box(line, StrawB, 0, 12, new Thickness(10, 8)));
        }
        var next = Btn(w.nextLabel, () => { int n = G.S.lvIdx + 1; G.toMenu(); if (n < STORY.Count) OpenBrief(mv, n); }); next.IsVisible = w.hasNext;
        var menu = Btn(T("В меню"), G.toMenu, "alt");
        Control card;
        if (ShortLand(mv)) card = TwoCol(mv, 640, new Control[] { Eyebrow(w.eyebrow), Logo(T("Победа!"), 34), Lead(w.lead) }, new Control[] { rows }, w.hasNext ? new[] { next, menu } : new[] { menu });
        else card = CardBox(380, Eyebrow(w.eyebrow), Logo(T("Победа!"), LogoSize(mv)), Lead(w.lead), rows, Col(10, next, menu));
        return Ov(mv, card, false);
    }

    // ---------- брифинг уровня ----------
    public static int briefIdx;
    public static string goalText(Level Lv)
    {
        var g = Lv.goal;
        if (g.type == "waves") return L.En ? "Clear " + g.n + " waves" : "Пройди " + g.n + " волн" + (g.n < 5 ? "ы" : "");
        if (g.type == "kill") return T("Победи: ") + (g.what == "any" ? T("любых врагов") : (killN(g.what) ?? ETYPE[g.what].name).ToLower(L.Culture)) + " ×" + g.n;
        if (g.type == "survive") return T("Продержись ") + g.t + T(" секунд");
        return T("Победи босса: ") + ETYPE[g.boss].name;
    }
    public static void OpenBrief(MainView mv, int i)
    {
        briefIdx = i; var Lv = STORY[i];
        var foes = string.Join(", ", Lv.mix.Keys.Concat(Lv.goal.boss != null ? new[] { Lv.goal.boss } : Array.Empty<string>()).Distinct().Select(k => ETYPE[k].name.ToLower(L.Culture)));
        var html = T("<span>Цель: ") + goalText(Lv) + T("</span><small>Карта: ") + G.MAPS[G.MAP_OF_CH[Lv.ch]].name + T(" · река с мостами, гнилые рушатся после второго прохода</small><small>Враги: ")
            + foes + (Lv.night ? T(". Ночь — обзор меньше") : "") + T("</small><small>Награда: ") + Lv.coins + T(" зёрен, ") + Lv.tokens + T(" жетонов</small>");
        var goal = Rich(html, 14, FontWeight.Bold); goal.LineHeight = 19;
        var goalBox = Box(goal, StrawB, 0, 14, new Thickness(12, 10));
        var hero = Best(T("Боец: ") + HEROES[SAVE.hero].name + " (" + CLASSES[clsOf(SAVE.hero)].name.ToLower(L.Culture) + T(") · ур. ") + heroLv(SAVE.hero) + " · " + WEAP[gunOf(SAVE.hero)].name);
        var go = Btn(T("В бой!"), () => { Audio.Init(); mv.Hide("brief"); G.startGame("story", briefIdx); });
        var back = Btn(T("Назад"), () => mv.Hide("brief"), "alt");
        var eyebrow = Eyebrow(T("Глава ") + (Lv.ch + 1) + T(" · уровень ") + (Lv.i + 1) + T(" из 20"));
        Control card;
        if (ShortLand(mv)) card = TwoCol(mv, 640, new Control[] { eyebrow, Logo(Lv.name, 30), Lead(Lv.text) }, new Control[] { goalBox, hero }, go, back);
        else card = CardBox(380, eyebrow, Logo(Lv.name, LogoSize(mv)), Lead(Lv.text), goalBox, hero, Col(10, go, back));
        mv.Show("brief", Ov(mv, card, true), 8);
    }
    static string StripTags(string s) => G.StripIcon(s);

    // ---------- настройки ----------
    static long lastSfxPreview; // пробный звук при перетаскивании ползунка — не чаще раза в 160 мс, а не на каждый шаг
    public static void OpenSettings(MainView mv) { Audio.Init(); mv.Show("setOv", Settings(mv), 8); }
    static Border SetRow(Control label, Control right)
    {
        var d = new DockPanel { LastChildFill = true }; DockPanel.SetDock(right, Dock.Right); right.VerticalAlignment = VerticalAlignment.Center; right.Margin = new Thickness(12, 0, 0, 0);
        d.Children.Add(right); label.VerticalAlignment = VerticalAlignment.Center; d.Children.Add(label);
        var b = Box(d, StrawB, 0, 14, new Thickness(12, 10)); b.MinHeight = 48; return b;
    }
    static Control Toggle(bool on, Action click)
    {
        var knob = new Border { Width = 24, Height = 24, CornerRadius = new CornerRadius(12), Background = White, BorderBrush = InkB, BorderThickness = new Thickness(2), HorizontalAlignment = on ? HorizontalAlignment.Right : HorizontalAlignment.Left, Margin = new Thickness(3) };
        var p = new Press(knob, on ? B("#5fcf3f") : B("#c9bfa8"), 2.5, 17, new Thickness(0), 0) { Width = 58, Height = 34 };
        p.Click += click; return p;
    }
    public static Control Settings(MainView mv)
    {
        Control seg()
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal };
            foreach (var (code, name) in new[] { ("ru", "Русский"), ("en", "English") })
            {
                var on = SET.lang == code;
                var p = new Press(Txt(name, 14, FontWeight.ExtraBold, InkB, wrap: false), on ? YolkB : PaperB, 0, 0, new Thickness(12, 10), 0);
                p.Click += () => { if (SET.lang == code) return; SetLang(mv, code); };
                if (row.Children.Count > 0) row.Children.Add(new Border { Width = 2.5, Background = InkB });
                row.Children.Add(p);
            }
            var b = Box(row, PaperB, 2.5, 12); b.ClipToBounds = true; return b;
        }
        Control slider(Func<double> get, Action<double> set)
        {
            var s = new Slider { Minimum = 0, Maximum = 100, SmallChange = 5, LargeChange = 5, TickFrequency = 5, IsSnapToTickEnabled = true, Value = Math.Round(get() * 100), MinWidth = 120, Width = 150 };
            s.Resources["SliderThumbBackground"] = BarnB; s.Resources["SliderTrackValueFill"] = BarnB; s.Resources["SliderThumbBackgroundPointerOver"] = BarnB; s.Resources["SliderTrackValueFillPointerOver"] = BarnB;
            s.Resources["SliderThumbBackgroundPressed"] = BarnB; s.Resources["SliderTrackValueFillPressed"] = BarnB;
            var v = Txt(Math.Round(get() * 100) + "%", 14, FontWeight.Black, InkB, TextAlignment.Right, false); v.Width = 44; v.VerticalAlignment = VerticalAlignment.Center; s.VerticalAlignment = VerticalAlignment.Center;
            s.PropertyChanged += (_, e) => { if (e.Property != RangeBase.ValueProperty) return; set(s.Value / 100); saveSet(); Audio.ApplyVolume(); v.Text = Math.Round(s.Value) + "%"; };
            return Row(6, s, v);
        }
        var rows = new List<Control>
        {
            Logo(T("Настройки"), LogoSize(mv)),
            SetRow(Txt(T("Язык"), 15, FontWeight.ExtraBold), seg()),
            SetRow(Txt(T("Музыка"), 15, FontWeight.ExtraBold), slider(() => SET.music, v => SET.music = v)),
            SetRow(Txt(T("Звуки"), 15, FontWeight.ExtraBold), slider(() => SET.sfx, v => { SET.sfx = v; if (Environment.TickCount64 - lastSfxPreview > 160) { lastSfxPreview = Environment.TickCount64; Sfx.pick(); } })),
            SetRow(Txt(T("Тряска экрана от взрывов"), 15, FontWeight.ExtraBold), Toggle(SET.shake, () => { SET.shake = !SET.shake; saveSet(); if (SET.shake) G.S.shake = Math.Max(G.S.shake, 6); mv.Show("setOv", Settings(mv), 8); })),
        };
        if (!G.COARSE)
            rows.Add(SetRow(Col(2, Txt(T("Автонаводка"), 15, FontWeight.ExtraBold), Txt(T("стреляет сама по ближнему врагу (T)"), 12, FontWeight.SemiBold, B("#b32b1d14"))), Toggle(SET.autoAim, mv.ToggleAuto)));
        rows.Add(Btn(T("Готово"), () => mv.Hide("setOv")));
        var card = Box(Col(10, rows.ToArray()), PaperB, 4, 24, new Thickness(18, 20, 18, 18), 8); card.MaxWidth = 440;
        return Ov(mv, card, true);
    }
    /// <summary>Язык меняется сразу: открытые окна и бой остаются на месте (setLang из menus.js).</summary>
    static void SetLang(MainView mv, string l)
    {
        SET.lang = l; L.Lang = l; saveSet();
        mv.RebuildAll();
        Sfx.pick();
    }

    // ---------- Soul Knight: выбор героя ----------
    public static string skPick; static bool skStats;
    public static void OpenSkSelect(MainView mv)
    {
        Audio.Init();
        if (skPick == null || !SAVE.heroes.ContainsKey(skPick)) skPick = SAVE.hero;
        mv.Show("skSel", SkSelect(mv), 8);
    }
    /// <summary>Компактный выбор героя: маленькие плитки; подробности — по кнопке «Характеристики».</summary>
    static Control SkSelect(MainView mv)
    {
        bool sh = ShortLand(mv);
        var grid = new AutoGrid { Min = sh ? 80 : 92, Gap = 8 };
        foreach (var id in HERO_IDS.OrderByDescending(x => SAVE.heroes.ContainsKey(x) ? 1 : 0))
        {
            var H = HEROES[id]; bool own = SAVE.heroes.ContainsKey(id), on = own && id == skPick;
            var pv = Panels.HeroPv(id, 76, sh ? 46 : 56, sh ? .85 : 1); pv.HorizontalAlignment = HorizontalAlignment.Center;
            var pvb = Box(pv, PvBg(own ? "grass" : "dark"), 2, 9); pvb.ClipToBounds = true;
            var nm = Txt(H.name, 11.5, FontWeight.ExtraBold, InkB, TextAlignment.Center); nm.LineHeight = 13;
            var p = new Press(Col(4, pvb, nm), on ? YolkB : own ? PaperB : B(Poor), 3, 14, new Thickness(5, 5, 5, 6), on ? 0 : 3);
            if (on) p.BoxShadow = BoxShadows.Parse("0 0 0 3 #C0392B");
            if (!own) { p.Opacity = .55; p.Click += () => mv.Toast(T("Герой ещё не открыт — выпадает из сундуков")); }
            else p.Click += () => { skPick = id; mv.Show("skSel", SkSelect(mv), 8); };
            grid.Children.Add(p);
        }
        var Hp = HEROES[skPick]; var pl = G.newPlayer(skPick);
        Control stats = null;
        if (skStats)
        {
            string f1(double v) => v.ToString("0.##", L.Culture);
            stats = Box(Col(5,
                Badges(Rar(RAR[Hp.rar].name, RAR[Hp.rar].c, 9.5), Rar(CLASSES[Hp.cls].name, CLASSES[Hp.cls].c, 9.5), Rar(T("Ур. ") + heroLv(skPick), "#6b5a48", 9.5)),
                Txt(Hp.name, 16, FontWeight.Black),
                Txt(T("Здоровье ") + pl.max + T(" · скорость ") + Math.Round(pl.spd) + T(" · урон ×") + f1(pl.dmgMul), 12.5, FontWeight.Bold),
                Txt(T("Оружие: ") + WEAP[pl.prim].name + T(" + Деревянная палка (смена — кнопкой у оружия или F)"), 12.5, FontWeight.Medium),
                Rich(T("<b>Ульта: ") + Hp.ult[0] + ".</b> " + Hp.ult[1], 12.5, FontWeight.Medium),
                Txt(Hp.perk, 12, FontWeight.Medium, B("#b32b1d14"))), StrawB, 0, 14, new Thickness(12, 10));
        }
        var statBtn = Btn(skStats ? T("Скрыть") : T("Характеристики"), () => { skStats = !skStats; mv.Show("skSel", SkSelect(mv), 8); }, "alt");
        var go = Btn(T("В путь!"), () => { mv.Hide("skSel"); G.startSK(skPick); });
        var back = Btn(T("Назад"), () => mv.Hide("skSel"), "alt");
        var pair = new UniformGrid { Rows = 1, ColumnSpacing = 8 }; pair.Children.Add(back); pair.Children.Add(go);
        var row = Col(8, statBtn, pair);
        var card = Box(Col(sh ? 8 : 12, Eyebrow(T("Режим Soul Knight")), Logo(T("Выбор героя"), sh ? 26 : 32), grid, stats, row), PaperB, 4, 24, sh ? new Thickness(14, 10) : new Thickness(18, 18, 18, 16), 8);
        card.MaxWidth = 540;
        return Ov(mv, card, true);
    }

    // ---------- об игре ----------
    public static void OpenInfo(MainView mv)
    {
        Audio.Init();
        var credits = Col(6, CREDITS.Select(c => (Control)Box(Col(2, Rich("<b>" + c[0] + " <small>" + c[1] + "</small></b>", 15, FontWeight.ExtraBold), Txt(T(c[2]), 12.5, FontWeight.Medium, B("#cc2b1d14"))), StrawB, 0, 12, new Thickness(12, 8))).ToArray());
        var notes = Col(10, PATCH_NOTES.Select(N =>
        {
            var items = Col(4, N.itemsRu.Select(t => (Control)Txt("• " + T(t), 13, FontWeight.Medium)).ToArray());
            return (Control)Box(Col(6, Txt(T("Версия ") + N.v, 15, FontWeight.Black), items), StrawB, 0, 12, new Thickness(12, 10));
        }).ToArray());
        var sv = new ScrollViewer { Content = notes, MaxHeight = Math.Max(160, mv.View.Height * (ShortLand(mv) ? .34 : .42)), HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        TextBlock h3(string s) { var t = Txt(s.ToUpper(L.Culture), 12, FontWeight.ExtraBold, BarnB); t.LetterSpacing = 1.2; t.Margin = new Thickness(0, 0, 0, 8); return t; }
        var card = Box(Col(14, Eyebrow(T("Информация об игре")), Logo(T("Боевой Курятник"), Math.Clamp(mv.View.Width * .08, 26, 34)), Best(T("Версия ") + GAME_VERSION),
            Col(0, h3(T("Создатели")), credits), Col(0, h3(T("Патчноуты")), sv), Btn(T("Закрыть"), () => mv.Hide("infoOv"))), PaperB, 4, 24, new Thickness(18, 20, 18, 18), 8);
        card.MaxWidth = 440;
        mv.Show("infoOv", Ov(mv, card, true), 8);
    }

    // ---------- как играть ----------
    public static void OpenHelp(MainView mv)
    {
        Audio.Init();
        Control li(string ic, string bg, string fg, string text)
        {
            var circle = Box(Txt(ic, 12, FontWeight.Black, B(fg), TextAlignment.Center), B(bg), 3, 17); circle.Width = 34; circle.Height = 34;
            ((TextBlock)circle.Child).VerticalAlignment = VerticalAlignment.Center;
            var d = new DockPanel(); DockPanel.SetDock(circle, Dock.Left); circle.Margin = new Thickness(0, 0, 10, 0); d.Children.Add(circle);
            var t = Txt(text, 13.5, FontWeight.Medium); t.VerticalAlignment = VerticalAlignment.Center; d.Children.Add(t); return d;
        }
        var how = Box(Col(8, li("◀", "#5fcf3f", "#2b1d14", T("Левый палец — бежать")), li("●", "#c0392b", "#ffffff", T("Правый палец — целиться и стрелять. Держи на месте — бьёт по ближнему")),
            li("", "#fff4dc", "#2b1d14", T("«Бум» — яйцо-граната")), li("★", "#ffc93a", "#2b1d14", T("Ульта копится от урона по врагам"))), StrawB, 0, 16, new Thickness(12));
        var desk = Txt(T("На компьютере: WASD — бег, автонаводка стреляет сама (T — вкл/выкл), зажатая мышь — ручная стрельба, Q — граната, R — ульта"), 12, FontWeight.Medium, B("#b32b1d14"), TextAlignment.Center);
        var card = CardBox(380, Logo(T("Как играть"), LogoSize(mv)), how, desk, Best(T("Версия ") + GAME_VERSION), Btn(T("Понятно"), () => mv.Hide("helpOv")));
        mv.Show("helpOv", Ov(mv, card, true), 8);
    }

    // ---------- фон меню ----------
    public static void OpenBg(MainView mv) { Audio.Init(); mv.Show("bgOv", Bg(mv), 8); }
    static Control Bg(MainView mv)
    {
        var grid = new AutoGrid { Min = 132, Gap = 10 };
        if (ShortLand(mv)) grid.Min = 100;
        foreach (var key in G.MAPS.Keys)
        {
            var M = G.MAPS[key]; bool on = G.menuMap() == key;
            var pv = new SkiaView((c, w, h) => G.drawMapPreview(c, key, w, h)) { Height = 78 };
            var pvb = Box(pv, null, 2, 9); pvb.ClipToBounds = true;
            var p = new Press(Col(6, pvb, Txt(M.name + (on ? " ✓" : ""), 13, FontWeight.ExtraBold, InkB, TextAlignment.Center)), on ? YolkB : StrawB, 3, 14, new Thickness(6), 3);
            p.Click += () =>
            {
                SAVE.menuBg = key; persist(); Sfx.pick();
                if (G.MAP != key) { G.buildWorld(key); G.RebuildMenuHero(); }
                mv.Show("bgOv", Bg(mv), 8);
            };
            grid.Children.Add(p);
        }
        var card = CardBox(ShortLand(mv) ? 760 : 520, Eyebrow(T("Главное меню")), Logo(T("Фон меню"), LogoSize(mv)), grid,
            Btn(T("Готово"), () => { mv.Hide("bgOv"); mv.Toast(T("Фон меню: ") + G.MAPS[G.menuMap()].name); }));
        return Ov(mv, card, true);
    }

    // ---------- свойства сундука ----------
    public static int ciTier = -1;
    static string pc(double v)
    {
        var x = v * 100;
        var s = x == 0 ? "0" : x >= 10 ? Math.Round(x, MidpointRounding.AwayFromZero).ToString(System.Globalization.CultureInfo.InvariantCulture) : x >= 1 ? x.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) : x.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        return s.Replace('.', ',') + "%";
    }
    static Control Table(string[] head, List<Control[]> rows)
    {
        var g = new Grid();
        g.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        for (int i = 1; i < head.Length; i++) g.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        for (int r = 0; r <= rows.Count; r++) g.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        for (int i = 0; i < head.Length; i++)
        {
            var t = Txt(head[i].ToUpper(L.Culture), 11, FontWeight.ExtraBold, InkB, i == 0 ? TextAlignment.Left : TextAlignment.Right); t.LetterSpacing = .9;
            var cell = new Border { Background = StrawB, Padding = new Thickness(8), Child = t, BorderBrush = B("#ead8ae"), BorderThickness = new Thickness(0, 0, 0, 2) };
            Grid.SetColumn(cell, i); g.Children.Add(cell);
        }
        for (int r = 0; r < rows.Count; r++)
            for (int i = 0; i < rows[r].Length; i++)
            {
                var ch = rows[r][i]; if (ch is TextBlock tb) tb.TextAlignment = i == 0 ? TextAlignment.Left : TextAlignment.Right;
                ch.HorizontalAlignment = i == 0 ? HorizontalAlignment.Left : HorizontalAlignment.Right; ch.VerticalAlignment = VerticalAlignment.Center;
                var cell = new Border { Padding = new Thickness(8), Child = ch, BorderBrush = B("#ead8ae"), BorderThickness = new Thickness(0, 0, 0, r == rows.Count - 1 ? 0 : 2) };
                Grid.SetRow(cell, r + 1); Grid.SetColumn(cell, i); g.Children.Add(cell);
            }
        var b = Box(g, PaperB, 3, 14); b.ClipToBounds = true; b.Margin = new Thickness(0, 4, 0, 0); return b;
    }
    static TextBlock Cell(string s, bool bold = false) => Rich(s, 13, bold ? FontWeight.ExtraBold : FontWeight.SemiBold);
    public static void OpenChestInfo(MainView mv, int tier)
    {
        ciTier = tier; Audio.Init(); Sfx.pick();
        var C = CHESTS[tier]; var o = Chests.Odds(tier);
        var pv = new SkiaView((c, w, h) => { c.translate(w / 2, h - 10); c.scale(.78, .78); G.drawChest(c, tier, 0, 0); }) { Height = 128 };
        var pvb = Box(pv, PvBg(new[] { "nest", "gold", "amet" }[tier]), 2, 14); pvb.IsVisible = !ShortLand(mv); pvb.ClipToBounds = true;
        TextBlock h(string s) { var t = Txt(s.ToUpper(L.Culture), 13, FontWeight.Black); t.LetterSpacing = 1; t.Margin = new Thickness(0, 10, 0, 2); return t; }
        Control rb(Rar R) => Rar(R.name, R.c);
        var t1 = Table(new[] { T("Награда"), T("Сколько") }, new()
        {
            new Control[] { Cell(T("<i class=\"coin\"></i>Зёрна")), Cell(C.coins[0] + "–" + C.coins[1]) },
            new Control[] { Cell(T("Пачки карт героев")), Cell(C.hpacks + " × " + C.hcard[0] + "–" + C.hcard[1] + T(" карт*")) },
            new Control[] { Cell(T("Пачки карт оружия")), Cell(C.wpacks + " × " + C.wcard[0] + "–" + C.wcard[1] + T(" карт*")) },
            new Control[] { Cell(T("<i class=\"gegg\"></i>Золотые яйца")), Cell((C.egg[0] >= 1 ? T("всегда") : T("шанс ") + pc(C.egg[0])) + " · " + C.egg[1] + "–" + C.egg[2]) },
        });
        var heroRows = new[] { "legendary", "epic", "rare", "common" }.Select(rk => new Control[] { rb(RAR[rk]), Cell(pc(o.hero[rk].p)), Cell(o.hero[rk].left.ToString()) }).ToList();
        heroRows.Add(new Control[] { Cell(T("<b>Хоть какой-то герой</b>"), true), Cell("<b>" + pc(o.anyHero) + "</b>", true), Cell("") });
        var t2 = Table(new[] { T("Редкость"), T("Шанс"), T("Ещё закрыто") }, heroRows);
        var t3 = Table(new[] { T("Карты героя"), T("Шанс пачки"), T("Карт в пачке") }, new[] { "common", "rare", "epic", "legendary" }.Select(rk => new Control[] { rb(RAR[rk]), Cell(pc(o.hcards[rk])), Cell(Math.Max(1, Math.Round(C.hcard[0] * RAR[rk].card, MidpointRounding.AwayFromZero)) + "–" + Math.Max(1, Math.Round(C.hcard[1] * RAR[rk].card, MidpointRounding.AwayFromZero))) }).ToList());
        var t4 = Table(new[] { T("Карты оружия"), T("Шанс пачки"), T("Карт в пачке") }, WRAR_ORDER.Select(rk => new Control[] { rb(WRAR[rk]), Cell(pc(o.wcards[rk])), Cell(Math.Max(1, Math.Round(C.wcard[0] * WRAR[rk].card, MidpointRounding.AwayFromZero)) + "–" + Math.Max(1, Math.Round(C.wcard[1] * WRAR[rk].card, MidpointRounding.AwayFromZero))) }).ToList());
        t4.Margin = new Thickness(0, 8, 0, 0);
        var note = Txt(T("Шансы считаются от твоего прогресса: уже открытые герои и стволы на максимальном уровне не выпадают. Сначала проверяется самая высокая редкость героя. * Чем выше редкость, тем меньше карт в пачке."), 12, FontWeight.SemiBold, B("#bf2b1d14"));
        note.Margin = new Thickness(0, 10, 0, 4);
        var sub = C.sub; sub = sub.Length > 0 ? char.ToUpper(sub[0]) + sub[1..] : sub;
        var body = Col(0, pvb, h(T("Всегда внутри")), t1, h(T("Новый герой") + (C.sure ? T(" · гарантирован") : "")), t2, h(T("Кому достанется пачка карт")), t3, t4, note);
        var card = Box(Col(14, Eyebrow(T("Свойства сундука")), Logo(C.name, Math.Clamp(mv.View.Width * .09, 28, 40)), Best(sub), body, Btn(T("Закрыть"), () => mv.Hide("chestInfoOv"))), PaperB, 4, 24, new Thickness(18, 20, 18, 18), 8);
        card.MaxWidth = 560;
        mv.Show("chestInfoOv", Ov(mv, card, true), 8);
    }
    public static IBrush PvBg(string kind) => kind switch
    {
        "nest" => new LinearGradientBrush { StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative), EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative), GradientStops = { new GradientStop(Color.Parse("#ffeab0"), 0), new GradientStop(Color.Parse("#f0c56a"), 1) } },
        "gold" => new RadialGradientBrush { Center = new RelativePoint(.5, .62, RelativeUnit.Relative), GradientOrigin = new RelativePoint(.5, .62, RelativeUnit.Relative), RadiusX = new RelativeScalar(.75, RelativeUnit.Relative), RadiusY = new RelativeScalar(.75, RelativeUnit.Relative), GradientStops = { new GradientStop(Color.Parse("#fff3c0"), 0), new GradientStop(Color.Parse("#f1b545"), .62), new GradientStop(Color.Parse("#c47a1f"), 1) } },
        "amet" => new RadialGradientBrush { Center = new RelativePoint(.5, .6, RelativeUnit.Relative), GradientOrigin = new RelativePoint(.5, .6, RelativeUnit.Relative), RadiusX = new RelativeScalar(.75, RelativeUnit.Relative), RadiusY = new RelativeScalar(.75, RelativeUnit.Relative), GradientStops = { new GradientStop(Color.Parse("#f3e2ff"), 0), new GradientStop(Color.Parse("#b88ae8"), .6), new GradientStop(Color.Parse("#6a3fb5"), 1) } },
        "dark" => new LinearGradientBrush { StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative), EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative), GradientStops = { new GradientStop(Color.Parse("#3b3550"), 0), new GradientStop(Color.Parse("#25213a"), 1) } },
        _ => new LinearGradientBrush { StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative), EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative), GradientStops = { new GradientStop(Color.Parse("#9ed46f"), 0), new GradientStop(Color.Parse("#7cc451"), 1) } },
    };

    // ---------- открытие сундука ----------
    sealed class ChestAnim { public int tier; public Chests.Result res; public double t; public string state; public SkiaView view; public StackPanel rewards; public Press take; public TextBlock takeT; }
    static ChestAnim anim;
    public static void OpenChestOverlay(MainView mv, int tier, Chests.Result res)
    {
        var view = new SkiaView((c, w, h) =>
        {
            if (anim == null) return;
            c.translate(w / 2, 150);
            if (anim.state == "shake") { double k = Math.Min(1, anim.t / 1.2), sh = Math.Sin(anim.t * 40) * .07 * k; G.drawChest(c, anim.tier, 0, sh, k); }
            else G.drawChest(c, anim.tier, Math.Min(1, anim.t / .35), 0);
        }) { Width = 240, Height = 170, Animate = true, HorizontalAlignment = HorizontalAlignment.Center, ClipToBounds = true };
        var viewHit = new Border { Child = view, Background = Brushes.Transparent };
        viewHit.PointerReleased += (_, _) => Reveal(mv);
        var rewards = Col(6);
        var takeT = Txt(T("Открыть"), 20, FontWeight.ExtraBold, White, TextAlignment.Center);
        var take = new Press(takeT, BarnB, 3, 16, new Thickness(18, 16), 5);
        anim = new ChestAnim { tier = tier, res = res, t = 0, state = "shake", view = view, rewards = rewards, take = take, takeT = takeT };
        take.Click += () =>
        {
            if (anim == null) return;
            if (anim.state == "shake") { Reveal(mv); return; }
            anim = null; mv.Hide("chestOv"); mv.AfterChange();
        };
        var title = Eyebrow(CHESTS[tier].name + (res.n > 1 ? " ×" + res.n : "") + " · " + CHESTS[tier].sub);
        var card = CardBox(380, title, viewHit, rewards, take);
        mv.Show("chestOv", Ov(mv, card, true), 8);
        mv.RefreshMenu(); Sfx.chest();
    }
    public static void ChestFrame(MainView mv, double dt)
    {
        if (anim == null || !mv.Shown("chestOv")) return;
        anim.t += dt;
        if (anim.state == "shake" && anim.t > 1.3) Reveal(mv);
    }
    static void Reveal(MainView mv)
    {
        if (anim == null || anim.state != "shake") return;
        anim.state = "open"; anim.t = 0; Sfx.boom(); Sfx.win();
        var res = anim.res; var R = anim.rewards; R.Children.Clear();
        Control row(Control icon, string text, string sub, string border = null)
        {
            var d = new DockPanel();
            if (icon != null) { DockPanel.SetDock(icon, Dock.Left); icon.Margin = new Thickness(0, 0, 10, 0); d.Children.Add(icon); }
            d.Children.Add(Col(1, Txt(text, 15, FontWeight.ExtraBold), sub == null ? null : Txt(sub, 12, FontWeight.SemiBold, B("#bf2b1d14"))));
            var b = Box(d, StrawB, 0, 12, new Thickness(10, 8));
            if (border != null) { b.BorderBrush = B(border); b.BorderThickness = new Thickness(6, 0, 0, 0); }
            return b;
        }
        Control framed(Control pv, string bg) { var b = Box(pv, PvBg(bg), 2, 8); b.ClipToBounds = true; return b; }
        foreach (var id in res.heroes)
        {
            var H = HEROES[id]; var Rr = RAR[H.rar];
            var t = Txt((T("Новый герой · ") + Rr.name).ToUpper(L.Culture), 12, FontWeight.Black, White, TextAlignment.Center); t.LetterSpacing = 1.6;
            var pv = Panels.HeroPv(id, 150, 96, 1.6); pv.HorizontalAlignment = HorizontalAlignment.Center;
            var nm = new OutlineText { Text = H.name, Display = true, FontSize = 26, Fill = White, Stroke = 1, ShadowY = 0, Align = TextAlignment.Center, HorizontalAlignment = HorizontalAlignment.Stretch };
            R.Children.Add(Box(Col(4, t, framed(pv, "grass"), nm), B(Rr.c), 0, 12, new Thickness(12)));
        }
        R.Children.Add(row(Icon("coin", 15), "+" + res.coins + T(" зёрен"), null));
        if (res.eggs > 0) R.Children.Add(row(Icon("gegg", 15), "+" + res.eggs + T(" золотых яиц"), res.tier < 2 ? G.StripIcon(T("<small>редкая находка!</small>")) : null));
        foreach (var (id, n) in res.cards)
        {
            var H = HEROES[id]; var h = SAVE.heroes[id]; int nc = h.lv < 10 ? needCards(id, h.lv) : 0;
            R.Children.Add(row(framed(Panels.HeroPv(id, 56, 46, .9), "grass"), "+" + n + T(" карт · ") + H.name, h.lv < 10 ? h.cards + " / " + nc + T(" до ур. ") + (h.lv + 1) : T("макс. уровень")));
        }
        foreach (var (id, n) in res.wcards)
        {
            var W = WEAP[id]; int lv = wLv(id); var wr = wRec(id); int nc = lv < 10 ? wNeed(id, lv) : 0;
            R.Children.Add(row(framed(Panels.GunPv(id), "nest"), "+" + n + T(" карт · ") + W.name, lv < 10 ? wr.cards + " / " + nc + T(" до ур. ") + (lv + 1) + T(" · улучшай в магазине") : T("макс. уровень"), WRAR[W.rar].c));
        }
        if (res.heroes.Count == 0) { int left = HERO_IDS.Count(id => !SAVE.heroes.ContainsKey(id)); if (left > 0) R.Children.Add(row(null, G.StripIcon(T("<small>Героя в этот раз нет. Ещё не открыто: ")) + left, null)); }
        anim.takeT.Text = T("Забрать");
    }
}
