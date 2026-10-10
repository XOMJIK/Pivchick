using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Kuriatnyk.Render;
using static Kuriatnyk.D;
using static Kuriatnyk.Store;
using static Kuriatnyk.UI.K;

namespace Kuriatnyk.UI;

/// <summary>Общие куски досок: превью, карточки, кнопки, покупка (mk/preview/actBtn/buy из menus.js).</summary>
public static class Panels
{
    public static Control Preview(double w, double h, Action<Ctx, double, double> fn) => new SkiaView(fn) { Width = w, Height = h };
    public static Control BirdPv(Look look, string gun, double w = 140, double h = 100, double k = 1.55)
    {
        if (look.sp == "goose") k *= .86;
        return Preview(w, h, (c, W, H) => { c.translate(W / 2 - 6, H - 14); c.scale(k, k); G.drawBird(c, new Sprite { face = 1, ang = -.18, kind = "player", gun = gun }.With(look)); });
    }
    public static Control HeroPv(string id, double w, double h, double k) => BirdPv(G.lookFor(id), gunOf(id), w, h, k);
    public static Control GunPv(string id, double w = 56, double h = 46, double k = 1.15) => Preview(w, h, (c, W, H) => { c.translate(W / 2 - 14 * k, H / 2 + 2); c.scale(k, k); G.drawGun(c, id); });

    /// <summary>Рамка превью (.pv): градиент, рамка, значки «Ур. N» слева и «Закрыто» справа.</summary>
    public static Control Pv(Control content, string bg = "grass", string lvl = null, string lockTxt = null, Control extra = null, string borderColor = null)
    {
        var g = new Grid(); content.HorizontalAlignment = HorizontalAlignment.Center; content.VerticalAlignment = VerticalAlignment.Center; g.Children.Add(content);
        if (lvl != null) { var b = Box(Txt(lvl, 12, FontWeight.Black, YolkB, wrap: false), InkB, 0, 8, new Thickness(7, 4)); b.HorizontalAlignment = HorizontalAlignment.Left; b.VerticalAlignment = VerticalAlignment.Top; b.Margin = new Thickness(8); g.Children.Add(b); }
        if (lockTxt != null) { var b = Box(Txt(lockTxt, 11, FontWeight.ExtraBold, PaperB, wrap: false), B("#d92b1d14"), 0, 8, new Thickness(7, 4)); b.HorizontalAlignment = HorizontalAlignment.Right; b.VerticalAlignment = VerticalAlignment.Top; b.Margin = new Thickness(8); g.Children.Add(b); }
        if (extra != null) g.Children.Add(extra);
        var box = Box(g, Overlays.PvBg(bg), 2, 12, borderBrush: borderColor != null ? B(borderColor) : null); box.ClipToBounds = true; return box;
    }

    /// <summary>Карточка (.item): верх — содержимое, низ — кнопки, прижаты к низу (.grow).</summary>
    public static Border Item(IEnumerable<Control> topPart, IEnumerable<Control> bottomPart, bool sel = false, string border = null)
    {
        var d = new DockPanel { LastChildFill = true };
        var bottom = Col(6, bottomPart.Where(x => x != null).ToArray()); DockPanel.SetDock(bottom, Dock.Bottom); bottom.Margin = new Thickness(0, 6, 0, 0);
        d.Children.Add(bottom); d.Children.Add(Col(6, topPart.Where(x => x != null).ToArray()));
        var b = Box(d, sel ? B("#fff8c9") : PaperB, 3, 18, new Thickness(10, 10, 10, 12), 0, border != null ? B(border) : null);
        b.BoxShadow = sel ? BoxShadows.Parse("0 4 0 0 #2B1D14, 0 0 0 4 #FFC93A") : Shadow(4, border != null ? Color.Parse(border) : Ink);
        return b;
    }
    public static TextBlock H4(string t, double size = 15) => Txt(t, size, FontWeight.ExtraBold);
    public static TextBlock P(string t) { var x = Txt(t, 12.5, FontWeight.Medium, B("#d92b1d14")); return x; }
    // «выстр/с» не рвём на две строки: после косой черты между буквами ставим неразрывный WORD JOINER
    public static TextBlock Meta(string html, bool bold = false) => Rich(System.Text.RegularExpressions.Regex.Replace(html, @"(?<=\p{L})/(?=\p{L})", "/⁠"), 12, bold ? FontWeight.ExtraBold : FontWeight.Bold, B("#bf2b1d14"));

    /// <summary>Кнопка в карточке (.pbtn): buy, poor, eq, on, max, go; thin — потоньше.</summary>
    public static Press PBtn(string html, string kind, Action click, bool thin = false)
    {
        var (bg, fg) = kind switch
        {
            "buy" => (B("#3fa535"), White), // хватает денег — зелёная
            "poor" => (B(Poor), B(PoorFg)), "eq" => (White, InkB), "on" => (B(Hp), InkB), "max" => (InkB, YolkB), _ => (YolkB, InkB)
        };
        var t = Rich(html, thin ? 13.5 : 15.5, FontWeight.ExtraBold, fg, TextAlignment.Center); t.HorizontalAlignment = HorizontalAlignment.Center; t.VerticalAlignment = VerticalAlignment.Center;
        var p = new Press(t, bg, 3, 12, thin ? new Thickness(8, 7) : new Thickness(8, 10), kind == "on" ? 0 : thin ? 2 : 3) { MinHeight = thin ? 38 : 46 };
        if (kind == "on" || kind == "max") p.Cursor = Avalonia.Input.Cursor.Default;
        if (click != null) p.Click += click;
        return p;
    }
    public static Control BRow(bool twoCols, params Control[] kids)
    {
        if (!twoCols) return Col(8, kids);
        var g = new UniformGrid { Columns = 2, ColumnSpacing = 8 }; foreach (var k in kids) g.Children.Add(k); return g;
    }
    public static string priceTag(long n, string cur) => (cur == "eggs" ? "<i class=\"gegg\"></i>" : "<i class=\"coin\"></i>") + L.Fmt(n);
    public static string coinTag(long n) => "<i class=\"coin\"></i>" + L.Fmt(n);
    public static string pct(double v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture).Replace('.', ',') + "%";

