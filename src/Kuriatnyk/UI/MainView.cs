using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using static Kuriatnyk.Store;
using static Kuriatnyk.UI.K;

namespace Kuriatnyk.UI;

/// <summary>Корневой экран: игровое поле, лобби, доски (магазин, герои, сюжет, сундуки), окна и тост.
/// Реализует IGameUi — то, что в веб-версии делали $('id').hidden и функции hud.js/menus.js.</summary>
public sealed partial class MainView : UserControl, IGameUi
{
    readonly Grid root = new();
    public readonly GameView Game = new();
    readonly Grid layer = new();          // всё поверх поля
    Lobby lobby;
    readonly Dictionary<string, Control> ov = new(); // открытые окна по id, как в index.html
    Control panel; string panelId;
    readonly Border toast;
    DispatcherTimer toastTimer;

    public MainView()
    {
        Background = B(Grass);
        Content = root;
        root.Children.Add(Game);
        root.Children.Add(layer);
        var tt = Txt("", 15, FontWeight.ExtraBold, PaperB, TextAlignment.Center);
        toast = Box(tt, InkB, 0, 14, new Thickness(16, 12));
        toast.HorizontalAlignment = HorizontalAlignment.Center; toast.VerticalAlignment = VerticalAlignment.Bottom;
        toast.Margin = new Thickness(16, 0, 16, 24); toast.IsHitTestVisible = false; toast.Opacity = 0; toast.ZIndex = 9;
        toast.Transitions = new Avalonia.Animation.Transitions { new Avalonia.Animation.DoubleTransition { Property = OpacityProperty, Duration = TimeSpan.FromMilliseconds(200) } };
        root.Children.Add(toast);
        G.Ui = this;
        Game.Frame += OnFrame;
        AttachedToVisualTree += (_, _) => Start();
    }

