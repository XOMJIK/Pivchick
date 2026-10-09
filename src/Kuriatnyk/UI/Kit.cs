using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Kuriatnyk.Render;
using SkiaSharp;

namespace Kuriatnyk.UI;

/// <summary>Цвета и шрифты из :root в style.css и мелкие строители элементов (аналог mk() из menus.js).</summary>
public static class K
{
    public static readonly Color Ink = Color.Parse("#2b1d14"), Paper = Color.Parse("#fff4dc"), Yolk = Color.Parse("#ffc93a"), Barn = Color.Parse("#c0392b"),
        BarnDark = Color.Parse("#8e2a21"), Straw = Color.Parse("#f6e3b8"), Hp = Color.Parse("#5fcf3f"), Tok = Color.Parse("#4cc3ff"), Btok = Color.Parse("#b06cff"),
        Grass = Color.Parse("#78bf4c"), GrassDk = Color.Parse("#5f9e3c"), Poor = Color.Parse("#d9cdb4"), PoorFg = Color.Parse("#6b5a48");
    public static IBrush B(Color c) => new ImmutableSolidColorBrush(c);
    public static IBrush B(string css) => new ImmutableSolidColorBrush(Color.Parse(css));
    public static readonly IBrush InkB = B(Ink), PaperB = B(Paper), YolkB = B(Yolk), BarnB = B(Barn), StrawB = B(Straw), White = Brushes.White;

    // Rubik и Rubik Bubbles из Assets/Fonts; если файлов нет — системный шрифт
    static bool HasFont(string file) { try { return AssetLoader.Exists(new Uri("avares://Kuriatnyk/Assets/Fonts/" + file)); } catch { return false; } }
    public static FontFamily Body = HasFont("Rubik-Black.ttf") ? new("avares://Kuriatnyk/Assets/Fonts#Rubik") : FontFamily.Default;
    public static FontFamily Display = HasFont("RubikBubbles-Regular.ttf") ? new("avares://Kuriatnyk/Assets/Fonts#Rubik Bubbles") : Body;

    public static string T(string s) => L.T(s);

    public static TextBlock Txt(string text, double size, FontWeight w = FontWeight.Medium, IBrush fg = null, TextAlignment align = TextAlignment.Left, bool wrap = true)
        => new() { Text = text, FontSize = size, FontWeight = w, Foreground = fg ?? InkB, FontFamily = Body, TextAlignment = align, TextWrapping = wrap ? TextWrapping.Wrap : TextWrapping.NoWrap };

    public static BoxShadows Shadow(double y, Color? c = null) => new(new BoxShadow { OffsetX = 0, OffsetY = y, Blur = 0, Spread = 0, Color = c ?? Ink });

    public static Border Box(Control child, IBrush bg, double border = 0, double radius = 0, Thickness? pad = null, double shadow = 0, IBrush borderBrush = null)
        => new() { Child = child, Background = bg, BorderBrush = borderBrush ?? InkB, BorderThickness = new Thickness(border), CornerRadius = new CornerRadius(radius), Padding = pad ?? new Thickness(0), BoxShadow = shadow > 0 ? Shadow(shadow) : default };

    public static StackPanel Col(double gap, params Control[] kids) { var s = new StackPanel { Spacing = gap }; foreach (var k in kids) if (k != null) s.Children.Add(k); return s; }
    public static StackPanel Row(double gap, params Control[] kids) { var s = new StackPanel { Orientation = Orientation.Horizontal, Spacing = gap }; foreach (var k in kids) if (k != null) s.Children.Add(k); return s; }

