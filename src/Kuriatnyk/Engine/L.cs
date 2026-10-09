using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace Kuriatnyk;

/// <summary>Переклад: ключ — російський рядок, як у web/js/lang.js (словник у Assets/Lang/en.json).</summary>
public static class L
{
    public static string Lang = "ru";
    static Dictionary<string, string> en;

    static Dictionary<string, string> EN
    {
        get
        {
            if (en != null) return en;
            try
            {
                using var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("en.json");
                en = s == null ? new() : JsonSerializer.Deserialize(s, LangJson.Default.DictionaryStringString) ?? new();
            }
            catch { en = new(); }
            return en;
        }
    }

    /// <summary>_t('текст') з веб-версії.</summary>
    public static string T(string s) => Lang == "en" && s != null && EN.TryGetValue(s, out var v) ? v : s;

    public static bool En => Lang == "en";
    public static CultureInfo Culture => En ? CultureInfo.GetCultureInfo("en-US") : CultureInfo.GetCultureInfo("ru-RU");

    /// <summary>toLocaleString для цілих: «1 234» або «1,234».</summary>
    public static string Fmt(double n) => System.Math.Round(n).ToString("#,0", Culture);
}

[System.Text.Json.Serialization.JsonSerializable(typeof(Dictionary<string, string>))]
internal partial class LangJson : System.Text.Json.Serialization.JsonSerializerContext { }
