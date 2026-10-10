using System;
using System.Collections.Generic;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Kuriatnyk.UI;

namespace Kuriatnyk.Desktop;

/// <summary>Самопроверка интерфейса: если задана переменная KUR_AUTOSHOT=папка, приложение проходит по экранам,
/// сохраняет их снимки в PNG и закрывается. В обычном запуске не делает ничего.</summary>
static class AutoShots
{
    static void Log(string dir, Window w, string what) =>
        File.AppendAllText(Path.Combine(dir, "display.txt"), $"{what}: state={w.WindowState} decorations={w.SystemDecorations} size={w.Width:0}x{w.Height:0} pos={w.Position} setting={Store.SET.display}\n");
    public static void Run(Window w)
    {
        var dir = Environment.GetEnvironmentVariable("KUR_AUTOSHOT"); if (string.IsNullOrEmpty(dir)) return;
        Directory.CreateDirectory(dir);
        var mv = (MainView)w.Content;
        var sz = Environment.GetEnvironmentVariable("KUR_SIZE");
        if (!string.IsNullOrEmpty(sz)) { var p = sz.Split('x'); w.Width = double.Parse(p[0]); w.Height = double.Parse(p[1]); }
        var steps = new List<(double wait, string name, Action act)>
        {
            (2.0, "a-lobby", null),
            (0.6, "a2-single", () => System.Linq.Enumerable.First(System.Linq.Enumerable.OfType<Lobby>(Avalonia.VisualTree.VisualExtensions.GetVisualDescendants(mv))).ShowSingle(true)),
            (0.6, "a3-quit", () => Overlays.OpenQuit(mv)),
            (0.6, "b-shop", () => { mv.Hide("quitOv"); mv.OpenPanel("shop"); }),
            (0.6, "c-heroes", () => mv.OpenPanel("heroes")),
            (0.6, "c2-heroinfo", () => Overlays.OpenHeroInfo(mv, "rooster")),
            (0.6, "c3-heroabil", () => { mv.Hide("heroInfo"); Overlays.SetHeroTab("abil"); Overlays.OpenHeroInfo(mv, Store.SAVE.hero); }),
            (0.6, "c4-weapons", () => { mv.Hide("heroInfo"); Overlays.SetHeroTab("stats"); mv.OpenPanel("weapons"); }),
            (0.6, "c5-weapons-sk", () => WeaponsPanel.ShowTab(mv, "sk")),
            (0.6, "d-story", () => mv.OpenPanel("storyP")),
            (0.6, "e-chests", () => mv.OpenPanel("chestP")),
            (0.8, "e2-profile", () => { ProfilePanel.tab = "me"; mv.OpenPanel("profile"); }),
            (0.6, "e3-achievements", () => { ProfilePanel.tab = "ach"; mv.OpenPanel("profile"); }),
            (0.6, "e4-accounts", () => { ProfilePanel.tab = "acc"; mv.OpenPanel("profile"); }),
            (0.6, "e5-quests", () => mv.OpenPanel("quests")),
            (0.6, "e6-pass", () => mv.OpenPanel("pass")),
            (0.8, "e7-mp", () => { mv.ClosePanels(); Overlays.OpenMultiplayer(mv); }),
            (0.6, "e8-mp-local", () => Overlays.OpenMultiplayer(mv, "local")),
            (0.8, "e9-mp-lobby", () => { Mp.Host(); Mp.SetMode("story", 0); Overlays.OpenMultiplayer(mv); }),
            (0.6, "f-settings", () => { Mp.Leave(); mv.Hide("mpOv"); mv.ClosePanels(); Overlays.OpenSettings(mv); }),
            (0.6, "f2-keys", () => Overlays.OpenKeys(mv)),
            (0.6, "f3-keys-wait", () => { mv.Capture = ("nade", 1); Overlays.OpenKeys(mv); }),
            (0.6, "g-info", () => { mv.Capture = null; mv.Hide("keysOv"); mv.Hide("setOv"); Overlays.OpenInfo(mv); }),
            (0.6, "h-brief", () => { mv.Hide("infoOv"); Overlays.OpenBrief(mv, 0); }),
            (0.6, "i-chestinfo", () => { mv.Hide("brief"); Overlays.OpenChestInfo(mv, 1); }),
            (0.6, "j-bg", () => { mv.Hide("chestInfoOv"); Overlays.OpenBg(mv); }),
            (0.6, "j2-sksel", () => { mv.Hide("bgOv"); Overlays.OpenSkSelect(mv); }),
            (4.0, "k-battle", () => { mv.Hide("skSel"); G.startGame("endless"); }),
            (2.0, "k2-sk", () => G.startSK(Store.SAVE.hero)),
            (1.0, "k3-sk-pick", () => { G.S.mode = "pick"; G.skOffer = new() { "bag", "dmg", "magnet" }; mv.ShowSkPick(G.skOffer); }),
            (0.6, "l-pause", () => { mv.Hide("skPick"); G.S.mode = "play"; G.pauseGame(); }),
            (2.5, "m-chestopen", () => { G.toMenu(); Store.SAVE.tokens += 300; mv.OpenPanel("chestP"); var r = Chests.Open(0, "own", 3); Overlays.OpenChestOverlay(mv, 0, r); }),
            // режимы окна: F11 и настройки (в файл — что получилось с окном)
            (1.0, "n-full", () => { mv.Hide("chestOv"); mv.ClosePanels(); DisplayMode.ToggleFull(mv); }),
            (1.0, "n-borderless", () => { Log(dir, w, "after F11"); DisplayMode.Set(mv, "borderless"); }),
            (1.0, "n-windowed", () => { Log(dir, w, "borderless"); DisplayMode.ToggleFull(mv); }),
            (1.0, "n-back", () => { Log(dir, w, "F11 from borderless"); DisplayMode.ToggleFull(mv); }),
            (1.0, "n-end", () => { Log(dir, w, "F11 again (back to borderless)"); DisplayMode.Set(mv, "windowed"); }),
            (0.5, "n-end2", () => Log(dir, w, "windowed")),
        };
        // KUR_AUTOSHOT_NOWIN=1 — без шагов с полным экраном и рамками окна
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("KUR_AUTOSHOT_NOWIN"))) steps.RemoveAll(s => s.name.StartsWith("n-"));
        int i = 0;
        void Next()
        {
            if (i >= steps.Count) { w.Close(); return; }
            var (wait, name, act) = steps[i++];
            try { act?.Invoke(); } catch (Exception ex) { File.AppendAllText(Path.Combine(dir, "errors.txt"), name + ": " + ex + "\n"); }
            DispatcherTimer.RunOnce(() =>
            {
                try
                {
                    var size = new PixelSize((int)w.ClientSize.Width, (int)w.ClientSize.Height);
                    using var bmp = new RenderTargetBitmap(size, new Vector(96, 96));
                    bmp.Render(mv); bmp.Save(Path.Combine(dir, name + ".png"));
                }
                catch (Exception ex) { File.AppendAllText(Path.Combine(dir, "errors.txt"), name + " shot: " + ex + "\n"); }
                Next();
            }, TimeSpan.FromSeconds(wait));
        }
        Next();
    }
}
