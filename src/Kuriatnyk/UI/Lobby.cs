using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Kuriatnyk.Render;
using SkiaSharp;
using static Kuriatnyk.D;
using static Kuriatnyk.Store;
using static Kuriatnyk.UI.K;

namespace Kuriatnyk.UI;

/// <summary>Иконки из SVG в index.html: рисуются через SKPath.ParseSvgPathData.</summary>
public static class Svg
{
    public record Item(string d, string fill, string stroke = null, double sw = 0);
    public static string Circle(double cx, double cy, double r) => $"M{cx - r} {cy}a{r} {r} 0 1 0 {2 * r} 0a{r} {r} 0 1 0 {-2 * r} 0z".Replace(',', '.');
    public static Control View(double box, double size, params Item[] items) => new SkiaView((c, w, h) =>
    {
        var cv = c.C; cv.Save(); cv.Scale((float)(w / box), (float)(h / box));
        foreach (var it in items)
        {
            using var p = SKPath.ParseSvgPathData(it.d); if (p == null) continue;
            using var paint = new SKPaint { IsAntialias = true };
            if (it.fill != null) { paint.Style = SKPaintStyle.Fill; paint.Color = Ctx.Col(it.fill); cv.DrawPath(p, paint); }
            if (it.stroke != null) { paint.Style = SKPaintStyle.Stroke; paint.Color = Ctx.Col(it.stroke); paint.StrokeWidth = (float)it.sw; paint.StrokeCap = SKStrokeCap.Round; paint.StrokeJoin = SKStrokeJoin.Round; cv.DrawPath(p, paint); }
        }
        cv.Restore();
    }) { Width = size, Height = size };

    public static Control Heroes(double s) => View(32, s, new("M8 26c0-7 3-12 9-12 4 0 7 3 7 7v5z", "#fff4dc"), new(Circle(19, 10, 5), "#fff4dc"), new("M16 5a2 2 0 0 1 3-1 2 2 0 0 1 3 1", "#e8322a"), new("M24 9l4 1.5-4 1.5z", "#f6a723"), new(Circle(20.5, 9.5, 1.2), "#2b1d14"));
    public static Control Chest(double s) => View(32, s, new("M6 15h20l-2 11H8z", "#d39a4f"), new("M5 12q11-8 22 0v4H5z", "#fff4dc"), new("M8 12h4v4H8zm8 0h4v4h-4zm8 0h3v4h-3", "#e8322a"), new("M8 4q8-6 16 0", null, "#c98a43", 2.5));
    public static Control Shop(double s) => View(32, s, new("M7 11h18l-2 16H9z", "#ffc93a"), new("M11 11a5 5 0 0 1 10 0", null, "#fff4dc", 2.5), new(Circle(16, 19, 3.5), "#fff4dc"));
    public static Control Info(double s) => View(32, s, new(Circle(16, 16, 11), "#ffc93a"), new(Circle(16, 10.5, 1.9), "#2b1d14"), new("M16 14.5v8", null, "#2b1d14", 3));
    public static Control Help(double s) => View(32, s, new(Circle(16, 16, 11), "#4cc3ff"), new("M12.5 13a3.5 3.5 0 1 1 5 3.2c-1 .5-1.5 1.2-1.5 2.3", null, "#fff", 2.6), new(Circle(16, 22.5, 1.6), "#fff"));
    public static Control Gear(double s) => View(24, s, new("M10.3 2h3.4l.5 2.6 1.9.8 2.2-1.5 2.4 2.4-1.5 2.2.8 1.9 2.6.5v3.4l-2.6.5-.8 1.9 1.5 2.2-2.4 2.4-2.2-1.5-1.9.8-.5 2.6h-3.4l-.5-2.6-1.9-.8-2.2 1.5-2.4-2.4 1.5-2.2-.8-1.9L2 13.7v-3.4l2.6-.5.8-1.9-1.5-2.2 2.4-2.4 2.2 1.5 1.9-.8z", "#fff4dc", "#2b1d14", 1.6), new(Circle(12, 12, 3.6), "#2b1d14"));
}

