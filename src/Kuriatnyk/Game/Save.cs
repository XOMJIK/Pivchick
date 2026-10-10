using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kuriatnyk;

// Сохранение прогресса и настроек (порт web/js/save.js и SET из core.js).
// Вместо localStorage — JSON-файлы в папке приложения.

public sealed class LvRec { public int lv { get; set; } = 1; public int cards { get; set; } }
public sealed class Best { public long score { get; set; } public int wave { get; set; } }
public sealed class StoryRec { public List<int> done { get; set; } = new(); }

public sealed class SaveData
{
    public int v { get; set; } = 2;
    public long coins { get; set; } = 100;
    public long tokens { get; set; }
    public int btokens { get; set; }
    public int eggs { get; set; }
    public Dictionary<string, int> eggMs { get; set; } = new();
    public int slots { get; set; }
    public List<string> squad { get; set; } = new();
    public Dictionary<string, string> squadGun { get; set; } = new();
    public string hero { get; set; } = "hen";
    public Dictionary<string, LvRec> heroes { get; set; } = new() { ["hen"] = new LvRec() };
    public string hat { get; set; } = "bandana";
    public List<string> hats { get; set; } = new() { "none", "bandana" };
    public string color { get; set; } = "native";
    public List<string> colors { get; set; } = new() { "native" };
    public Dictionary<string, string> loadout { get; set; } = new() { ["assault"] = "pistol", ["tank"] = "granny", ["sniper"] = "cornrifle", ["medic"] = "syringe" };
    public List<string> guns { get; set; } = new() { "pistol", "granny", "cornrifle", "syringe" };
    public Best best { get; set; } = new();
    public StoryRec story { get; set; } = new();
    public int skinRefund { get; set; } = 1;
    public Dictionary<string, LvRec> wlv { get; set; } = new();
    public int schests { get; set; }
    public string menuBg { get; set; } = "yard";
    public string shopDay { get; set; } // день, на который разложены карточки в магазине
    public List<Offer> offers { get; set; } = new();
}

public sealed class Settings
{
    public string lang { get; set; } = "ru";
    public double music { get; set; } = .6;
    public double sfx { get; set; } = .8;
    public bool shake { get; set; } = true;
    public bool muted { get; set; }
    public bool autoAim { get; set; } = true;
    /// <summary>Окно на ПК: windowed — обычное окно, borderless — окно без рамок на весь рабочий стол, fullscreen — полный экран.</summary>
    public string display { get; set; } = "windowed";
    public string lastWin { get; set; } = "windowed"; // куда вернуться из полного экрана по F11
    /// <summary>Свои клавиши: действие → две клавиши (основная и запасная, "" — нет).</summary>
    public Dictionary<string, List<string>> keys { get; set; } = new();
}

/// <summary>Предложение в магазине: пачка карт героя (hero) или оружия (weap) за зёрна.</summary>
public sealed class Offer { public string kind { get; set; } public string id { get; set; } public int n { get; set; } public int price { get; set; } public bool bought { get; set; } }

[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(SaveData))]
[JsonSerializable(typeof(Settings))]
internal partial class SaveJson : JsonSerializerContext { }

public static class Store
{
    public static string Dir
    {
        get
        {
            var d = Environment.GetEnvironmentVariable("KUR_DATA_DIR");
            if (string.IsNullOrEmpty(d)) d = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BoevoyKuryatnik");
            try { Directory.CreateDirectory(d); } catch { }
            return d;
        }
    }
    static string SavePath => Path.Combine(Dir, "save.json");
    static string SetPath => Path.Combine(Dir, "settings.json");

    public static SaveData SAVE;
    public static Settings SET;

    public static void Load()
    {
        SET = Read(SetPath, SaveJson.Default.Settings) ?? new Settings { lang = DefaultLang() };
        SET.music = Math.Clamp(SET.music, 0, 1); SET.sfx = Math.Clamp(SET.sfx, 0, 1);
        if (!L.Has(SET.lang)) SET.lang = "ru";
        if (SET.display is not ("windowed" or "borderless" or "fullscreen")) SET.display = "windowed";
        if (SET.lastWin is not ("windowed" or "borderless")) SET.lastWin = "windowed";
        SET.keys ??= new();
        foreach (var k in SET.keys.Keys.ToList()) if (SET.keys[k] == null || !G.ACTIONS.Any(a => a.id == k)) SET.keys.Remove(k);
        L.Lang = SET.lang;
        SAVE = Validate(Read(SavePath, SaveJson.Default.SaveData) ?? new SaveData());
        persist();
    }

    static string DefaultLang()
    {
        var n = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();
        return L.Has(n) ? n : n is "be" or "kk" ? "ru" : "en";
    }