    public static Press ActBtn(MainView mv, bool owned, bool equipped, long price, Action onBuy, Action onEquip, string cur = "coins")
    {
        if (equipped) return PBtn(T("Выбрано"), "on", null);
        if (owned) return PBtn(T("Выбрать"), "eq", onEquip);
        bool poor = (cur == "eggs" ? SAVE.eggs : SAVE.coins) < price;
        return PBtn(priceTag(price, cur), poor ? "poor" : "buy", () => Buy(mv, price, onBuy, cur));
    }
    /// <summary>Закрытая кнопка покупки: ствол класса, для которого ещё нет ни одного бойца.</summary>
    public static Press ClassLock(MainView mv, string cls)
    {
        var lockIc = Icon("lock", 13); lockIc.Height = 14;
        var t = Txt(T("Нужен боец: ") + CLASSES[cls].name, 13.5, FontWeight.ExtraBold, B(PoorFg), TextAlignment.Center);
        var row = new DockPanel { HorizontalAlignment = HorizontalAlignment.Center }; DockPanel.SetDock(lockIc, Dock.Left); lockIc.Margin = new Thickness(0, 0, 6, 0);
        row.Children.Add(lockIc); row.Children.Add(t);
        var p = PBtn("", "poor", () => { mv.Toast(T("Сначала открой героя класса «") + CLASSES[cls].name + T("» — герои выпадают из сундуков")); Sfx.hurt(); });
        p.Child = row; return p;
    }
    public static void Buy(MainView mv, long price, Action fn, string cur = "coins")
    {
        long have = cur == "eggs" ? SAVE.eggs : SAVE.coins;
        if (have < price) { mv.Toast(cur == "eggs" ? T("Не хватает ") + (price - have) + T(" золотых яиц") : T("Не хватает ") + L.Fmt(price - have) + T(" зёрен")); Sfx.hurt(); return; }
        if (cur == "eggs") SAVE.eggs -= (int)price; else SAVE.coins -= price;
        fn(); persist(); Sfx.buy(); mv.Toast(T("Куплено!")); mv.AfterChange();
    }
    public static void Equip(MainView mv, Action fn, string msg = null) { fn(); persist(); Sfx.pick(); mv.Toast(msg ?? T("Выбрано")); mv.AfterChange(); }
    public static void UpgradeGun(MainView mv, string id)
    {
        int lv = wLv(id); if (lv >= 10 || !SAVE.guns.Contains(id)) return;
        var r = wRec(id); int nc = wNeed(id, lv), co = wCost(id, lv);
        if (r.cards < nc) { mv.Toast(T("Нужно ещё ") + (nc - r.cards) + T(" карт оружия — они падают из сундуков")); Sfx.hurt(); return; }
        if (SAVE.coins < co) { mv.Toast(T("Не хватает ") + L.Fmt(co - SAVE.coins) + T(" зёрен")); Sfx.hurt(); return; }
        r.cards -= nc; SAVE.coins -= co; r.lv = lv + 1; persist(); Sfx.buy();
        mv.Toast(WEAP[id].name + T(": уровень ") + r.lv + T(" · урон +") + Math.Round(W_STEP * (r.lv - 1) * 100) + "%");
        mv.AfterChange();
    }
    public static void UpgradeHero(MainView mv, string id)
    {
        if (!SAVE.heroes.TryGetValue(id, out var h) || h.lv >= 10) return;
        int nc = needCards(id, h.lv), co = needCoins(id, h.lv);
        if (h.cards < nc) { mv.Toast(T("Нужно ещё ") + (nc - h.cards) + T(" карт — открывай сундуки")); Sfx.hurt(); return; }
        if (SAVE.coins < co) { mv.Toast(T("Не хватает ") + L.Fmt(co - SAVE.coins) + T(" зёрен")); Sfx.hurt(); return; }
        h.cards -= nc; SAVE.coins -= co; h.lv++; int eg = h.lv == 5 ? 3 : h.lv == 10 ? 10 : 0; SAVE.eggs += eg; persist(); Sfx.buy();
        var H = HEROES[id];
        mv.Toast(h.lv == 5 ? T("Уровень 5! Пассивка «") + H.p5[0] + T("» и +3 золотых яйца") : h.lv == 10 ? T("Уровень 10! Пассивка «") + H.p10[0] + T("» и +10 золотых яиц") : H.name + T(": уровень ") + h.lv);
        mv.AfterChange();
    }
}