/// <summary>Главное меню (лобби): бренд и кошелёк, сцена с героем, режимы, разделы. Порт .lobby из index.html/style.css.</summary>
public sealed class Lobby : Grid
{
    readonly MainView mv;
    readonly Border shadeV = new() { IsHitTestVisible = false }, shadeH = new() { IsHitTestVisible = false };
    readonly Grid grid = new();
    Control top, hero, modes, nav; Border plate; Control turn;
    TextBlock heroName, heroLine, storySub, bestText; WrapPanel heroBadges; StackPanel wallet; Border chestBadge;
    StackPanel modesStack; Grid navGrid;
    readonly OutlineText logo = new() { Display = true, FontSize = 34, Stroke = 1.6, ShadowY = 3 };

    public Lobby(MainView mv)
    {
        this.mv = mv;
        Children.Add(shadeV); Children.Add(shadeH); Children.Add(grid);
        Build();
        SizeChanged += (_, _) => Relayout();
    }

    bool Land => Bounds.Width > Bounds.Height;
    bool Short => Land && Bounds.Height <= 500;

    void Build()
    {
        // --- шапка: логотип, версия, настройки, кошелёк ---
        logo.Text = T("Боевой Курятник");
        var ver = new Press(Txt("v" + GAME_VERSION, 12, FontWeight.Black, YolkB, wrap: false), InkB, 2, 8, new Thickness(9, 5), 0, YolkB) { VerticalAlignment = VerticalAlignment.Center };
        ver.Click += () => Overlays.OpenInfo(mv);
        var gear = new Press(Svg.Gear(26), BarnB, 2.5, 12, new Thickness(5), 3) { Width = 40, Height = 40, VerticalAlignment = VerticalAlignment.Center };
        gear.Click += () => Overlays.OpenSettings(mv);
        var brand = Row(8, logo, ver, gear); brand.VerticalAlignment = VerticalAlignment.Center;
        wallet = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center };
        var topP = new SpaceBetweenWrap(); topP.Children.Add(brand); topP.Children.Add(wallet);
        top = topP;

