using System;
using System.Collections.Generic;
using System.Globalization;
using SkiaSharp;

namespace Kuriatnyk.Render;

/// <summary>Стиль заливки або обводки: колір або градієнт (як fillStyle у canvas).</summary>
public readonly struct Style
{
    public readonly SKColor Color;
    public readonly Grad Grad;
    Style(SKColor c, Grad g) { Color = c; Grad = g; }
    public static implicit operator Style(string css) => new(Ctx.Col(css), null);
    public static implicit operator Style(Grad g) => new(SKColors.Black, g);
    public static implicit operator Style(SKColor c) => new(c, null);
}

/// <summary>Лінійний або радіальний градієнт (createLinearGradient / createRadialGradient).</summary>
public sealed class Grad
{
    readonly bool radial;
    readonly float x0, y0, r0, x1, y1, r1;
    readonly List<float> pos = new();
    readonly List<SKColor> cols = new();
    SKShader shader;
    internal Grad(float x0, float y0, float x1, float y1) { this.x0 = x0; this.y0 = y0; this.x1 = x1; this.y1 = y1; }
    internal Grad(float x0, float y0, float r0, float x1, float y1, float r1) { radial = true; this.x0 = x0; this.y0 = y0; this.r0 = r0; this.x1 = x1; this.y1 = y1; this.r1 = r1; }
    public void addColorStop(double offset, string css) { pos.Add((float)offset); cols.Add(Ctx.Col(css)); shader?.Dispose(); shader = null; }
    internal SKShader Shader
    {
        get
        {
            if (shader != null) return shader;
            var c = cols.ToArray(); var p = pos.ToArray();
            if (c.Length == 0) { c = new[] { SKColors.Transparent, SKColors.Transparent }; p = new[] { 0f, 1f }; }
            shader = radial
                ? SKShader.CreateTwoPointConicalGradient(new SKPoint(x0, y0), r0, new SKPoint(x1, y1), r1, c, p, SKShaderTileMode.Clamp)
                : SKShader.CreateLinearGradient(new SKPoint(x0, y0), new SKPoint(x1, y1), c, p, SKShaderTileMode.Clamp);
            return shader;
        }
    }
}

/// <summary>
/// Обгортка над SKCanvas з API як у CanvasRenderingContext2D. Імена навмисно як у JS,
/// щоб код відмальовки з веб-версії переносився рядок у рядок.
/// </summary>
public sealed class Ctx
{
    public const double TAU = Math.PI * 2;
    public SKCanvas C { get; private set; }
    SKMatrix baseM = SKMatrix.Identity;

    readonly SKPath path = new() { FillType = SKPathFillType.Winding };
    readonly SKPath tmp = new();
    bool hasPoint;
    readonly SKPaint paint = new() { IsAntialias = true };

    struct State
    {
        public Style Fill, Stroke;
        public float LineWidth, Alpha, DashOff, ShadowBlur;
        public SKStrokeCap Cap; public SKStrokeJoin Join;
        public float[] Dash;
        public string Font, Align;
        public SKColor ShadowColor;
    }
    State st;
    readonly Stack<State> stack = new();

    public Ctx() { Reset(); }

    /// <summary>Значення за замовчуванням, як у нового canvas.</summary>
    public void Reset()
    {
        st = new State
        {
            Fill = SKColors.Black, Stroke = SKColors.Black, LineWidth = 1, Alpha = 1,
            Cap = SKStrokeCap.Butt, Join = SKStrokeJoin.Miter, Font = "10px sans-serif", Align = "start",
            ShadowColor = SKColors.Transparent
        };
        stack.Clear();
    }

    /// <summary>Почати малювати на новому полотні; стан стилів зберігається між кадрами, як у JS.</summary>
    public void Begin(SKCanvas canvas)
    {
        C = canvas; stack.Clear(); baseM = canvas.TotalMatrix; path.Reset(); hasPoint = false;
    }