    /// <summary>Иконки валют: coin, tok, btok, gegg, sch (как .coin/.tok/… в CSS).</summary>
    public static Control Icon(string kind, double size = 14)
    {
        var v = new SkiaView((c, w, h) => DrawIcon(c, kind, w, h)) { Width = kind is "gegg" or "sch" ? size * .82 : size, Height = size, VerticalAlignment = VerticalAlignment.Center };
        return v;
    }
    public static void DrawIcon(Ctx c, string kind, double w, double h)
    {
        double cx = w / 2, cy = h / 2, s = Math.Min(w, h);
        switch (kind)
        {
            case "coin": Hud.Coin(c, cx, cy, s); break;
            case "tok": Hud.Tok(c, cx, cy, s); break;
            case "btok": Hud.Tok(c, cx, cy, s * 1.08, "#b06cff", "#ffe9a8"); break;
            case "gegg":
            case "sch":
                {
                    bool g = kind == "gegg";
                    var gr = c.createRadialGradient(w * .35, h * .3, 0, w * .35, h * .3, h * .85);
                    if (g) { gr.addColorStop(0, "#fffbe0"); gr.addColorStop(.17, "#fffbe0"); gr.addColorStop(.38, "#ffd84a"); gr.addColorStop(.9, "#e9a21c"); }
                    else { gr.addColorStop(0, "#fbefff"); gr.addColorStop(.17, "#fbefff"); gr.addColorStop(.47, "#b46cff"); gr.addColorStop(1, "#5a2aa8"); }
                    c.fillStyle = gr; c.strokeStyle = G.INK; c.lineWidth = 2;
                    c.beginPath(); c.ellipse(cx, h * .58, w / 2 - 1, h * .58 - 1, 0, Math.PI, Ctx.TAU); c.ellipse(cx, h * .58, w / 2 - 1, h * .42 - 1, 0, 0, Math.PI); c.closePath(); c.fill(); c.stroke();
                    break;
                }
            case "lock":
                {
                    c.fillStyle = "#6b5a48"; G.rr(c, w * .1, h * .45, w * .8, h * .5, 3); c.fill();
                    c.strokeStyle = "#6b5a48"; c.lineWidth = 3; c.beginPath(); c.arc(w / 2, h * .45, w * .26, Math.PI, Ctx.TAU); c.stroke();
                    break;
                }
        }
    }

    /// <summary>Мини-разметка для строк с тегами из перевода: &lt;b&gt;, &lt;small&gt;, &lt;i class="coin"&gt;, &lt;span class="k"&gt;.</summary>
    public static TextBlock Rich(string html, double size, FontWeight w = FontWeight.Medium, IBrush fg = null, TextAlignment align = TextAlignment.Left)
    {
        var tb = Txt("", size, w, fg, align); tb.Text = null;
        var inl = tb.Inlines = new InlineCollection();
        bool bold = false, small = false, k = false; int i = 0; html ??= "";
        var sb = new System.Text.StringBuilder();
        void flush()
        {
            if (sb.Length == 0) return; var t = System.Net.WebUtility.HtmlDecode(sb.ToString()); sb.Clear();
            if (k)
            {
                var pill = Box(Txt(t, size * .78, FontWeight.Black, PaperB, wrap: false), InkB, 0, 5, new Thickness(5, 1));
                pill.Margin = new Thickness(0, 0, 4, 0);
                inl.Add(new InlineUIContainer { Child = pill, BaselineAlignment = BaselineAlignment.Center });
                return;
            }
            var r = new Run(t);
            if (bold) r.FontWeight = FontWeight.ExtraBold;
            if (small) { r.FontSize = Math.Round(size * .86); r.FontWeight = FontWeight.SemiBold; r.Foreground = new SolidColorBrush((fg as ISolidColorBrush)?.Color ?? Ink, .75); }
            inl.Add(r);
        }
        while (i < html.Length)
        {
            if (html[i] == '<')
            {
                int e = html.IndexOf('>', i); if (e < 0) { sb.Append(html[i..]); break; }
                var tag = html.Substring(i + 1, e - i - 1).Trim(); i = e + 1;
                flush();
                if (tag == "b") bold = true; else if (tag == "/b") bold = false;
                else if (tag == "small") { small = true; inl.Add(new LineBreak()); } else if (tag == "/small") small = false;
                else if (tag.StartsWith("span") && tag.Contains("\"k\"")) k = true; else if (tag == "/span") k = false;
                else if (tag == "br") inl.Add(new LineBreak());
                else if (tag.StartsWith("i "))
                {
                    var m = System.Text.RegularExpressions.Regex.Match(tag, "class=\"(\\w+)\"");
                    if (m.Success && m.Groups[1].Value != "lockic")
                    {
                        var ic = Icon(m.Groups[1].Value, size * .95); ic.Margin = new Thickness(0, 0, size * .32, 0);
                        inl.Add(new InlineUIContainer { Child = ic, BaselineAlignment = BaselineAlignment.Center });
                    }
                }
                continue;
            }
            sb.Append(html[i]); i++;
        }
        flush();
        return tb;
    }