    bool started;
    void Start()
    {
        if (started) return; started = true;
        var top = TopLevel.GetTopLevel(this);
        if (top != null)
        {
            top.AddHandler(KeyDownEvent, OnKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
            top.AddHandler(KeyUpEvent, (_, e) => G.KeyUp(Code(e.Key)), Avalonia.Interactivity.RoutingStrategies.Tunnel);
            if (top is Window win) win.Deactivated += (_, _) => G.ReleaseAll();
            var ins = top.InsetsManager;
            if (ins != null)
            {
                try { ins.DisplayEdgeToEdge = true; ins.IsSystemBarVisible = false; } catch { }
                void apply() { var p = ins.SafeAreaPadding; Hud.SafeL = p.Left; Hud.SafeT = p.Top; Hud.SafeR = p.Right; Hud.SafeB = p.Bottom; layer.Margin = default; }
                ins.SafeAreaChanged += (_, _) => apply(); apply();
            }
        }
        Audio.Init();
        G.buildWorld(G.menuMap()); G.P = G.newPlayer(); G.toMenu();
    }
    public Thickness Safe => new(Hud.SafeL, Hud.SafeT, Hud.SafeR, Hud.SafeB);

    /// <summary>Свернули приложение (visibilitychange): ставим бой на паузу.</summary>
    public void OnBackground() { if (G.S.mode == "play") G.pauseGame(); G.ReleaseAll(); }

    // ---------- окна ----------
    public bool Shown(string id) => ov.ContainsKey(id);
    public void Show(string id, Control c, int z = 5)
    {
        Hide(id); c.ZIndex = z; ov[id] = c; layer.Children.Add(c);
    }
    public void Hide(string id) { if (ov.Remove(id, out var c)) layer.Children.Remove(c); }
    public static readonly string[] PANELS = { "shop", "heroes", "storyP", "chestP" };
    public bool AnyPanel => panel != null;
    public void OpenPanel(string id)
    {
        Audio.Init(); ClosePanels(); panelId = id; RefreshMenu();
        panel = id switch { "shop" => new ShopPanel(this), "heroes" => new HeroesPanel(this), "storyP" => new StoryPanel(this), _ => new ChestPanel(this) };
        panel.ZIndex = 7; layer.Children.Add(panel);
    }
    public void ClosePanels() { if (panel != null) layer.Children.Remove(panel); panel = null; panelId = null; }
    public void ShowLobby(bool on)
    {
        if (on) { if (lobby == null) { lobby = new Lobby(this) { ZIndex = 5 }; layer.Children.Add(lobby); } lobby.IsVisible = true; }
        else if (lobby != null) lobby.IsVisible = false;
    }

    /// <summary>afterChange() из menus.js: обновить меню, героя на сцене и открытую доску.</summary>
    public void AfterChange()
    {
        RefreshMenu(); G.RebuildMenuHero();
        (panel as BoardPanel)?.Render();
    }

    /// <summary>Сменился язык: пересобрать всё, что сейчас на экране, не закрывая окна и не прерывая бой.</summary>
    public void RebuildAll()
    {
        if (lobby != null) { bool vis = lobby.IsVisible; layer.Children.Remove(lobby); lobby = null; if (vis) ShowLobby(true); }
        if (panelId != null) OpenPanel(panelId);
        if (Shown("infoOv")) Overlays.OpenInfo(this);
        if (Shown("helpOv")) Overlays.OpenHelp(this);
        if (Shown("brief")) Overlays.OpenBrief(this, Overlays.briefIdx);
        if (Shown("chestInfoOv") && Overlays.ciTier >= 0) Overlays.OpenChestInfo(this, Overlays.ciTier);
        if (Shown("bgOv")) Overlays.OpenBg(this);
        if (Shown("pause")) Show("pause", Overlays.Pause(this));
        if (Shown("setOv")) Show("setOv", Overlays.Settings(this), 8);
        RefreshMenu();
    }

    // ---------- IGameUi ----------
    public void Toast(string msg)
    {
        ((TextBlock)toast.Child).Text = msg; toast.Opacity = 1; toast.MaxWidth = Math.Max(120, Bounds.Width - 32);
        toastTimer?.Stop(); toastTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1800) };
        toastTimer.Tick += (_, _) => { toast.Opacity = 0; toastTimer.Stop(); }; toastTimer.Start();
    }
    public void ToastClear() { toastTimer?.Stop(); toast.Opacity = 0; }
    public void OnGameStart()
    {
        ClosePanels(); ShowLobby(false);
        foreach (var id in new[] { "over", "pause", "win", "brief", "chestOv", "setOv" }) Hide(id);
    }
    public void ShowPause(bool show) { if (show) Show("pause", Overlays.Pause(this)); else { Hide("pause"); Hide("setOv"); } }
    public void ShowOver(OverInfo info) => Show("over", Overlays.Over(this, info));
    public void ShowWin(WinInfo info) { Show("win", Overlays.Win(this, info)); RefreshMenu(); }
    public void OnMenu()
    {
        foreach (var id in new[] { "setOv", "pause", "over", "win", "brief" }) Hide(id);
        ShowLobby(true); RefreshMenu();
    }
    public void RefreshMenu() { lobby?.Refresh(); (panel as BoardPanel)?.RefreshPurse(); }

    void OnFrame(double dt) { Overlays.ChestFrame(this, dt); lobby?.UpdateAnchor(); }

    // ---------- клавиатура (input.js) ----------
    static string Code(Key k) => k switch
    {
        >= Key.A and <= Key.Z => "Key" + k,
        Key.Up => "ArrowUp", Key.Down => "ArrowDown", Key.Left => "ArrowLeft", Key.Right => "ArrowRight",
        Key.Space => "Space", Key.Escape => "Escape", _ => k.ToString()
    };
    void OnKeyDown(object s, KeyEventArgs e)
    {
        var code = Code(e.Key);
        if (e.Source is TextBox) return;
        if (G.KeyDown(code)) { e.Handled = true; return; }
        if (code == "KeyT") ToggleAuto();
        if (code == "Escape")
        {
            foreach (var id in new[] { "bgOv", "chestInfoOv", "setOv", "infoOv", "helpOv", "brief" })
                if (Shown(id)) { Hide(id); e.Handled = true; return; }
            if (AnyPanel) { ClosePanels(); RefreshMenu(); e.Handled = true; return; }
        }
        if (code is "Escape" or "KeyP")
        {
            if (G.S.mode == "play") G.pauseGame(); else if (G.S.mode == "paused") G.resumeGame();
            e.Handled = true;
        }
    }
    public void ToggleAuto()
    {
        if (G.COARSE) return;
        SET.autoAim = !SET.autoAim; saveSet();
        Toast(SET.autoAim ? T("Автонаводка включена") : T("Автонаводка выключена: целься мышью"));
        if (Shown("setOv")) Show("setOv", Overlays.Settings(this), 8);
    }
}