    // ---------- стан ----------
    public Style fillStyle { get => st.Fill; set => st.Fill = value; }
    public Style strokeStyle { get => st.Stroke; set => st.Stroke = value; }
    public double lineWidth { get => st.LineWidth; set { if (value > 0 && !double.IsInfinity(value)) st.LineWidth = (float)value; } }
    public double globalAlpha { get => st.Alpha; set { if (value >= 0 && value <= 1) st.Alpha = (float)value; else if (value > 1) st.Alpha = 1; } }
    public double lineDashOffset { get => st.DashOff; set => st.DashOff = (float)value; }
    public double shadowBlur { get => st.ShadowBlur; set => st.ShadowBlur = (float)value; }
    public string shadowColor { set => st.ShadowColor = Col(value); }
    public string lineCap { set => st.Cap = value == "round" ? SKStrokeCap.Round : value == "square" ? SKStrokeCap.Square : SKStrokeCap.Butt; }
    public string lineJoin { set => st.Join = value == "round" ? SKStrokeJoin.Round : value == "bevel" ? SKStrokeJoin.Bevel : SKStrokeJoin.Miter; }
    public string font { get => st.Font; set => st.Font = value; }
    public string textAlign { get => st.Align; set => st.Align = value; }
    public void setLineDash(double[] d)
    {
        if (d == null || d.Length == 0) { st.Dash = null; return; }
        var a = new float[d.Length % 2 == 1 ? d.Length * 2 : d.Length];
        for (int i = 0; i < a.Length; i++) a[i] = (float)d[i % d.Length];
        st.Dash = a;
    }

    public void save() { stack.Push(st); C.Save(); }
    /// <summary>save() со слоем: всё до restore() внутри прямоугольника перекрашивается матрицей цвета 4×5 (так рисуются каменные статуи).</summary>
    public void saveLayerColor(double x, double y, double w, double h, float[] matrix)
    {
        stack.Push(st);
        using var p = new SKPaint { ColorFilter = SKColorFilter.CreateColorMatrix(matrix) };
        C.SaveLayer(SKRect.Create((float)x, (float)y, (float)w, (float)h), p);
    }
    public void restore() { if (stack.Count == 0) return; st = stack.Pop(); C.Restore(); }

    // ---------- трансформації ----------
    public void translate(double x, double y) => C.Translate((float)x, (float)y);
    public void rotate(double a) => C.RotateRadians((float)a);
    public void scale(double x, double y) => C.Scale((float)x, (float)y);
    public void transform(double a, double b, double c, double d, double e, double f)
    {
        var m = new SKMatrix((float)a, (float)c, (float)e, (float)b, (float)d, (float)f, 0, 0, 1);
        C.Concat(ref m);
    }
    public void setTransform(double a, double b, double c, double d, double e, double f)
    {
        var m = new SKMatrix((float)a, (float)c, (float)e, (float)b, (float)d, (float)f, 0, 0, 1);
        C.SetMatrix(baseM.PreConcat(m));
    }
    public void resetTransform() => C.SetMatrix(baseM);

    // ---------- контур ----------
    public void beginPath() { path.Reset(); hasPoint = false; }
    public void moveTo(double x, double y) { path.MoveTo((float)x, (float)y); hasPoint = true; }
    public void lineTo(double x, double y)
    {
        if (!hasPoint) { moveTo(x, y); return; }
        path.LineTo((float)x, (float)y);
    }
    public void closePath() { if (hasPoint) path.Close(); }
    public void quadraticCurveTo(double cx, double cy, double x, double y)
    {
        if (!hasPoint) moveTo(cx, cy);
        path.QuadTo((float)cx, (float)cy, (float)x, (float)y);
    }
    public void bezierCurveTo(double c1x, double c1y, double c2x, double c2y, double x, double y)
    {
        if (!hasPoint) moveTo(c1x, c1y);
        path.CubicTo((float)c1x, (float)c1y, (float)c2x, (float)c2y, (float)x, (float)y);
    }
    public void arcTo(double x1, double y1, double x2, double y2, double r)
    {
        if (!hasPoint) moveTo(x1, y1);
        path.ArcTo((float)x1, (float)y1, (float)x2, (float)y2, (float)r);
    }
    public void rect(double x, double y, double w, double h)
    {
        path.AddRect(SKRect.Create((float)x, (float)y, (float)w, (float)h));
        path.MoveTo((float)x, (float)y); hasPoint = true;
    }
    public void arc(double x, double y, double r, double a0, double a1, bool ccw = false) => ellipse(x, y, r, r, 0, a0, a1, ccw);