    /// <summary>Значок редкости/класса (.rar).</summary>
    public static Border Rar(string text, string color, double size = 10.5)
    {
        var t = Txt(text.ToUpper(L.Culture), size, FontWeight.Black, White, wrap: false); t.LetterSpacing = size * .08;
        var b = Box(t, B(color), 2, 8, new Thickness(7, 3)); b.HorizontalAlignment = HorizontalAlignment.Left; return b;
    }

    /// <summary>Значки в ряд с переносом (.badges: flex-wrap).</summary>
    public static WrapPanel Badges(params Control[] kids)
    {
        var w = new WrapPanel { Orientation = Orientation.Horizontal };
        foreach (var k in kids) { k.Margin = new Thickness(0, 0, 5, 5); w.Children.Add(k); }
        w.Margin = new Thickness(0, 0, 0, -5); return w;
    }

    /// <summary>Заголовок секции (.sect): белые заглавные с тенью.</summary>
    public static Control Sect(string text)
    {
        var t = new OutlineText { Text = text.ToUpper(L.Culture), FontSize = 13, Weight = FontWeight.ExtraBold, Fill = White, Stroke = 0, ShadowY = 2, Letter = 1.3 };
        t.Margin = new Thickness(0, 4, 0, 10); return t;
    }
    public static Control Note(string html)
    {
        var t = Rich(html, 13, FontWeight.SemiBold, White); t.MaxWidth = 620; t.HorizontalAlignment = HorizontalAlignment.Left; t.Margin = new Thickness(0, 0, 0, 12);
        t.Effect = new DropShadowEffect { OffsetX = 0, OffsetY = 1, BlurRadius = 0, Color = Ink, Opacity = 1 };
        return t;
    }

    /// <summary>Полоса прогресса (.prog).</summary>
    public static Control Prog(double frac, string color = "#5fcf3f")
    {
        var fill = new Border { Background = B(color), HorizontalAlignment = HorizontalAlignment.Left };
        var g = new Grid(); g.Children.Add(fill);
        var b = Box(g, B("#e3d3ad"), 2, 7); b.Height = 12; b.ClipToBounds = true;
        b.SizeChanged += (_, e) => fill.Width = Math.Max(0, Math.Min(1, frac)) * Math.Max(0, e.NewSize.Width - 4);
        return b;
    }
    public static Control ProgT(string left, string right, IBrush fg = null)
    {
        var g = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        var a = Rich(left, 12, FontWeight.ExtraBold, fg); var b = Txt(right, 12, FontWeight.ExtraBold, fg, wrap: false); Grid.SetColumn(b, 1);
        g.Children.Add(a); g.Children.Add(b); return g;
    }
}

/// <summary>Нажимаемый блок с эффектом «вдавливания», как :active у кнопок в CSS. Не срабатывает, если палец ушёл в прокрутку.</summary>
public class Press : Border
{
    public event Action Click;
    public bool Enabled { get; set; } = true;
    readonly double shadowY;
    bool down;
    public Press(Control child, IBrush bg, double border, double radius, Thickness pad, double shadow, IBrush borderBrush = null)
    {
        Child = child; Background = bg; BorderBrush = borderBrush ?? K.InkB; BorderThickness = new Thickness(border); CornerRadius = new CornerRadius(radius); Padding = pad;
        shadowY = shadow; if (shadow > 0) BoxShadow = K.Shadow(shadow, (borderBrush as ISolidColorBrush)?.Color ?? K.Ink);
        Cursor = new Cursor(StandardCursorType.Hand);
        RenderTransform = new TranslateTransform();
    }
    void Set(bool d)
    {
        down = d; var tt = (TranslateTransform)RenderTransform;
        if (shadowY > 0) { tt.Y = d ? Math.Min(shadowY, 3) : 0; BoxShadow = K.Shadow(d ? 1 : shadowY, (BorderBrush as ISolidColorBrush)?.Color ?? K.Ink); }
    }
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!Enabled) return;
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed || e.Pointer.Type != PointerType.Mouse) { Set(true); e.Pointer.Capture(this); }
    }
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (!down) return; Set(false); e.Pointer.Capture(null);
        var p = e.GetPosition(this);
        if (p.X >= -4 && p.Y >= -4 && p.X <= Bounds.Width + 4 && p.Y <= Bounds.Height + 4) { e.Handled = true; Audio.Init(); Click?.Invoke(); }
    }
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e) { base.OnPointerCaptureLost(e); if (down) Set(false); }
}

