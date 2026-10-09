using Avalonia;
using Avalonia.Controls;
using static Kuriatnyk.Store;

namespace Kuriatnyk.UI;

/// <summary>Режим окна на ПК: обычное окно, окно без рамок на весь рабочий стол (панель задач видна) и полный экран.
/// На телефоне окна нет — там ничего не делаем.</summary>
public static class DisplayMode
{
    public static readonly (string id, string nameRu)[] MODES = { ("windowed", "Оконный"), ("borderless", "Без рамок"), ("fullscreen", "Полный экран") };
    static PixelPoint? savedPos; static Size? savedSize;

    public static Window Win(Visual v) => TopLevel.GetTopLevel(v) as Window;
    public static bool Available(Visual v) => Win(v) != null;

    public static void Apply(Window w, string mode)
    {
        if (w == null) return;
        // размер и место обычного окна запоминаем, чтобы вернуться к ним из полного экрана и окна без рамок
        if (w.WindowState == WindowState.Normal && w.SystemDecorations == SystemDecorations.Full) { savedPos = w.Position; savedSize = new Size(w.Width, w.Height); }
        switch (mode)
        {
            case "fullscreen":
                w.WindowState = WindowState.FullScreen;
                break;
            case "borderless":
                w.WindowState = WindowState.Normal; w.SystemDecorations = SystemDecorations.None; w.CanResize = false;
                var sc = w.Screens.ScreenFromVisual(w) ?? w.Screens.Primary;
                if (sc != null)
                {
                    var wa = sc.WorkingArea; double k = sc.Scaling > 0 ? sc.Scaling : 1;
                    w.Position = wa.Position; w.Width = wa.Width / k; w.Height = wa.Height / k;
                }
                break;
            default:
                w.WindowState = WindowState.Normal; w.SystemDecorations = SystemDecorations.Full; w.CanResize = true;
                if (savedSize is { } s) { w.Width = s.Width; w.Height = s.Height; }
                if (savedPos is { } p) w.Position = p;
                break;
        }
    }

    public static void Set(MainView mv, string mode)
    {
        if (SET.display == mode) return;
        if (mode != "fullscreen") SET.lastWin = mode;
        SET.display = mode; saveSet(); Apply(Win(mv), mode);
    }

    /// <summary>F11: полный экран ↔ прежний режим окна.</summary>
    public static void ToggleFull(MainView mv)
    {
        var w = Win(mv); if (w == null) return;
        if (SET.display == "fullscreen") SET.display = SET.lastWin is "borderless" ? "borderless" : "windowed";
        else { SET.lastWin = SET.display; SET.display = "fullscreen"; }
        saveSet(); Apply(w, SET.display);
        if (mv.Shown("setOv")) mv.Show("setOv", Overlays.Settings(mv), 8);
    }
}