        // --- сцена с героем: табличка с именем ---
        heroBadges = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Center };
        heroName = Txt("", 17, FontWeight.Black, White, TextAlignment.Center);
        heroLine = Txt("", 12, FontWeight.SemiBold, B("#d9fff4dc"), TextAlignment.Center);
        var swap = new Press(Txt(T("Сменить бойца"), 14, FontWeight.ExtraBold, InkB, wrap: false), YolkB, 2, 10, new Thickness(14, 9), 2);
        swap.Click += () => mv.OpenPanel("heroes");
        var bg = new Press(Txt(T("Фон"), 14, FontWeight.ExtraBold, InkB, wrap: false), PaperB, 2, 10, new Thickness(14, 9), 2);
        bg.Click += () => Overlays.OpenBg(mv);
        var chips = Row(6, swap, bg); chips.HorizontalAlignment = HorizontalAlignment.Center; chips.Margin = new Thickness(0, 4, 0, 0);
        var plateCol = Col(4, heroBadges, heroName, heroLine, chips);
        plate = Box(plateCol, B("#c72b1d14"), 3, 16, new Thickness(14, 8, 14, 10), 4);
        plate.HorizontalAlignment = HorizontalAlignment.Center; plate.VerticalAlignment = VerticalAlignment.Bottom;
        var heroG = new Grid(); heroG.Children.Add(plate); hero = heroG;

        builtShort = Short;
        modes = BuildModes(builtShort); nav = BuildNav(builtShort);
        grid.Children.Add(top); grid.Children.Add(hero); grid.Children.Add(modes); grid.Children.Add(nav);
        Relayout(); Refresh();
    }

    bool builtShort;
    /// <summary>Кнопки режимов; на низком экране (max-height:500px, landscape) — компактнее.</summary>
    Control BuildModes(bool sh)
    {
        storySub = Txt("", 12, FontWeight.SemiBold, B("#e6ffffff"));
        var storyT = new OutlineText { Text = T("Сюжет"), Display = true, FontSize = sh ? 24 : 28, Fill = White, Stroke = 1, ShadowY = 0 };
        var storyCol = Col(sh ? 1 : 3, storyT, storySub);
        var arrow = new SkiaView((c, w, h) => { c.fillStyle = G.INK; c.beginPath(); c.moveTo(0, 2); c.lineTo(16, 13); c.lineTo(0, 24); c.closePath(); c.fill(); c.fillStyle = "#ffc93a"; c.beginPath(); c.moveTo(0, 0); c.lineTo(16, 11); c.lineTo(0, 22); c.closePath(); c.fill(); }) { Width = 16, Height = 24, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
        var storyG = new Grid(); storyG.Children.Add(storyCol); storyG.Children.Add(arrow);
        // красная кнопка с косой светлой полосой (linear-gradient 100deg в .mode.main)
        var shine = new LinearGradientBrush { StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative), EndPoint = new RelativePoint(1, .18, RelativeUnit.Relative) };
        foreach (var (c, at) in new[] { ("#c0392b", 0.0), ("#c0392b", .6), ("#c85144", .6), ("#c85144", .7), ("#c0392b", .7), ("#c0392b", 1.0) }) shine.GradientStops.Add(new GradientStop(Color.Parse(c), at));
        var story = new Press(storyG, shine, 3, 16, sh ? new Thickness(16, 8, 26, 8) : new Thickness(18, 12, 26, 12), 4) { MinHeight = sh ? 58 : 70 };
        story.Click += () => mv.OpenPanel("storyP");
        bestText = Txt("", 12, FontWeight.SemiBold, B("#b32b1d14"));
        var pad = sh ? new Thickness(14, 7) : new Thickness(16, 11);
        double tsz = sh ? 15 : 17, minH = sh ? 46 : 54;
        var endless = new Press(Col(3, Txt(T("Бесконечный бой"), tsz, FontWeight.Black), bestText), PaperB, 3, 16, pad, 4) { MinHeight = minH };
        endless.Click += () => { Audio.Init(); G.startGame("endless"); };
        Control locked(string title, string msg)
        {
            var soon = Box(Txt(T("Скоро").ToUpper(L.Culture), 10.5, FontWeight.ExtraBold, B("#f6e3b8"), wrap: false), B("#6b5a48"), 0, 6, new Thickness(7, 2));
            soon.HorizontalAlignment = HorizontalAlignment.Left;
            var ic = Icon("lock", 18); ic.Width = 18; ic.Height = 20; ic.Margin = new Thickness(0, 0, 12, 0);
            var row = new DockPanel(); DockPanel.SetDock(ic, Dock.Left); row.Children.Add(ic); row.Children.Add(Col(3, Txt(T(title), tsz, FontWeight.Black, B("#6b5a48")), soon));
            var p = new Press(row, B(Poor), 3, 16, pad, 4, B("#6b5a48")) { MinHeight = minH };
            p.Click += () => mv.Toast(T(msg)); return p;
        }
        turn = Txt(T("Удобнее играть, повернув телефон горизонтально"), 13, FontWeight.Bold, PaperB, TextAlignment.Center);
        turn.IsVisible = !Land;
        modesStack = Col(9, story, endless, locked("Многопользовательская игра", "Многопользовательская игра появится в одном из следующих обновлений"), locked("Режим Soul Knight", "Режим Soul Knight пока в разработке — следи за обновлениями"), turn);
        return modesStack;
    }
    /// <summary>Нижние разделы: герои, сундуки, магазин, об игре, как играть.</summary>
    Control BuildNav(bool sh)
    {
        Control navBtn(Func<double, Control> icon, string label, Action click, bool badge = false)
        {
            var t = Txt(T(label), 12, FontWeight.ExtraBold, PaperB, TextAlignment.Center); t.TextWrapping = T(label).Contains(' ') ? TextWrapping.Wrap : TextWrapping.NoWrap; t.LineHeight = 13; t.HorizontalAlignment = HorizontalAlignment.Center;
            var col = Col(3, icon(sh ? 24 : 30), t);
            ((Control)col.Children[0]).HorizontalAlignment = HorizontalAlignment.Center;
            var g = new Grid(); g.Children.Add(col); col.VerticalAlignment = VerticalAlignment.Center;
            var p = new Press(g, B("#3a2a1e"), 3, 14, sh ? new Thickness(4, 5, 4, 6) : new Thickness(4, 7, 4, 8), 4) { MinHeight = sh ? 56 : 62 };
            p.Click += click;
            if (!badge) return p;
            chestBadge = Box(Txt("!", 12, FontWeight.Black, White, TextAlignment.Center), BarnB, 3, 11, new Thickness(4, 0));
            chestBadge.MinWidth = 22; chestBadge.Height = 22; chestBadge.HorizontalAlignment = HorizontalAlignment.Right; chestBadge.VerticalAlignment = VerticalAlignment.Top; chestBadge.Margin = new Thickness(0, -8, -6, 0);
            var wrap = new Grid(); wrap.Children.Add(p); wrap.Children.Add(chestBadge); return wrap;
        }
        navGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*,*,*,*") };
        var items = new[]
        {
            navBtn(Svg.Heroes, "Герои", () => mv.OpenPanel("heroes")),
            navBtn(Svg.Chest, "Сундуки", () => mv.OpenPanel("chestP"), true),
            navBtn(Svg.Shop, "Магазин", () => mv.OpenPanel("shop")),
            navBtn(Svg.Info, "Об игре", () => Overlays.OpenInfo(mv)),
            navBtn(Svg.Help, "Как играть", () => Overlays.OpenHelp(mv)),
        };
        for (int i = 0; i < items.Length; i++) { Grid.SetColumn(items[i], i); items[i].Margin = new Thickness(i == 0 ? 0 : 3.5, 0, i == 4 ? 0 : 3.5, 0); navGrid.Children.Add(items[i]); }
        return navGrid;
    }

    void Relayout()
    {
        double W = Bounds.Width, H = Bounds.Height; if (W < 1) return;
        var safe = mv.Safe;
        double side = W >= 900 && H >= 560 ? Math.Max(32, (W - 1180) / 2) : 16;
        grid.Margin = new Thickness(safe.Left + side, safe.Top + 12, safe.Right + side, safe.Bottom + 12);
        grid.RowDefinitions.Clear(); grid.ColumnDefinitions.Clear();
        bool land = Land, sh = Short;
        if (sh != builtShort)
        {   // высота экрана перешла порог — пересобрать режимы и разделы в нужном размере
            builtShort = sh; grid.Children.Remove(modes); grid.Children.Remove(nav);
            modes = BuildModes(sh); nav = BuildNav(sh); grid.Children.Add(modes); grid.Children.Add(nav); Refresh();
        }
        logo.FontSize = sh ? 24 : Math.Clamp(W * .07, 24, 34); logo.InvalidateMeasure();
        turn.IsVisible = !land;
        if (!land)
        {
            grid.RowDefinitions = new RowDefinitions("Auto,*,Auto,Auto"); grid.RowSpacing = 12;
            Place(top, 0, 0, 1, 1); Place(hero, 1, 0, 1, 1); Place(modes, 2, 0, 1, 1); Place(nav, 3, 0, 1, 1);
            ((Control)nav).MaxWidth = double.PositiveInfinity; nav.HorizontalAlignment = HorizontalAlignment.Stretch;
            modes.VerticalAlignment = VerticalAlignment.Top;
            shadeV.Background = Grad(true, (0, .55), (.2, 0), (.42, 0), (.72, .55), (1, .7)); shadeH.Background = null;
        }
        else
        {
            double right = Math.Max(270, W * .36);
            grid.ColumnDefinitions = new ColumnDefinitions($"*,{right.ToString(System.Globalization.CultureInfo.InvariantCulture)}"); grid.ColumnSpacing = sh ? 14 : 18;
            grid.RowDefinitions = new RowDefinitions("Auto,*,Auto"); grid.RowSpacing = sh ? 8 : 10;
            Place(top, 0, 0, 1, 2); Place(hero, 1, 0, 1, 1); Place(nav, 2, 0, 1, 1); Place(modes, 1, 1, 2, 1);
            ((Control)nav).MaxWidth = 460; nav.HorizontalAlignment = HorizontalAlignment.Center; nav.VerticalAlignment = VerticalAlignment.Bottom;
            modes.VerticalAlignment = VerticalAlignment.Center;
            shadeV.Background = Grad(true, (0, .5), (.22, 0), (.7, 0), (1, .5)); shadeH.Background = Grad(false, (.4, 0), (.75, .6));
        }
    }
    static void Place(Control c, int r, int col, int rs, int cs) { Grid.SetRow(c, r); Grid.SetColumn(c, col); Grid.SetRowSpan(c, rs); Grid.SetColumnSpan(c, cs); }
    static IBrush Grad(bool vertical, params (double at, double a)[] stops)
    {
        var g = new LinearGradientBrush { StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative), EndPoint = vertical ? new RelativePoint(0, 1, RelativeUnit.Relative) : new RelativePoint(1, 0, RelativeUnit.Relative) };
        foreach (var (at, a) in stops) g.GradientStops.Add(new GradientStop(Color.FromArgb((byte)(a * 255), 43, 29, 20), at));
        return g;
    }

    /// <summary>Где на экране стоит герой: по центру сцены, над табличкой (menuAnchor из update.js).</summary>
    public void UpdateAnchor()
    {
        if (!IsVisible || plate.Bounds.Width < 1) return;
        var r = hero.TranslatePoint(new Point(hero.Bounds.Width / 2, 0), mv.Game); var q = plate.TranslatePoint(new Point(0, 0), mv.Game);
        if (r == null || q == null) return;
        G.MenuAnchor = (r.Value.X, Math.Max(r.Value.Y + 60, q.Value.Y - 14));
    }

    static Control Pill(string icon, string val, string suffix = null)
    {
        var row = Row(0, Icon(icon, 13 * .95), Txt(val, 13, FontWeight.Bold, YolkB, wrap: false));
        ((Control)row.Children[0]).Margin = new Thickness(0, 0, 4, 0);
        if (suffix != null) row.Children.Add(Txt(suffix, 13, FontWeight.Bold, B("#99fff4dc"), wrap: false));
        var b = Box(row, InkB, 0, 10, new Thickness(9, 6)); b.BoxShadow = Shadow(2, Color.FromArgb(64, 0, 0, 0)); return b;
    }

    public void Refresh()
    {
        if (heroName == null) return;
        logo.Text = T("Боевой Курятник"); logo.InvalidateMeasure(); logo.InvalidateVisual();
        wallet.Children.Clear();
        wallet.Children.Add(Pill("coin", L.Fmt(SAVE.coins))); wallet.Children.Add(Pill("tok", L.Fmt(SAVE.tokens), "/100"));
        wallet.Children.Add(Pill("btok", SAVE.btokens.ToString(), "/10")); wallet.Children.Add(Pill("gegg", SAVE.eggs.ToString()));
        var H = HEROES[SAVE.hero]; int sq = squadIds().Count;
        heroName.Text = H.name;
        heroLine.Text = T("Ур. ") + heroLv(SAVE.hero) + " · " + WEAP[gunOf(SAVE.hero)].name + (sq > 0 ? T(" · отряд: ") + sq : "");
        heroBadges.Children.Clear();
        var b1 = Rar(RAR[H.rar].name, RAR[H.rar].c, 9.5); b1.Margin = new Thickness(0, 0, 5, 0); heroBadges.Children.Add(b1);
        heroBadges.Children.Add(Rar(CLASSES[H.cls].name, CLASSES[H.cls].c, 9.5));
        int done = SAVE.story.done.Count;
        if (done >= STORY.Count) storySub.Text = T("Все 4 главы пройдены · можно переигрывать");
        else
        {
            var Lv = STORY.FirstOrDefault(l => !SAVE.story.done.Contains(l.gid)) ?? STORY[0];
            storySub.Text = T("Глава ") + (Lv.ch + 1) + (L.En ? " “" : " «") + CHAPTERS[Lv.ch].name + (L.En ? "” · " : "» · ") + CHAPTERS[Lv.ch].levels.Count(l => SAVE.story.done.Contains(l.gid)) + "/20";
        }
        bestText.Text = SAVE.best.score > 0 ? T("Рекорд: волна ") + SAVE.best.wave + " · " + L.Fmt(SAVE.best.score) + T(" очк.") : T("Рекорда пока нет — поставь первый");
        chestBadge.IsVisible = SAVE.tokens >= 100 || SAVE.btokens >= 10 || SAVE.schests > 0;
    }
}