/// <summary>Доска на весь экран (.panel): шапка с кнопкой «назад», заголовком и кошельком, прокручиваемое тело.</summary>
public abstract class BoardPanel : Grid
{
    protected readonly MainView mv;
    protected readonly StackPanel body = new() { Spacing = 0 };
    protected readonly ScrollViewer scroll;
    readonly WrapPanel purse = new() { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
    protected readonly StackPanel tabsHost = new();
    readonly OutlineText title = new() { Display = true, FontSize = 30, Stroke = 1.6, ShadowY = 3, VerticalAlignment = VerticalAlignment.Center };

    protected BoardPanel(MainView mv, string titleRu)
    {
        this.mv = mv;
        Background = B(GrassDk);
        Children.Add(new SkiaView((c, w, h) =>
        {   // точечный узор фона, как radial-gradient в .panel
            c.fillStyle = "rgba(255,255,255,.07)"; for (double y = 0; y < h; y += 38) for (double x = 0; x < w; x += 38) { c.beginPath(); c.arc(x + 7.6, y + 11.4, 2.5, 0, Ctx.TAU); c.fill(); }
            c.fillStyle = "rgba(0,0,0,.06)"; for (double y = 0; y < h; y += 52) for (double x = 0; x < w; x += 52) { c.beginPath(); c.arc(x + 36.4, y + 41.6, 2.5, 0, Ctx.TAU); c.fill(); }
        }) { IsHitTestVisible = false });
        var safe = mv.Safe; bool sh = mv.View.Height <= 500 && mv.View.Width > mv.View.Height;
        var back = new Press(Txt("‹", 22, FontWeight.Black, InkB, TextAlignment.Center), PaperB, 3, 15, new Thickness(0, 0, 0, 3), 3) { Width = sh ? 46 : 50, Height = sh ? 46 : 50 };
        ((TextBlock)back.Child).VerticalAlignment = VerticalAlignment.Center;
        back.Click += () => { mv.ClosePanels(); mv.RefreshMenu(); };
        title.Text = T(titleRu); title.FontSize = sh ? 26 : 30; title.Margin = new Thickness(10, 0, 0, 0); title.NoWrap = true;
        var head = new SpaceBetweenWrap { Margin = new Thickness(0, 0, 0, sh ? 6 : 10) };
        var left = Row(0, back, title); head.Children.Add(left); head.Children.Add(purse);
        purse.Margin = new Thickness(0, 0, 0, 0);
        scroll = new ScrollViewer { Content = body, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        body.Margin = new Thickness(0, 0, 0, safe.Bottom + 24);
        var dp = new DockPanel { Margin = new Thickness(safe.Left + 16, safe.Top + 12, safe.Right + 16, 0) };
        DockPanel.SetDock(head, Dock.Top); dp.Children.Add(head);
        DockPanel.SetDock(tabsHost, Dock.Top); dp.Children.Add(tabsHost);
        dp.Children.Add(scroll);
        Children.Add(dp);
        RefreshPurse();
    }
    public void RefreshPurse()
    {
        purse.Children.Clear();
        foreach (var (ic, v) in new[] { ("coin", L.Fmt(SAVE.coins)), ("tok", L.Fmt(SAVE.tokens)), ("btok", SAVE.btokens.ToString()), ("gegg", SAVE.eggs.ToString()) })
        {
            var r = Row(4, Icon(ic, 14 * .95), Txt(v, 14, FontWeight.Black, YolkB, wrap: false));
            var b = Box(r, InkB, 0, 10, new Thickness(9, 7)); b.Margin = new Thickness(6, 2, 0, 2); purse.Children.Add(b);
        }
    }
    protected static Control Tabs(IEnumerable<(string label, bool on, Action click, bool dim)> tabs)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        foreach (var (label, on, click, dim) in tabs)
        {
            var p = new Press(Txt(label, 16, FontWeight.ExtraBold, on ? InkB : PaperB, wrap: false), on ? YolkB : Brushes.Transparent, 0, 11, new Thickness(16, 12), 0);
            if (dim) p.Opacity = .55; p.Click += click; row.Children.Add(p);
        }
        var sv = new ScrollViewer { Content = row, HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden, VerticalScrollBarVisibility = ScrollBarVisibility.Disabled };
        var b = Box(sv, InkB, 0, 16, new Thickness(5)); b.HorizontalAlignment = HorizontalAlignment.Left; b.Margin = new Thickness(0, 0, 0, 12); return b;
    }
    /// <summary>Перерисовать содержимое, сохранив прокрутку (renderShop/renderHeroes/…).</summary>
    public void Render()
    {
        var off = scroll.Offset; body.Children.Clear(); Build();
        Avalonia.Threading.Dispatcher.UIThread.Post(() => scroll.Offset = off, Avalonia.Threading.DispatcherPriority.Loaded);
        RefreshPurse();
    }
    protected abstract void Build();
}

/* ---------- оружие: стволы сюжета (покупка, выбор, улучшение) и справочник оружия Soul Knight ---------- */
public sealed class WeaponsPanel : BoardPanel
{
    static string tab = "main";
    public WeaponsPanel(MainView mv) : base(mv, "Оружие") { Render(); }
    public static void ShowTab(MainView mv, string t) { tab = t; mv.OpenPanel("weapons"); }
    protected override void Build()
    {
        tabsHost.Children.Clear();
        tabsHost.Children.Add(Tabs(new[] { (T("Сюжет и бой"), tab == "main", (Action)(() => { tab = "main"; scroll.Offset = default; Render(); }), false), (T("Soul Knight"), tab == "sk", () => { tab = "sk"; scroll.Offset = default; Render(); }, false) }));
        if (tab == "sk") { SkCatalog(); return; }
        var myC = clsOf(SAVE.hero);
        body.Children.Add(Note(T("У каждого класса своё оружие: герой берёт в бой только стволы своего класса. Сейчас в бою ") + HEROES[SAVE.hero].name + T(" — класс «") + CLASSES[myC].name + T("». Патроны у основного ствола бесконечные.")));
        var order = new[] { myC }.Concat(CLASS_ORDER.Where(k => k != myC)).ToList();
        for (int oi = 0; oi < order.Count; oi++)
        {
            var ck = order[oi]; var C = CLASSES[ck]; bool haveCls = hasClass(ck);
            var sect = Sect(C.name + (ck == myC ? T(" · твой класс") : !haveCls ? T(" · нет бойца") : "") + " — " + C.desc); if (oi > 0) sect.Margin = new Thickness(0, 18, 0, 10);
            body.Children.Add(sect);
            var g = new AutoGrid { Min = 150, Gap = 12 };
            var ids = PRIMARY.Where(w => WEAP[w].cls == ck).OrderBy(a => Array.IndexOf(WRAR_ORDER, WEAP[a].rar)).ThenBy(a => WEAP[a].eggs * 30 + WEAP[a].price).ToList();
            foreach (var id in ids)
            {
                var W = WEAP[id]; bool owned = SAVE.guns.Contains(id), eq = SAVE.loadout.GetValueOrDefault(ck) == id; var WR = WRAR[W.rar]; int lv = wLv(id);
                var top = new List<Control>
                {
                    Panels.Pv(Panels.Preview(140, 90, (c, w, h) => { c.translate(w / 2 - 32, h / 2 + 2); c.scale(2.1, 2.1); G.drawGun(c, id); }), "grass", owned ? T("Ур. ") + lv : null, null, null, WR.c),
                    Badges(Rar(C.name, C.c), Rar(WR.name, WR.c)),
                    Panels.H4(W.name), Panels.P(W.desc),
                    Panels.Meta(W.meta ?? (T("Урон ") + W.dmg + (W.pellets > 1 ? "×" + W.pellets : "") + " · " + (1 / W.rate).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + T(" выстр/с · ≈") + Math.Round(W.dmg * W.pellets / W.rate) + T(" в сек")))
                };
                if (owned)
                {
                    top.Add(Panels.Meta(T("Уровень ") + lv + T(" из 10") + (lv > 1 ? T(" · урон +") + Math.Round(W_STEP * (lv - 1) * 100) + "%" : ""), true));
                    if (lv < 10)
                    {
                        int nc = wNeed(id, lv), have = wRec(id).cards;
                        top.Add(Col(3, ProgT(G.StripIcon(T("<div class=\"progt\"><span>Карты оружия</span><span>")), have + " / " + nc), Prog((double)have / nc, have >= nc ? "#5fcf3f" : WR.c)));
                    }
                }
                var sel = !owned && !haveCls ? Panels.ClassLock(mv, ck)
                    : Panels.ActBtn(mv, owned, eq, W.eggs > 0 ? W.eggs : W.price, () => { SAVE.guns.Add(id); SAVE.loadout[ck] = id; }, () => Panels.Equip(mv, () => SAVE.loadout[ck] = id, C.name + ": " + W.name), W.eggs > 0 ? "eggs" : "coins");
                var bottom = new List<Control> { sel };
                if (owned && lv < 10)
                {
                    int nc = wNeed(id, lv), co = wCost(id, lv); bool ok = wRec(id).cards >= nc && SAVE.coins >= co;
                    bottom.Add(Panels.PBtn(T("Улучшить · ") + Panels.coinTag(co), ok ? "go" : "poor", () => Panels.UpgradeGun(mv, id)));
                }
                g.Children.Add(Panels.Item(top, bottom, eq));
            }
            body.Children.Add(g);
        }
    }
    /// <summary>Справочник оружия Soul Knight: его не покупают здесь — оно падает в подземелье.</summary>
    void SkCatalog()
    {
        body.Children.Add(Note(T("Это оружие есть только в режиме Soul Knight: оно падает из сундуков в подземелье и продаётся у торговца за пивчики. С собой можно нести 2 ствола (пассивка «Рюкзак» даёт третье место). Стамины в забеге 100: рогатка, водный пистолет и почти всё ближнее оружие её не тратят.")));
        var groups = new[] { ("melee", T("Ближний бой")), ("gun", T("Стрелковое")), ("staff", T("Посохи")) };
        for (int gi = 0; gi < groups.Length; gi++)
        {
            var (key, title) = groups[gi];
            var ids = SKW.LIST.Where(w => SKW.Group(w) == key).OrderBy(w => Array.IndexOf(new[] { "common", "rare", "epic", "legendary" }, w.skr)).ToList();
            var sect = Sect(title + " · " + ids.Count); if (gi > 0) sect.Margin = new Thickness(0, 18, 0, 10); body.Children.Add(sect);
            var g = new AutoGrid { Min = 170, Gap = 12 };
            foreach (var W in ids)
            {
                var R = RAR[W.skr];
                string dps = W.special == "melee" ? T("Урон ") + W.dmg + " · " + (1 / W.rate).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + T(" удар/с")
                    : W.special == "chain" ? T("Урон ") + W.dmg + T(" · цепь на ") + W.chain
                    : T("Урон ") + W.dmg + (W.pellets > 1 ? "×" + W.pellets : W.ring > 0 ? "×" + W.ring : "") + " · " + (1 / W.rate).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + T(" выстр/с");
                string cost = W.coin ? T("1 пивчик за выстрел") : W.stam > 0 ? T("Стамина ") + W.stam.ToString("0.##", L.Culture) + T(" за выстрел") : T("Без стамины");
                var top = new List<Control>
                {
                    Panels.Pv(Panels.Preview(140, 80, (c, w, h) => { c.translate(w / 2 - 34, h / 2 + 2); c.scale(1.9, 1.9); G.drawGun(c, W.id); }), "grass", null, null, null, R.c),
                    Badges(Rar(R.name, R.c), Rar(T("В лавке: ") + SKW.PRICE[W.skr], "#6b4428")),
                    Panels.H4(W.name), Panels.P(W.desc), Panels.Meta(dps), Panels.Meta(cost, true)
                };
                if (SKW.START.Contains(W.id)) top.Add(Panels.Meta(T("С ним начинается забег")));
                g.Children.Add(Panels.Item(top, Array.Empty<Control>()));
            }
            body.Children.Add(g);
        }
    }
}

/* ---------- магазин: карточки дня и золотые яйца ---------- */
public sealed class ShopPanel : BoardPanel
{
    static string tab = "cards";
    public ShopPanel(MainView mv) : base(mv, "Магазин") { Render(); }
    protected override void Build()
    {
        tabsHost.Children.Clear();
        tabsHost.Children.Add(Tabs(new[] { (T("Карточки"), tab == "cards", (Action)(() => { tab = "cards"; scroll.Offset = default; Render(); }), false), (T("Золотые яйца"), tab == "gems", () => { tab = "gems"; scroll.Offset = default; Render(); }, false) }));
        if (tab == "gems") Gems(); else Cards();
    }
    void Cards()
    {
        var left = Offers.Left; var offers = Offers.Get();
        body.Children.Add(Note(T("Каждый день — две случайные пачки карт для твоих героев и стволов. Карты сразу идут в прокачку. Новые карточки через ") + (int)left.TotalHours + T(" ч ") + left.Minutes + T(" мин.")));
        var more = new Press(Txt(T("Оружие — во вкладке «Оружие»"), 13, FontWeight.ExtraBold, InkB, wrap: false), PaperB, 2, 10, new Thickness(12, 7), 2) { HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 12) };
        more.Click += () => mv.OpenPanel("weapons"); body.Children.Add(more);
        if (offers.Count == 0) { body.Children.Add(Note(T("Все герои и стволы на максимальном уровне — карточек сегодня нет."))); return; }
        var g = new AutoGrid { Min = 210, Gap = 12 };
        foreach (var o in offers)
        {
            bool hero = o.kind == "hero"; string name, rarN, rarC; int lv, have, need;
            Control pv;
            if (hero)
            {
                var H = HEROES[o.id]; var R = RAR[H.rar]; name = H.name; rarN = R.name; rarC = R.c; lv = heroLv(o.id); have = SAVE.heroes[o.id].cards; need = lv < 10 ? needCards(o.id, lv) : 0;
                pv = Panels.Pv(Panels.HeroPv(o.id, 200, 110, 1.75), "grass", T("Ур. ") + lv, null, null, R.c);
            }
            else
            {
                var W = WEAP[o.id]; var R = WRAR[W.rar]; name = W.name; rarN = R.name; rarC = R.c; lv = wLv(o.id); have = wRec(o.id).cards; need = lv < 10 ? wNeed(o.id, lv) : 0;
                pv = Panels.Pv(Panels.Preview(160, 110, (c, w, h) => { c.translate(w / 2 - 32, h / 2 + 2); c.scale(2.1, 2.1); G.drawGun(c, o.id); }), "nest", T("Ур. ") + lv, null, null, R.c);
            }
            var top = new List<Control>
            {
                pv, Badges(Rar(rarN, rarC), Rar(hero ? T("Карты героя") : T("Карты оружия"), hero ? "#c0392b" : "#6b4428")),
                Panels.H4("+" + o.n + T(" карт · ") + name),
            };
            if (lv < 10) top.Add(Col(3, ProgT(T("До ур. ") + (lv + 1), have + " / " + need), Prog((double)have / need, have >= need ? "#5fcf3f" : rarC)));
            var oo = o;
            var btn = o.bought ? Panels.PBtn(T("Куплено"), "on", null)
                : Panels.PBtn(Panels.coinTag(o.price), SAVE.coins >= o.price ? "buy" : "poor", () =>
                {
                    if (SAVE.coins < oo.price) { mv.Toast(T("Не хватает ") + L.Fmt(oo.price - SAVE.coins) + T(" зёрен")); Sfx.hurt(); return; }
                    if (Offers.Buy(oo)) { Sfx.buy(); mv.Toast("+" + oo.n + T(" карт · ") + name); mv.AfterChange(); }
                });
            g.Children.Add(Panels.Item(top, new Control[] { btn }));
        }
        body.Children.Add(g);
    }
    void Gems()
    {
        body.Children.Add(Sect(T("Золотые яйца")));
        body.Children.Add(Note(T("Наборы золотых яиц появятся позже — пока кнопки закрыты.")));
        var g = new AutoGrid { Min = 140, Gap = 12 };
        int[] piles = { 1, 2, 3, 5, 8, 12 };
        for (int i = 0; i < GEM_PACKS.Length; i++)
        {
            var p = GEM_PACKS[i]; int n = piles[i];
            var pv = Panels.Pv(Panels.Preview(140, 86, (c, w, h) => { c.translate(w / 2, h - 24); G.drawEggPile(c, n); }), "gold", p.tagRu != null ? T(p.tagRu) : null);
            var h4 = Row(0, Icon("gegg", 18 * .95), Txt(L.Fmt(p.eggs), 18, FontWeight.ExtraBold)); ((Control)h4.Children[0]).Margin = new Thickness(0, 0, 6, 0);
            var btn = Panels.PBtn(T("Скоро"), "poor", () => mv.Toast(T("Донат пока закрыт — скоро откроем")));
            var lockIc = Icon("lock", 13); lockIc.Height = 14;
            btn.Child = Row(6, lockIc, Txt(T("Скоро"), 15.5, FontWeight.ExtraBold, B(PoorFg), wrap: false)); ((StackPanel)btn.Child).HorizontalAlignment = HorizontalAlignment.Center;
            g.Children.Add(Panels.Item(new Control[] { pv, h4, Panels.P(p.name) }, new Control[] { btn }));
        }
        body.Children.Add(g);
        var s2 = Sect(T("Зёрна за золотые яйца")); s2.Margin = new Thickness(0, 18, 0, 10); body.Children.Add(s2);
        body.Children.Add(Note(T("Обменяй золотые яйца на зёрна: чем больше набор, тем выгоднее.")));
        var g2 = new AutoGrid { Min = 140, Gap = 12 };
        for (int i = 0; i < GRAIN_PACKS.Length; i++)
        {
            var p = GRAIN_PACKS[i]; int k = i;
            double bonus = Math.Round(((double)p.coins / p.eggs / ((double)GRAIN_PACKS[0].coins / GRAIN_PACKS[0].eggs) - 1) * 100);
            Control extra = null;
            if (bonus > 0) { var bb = Box(Txt("+" + bonus + "%", 12, FontWeight.Black, White, wrap: false), B("#e8322a"), 2, 8, new Thickness(6, 3)); bb.HorizontalAlignment = HorizontalAlignment.Right; bb.VerticalAlignment = VerticalAlignment.Top; bb.Margin = new Thickness(8); extra = bb; }
            var pv = Panels.Pv(Panels.Preview(140, 86, (c, w, h) => { c.translate(w / 2, h / 2 + 10); G.drawSack(c, k); }), "nest", null, null, extra);
            var h4 = Row(0, Icon("coin", 18 * .95), Txt(L.Fmt(p.coins), 18, FontWeight.ExtraBold)); ((Control)h4.Children[0]).Margin = new Thickness(0, 0, 6, 0);
            bool ok = SAVE.eggs >= p.eggs;
            var btn = Panels.PBtn(Panels.priceTag(p.eggs, "eggs"), ok ? "buy" : "poor", () => Panels.Buy(mv, p.eggs, () => SAVE.coins += p.coins, "eggs"));
            g2.Children.Add(Panels.Item(new Control[] { pv, h4, Panels.P(p.name) }, new Control[] { btn }));
        }
        body.Children.Add(g2);
    }
}

