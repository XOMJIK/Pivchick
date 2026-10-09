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
            (0.6, "b-shop", () => mv.OpenPanel("shop")),
            (0.6, "c-heroes", () => mv.OpenPanel("heroes")),
            (0.6, "d-story", () => mv.OpenPanel("storyP")),
            (0.6, "e-chests", () => mv.OpenPanel("chestP")),
            (0.6, "f-settings", () => { mv.ClosePanels(); Overlays.OpenSettings(mv); }),
            (0.6, "g-info", () => { mv.Hide("setOv"); Overlays.OpenInfo(mv); }),
            (0.6, "h-brief", () => { mv.Hide("infoOv"); Overlays.OpenBrief(mv, 0); }),
            (0.6, "i-chestinfo", () => { mv.Hide("brief"); Overlays.OpenChestInfo(mv, 1); }),
            (0.6, "j-bg", () => { mv.Hide("chestInfoOv"); Overlays.OpenBg(mv); }),
            (0.6, "j2-sksel", () => { mv.Hide("bgOv"); Overlays.OpenSkSelect(mv); }),
            (4.0, "k-battle", () => { mv.Hide("skSel"); G.startGame("endless"); }),
            (2.0, "k2-sk", () => G.startSK(Store.SAVE.hero)),
            (0.6, "l-pause", () => G.pauseGame()),
            (2.5, "m-chestopen", () => { G.toMenu(); Store.SAVE.tokens += 300; mv.OpenPanel("chestP"); var r = Chests.Open(0, "own", 3); Overlays.OpenChestOverlay(mv, 0, r); }),
        };
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