/// <summary>Два элемента по краям (justify-content: space-between), а если не влезают — второй переносится вниз (flex-wrap).</summary>
public class SpaceBetweenWrap : Panel
{
    public double Gap = 10;
    bool Wrapped(double w) => Children.Count > 1 && Children[0].DesiredSize.Width + Gap + Children[1].DesiredSize.Width > w;
    protected override Size MeasureOverride(Size avail)
    {
        foreach (var c in Children) c.Measure(new Size(avail.Width, double.PositiveInfinity));
        if (Children.Count < 2) return Children.Count == 1 ? Children[0].DesiredSize : default;
        double w = double.IsInfinity(avail.Width) ? Children[0].DesiredSize.Width + Gap + Children[1].DesiredSize.Width : avail.Width;
        return Wrapped(w) ? new Size(w, Children[0].DesiredSize.Height + Gap + Children[1].DesiredSize.Height)
                          : new Size(w, Math.Max(Children[0].DesiredSize.Height, Children[1].DesiredSize.Height));
    }
    protected override Size ArrangeOverride(Size fin)
    {
        if (Children.Count == 0) return fin;
        var a = Children[0].DesiredSize;
        if (Children.Count == 1) { Children[0].Arrange(new Rect(0, 0, Math.Min(a.Width, fin.Width), a.Height)); return fin; }
        var b = Children[1].DesiredSize;
        if (Wrapped(fin.Width)) { Children[0].Arrange(new Rect(0, 0, Math.Min(a.Width, fin.Width), a.Height)); Children[1].Arrange(new Rect(0, a.Height + Gap, Math.Min(b.Width, fin.Width), b.Height)); }
        else { double h = Math.Max(a.Height, b.Height); Children[0].Arrange(new Rect(0, (h - a.Height) / 2, a.Width, a.Height)); Children[1].Arrange(new Rect(fin.Width - b.Width, (h - b.Height) / 2, b.Width, b.Height)); }
        return fin;
    }
}

/// <summary>Сетка как grid-template-columns: repeat(auto-fill, minmax(min, 1fr)); элементы в ряду одной высоты.</summary>
public class AutoGrid : Panel
{
    public double Min = 150, Gap = 12;
    int Cols(double w) => Math.Max(1, (int)Math.Floor((w + Gap) / (Min + Gap)));
    protected override Size MeasureOverride(Size avail)
    {
        double w = double.IsInfinity(avail.Width) ? Min * 2 + Gap : avail.Width; int n = Cols(w); double cw = (w - Gap * (n - 1)) / n, total = 0, rowH = 0;
        for (int i = 0; i < Children.Count; i++)
        {
            Children[i].Measure(new Size(cw, double.PositiveInfinity)); rowH = Math.Max(rowH, Children[i].DesiredSize.Height);
            if (i % n == n - 1 || i == Children.Count - 1) { total += rowH + (i == Children.Count - 1 ? 0 : Gap); rowH = 0; }
        }
        return new Size(w, total);
    }
    protected override Size ArrangeOverride(Size fin)
    {
        int n = Cols(fin.Width); double cw = (fin.Width - Gap * (n - 1)) / n, y = 0;
        for (int r = 0; r * n < Children.Count; r++)
        {
            double h = 0; for (int k = r * n; k < Math.Min(Children.Count, r * n + n); k++) h = Math.Max(h, Children[k].DesiredSize.Height);
            for (int k = r * n; k < Math.Min(Children.Count, r * n + n); k++) Children[k].Arrange(new Rect((k - r * n) * (cw + Gap), y, cw, h));
            y += h + Gap;
        }
        return fin;
    }
}

