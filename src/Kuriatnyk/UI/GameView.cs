using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace Kuriatnyk.UI;

/// <summary>Игровое поле на весь экран: главный цикл кадров (main.js) и ввод (input.js).</summary>
public sealed class GameView : Control
{
    TimeSpan? last;
    public event Action<double> Frame; // для анимаций интерфейса (сундук)
    /// <summary>Поле целиком закрыто доской — не перерисовываем его (иначе каждый кадр перерисовывается и доска сверху).</summary>
    public Func<bool> Covered;
    TopLevel top;

    public GameView() { Focusable = false; ClipToBounds = true; }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        top = TopLevel.GetTopLevel(this);
        top?.RequestAnimationFrame(Tick);
    }
    protected override void OnSizeChanged(SizeChangedEventArgs e) { base.OnSizeChanged(e); G.Resize(e.NewSize.Width, e.NewSize.Height); }

    void Tick(TimeSpan now)
    {
        double dt = last.HasValue ? (now - last.Value).TotalSeconds : 1 / 60.0; last = now;
        double real = Math.Max(0, dt);
        if (dt > .05) dt = .05; if (dt < 0) dt = 0;
        try
        {
            G.TickTimers(real);
            Mp.Poll(real); // сетевая игра: входящие сообщения, снимки и ввод
            var S = G.S;
            if (S.mode != "paused") G.update(dt);
            if (S.mode == "menu" && G.P != null)
            {
                foreach (var en in G.enemies) { en.phase += dt * 3; en.ang = Math.Atan2(G.P.y - en.y, G.P.x - en.x) + Math.Sin(S.t + en.x) * .1; }
                G.P.ang = Math.Sin(S.t * .8) * .6 + (Math.Sin(S.t * .3) > 0 ? 0 : Math.PI); G.P.face = Math.Cos(G.P.ang) >= 0 ? 1 : -1;
            }
            if (S.boss != null && S.boss.dead) { S.boss = null; G.bossBarOn = false; if (S.mode == "play") Music.Play("battle"); }
            Frame?.Invoke(dt);
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
        if (Covered?.Invoke() != true) InvalidateVisual();
        (top ?? TopLevel.GetTopLevel(this))?.RequestAnimationFrame(Tick);
    }

    public override void Render(DrawingContext context)
    {
        var size = Bounds.Size; if (size.Width < 1 || size.Height < 1) return;
        using var rec = new SKPictureRecorder();
        var cnv = rec.BeginRecording(new SKRect(0, 0, (float)size.Width, (float)size.Height));
        try { G.render(cnv); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
        var pic = rec.EndRecording();
        context.Custom(new Op(new Rect(size), pic));
    }

    sealed class Op : ICustomDrawOperation
    {
        readonly SKPicture pic;
        public Op(Rect b, SKPicture p) { Bounds = b; pic = p; }
        public Rect Bounds { get; }
        public bool HitTest(Point p) => true;
        public bool Equals(ICustomDrawOperation other) => false;
        public void Render(ImmediateDrawingContext context)
        {
            var lease = context.TryGetFeature<ISkiaSharpApiLeaseFeature>(); if (lease == null) return;
            using var l = lease.Lease(); l.SkCanvas.DrawPicture(pic);
        }
        public void Dispose() => pic.Dispose();
    }

    // ---------- ввод ----------
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var pt = e.GetCurrentPoint(this); bool mouse = e.Pointer.Type == PointerType.Mouse;
        if (!mouse) G.COARSE = true;
        G.PointerDown(e.Pointer.Id, pt.Position.X, pt.Position.Y, mouse, pt.Properties.IsRightButtonPressed);
        if (!mouse || G.S.mode == "play") { e.Pointer.Capture(this); e.Handled = true; }
    }
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var pt = e.GetCurrentPoint(this);
        G.PointerMove(e.Pointer.Id, pt.Position.X, pt.Position.Y, e.Pointer.Type == PointerType.Mouse);
    }
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        bool mouse = e.Pointer.Type == PointerType.Mouse;
        if (mouse && e.InitialPressMouseButton == MouseButton.Right) return;
        G.PointerUp(e.Pointer.Id, mouse); e.Pointer.Capture(null);
    }
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        G.PointerUp(e.Pointer.Id, e.Pointer.Type == PointerType.Mouse);
    }
}