/* ---------- герои и отряд ---------- */
public sealed class HeroesPanel : BoardPanel
{
    public HeroesPanel(MainView mv) : base(mv, "Герои") { Render(); }
    protected override void Build()
    {
        body.Children.Add(Note(T("Новых героев выбивают из сундуков. Карты героя из сундуков вместе с зёрнами повышают его уровень: +6% здоровья и +5% урона за уровень. На 5-м уровне открывается первая пассивка, на 10-м вторая.")));
        body.Children.Add(Sect(T("Отряд")));
        body.Children.Add(Note(T("Бойцы отряда (зелёный круг под ногами) идут рядом, стреляют из выбранного оружия, сами применяют ульту (жёлтая полоска под здоровьем) и принимают удары на себя. Если бойца выбили, через 18 секунд он возвращается, а рядом с живым медиком — вдвое быстрее. Сила зависит от уровня героя. Места в отряде открываются за золотые яйца.")));
        var sg = new AutoGrid { Min = 170, Gap = 12 };
        for (int k = 0; k < 3; k++)
        {
            int kk = k;
            if (k < SAVE.slots)
            {
                var id = k < SAVE.squad.Count ? SAVE.squad[k] : null;
                if (id != null && SAVE.heroes.ContainsKey(id) && id != SAVE.hero)
                {
                    var g0 = gunForAlly(id); var opts = PRIMARY.Where(w => WEAP[w].cls == clsOf(id) && SAVE.guns.Contains(w)).ToList();
                    var look = G.lookFor(id);
                    var top = new Control[]
                    {
                        Panels.Pv(Panels.BirdPv(look, g0, 140, 92, 1.45), "grass", T("Ур. ") + heroLv(id)),
                        Panels.H4(HEROES[id].name), Panels.P(T("Место ") + (k + 1) + " · " + CLASSES[clsOf(id)].name + T(" · ульта: ") + HEROES[id].ult[0]),
                        Panels.Meta(T("Оружие: <b>") + WEAP[g0].name + "</b>" + (opts.Count > 1 ? "" : T(" · купи ещё стволы класса в разделе «Оружие»")))
                    };
                    var wb = Panels.PBtn(T("Сменить оружие"), opts.Count > 1 ? "go" : "poor", () =>
                    {
                        if (opts.Count < 2) { mv.Toast(T("У класса «") + CLASSES[clsOf(id)].name + T("» пока один ствол — купи ещё в разделе «Оружие»")); return; }
                        var nx = opts[(opts.IndexOf(g0) + 1) % opts.Count]; Panels.Equip(mv, () => SAVE.squadGun[id] = nx, HEROES[id].name + ": " + WEAP[nx].name);
                    });
                    var rm = Panels.PBtn(T("Убрать из отряда"), "eq", () => Panels.Equip(mv, () => SAVE.squad.RemoveAt(kk), T("Боец ушёл из отряда")), true);
                    sg.Children.Add(Panels.Item(top, new Control[] { Panels.BRow(false, wb, rm) }));
                }
                else
                {
                    var pv = Panels.Pv(Txt(T("Свободно"), 12, FontWeight.Bold, B("#bf2b1d14")), "grass"); pv.Height = 92;
                    sg.Children.Add(Panels.Item(new Control[] { pv, Panels.H4(T("Место ") + (k + 1)), Panels.P(T("Нажми «В отряд» у героя ниже")) }, Array.Empty<Control>()));
                }
            }
            else
            {
                var pv = Panels.Pv(new Border(), "dark", null, T("Закрыто")); pv.Height = 92;
                Control bottom;
                if (k == SAVE.slots) { int price = SLOT_PRICE[k]; bottom = Panels.PBtn(T("Открыть · ") + Panels.priceTag(price, "eggs"), SAVE.eggs >= price ? "buy" : "poor", () => Panels.Buy(mv, price, () => SAVE.slots = kk + 1, "eggs")); }
                else bottom = Panels.Meta(T("Сначала открой место ") + k);
                sg.Children.Add(Panels.Item(new Control[] { pv, Panels.H4(T("Место ") + (k + 1)) }, new[] { bottom }));
            }
        }
        body.Children.Add(sg);
        var s2 = Sect(T("Герои")); s2.Margin = new Thickness(0, 18, 0, 10); body.Children.Add(s2);
        var g = new AutoGrid { Min = 230, Gap = 12 };
        foreach (var id in HERO_IDS.OrderByDescending(x => SAVE.heroes.ContainsKey(x) ? 1 : 0))
        {
            var H = HEROES[id]; SAVE.heroes.TryGetValue(id, out var own); bool eq = SAVE.hero == id; var R = RAR[H.rar];
            // карточка компактная: характеристики, улучшения и способности — в окне по кнопке «Свойства»
            var st = G.heroStats(id, own?.lv ?? 1);
            var props = new Press(Row(6, InfoDot(), Txt(T("Свойства"), 12.5, FontWeight.ExtraBold, InkB, wrap: false)), PaperB, 2, 999, new Thickness(6, 6, 11, 6), 2)
            { HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(8) };
            props.Click += () => Overlays.OpenHeroInfo(mv, id);
            var top = new List<Control>
            {
                Panels.Pv(Panels.HeroPv(id, 200, 120, 1.85), own != null ? "grass" : "dark", own != null ? T("Ур. ") + own.lv : null, null, props),
                Badges(Rar(R.name, R.c), Rar(CLASSES[H.cls].name, CLASSES[H.cls].c)),
                Panels.H4(H.name), Panels.P(H.perk),
                Panels.Meta(T("Здоровье ") + st.hp + T(" · урон ×") + st.dmg.ToString("0.##", L.Culture) + T(" · ульта: ") + H.ult[0]),
            };
            if (own == null) top.Add(Panels.Meta(T("Не открыт"), true));
            var bottom = new List<Control>();
            if (own != null)
            {
                if (own.lv < 10)
                {
                    int nc = needCards(id, own.lv);
                    top.Add(Col(3, ProgT(G.StripIcon(T("<div class=\"progt\"><span>Карты до ур. ")) + (own.lv + 1), own.cards + " / " + nc), Prog((double)own.cards / nc, R.c)));
                }
                else top.Add(Panels.Meta(T("Максимальный уровень · карт в запасе: ") + own.cards));
                var sel = eq ? Panels.PBtn(T("Выбрано"), "on", null) : Panels.PBtn(T("Выбрать"), "eq", () => Panels.Equip(mv, () => { SAVE.hero = id; SAVE.squad = SAVE.squad.Where(x => x != id).ToList(); }, T("В бой идёт ") + H.name));
                Control up;
                if (own.lv < 10) { int nc = needCards(id, own.lv), co = needCoins(id, own.lv); bool ok = own.cards >= nc && SAVE.coins >= co; up = Panels.PBtn(T("Ур. ") + (own.lv + 1) + " · " + Panels.coinTag(co), ok ? "go" : "poor", () => Panels.UpgradeHero(mv, id)); }
                else up = Panels.PBtn(T("Максимум"), "max", null);
                bottom.Add(Panels.BRow(true, sel, up));
                if (!eq && SAVE.slots > 0)
                {
                    bool inSq = SAVE.squad.Contains(id), free = SAVE.squad.Count(x => SAVE.heroes.ContainsKey(x) && x != SAVE.hero) < SAVE.slots;
                    bottom.Add(Panels.PBtn(inSq ? T("Убрать из отряда") : free ? T("В отряд") : T("В отряде нет мест"), inSq ? "eq" : free ? "go" : "poor", () =>
                    {
                        if (inSq) Panels.Equip(mv, () => SAVE.squad = SAVE.squad.Where(x => x != id).ToList(), T("Боец ушёл из отряда"));
                        else if (free) Panels.Equip(mv, () => { SAVE.squad = SAVE.squad.Where(x => SAVE.heroes.ContainsKey(x) && x != SAVE.hero).ToList(); SAVE.squad.Add(id); }, H.name + T(" в отряде!"));
                        else mv.Toast(T("Купи ещё место для отряда"));
                    }));
                }
            }
            else
            {
                bottom.Add(Panels.Meta(T("Выпадает как «") + R.name.ToLower(L.Culture) + T("» герой: ") + Panels.pct(R.chance[0]) + T(" в обычном сундуке, ") + Panels.pct(R.chance[1]) + T(" в большом")));
                bottom.Add(Panels.PBtn(T("К сундукам"), "go", () => mv.OpenPanel("chestP")));
            }
            g.Children.Add(Panels.Item(top, bottom, eq));
        }
        body.Children.Add(g);
    }
    /// <summary>Кружок «i» на кнопке «Свойства».</summary>
    public static Control InfoDot()
    {
        var iTxt = Txt("i", 12, FontWeight.Black, YolkB, TextAlignment.Center); iTxt.VerticalAlignment = VerticalAlignment.Center; iTxt.FontStyle = FontStyle.Italic;
        var ib = Box(iTxt, InkB, 0, 9); ib.Width = 18; ib.Height = 18; return ib;
    }
}