/// <summary>Текст с обводкой и тенью — заголовки в стиле -webkit-text-stroke + text-shadow.</summary>
public class OutlineText : Control
{
    public string Text = ""; public double FontSize = 30; public FontWeight Weight = FontWeight.Normal; public bool Display;
    public IBrush Fill = K.YolkB; public double Stroke = 2, ShadowY = 4, Letter; public TextAlignment Align = TextAlignment.Left; public bool NoWrap;
    FormattedText Make(double maxW)
    {
        var ft = new FormattedText(Text ?? "", L.Culture, FlowDirection.LeftToRight, new Typeface(Display ? K.Display : K.Body, FontStyle.Normal, Weight), FontSize, Fill);
        if (!NoWrap && !double.IsInfinity(maxW) && maxW > 0) ft.MaxTextWidth = maxW;
        ft.TextAlignment = Align;
        return ft;
    }
    protected override Size MeasureOverride(Size avail)
    {
        var ft = Make(avail.Width - Stroke * 2);
        double w = ft.WidthIncludingTrailingWhitespace + Stroke * 2 + 1;
        return new Size(NoWrap ? w : Math.Min(avail.Width, w), ft.Height + ShadowY + Stroke);
    }
    public override void Render(DrawingContext ctx)
    {
        var ft = Make(Bounds.Width - Stroke * 2);
        double x = Stroke; if (Align == TextAlignment.Center) { ft.MaxTextWidth = Math.Max(1, Bounds.Width - Stroke * 2); }
        var origin = new Point(x, Stroke / 2);
        var geo = ft.BuildGeometry(origin);
        if (geo == null) return;
        if (ShadowY > 0) using (ctx.PushTransform(Matrix.CreateTranslation(0, ShadowY))) { ctx.DrawGeometry(K.InkB, Stroke > 0 ? new Pen(K.InkB, Stroke * 2, lineJoin: PenLineJoin.Round) : null, geo); }
        if (Stroke > 0) ctx.DrawGeometry(null, new Pen(K.InkB, Stroke * 2, lineJoin: PenLineJoin.Round), geo);
        ctx.DrawGeometry(Fill, null, geo);
    }
}

/// <summary>Элемент, который рисует через Ctx (Skia): превью героев, стволов, сундуков, иконки.
/// Кадр записывается в SKPicture в потоке интерфейса и проигрывается в потоке отрисовки.</summary>
public class SkiaView : Control
{
    public Action<Ctx, double, double> DrawFn;
    public bool Animate;
    readonly Ctx ctx = new();
    SKPicture pic; Size picSize;
    public SkiaView(Action<Ctx, double, double> draw = null) { DrawFn = draw; ClipToBounds = false; }
    public void Redraw() { pic = null; InvalidateVisual(); } // старую картинку может ещё рисовать поток отрисовки — освободит сборщик мусора
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e) { base.OnDetachedFromVisualTree(e); pic = null; }

    public override void Render(DrawingContext context)
    {
        var size = Bounds.Size; if (size.Width < 1 || size.Height < 1 || DrawFn == null) return;
        SKPicture p;
        if (!Animate && pic != null && picSize == size) p = pic;
        else
        {
            using var rec = new SKPictureRecorder();
            var cnv = rec.BeginRecording(new SKRect(-200, -200, (float)size.Width + 200, (float)size.Height + 200));
            ctx.Reset(); ctx.Begin(cnv);
            try { DrawFn(ctx, size.Width, size.Height); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
            p = rec.EndRecording();
            if (!Animate) { pic = p; picSize = size; }
        }
        context.Custom(new PicOp(new Rect(size), p, Animate));
        if (Animate) Avalonia.Threading.Dispatcher.UIThread.Post(InvalidateVisual, Avalonia.Threading.DispatcherPriority.Background);
    }

    sealed class PicOp : ICustomDrawOperation
    {
        readonly SKPicture pic; readonly bool own;
        public PicOp(Rect b, SKPicture p, bool owns) { Bounds = b; pic = p; own = owns; }
        public Rect Bounds { get; }
        public bool HitTest(Point p) => false;
        public bool Equals(ICustomDrawOperation other) => false;
        public void Render(ImmediateDrawingContext context)
        {
            var lease = context.TryGetFeature<ISkiaSharpApiLeaseFeature>(); if (lease == null) return;
            using var l = lease.Lease(); l.SkCanvas.DrawPicture(pic);
        }
        public void Dispose() { if (own) pic.Dispose(); }
    }
}