    static T Read<T>(string path, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> ti) where T : class
    {
        try { if (File.Exists(path)) return JsonSerializer.Deserialize(File.ReadAllText(path), ti); } catch { }
        return null;
    }
    static void Write<T>(string path, T obj, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> ti)
    {
        try { var tmp = path + ".tmp"; File.WriteAllText(tmp, JsonSerializer.Serialize(obj, ti)); File.Move(tmp, path, true); } catch { }
    }

    public static void persist() => Write(SavePath, SAVE, SaveJson.Default.SaveData);
    public static void saveSet() => Write(SetPath, SET, SaveJson.Default.Settings);

    static SaveData Validate(SaveData o)
    {
        o.eggMs ??= new(); o.squadGun ??= new(); o.squad ??= new();
        o.slots = Math.Clamp(o.slots, 0, 3);
        o.squad = o.squad.Where(id => id != null && D.HEROES.ContainsKey(id)).Distinct().Take(o.slots).ToList();
        if (o.heroes == null) o.heroes = new() { ["hen"] = new LvRec() };
        foreach (var id in o.heroes.Keys.ToList())
        {
            if (!D.HEROES.ContainsKey(id) || o.heroes[id] == null) { o.heroes.Remove(id); continue; }
            var h = o.heroes[id]; h.lv = Math.Clamp(h.lv == 0 ? 1 : h.lv, 1, 10); h.cards = Math.Max(0, h.cards);
        }
        if (!o.heroes.ContainsKey("hen")) o.heroes["hen"] = new LvRec();
        o.hats ??= new() { "none", "bandana" }; o.colors ??= new() { "native" }; o.guns ??= new() { "pistol", "granny", "cornrifle", "syringe" };
        if (o.hero == null || !o.heroes.ContainsKey(o.hero)) o.hero = "hen";
        if (o.hat == null || !D.HATS.ContainsKey(o.hat) || !o.hats.Contains(o.hat)) o.hat = "bandana";
        if (o.color == null || !D.COLORS.ContainsKey(o.color) || !o.colors.Contains(o.color)) o.color = "native";
        foreach (var k in D.CLASS_ORDER) { var s = D.CLASSES[k].start; if (!o.guns.Contains(s)) o.guns.Add(s); }
        o.loadout ??= new();
        foreach (var k in D.CLASS_ORDER)
        {
            o.loadout.TryGetValue(k, out var g);
            if (g == null || !D.WEAP.ContainsKey(g) || D.WEAP[g].cls != k || !o.guns.Contains(g)) o.loadout[k] = D.CLASSES[k].start;
        }
        o.story ??= new(); o.story.done ??= new();
        o.wlv ??= new();
        foreach (var id in o.wlv.Keys.ToList())
        {
            var w = o.wlv[id];
            if (!D.WEAP.ContainsKey(id) || D.WEAP[id].rar == null || w == null) { o.wlv.Remove(id); continue; }
            w.lv = Math.Clamp(w.lv == 0 ? 1 : w.lv, 1, 10); w.cards = Math.Max(0, w.cards);
        }
        o.best ??= new();
        o.schests = Math.Max(0, o.schests); o.menuBg ??= "yard";
        o.offers ??= new(); o.offers = o.offers.Where(f => f != null && (f.kind == "hero" ? D.HEROES.ContainsKey(f.id ?? "") : D.WEAP.TryGetValue(f.id ?? "", out var fw) && fw.rar != null)).ToList();
        o.v = 2; return o;
    }

    public static LvRec wRec(string id) { if (!SAVE.wlv.TryGetValue(id, out var r)) SAVE.wlv[id] = r = new LvRec(); return r; }
    public static int wLv(string id) => SAVE.wlv.TryGetValue(id, out var r) && r.lv > 0 ? r.lv : 1;
    public static double wMul(string id) => D.WEAP.TryGetValue(id, out var w) && w.rar != null ? 1 + D.W_STEP * (wLv(id) - 1) : 1;
    public static int heroLv(string id) => SAVE.heroes.TryGetValue(id, out var h) && h.lv > 0 ? h.lv : 1;
    public static string gunOf(string heroId) => SAVE.loadout.TryGetValue(D.clsOf(heroId), out var g) && g != null ? g : D.CLASSES[D.clsOf(heroId)].start;
    /// <summary>Есть ли открытый герой этого класса: без него стволы класса не продаются и их карты не выпадают.</summary>
    public static bool hasClass(string cls) => SAVE.heroes.Keys.Any(id => D.clsOf(id) == cls);
    public static List<string> squadIds() => SAVE.squad.Where(id => SAVE.heroes.ContainsKey(id) && id != SAVE.hero).Take(SAVE.slots).ToList();
    public static string gunForAlly(string id)
    {
        SAVE.squadGun.TryGetValue(id, out var g); var c = D.clsOf(id);
        return g != null && D.WEAP.TryGetValue(g, out var w) && w.cls == c && SAVE.guns.Contains(g) ? g : gunOf(id);
    }
}