/* ---------- сюжет ---------- */
public sealed class StoryPanel : BoardPanel
{
    public StoryPanel(MainView mv) : base(mv, "Сюжет") { Render(); }
    static bool chapterOpen(int k) => k == 0 || SAVE.story.done.Contains(k * 20 - 1);
    protected override void Build()
    {
        if (G.storyCh < 0) { G.storyCh = 0; for (int k = 0; k < CHAPTERS.Count; k++) if (chapterOpen(k)) G.storyCh = k; }
        body.Children.Add(Tabs(CHAPTERS.Select((C, k) => (T("Глава ") + (k + 1), k == G.storyCh, (Action)(() => { G.storyCh = k; scroll.Offset = default; Render(); }), !chapterOpen(k)))));
        var Ch = CHAPTERS[G.storyCh]; bool open = chapterOpen(G.storyCh); int doneN = Ch.levels.Count(l => SAVE.story.done.Contains(l.gid));
        var head = new List<Control>
        {
            Txt((T("Глава ") + (G.storyCh + 1) + T(" из ") + CHAPTERS.Count + T(" · пройдено ") + doneN + "/20").ToUpper(L.Culture), 12, FontWeight.ExtraBold, BarnB),
            Panels.H4(Ch.name), Panels.P(Ch.intro), Prog(doneN / 20.0)
        };
        if (doneN == 20) head.Add(Box(Rich(T("<span class=\"k\">ФИНАЛ ГЛАВЫ</span>") + Ch.outro, 12, FontWeight.Medium), B("#ffe9a8"), 0, 10, new Thickness(8, 6)));
        if (!open) { var o = Box(Txt(T("Глава откроется после победы над финальным боссом главы ") + G.storyCh + ".", 12, FontWeight.Medium), StrawB, 0, 10, new Thickness(8, 6)); o.Opacity = .55; head.Add(o); }
        var hb = Panels.Item(head, Array.Empty<Control>()); hb.Margin = new Thickness(0, 0, 0, 14);
        body.Children.Add(hb);
        var g = new AutoGrid { Min = 230, Gap = 12 };
        foreach (var Lv in Ch.levels)
        {
            bool done = SAVE.story.done.Contains(Lv.gid), lopen = Lv.gid == 0 || SAVE.story.done.Contains(Lv.gid - 1) || done, boss = Lv.goal.type == "boss";
            var num = Box(Txt((Lv.i + 1).ToString(), 17, FontWeight.Black, boss && !done ? White : InkB, TextAlignment.Center), done ? B(Hp) : boss ? BarnB : YolkB, 3, 20);
            num.Width = 40; num.Height = 40; ((TextBlock)num.Child).VerticalAlignment = VerticalAlignment.Center;
            var st = Txt((done ? T("Пройден") : lopen ? (boss ? T("Босс") : T("Доступен")) : T("Закрыт")) + (Lv.night ? T(" · ночь") : ""), 12, FontWeight.ExtraBold);
            var topRow = new DockPanel(); DockPanel.SetDock(num, Dock.Left); num.Margin = new Thickness(0, 0, 10, 0); topRow.Children.Add(num);
            var nm = Col(2, Panels.H4(Lv.name), st); nm.VerticalAlignment = VerticalAlignment.Center; topRow.Children.Add(nm);
            var meta = Panels.Meta("<i class=\"coin\"></i>" + Lv.coins + " · <i class=\"tok\"></i>" + Lv.tokens + (done ? T(" · повтор: меньше") : " · <i class=\"gegg\"></i>" + Lv.eggs));
            var card = Panels.Item(new Control[] { topRow, Panels.P(Overlays.goalText(Lv)), meta }, Array.Empty<Control>());
            if (!lopen) { card.Opacity = .55; g.Children.Add(card); continue; }
            var press = new Press(card, null, 0, 18, new Thickness(0), 0); int gid = Lv.gid;
            press.Click += () => Overlays.OpenBrief(mv, gid);
            g.Children.Add(press);
        }
        body.Children.Add(g);
    }
}

