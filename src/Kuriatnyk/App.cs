using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Themes.Fluent;
using Kuriatnyk.UI;

namespace Kuriatnyk;

/// <summary>Приложение: тема, загрузка сохранения, главное окно (ПК) или единственный экран (телефон).</summary>
public sealed class App : Application
{
    public static System.Action<Window> WindowOpened;
    public override void Initialize()
    {
        RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Light;
        Styles.Add(new FluentTheme());
    }

    public override void OnFrameworkInitializationCompleted()
    {
        Store.Load();
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desk)
        {
            var w = new Window
            {
                Title = L.T("Боевой Курятник"), Width = 1180, Height = 720, MinWidth = 360, MinHeight = 360,
                Content = new MainView(), Background = K.B(K.Grass), WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            try { w.Icon = new WindowIcon(Avalonia.Platform.AssetLoader.Open(new System.Uri("avares://Kuriatnyk/Assets/icon.png"))); } catch { }
            w.Closing += (_, _) => { if (G.S.mode is "play" or "paused" or "dying" or "pick") G.bankCoins(); Store.persist(); Synth.Stop(); };
            desk.MainWindow = w;
            w.Opened += (_, _) => { if (Store.SET.display != "windowed") DisplayMode.Apply(w, Store.SET.display); WindowOpened?.Invoke(w); };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime single)
        {
            single.MainView = new MainView();
        }
        base.OnFrameworkInitializationCompleted();
    }
}
