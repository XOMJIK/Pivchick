using System.Collections.Generic;
using System.IO;
using System.Reflection;
using SkiaSharp;

namespace Kuriatnyk.Render;

/// <summary>Шрифты для рисования на полотне: Rubik (жирности 500–900) и Rubik Bubbles для заголовков.
/// Файлы лежат в Assets/Fonts; если их нет — берём системный шрифт нужной жирности.</summary>
public static class Fonts
{
    static readonly Dictionary<int, SKTypeface> cache = new();
    static SKTypeface display; static bool displayTried;

    static SKTypeface Load(string file)
    {
        try
        {
            var asm = Assembly.GetExecutingAssembly();
            using var s = asm.GetManifestResourceStream("Fonts." + file);
            if (s == null) return null;
            var ms = new MemoryStream(); s.CopyTo(ms); ms.Position = 0;
            return SKTypeface.FromStream(ms);
        }
        catch { return null; }
    }

    public static SKTypeface Typeface(int weight)
    {
        lock (cache)
        {
            int w = weight >= 850 ? 900 : weight >= 750 ? 800 : weight >= 600 ? 700 : 500;
            if (cache.TryGetValue(w, out var tf)) return tf;
            tf = Load(w switch { 900 => "Rubik-Black.ttf", 800 => "Rubik-ExtraBold.ttf", 700 => "Rubik-Bold.ttf", _ => "Rubik-Medium.ttf" })
                 ?? SKTypeface.FromFamilyName(null, (SKFontStyleWeight)w, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
                 ?? SKTypeface.Default;
            cache[w] = tf; return tf;
        }
    }

    public static SKTypeface Display
    {
        get
        {
            if (!displayTried) { displayTried = true; display = Load("RubikBubbles-Regular.ttf"); }
            return display ?? Typeface(900);
        }
    }
}