/* ---------- сундуки ---------- */
public sealed class ChestPanel : BoardPanel
{
    static readonly int[] chestN = { 1, 1, 1 }; // сколько сундуков открыть за раз: ×1, ×3 или ×10
    public ChestPanel(MainView mv) : base(mv, "Сундуки") { Render(); }
    Control ChestCard(int tier)
    {
        var C = CHESTS[tier];
        var iTxt = Txt("i", 12, FontWeight.Black, YolkB, TextAlignment.Center); iTxt.VerticalAlignment = VerticalAlignment.Center; iTxt.FontStyle = FontStyle.Italic;
        var ib = Box(iTxt, InkB, 0, 9); ib.Width = 18; ib.Height = 18;
        var props = new Press(Row(6, ib, Txt(T("Свойства"), 12.5, FontWeight.ExtraBold, InkB, wrap: false)), PaperB, 2, 999, new Thickness(6, 6, 11, 6), 2)
        { HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(8) };
        props.Click += () => Overlays.OpenChestInfo(mv, tier);
        var pv = Panels.Pv(Panels.Preview(220, 150, (c, w, h) => { c.translate(w / 2, h - 12); c.scale(.9, .9); G.drawChest(c, tier, 0, 0); }), new[] { "nest", "gold", "amet" }[tier], null, null, props);
        var eggTxt = C.egg[0] >= 1 ? C.egg[1] + "–" + C.egg[2] + T(" золотых яиц") : T("шанс золотых яиц");
        var top = new List<Control> { pv, Panels.H4(C.name),
            Rich(C.coins[0] + "–" + C.coins[1] + T(" зёрен · ") + C.hpacks + T(" пачки карт героев · ") + C.wpacks + (C.wpacks > 1 ? T(" пачки") : T(" пачка")) + T(" карт оружия · ") + eggTxt, 12.5, FontWeight.Medium, B("#d92b1d14")) };
        Control bar(long have, long need, string ic, string label, string col) => Col(3, ProgT("<i class=\"" + ic + "\"></i>" + label, L.Fmt(have) + " / " + need), Prog((double)have / need, col));
        if (tier == 0) { top.Add(bar(SAVE.tokens, 100, "tok", T("Жетоны"), "#4cc3ff")); top.Add(Panels.Meta(T("Жетоны дают за врагов, волны и уровни сюжета."))); }
        else if (tier == 1) { top.Add(bar(SAVE.btokens, 10, "btok", T("Большие жетоны"), "#b06cff")); top.Add(Panels.Meta(T("Большой жетон дают за первое прохождение каждого уровня сюжета и за каждые 5 волн в бесконечном бою."))); }
        else top.Add(Panels.Meta("<b>" + T("Есть: ") + SAVE.schests + "</b>" + T(" · по одному за каждого финального босса главы (20-й уровень, первое прохождение) и за 25-ю и 50-ю волну в бесконечном бою.")));
        // выбор количества
        var seg = new UniformGrid { Columns = 3 };
        foreach (var n in new[] { 1, 3, 10 })
        {
            bool on = chestN[tier] == n; int nn = n;
            var p = new Press(Txt("×" + n, 14, FontWeight.Black, InkB, TextAlignment.Center), on ? YolkB : PaperB, 0, 0, new Thickness(0, 9), 0) { MinHeight = 38 };
            p.Click += () => { chestN[tier] = nn; Render(); };
            if (seg.Children.Count > 0) p.BorderBrush = InkB;
            p.BorderThickness = new Thickness(seg.Children.Count > 0 ? 2.5 : 0, 0, 0, 0);
            seg.Children.Add(p);
        }
        var segB = Box(seg, PaperB, 2.5, 12); segB.ClipToBounds = true;
        int cnt = chestN[tier];
        var own = Chests.chestCost(tier, null, cnt); bool ok = own.have >= own.need;
        var ic = own.cur switch { "tok" => "<i class=\"tok\"></i>", "btok" => "<i class=\"btok\"></i>", _ => "<i class=\"sch\"></i>" };
        var b = Panels.PBtn(ok ? T("Открыть") + (cnt > 1 ? " ×" + cnt : "") : ic + T("ещё ") + (own.need - own.have), ok ? "go" : "poor", () =>
        {
            if (!ok)
            {
                mv.Toast(tier == 0 ? T("Нужно ещё ") + (own.need - own.have) + T(" жетонов") : tier == 1 ? T("Большие жетоны: за первое прохождение уровня сюжета и каждые 5 волн в бесконечном бою") : T("Победи финального босса главы — получишь сверхбольшой сундук"));
                return;
            }
            Open(tier, "own", cnt);
        });
        Control row;
        if (tier > 0)
        {
            var eg = Chests.chestCost(tier, "eggs", cnt); bool eok = eg.have >= eg.need;
            var b2 = Panels.PBtn(T("Купить · <i class=\"gegg\"></i>") + eg.need, eok ? "buy" : "poor", () =>
            {
                if (!eok) { mv.Toast(T("Нужно ещё ") + (eg.need - eg.have) + T(" золотых яиц")); Sfx.hurt(); return; }
                Open(tier, "eggs", cnt);
            });
            row = Panels.BRow(true, b, b2);
        }
        else row = b;
        var card = Panels.Item(top, new Control[] { segB, row }, false, tier == 2 ? "#6a3fb5" : null);
        return card;
    }
    void Open(int tier, string pay, int n)
    {
        var res = Chests.Open(tier, pay, n); if (res == null) return;
        Overlays.OpenChestOverlay(mv, tier, res);
    }
    protected override void Build()
    {
        var g = new AutoGrid { Min = 230, Gap = 12 };
        for (int t = 0; t < 3; t++) g.Children.Add(ChestCard(t));
        body.Children.Add(g);
        var n = Note(T("У каждого сундука свои шансы — нажми «Свойства» на картинке сундука. Герои выпадают только из сундуков. Карты героев падают только для открытых героев, карты оружия — только для купленных стволов тех классов, для которых у тебя есть боец."));
        n.Margin = new Thickness(0, 12, 0, 12); body.Children.Add(n);
        body.Children.Add(Sect(T("Золотые яйца")));
        body.Children.Add(Note(T("Редкая валюта: у тебя <b>") + SAVE.eggs + T("</b>. Большой сундук стоит ") + EGG_PRICE + T(", сверхбольшой — ") + SUPER_PRICE + T(". Где взять: первое прохождение уровней сюжета (3–15 за уровень), волны 5, 10, 15, 20, 25, 30, 40 и 50 в бесконечном бою (один раз), Soul Knight, 5-й и 10-й уровень героя, сундуки. Ещё за яйца продаются Нимб, Платиновый окрас и Рельсотрон.")));
    }
}
