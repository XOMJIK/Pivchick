using System;
using System.Collections.Generic;
using System.Linq;
using static Kuriatnyk.D;
using static Kuriatnyk.Store;

namespace Kuriatnyk;

/// <summary>Карточки в магазине: каждый день две случайные пачки карт — героя или оружия — за зёрна.
/// Карты только для открытых героев и купленных стволов (своего класса), которые ещё не на 10-м уровне.</summary>
public static class Offers
{
    // зёрен за одну карту и сколько карт в пачке: чем реже герой или ствол, тем меньше карт и дороже каждая
    static readonly Dictionary<string, int> HERO_CARD = new() { ["common"] = 12, ["rare"] = 20, ["epic"] = 32, ["legendary"] = 50 };
    static readonly Dictionary<string, int> WEAP_CARD = new() { ["common"] = 10, ["rare"] = 18, ["epic"] = 28, ["legendary"] = 40 };
    /// <summary>Магазин обновляется каждые 24 часа — в полночь по UTC, одинаково для всех часовых поясов.</summary>
    public static string Today => DateTime.UtcNow.ToString("yyyy-MM-dd");
    /// <summary>Сколько осталось до новых карточек (до полуночи UTC).</summary>
    public static TimeSpan Left => DateTime.UtcNow.Date.AddDays(1) - DateTime.UtcNow;

    static bool Valid(Offer o) => o.kind == "hero" ? SAVE.heroes.TryGetValue(o.id, out var h) && h.lv < 10
        : SAVE.guns.Contains(o.id) && wLv(o.id) < 10 && hasClass(WEAP[o.id].cls);

    public static List<Offer> Get()
    {
        if (SAVE.shopDay != Today || SAVE.offers == null) Roll();
        // герой или ствол успел дорасти до 10-го уровня — такое предложение заменяем
        if (SAVE.offers.Any(o => !o.bought && !Valid(o))) { var keep = SAVE.offers.Where(o => o.bought || Valid(o)).ToList(); Fill(keep); }
        return SAVE.offers;
    }
    public static void Roll() { SAVE.shopDay = Today; Fill(new List<Offer>()); }
    static void Fill(List<Offer> keep)
    {
        var pool = new List<Offer>();
        foreach (var id in SAVE.heroes.Keys.Where(id => SAVE.heroes[id].lv < 10)) pool.Add(Make("hero", id));
        foreach (var id in PRIMARY.Where(id => SAVE.guns.Contains(id) && wLv(id) < 10 && hasClass(WEAP[id].cls))) pool.Add(Make("weap", id));
        pool = pool.Where(p => !keep.Any(k => k.kind == p.kind && k.id == p.id)).ToList();
        while (keep.Count < 2 && pool.Count > 0) { var i = (int)(G.rnd() * pool.Count); keep.Add(pool[i]); pool.RemoveAt(i); }
        SAVE.offers = keep; persist();
    }
    static Offer Make(string kind, string id)
    {
        if (kind == "hero")
        {
            var r = HEROES[id].rar; int n = Math.Max(1, (int)Math.Round(10 * RAR[r].card, MidpointRounding.AwayFromZero));
            return new Offer { kind = kind, id = id, n = n, price = Round5(n * HERO_CARD[r]) };
        }
        var wr = WEAP[id].rar; int wn = Math.Max(1, (int)Math.Round(10 * WRAR[wr].card, MidpointRounding.AwayFromZero));
        return new Offer { kind = kind, id = id, n = wn, price = Round5(wn * WEAP_CARD[wr]) };
    }
    static int Round5(double v) => (int)(Math.Round(v / 5, MidpointRounding.AwayFromZero) * 5);

    /// <summary>Купить пачку: карты сразу в копилку героя или ствола.</summary>
    public static bool Buy(Offer o)
    {
        if (o.bought || SAVE.coins < o.price || !Valid(o)) return false;
        SAVE.coins -= o.price; o.bought = true;
        if (o.kind == "hero") SAVE.heroes[o.id].cards += o.n; else wRec(o.id).cards += o.n;
        persist(); return true;
    }
}