    public void ellipse(double x, double y, double rx, double ry, double rot, double a0, double a1, bool ccw = false)
    {
        if (rx < 0 || ry < 0) return;
        double sweep;
        if (!ccw) { var d = a1 - a0; sweep = d >= TAU ? TAU : Mod(d, TAU); if (sweep == 0 && d > 0) sweep = TAU; }
        else { var d = a0 - a1; sweep = d >= TAU ? -TAU : -Mod(d, TAU); if (sweep == 0 && d > 0) sweep = -TAU; }
        var oval = new SKRect((float)-rx, (float)-ry, (float)rx, (float)ry);
        float startDeg = (float)(a0 * 180 / Math.PI), sw = (float)(sweep * 180 / Math.PI);
        tmp.Reset();
        if (Math.Abs(sw) >= 359.99f)
        {   // полное кольцо — двумя половинами, чтобы Skia не схлопнула дугу
            tmp.ArcTo(oval, startDeg, sw / 2, true);
            tmp.ArcTo(oval, startDeg + sw / 2, sw / 2, false);
        }
        else tmp.ArcTo(oval, startDeg, sw, true);
        var m = SKMatrix.CreateRotation((float)rot).PostConcat(SKMatrix.CreateTranslation((float)x, (float)y));
        if (!hasPoint) { path.AddPath(tmp, ref m, SKPathAddMode.Append); hasPoint = true; }
        else path.AddPath(tmp, ref m, SKPathAddMode.Extend);
    }
    static double Mod(double a, double b) { var r = a % b; return r < 0 ? r + b : r; }

    // ---------- малювання ----------
    SKPaint Prep(Style s, bool stroke)
    {
        paint.Reset();
        paint.IsAntialias = true;
        paint.Style = stroke ? SKPaintStyle.Stroke : SKPaintStyle.Fill;
        if (s.Grad != null) { paint.Shader = s.Grad.Shader; paint.Color = new SKColor(0, 0, 0, (byte)Math.Round(255 * st.Alpha)); }
        else { var c = s.Color; paint.Color = st.Alpha >= 1 ? c : c.WithAlpha((byte)Math.Round(c.Alpha * st.Alpha)); }
        if (stroke)
        {
            paint.StrokeWidth = st.LineWidth; paint.StrokeCap = st.Cap; paint.StrokeJoin = st.Join; paint.StrokeMiter = 10;
            if (st.Dash != null) paint.PathEffect = SKPathEffect.CreateDash(st.Dash, st.DashOff);
        }
        if (st.ShadowBlur > 0 && st.ShadowColor.Alpha > 0)
            paint.ImageFilter = SKImageFilter.CreateDropShadow(0, 0, st.ShadowBlur / 2, st.ShadowBlur / 2, st.ShadowColor);
        return paint;
    }
    void Done() { paint.PathEffect?.Dispose(); paint.PathEffect = null; paint.ImageFilter?.Dispose(); paint.ImageFilter = null; paint.Shader = null; }

    public void fill() { C.DrawPath(path, Prep(st.Fill, false)); Done(); }
    public void stroke() { C.DrawPath(path, Prep(st.Stroke, true)); Done(); }
    public void clip() { C.ClipPath(path, SKClipOperation.Intersect, true); }
    public void fillRect(double x, double y, double w, double h) { C.DrawRect(SKRect.Create((float)x, (float)y, (float)w, (float)h), Prep(st.Fill, false)); Done(); }
    public void strokeRect(double x, double y, double w, double h) { C.DrawRect(SKRect.Create((float)x, (float)y, (float)w, (float)h), Prep(st.Stroke, true)); Done(); }
    public void clearRect(double x, double y, double w, double h)
    {
        paint.Reset(); paint.BlendMode = SKBlendMode.Clear;
        C.DrawRect(SKRect.Create((float)x, (float)y, (float)w, (float)h), paint);
        paint.BlendMode = SKBlendMode.SrcOver;
    }
    public void drawImage(SKImage img, double x, double y)
    {
        paint.Reset(); paint.IsAntialias = true;
        if (st.Alpha < 1) paint.Color = new SKColor(0, 0, 0, (byte)(255 * st.Alpha));
        C.DrawImage(img, (float)x, (float)y, paint);
    }
    public void drawImage(SKImage img, double x, double y, double w, double h)
    {
        paint.Reset(); paint.IsAntialias = true;
        if (st.Alpha < 1) paint.Color = new SKColor(0, 0, 0, (byte)(255 * st.Alpha));
        paint.FilterQuality = SKFilterQuality.Medium; C.DrawImage(img, new SKRect((float)x, (float)y, (float)(x + w), (float)(y + h)), paint);
    }
    public Grad createLinearGradient(double x0, double y0, double x1, double y1) => new((float)x0, (float)y0, (float)x1, (float)y1);
    public Grad createRadialGradient(double x0, double y0, double r0, double x1, double y1, double r1) => new((float)x0, (float)y0, (float)r0, (float)x1, (float)y1, (float)r1);

