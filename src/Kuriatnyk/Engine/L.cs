using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace Kuriatnyk;

/// <summary>Переклад: ключ — російський рядок, як у web/js/lang.js (словники в Assets/Lang/&lt;код&gt;.json).</summary>
public static class L
{
    public static string Lang = "ru";
    /// <summary>Мови гри: код і назва мовою самої мови (порядок — як у налаштуваннях).</summary>
    public static readonly (string code, string name)[] LANGS =
    {
        ("ru", "Русский"), ("uk", "Українська"), ("en", "English"), ("es", "Español"),
        ("de", "Deutsch"), ("fr", "Français"), ("pt", "Português"), ("pl", "Polski"),
    };
    public static bool Has(string code) => LANGS.Any(l => l.code == code);

    static readonly Dictionary<string, Dictionary<string, string>> dicts = new();
    static Dictionary<string, string> Dict(string code)
    {
        if (dicts.TryGetValue(code, out var d)) return d;
        try
        {
            using var s = Assembly.GetExecutingAssembly().GetManifestResourceStream(code + ".json");
            d = s == null ? new() : JsonSerializer.Deserialize(s, LangJson.Default.DictionaryStringString) ?? new();
        }
        catch { d = new(); }
        return dicts[code] = d;
    }

    /// <summary>_t('текст') з веб-версії. Немає перекладу — англійський, а якщо й його немає — російський.</summary>
    public static string T(string s)
    {
        if (Lang == "ru" || s == null) return s;
        if (Dict(Lang).TryGetValue(s, out var v)) return v;
        return Lang != "en" && Dict("en").TryGetValue(s, out var e) ? e : s;
    }
    /// <summary>Переклад з підстановкою: L.F("Пройди {0} волн", 7).</summary>
    public static string F(string s, params object[] args) => string.Format(Culture, T(s), args);

    public static bool En => Lang == "en";
    static readonly Dictionary<string, CultureInfo> cultures = new();
    public static CultureInfo Culture
    {
        get
        {
            if (cultures.TryGetValue(Lang, out var c)) return c;
            var name = Lang switch { "en" => "en-US", "uk" => "uk-UA", "es" => "es-ES", "de" => "de-DE", "fr" => "fr-FR", "pt" => "pt-BR", "pl" => "pl-PL", _ => "ru-RU" };
            try { c = CultureInfo.GetCultureInfo(name); } catch { c = CultureInfo.InvariantCulture; }
            return cultures[Lang] = c;
        }
    }
    /// <summary>Відкривна і закривна лапки мови: «…», “…”, „…“.</summary>
    public static string Qo => Lang switch { "en" or "pt" => "“", "de" or "pl" => "„", _ => "«" };
    public static string Qc => Lang switch { "en" or "pt" => "”", "de" => "“", "pl" => "”", _ => "»" };

    /// <summary>toLocaleString для цілих: «1 234» або «1,234».</summary>
    public static string Fmt(double n) => System.Math.Round(n).ToString("#,0", Culture);
}

[System.Text.Json.Serialization.JsonSerializable(typeof(Dictionary<string, string>))]
internal partial class LangJson : System.Text.Json.Serialization.JsonSerializerContext { }
