using System;
using System.Collections.Generic;
using static Kuriatnyk.Store;

namespace Kuriatnyk;

public sealed class Stick { public long id; public double ox, oy, x, y; }
public sealed class InputState { public bool mouse, firing; public double mx, my; public HashSet<string> keys = new(); }

// Управление: стики, мышь, клавиатура (порт web/js/input.js). Координаты — логические пиксели экрана.
public static partial class G
{
    public static readonly InputState input = new();
    public static class sticks { public static Stick move, aim; }
    public const double STICK_R = 56;
    public static bool pcAuto() => SET.autoAim && !COARSE;

    public static (double x, double y) stickVal(Stick s)
    {
        double dx = s.x - s.ox, dy = s.y - s.oy; var l = hypot(dx, dy);
        if (l > STICK_R) { dx *= STICK_R / l; dy *= STICK_R / l; }
        return (dx / STICK_R, dy / STICK_R);
    }

    /// <summary>Нажатие на игровом поле. Возвращает true, если событие обработано (кнопки HUD или стики).</summary>
    public static void PointerDown(long id, double x, double y, bool isMouse, bool rightButton)
    {
        Audio.Init();
        if (S.mode != "play") return;
        if (Hud.HitButton(x, y, out var btn)) { Hud.Press(btn); return; }
        if (isMouse)
        {
            input.mouse = true; input.mx = x; input.my = y;
            if (rightButton) { throwNade(); return; }
            input.firing = true; return;
        }
        input.mouse = false;
        var st = new Stick { id = id, ox = x, oy = y, x = x, y = y };
        if (x < VW / 2) { if (sticks.move == null) sticks.move = st; else if (sticks.aim == null) sticks.aim = st; }
        else { if (sticks.aim == null) sticks.aim = st; else if (sticks.move == null) sticks.move = st; }
    }
    public static void PointerMove(long id, double x, double y, bool isMouse)
    {
        if (isMouse) { input.mouse = true; input.mx = x; input.my = y; return; }
        foreach (var s in new[] { sticks.move, sticks.aim })
        {
            if (s == null || s.id != id) continue;
            s.x = x; s.y = y;
            double dx = s.x - s.ox, dy = s.y - s.oy, l = hypot(dx, dy);
            if (l > STICK_R * 1.4) { var ex = l - STICK_R * 1.4; s.ox += dx / l * ex; s.oy += dy / l * ex; }
        }
    }
    public static void PointerUp(long id, bool isMouse)
    {
        if (isMouse) { input.firing = false; return; }
        if (sticks.move != null && sticks.move.id == id) sticks.move = null;
        if (sticks.aim != null && sticks.aim.id == id) sticks.aim = null;
    }
    public static void ReleaseAll() { input.keys.Clear(); input.firing = false; sticks.move = sticks.aim = null; }

    /// <summary>Клавиша нажата; code — как KeyboardEvent.code в браузере (KeyW, ArrowUp, Space…).
    /// Возвращает true, если игра её обработала.</summary>
    public static bool KeyDown(string code)
    {
        input.keys.Add(code);
        bool handled = false;
        if (S.mode == "play")
        {
            if (code is "KeyQ" or "KeyE" or "Space") { throwNade(); handled = true; }
            if (code == "KeyR") { useUlt(); handled = true; }
            if (code == "KeyF" && P?.sec != null) { swapWeapon(); handled = true; }
        }
        return handled;
    }
    public static void KeyUp(string code) => input.keys.Remove(code);
}
