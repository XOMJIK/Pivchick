using System;
using System.Collections.Generic;
using System.Linq;
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

    /* ---------- клавиши: у каждого действия две (основная и запасная), их можно переназначить в настройках ---------- */
    public sealed class Act { public string id, nameRu; public string[] def; public string name => L.T(nameRu); }
    public static readonly Act[] ACTIONS =
    {
        new() { id = "up", nameRu = "Вверх", def = new[] { "KeyW", "ArrowUp" } },
        new() { id = "down", nameRu = "Вниз", def = new[] { "KeyS", "ArrowDown" } },
        new() { id = "left", nameRu = "Влево", def = new[] { "KeyA", "ArrowLeft" } },
        new() { id = "right", nameRu = "Вправо", def = new[] { "KeyD", "ArrowRight" } },
        new() { id = "nade", nameRu = "Яйцо-граната", def = new[] { "KeyQ", "Space" } },
        new() { id = "ult", nameRu = "Ульта", def = new[] { "KeyR", "" } },
        new() { id = "swap", nameRu = "Сменить оружие", def = new[] { "KeyF", "" } },
        new() { id = "act", nameRu = "Взять или купить", def = new[] { "KeyE", "" } },
        new() { id = "pause", nameRu = "Пауза", def = new[] { "KeyP", "" } },
        new() { id = "auto", nameRu = "Автонаводка вкл/выкл", def = new[] { "KeyT", "" } },
        new() { id = "full", nameRu = "Полный экран", def = new[] { "F11", "" } },
    };
    /// <summary>Клавиши действия: свои из настроек или по умолчанию. Всегда два элемента.</summary>
    public static string[] keysOf(string act)
    {
        if (SET?.keys != null && SET.keys.TryGetValue(act, out var k) && k != null) return new[] { k.Count > 0 ? k[0] ?? "" : "", k.Count > 1 ? k[1] ?? "" : "" };
        foreach (var a in ACTIONS) if (a.id == act) return a.def;
        return new[] { "", "" };
    }
    public static bool held(string act) { foreach (var k in keysOf(act)) if (k.Length > 0 && input.keys.Contains(k)) return true; return false; }
    public static string actionOf(string code)
    {
        if (string.IsNullOrEmpty(code)) return null;
        foreach (var a in ACTIONS) foreach (var k in keysOf(a.id)) if (k == code) return a.id;
        return null;
    }
    /// <summary>Назначить клавишу действию; если она была у другого действия — там она освобождается.</summary>
    public static void bindKey(string act, int slot, string code)
    {
        foreach (var a in ACTIONS)
        {
            var ks = keysOf(a.id); bool ch = false;
            for (int i = 0; i < 2; i++) if (ks[i] == code && !(a.id == act && i == slot)) { ks[i] = ""; ch = true; }
            if (ch) SET.keys[a.id] = ks.ToList();
        }
        var mine = keysOf(act); mine[slot] = code ?? ""; SET.keys[act] = mine.ToList();
        saveSet();
    }
    public static void resetKeys() { SET.keys = new(); saveSet(); }
    /// <summary>Как подписать клавишу: «W», «↑», «Пробел», «F11».</summary>
    public static string keyName(string code)
    {
        if (string.IsNullOrEmpty(code)) return "—";
        if (code.StartsWith("Key") && code.Length == 4) return code[3..];
        if (code.Length == 2 && code[0] == 'D' && char.IsDigit(code[1])) return code[1..];
        if (code.StartsWith("NumPad")) return "Num " + code[6..];
        return code switch
        {
            "ArrowUp" => "↑", "ArrowDown" => "↓", "ArrowLeft" => "←", "ArrowRight" => "→", "Space" => T("Пробел"), "Escape" => "Esc",
            "LeftShift" => "L Shift", "RightShift" => "R Shift", "LeftCtrl" => "L Ctrl", "RightCtrl" => "R Ctrl", "LeftAlt" => "L Alt", "RightAlt" => "R Alt",
            "Return" or "Enter" => "Enter", "Back" => "Backspace", "OemTilde" => "~", "OemMinus" => "-", "OemPlus" => "+", "OemComma" => ",", "OemPeriod" => ".",
            _ => code
        };
    }
    /// <summary>Подсказка на кнопке: основная клавиша действия.</summary>
    public static string keyHint(string act) { var k = keysOf(act); return keyName(k[0].Length > 0 ? k[0] : k[1]); }

    /// <summary>Клавиша нажата; code — как KeyboardEvent.code в браузере (KeyW, ArrowUp, Space…).
    /// Возвращает true, если игра её обработала.</summary>
    public static bool KeyDown(string code)
    {
        input.keys.Add(code);
        bool handled = false;
        if (S.mode == "play")
        {
            var a = actionOf(code);
            if (a == "nade") { throwNade(); handled = true; }
            if (a == "ult") { useUlt(); handled = true; }
            if (a == "swap" && P?.bag != null) { swapWeapon(); handled = true; }
            if (a == "act") { interact(); handled = true; }
            if (P?.bag != null && code is "D1" or "D2" or "D3") { selectSlot(code[1] - '1'); handled = true; } // в подземелье 1-3 — слоты рюкзака
        }
        return handled;
    }
    public static void KeyUp(string code) => input.keys.Remove(code);
}