    // ---------- текст ----------
    readonly Dictionary<string, SKFont> fonts = new();
    SKFont Font()
    {
        if (fonts.TryGetValue(st.Font, out var f)) return f;
        int weight = 400; float size = 10;
        foreach (var part in st.Font.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (part.EndsWith("px") && float.TryParse(part[..^2], NumberStyles.Float, CultureInfo.InvariantCulture, out var px)) { size = px; break; }
            if (int.TryParse(part, out var w)) weight = w;
            else if (part == "bold") weight = 700;
        }
        var tf = st.Font.Contains("Bubbles") ? Fonts.Display : Fonts.Typeface(weight);
        f = new SKFont(tf, size) { Subpixel = true, Edging = SKFontEdging.Antialias };
        fonts[st.Font] = f; return f;
    }
    readonly SKPaint measure = new();
    float Measure(string text, SKFont f) { measure.Typeface = f.Typeface; measure.TextSize = f.Size; return measure.MeasureText(text ?? ""); }
    float AlignShift(string text, SKFont f)
    {
        if (st.Align == "center") return -Measure(text, f) / 2;
        if (st.Align == "right" || st.Align == "end") return -Measure(text, f);
        return 0;
    }
    public double measureText(string text) => Measure(text, Font());
    public void fillText(string text, double x, double y)
    {
        if (string.IsNullOrEmpty(text)) return;
        var f = Font();
        C.DrawText(text, (float)x + AlignShift(text, f), (float)y, f, Prep(st.Fill, false)); Done();
    }
    public void strokeText(string text, double x, double y)
    {
        if (string.IsNullOrEmpty(text)) return;
        var f = Font();
        C.DrawText(text, (float)x + AlignShift(text, f), (float)y, f, Prep(st.Stroke, true)); Done();
    }

    // ---------- кольори ----------
    static readonly Dictionary<string, SKColor> colCache = new();
    public static SKColor Col(string css)
    {
        if (css == null) return SKColors.Black;
        lock (colCache)
        {
            if (colCache.TryGetValue(css, out var c)) return c;
            if (colCache.Count > 4096) colCache.Clear(); // строки с плавающей прозрачностью не копятся бесконечно
            c = Parse(css.Trim()); colCache[css] = c; return c;
        }
    }
    static SKColor Parse(string s)
    {
        if (s.StartsWith('#'))
        {
            var h = s[1..];
            if (h.Length == 3) h = new string(new[] { h[0], h[0], h[1], h[1], h[2], h[2] });
            if (h.Length == 6 && uint.TryParse(h, NumberStyles.HexNumber, null, out var v)) return new SKColor((byte)(v >> 16), (byte)(v >> 8), (byte)v);
            if (h.Length == 8 && uint.TryParse(h, NumberStyles.HexNumber, null, out var v8)) return new SKColor((byte)(v8 >> 24), (byte)(v8 >> 16), (byte)(v8 >> 8), (byte)v8);
            return SKColors.Black;
        }
        if (s.StartsWith("rgb"))
        {
            int a = s.IndexOf('('), b = s.LastIndexOf(')');
            var parts = s[(a + 1)..b].Split(',');
            byte P(int i) => (byte)Math.Clamp(Math.Round(double.Parse(parts[i].Trim(), CultureInfo.InvariantCulture)), 0, 255);
            double al = parts.Length > 3 ? double.Parse(parts[3].Trim(), CultureInfo.InvariantCulture) : 1;
            return new SKColor(P(0), P(1), P(2), (byte)Math.Clamp(Math.Round(al * 255), 0, 255));
        }
        return s switch { "white" => SKColors.White, "black" => SKColors.Black, "transparent" => SKColors.Transparent, _ => SKColors.Black };
    }
}
