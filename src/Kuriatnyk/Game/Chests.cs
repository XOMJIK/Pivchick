using System;
using System.Collections.Generic;
using System.Linq;
using static Kuriatnyk.D;
using static Kuriatnyk.Store;

namespace Kuriatnyk;

// Сундуки: что выпадает, шансы, цена (порт части web/js/menus.js).
public static class Chests
{
    public sealed class Result
    {
        public int tier, n = 1, eggs; public long coins; public List<string> heroes = new();
        public Dictionary<string, int> cards = new(), wcards = new();
    }
    static double rnd() => G.rnd();
    static double rand(double a, double b) => G.rand(a, b);
    static T pickW<T>(List<T> pool, Func<T, double> wf)
    {
        double tot = 0; foreach (var x in pool) tot += wf(x); double r = rnd() * tot;
        foreach (var x in pool) { r -= wf(x); if (r <= 0) return x; }
        return pool[^1];
    }
    static Result rollChest(int tier)
    {
        var C = CHESTS[tier]; var res = new Result { tier = tier, coins = (long)Math.Round(rand(C.coins[0], C.coins[1]), MidpointRounding.AwayFromZero) };
        string hero = null;
        foreach (var rk in RAR_ORDER)
        {
            var pool = HERO_IDS.Where(id => !SAVE.heroes.ContainsKey(id) && HEROES[id].chance != null && HEROES[id].rar == rk).ToList();
            if (pool.Count > 0 && rnd() * 100 < RAR[rk].chance[tier]) { hero = pool[(int)Math.Floor(rnd() * pool.Count)]; break; }
        }
        res.eggs = rnd() < C.egg[0] ? (int)Math.Round(rand(C.egg[1], C.egg[2]), MidpointRounding.AwayFromZero) : 0;
        for (int i = 0; i < C.hpacks; i++)
        {
            var pool = SAVE.heroes.Keys.Where(id => SAVE.heroes[id].lv < 10).ToList();
            if (pool.Count == 0) { res.coins += 10 * (tier + 1); continue; }
            var pick = pickW(pool, id => RAR[HEROES[id].rar].w * C.hb[HEROES[id].rar]);
            res.cards[pick] = res.cards.GetValueOrDefault(pick) + Math.Max(1, (int)Math.Round(rand(C.hcard[0], C.hcard[1]) * RAR[HEROES[pick].rar].card, MidpointRounding.AwayFromZero));
        }
        for (int i = 0; i < C.wpacks; i++)
        {
            var pool = PRIMARY.Where(id => SAVE.guns.Contains(id) && wLv(id) < 10 && hasClass(WEAP[id].cls)).ToList();
            if (pool.Count == 0) { res.coins += 10 * (tier + 1); continue; }
            var pick = pickW(pool, id => WRAR[WEAP[id].rar].w * C.wb[WEAP[id].rar]);
            res.wcards[pick] = res.wcards.GetValueOrDefault(pick) + Math.Max(1, (int)Math.Round(rand(C.wcard[0], C.wcard[1]) * WRAR[WEAP[pick].rar].card, MidpointRounding.AwayFromZero));
        }
        SAVE.coins += res.coins; SAVE.eggs += res.eggs;
        foreach (var (id, n) in res.cards) SAVE.heroes[id].cards += n;
        foreach (var (id, n) in res.wcards) wRec(id).cards += n;
        if (hero != null) { SAVE.heroes[hero] = new LvRec(); res.heroes.Add(hero); }
        persist(); return res;
    }

    public sealed class Cost { public long have, need; public string cur; }
    /// <summary>Что и сколько списать за n сундуков: жетоны, большие жетоны, сверхбольшие или золотые яйца.</summary>
    public static Cost chestCost(int tier, string pay, int n)
    {
        if (pay == "eggs") { long p = (tier == 3 ? LEG_PRICE : tier == 2 ? SUPER_PRICE : EGG_PRICE) * n; return new Cost { have = SAVE.eggs, need = p, cur = "eggs" }; }
        if (tier == 3) return new Cost { have = SAVE.lchests, need = n, cur = "lch" };
        if (tier == 2) return new Cost { have = SAVE.schests, need = n, cur = "sch" };
        if (tier == 1) return new Cost { have = SAVE.btokens, need = 10 * n, cur = "btok" };
        return new Cost { have = SAVE.tokens, need = 100 * n, cur = "tok" };
    }
    public static Result Open(int tier, string pay, int n)
    {
        var c = chestCost(tier, pay == "eggs" ? "eggs" : null, n); if (c.have < c.need) return null;
        if (c.cur == "eggs") SAVE.eggs -= (int)c.need; else if (c.cur == "lch") SAVE.lchests -= (int)c.need; else if (c.cur == "sch") SAVE.schests -= (int)c.need; else if (c.cur == "btok") SAVE.btokens -= (int)c.need; else SAVE.tokens -= c.need;
        var res = new Result { tier = tier, n = n }; Stats.AddLocal("chests", n);
        for (int i = 0; i < n; i++)
        {
            var r = rollChest(tier); res.coins += r.coins; res.eggs += r.eggs; res.heroes.AddRange(r.heroes);
            foreach (var (id, k) in r.cards) res.cards[id] = res.cards.GetValueOrDefault(id) + k;
            foreach (var (id, k) in r.wcards) res.wcards[id] = res.wcards.GetValueOrDefault(id) + k;
        }
        persist();
        return res;
    }

    /* «Свойства» сундука: что внутри и с каким шансом. Шансы показываются всегда — для полного набора героев и стволов,
       даже если героев какой-то редкости уже не осталось или карт такой редкости у игрока пока нет; left — сколько ещё закрыто */
    public sealed class OddsInfo { public Dictionary<string, (double p, int left)> hero = new(); public double anyHero; public Dictionary<string, double> hcards = new(), wcards = new(); }
    public static OddsInfo Odds(int tier)
    {
        var C = CHESTS[tier]; var o = new OddsInfo(); double miss = 1;
        int Left(string rk) => HERO_IDS.Count(id => !SAVE.heroes.ContainsKey(id) && HEROES[id].chance != null && HEROES[id].rar == rk);
        foreach (var rk in RAR_ORDER) { int left = Left(rk); double p = RAR[rk].chance[tier] / 100; o.hero[rk] = (miss * p, left); miss *= 1 - p; }
        o.anyHero = 1 - miss;
        Dictionary<string, double> share(List<string> ids, Func<string, double> wf, string[] keys, Func<string, string> key)
        {
            double t = ids.Sum(wf); var r = keys.ToDictionary(k => k, _ => 0.0);
            foreach (var id in ids) r[key(id)] += t > 0 ? wf(id) / t : 0;
            return r;
        }
        var hp = HERO_IDS.ToList(); var wp = PRIMARY.ToList();
        o.hcards = share(hp, id => RAR[HEROES[id].rar].w * C.hb[HEROES[id].rar], new[] { "common", "rare", "epic", "legendary" }, id => HEROES[id].rar);
        o.wcards = share(wp, id => WRAR[WEAP[id].rar].w * C.wb[WEAP[id].rar], WRAR_ORDER, id => WEAP[id].rar);
        return o;
    }
}
