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

/// <summary>Общие кусочки для профиля, заданий и пропуска: аватарка, награды, полоска опыта.</summary>
public static class MetaUi
{
    public static Control Avatar(string id, double size, bool locked = false) => new SkiaView((c, w, h) => Avatars.Draw(c, id, 0, 0, Math.Min(w, h), locked)) { Width = size, Height = size };
    /// <summary>Награда значками в ряд: «🪙 +150 · 🥚 +3 · аватарка».</summary>
    public static Control Rewards(Reward r, double size = 13, IBrush fg = null)
    {
        var w = new WrapPanel { Orientation = Orientation.Horizontal };
        foreach (var (icon, text) in r.Items())
        {
            Control ic = icon == "ava" && r.avatar != null ? Avatar(r.avatar, size + 4) : Icon(icon, size);
            var row = Row(4, ic, Txt(text, size, FontWeight.ExtraBold, fg ?? InkB, wrap: false)); row.Margin = new Thickness(0, 0, 10, 4);
            w.Children.Add(row);
        }
        return w;
    }
    public static string Timer(TimeSpan t) => t.TotalDays >= 1 ? (int)t.TotalDays + T(" д ") + t.Hours + T(" ч") : (int)t.TotalHours + T(" ч ") + t.Minutes + T(" мин");
    /// <summary>Плашка уровня аккаунта: «Ур. 7» и полоска PTS.</summary>
    public static Control LevelBar(double width = 160)
    {
        var (have, need) = Account.Progress(); int lv = Account.Level;
        var lvB = Box(Txt(T("Ур. ") + lv, 12, FontWeight.Black, YolkB, wrap: false), InkB, 0, 8, new Thickness(7, 3));
        var bar = Prog((double)have / need, "#4c7dff"); bar.Width = width; bar.HorizontalAlignment = HorizontalAlignment.Left;
        var t = Txt(lv >= Account.MAX_LV ? T("максимум") : have + " / " + need + " PTS", 11, FontWeight.ExtraBold, B("#cc2b1d14"), wrap: false);
        return Col(3, Row(6, lvB, t), bar);
    }
}

/* ---------- профиль: имя, аватарка, уровень, статистика, достижения, аккаунты ---------- */
public sealed class ProfilePanel : BoardPanel
{
    public static string tab = "me";
    public ProfilePanel(MainView mv) : base(mv, "Профиль") { Render(); }
    protected override void Build()
    {
        tabsHost.Children.Clear();
        int achN = Ach.LIST.Count(a => Ach.Done(a.id));
        tabsHost.Children.Add(Tabs(new[]
        {
            (T("Профиль"), tab == "me", (Action)(() => { tab = "me"; scroll.Offset = default; Render(); }), false),
            (T("Достижения") + " " + achN + "/" + Ach.LIST.Length, tab == "ach", () => { tab = "ach"; scroll.Offset = default; Render(); }, false),
            (T("Аккаунты"), tab == "acc", () => { tab = "acc"; scroll.Offset = default; Render(); }, false),
        }));
        if (tab == "ach") Achievements(); else if (tab == "acc") Accounts(); else Me();
    }

    void Me()
    {
        // шапка: аватарка, имя (меняется), уровень
        var ava = MetaUi.Avatar(SAVE.avatar, 96);
        var nameBox = new TextBox { Text = SAVE.name, MaxLength = 16, FontFamily = Body, FontSize = 18, FontWeight = FontWeight.ExtraBold, MinWidth = 180, Watermark = T("Имя игрока") };
        var save = Panels.PBtn(T("Сохранить имя"), "go", () =>
        {
            var n = CleanName(nameBox.Text);
            if (n == null) { mv.Toast(T("Имя — от 2 до 16 символов")); Sfx.hurt(); return; }
            SAVE.name = n; persist(); Sfx.pick(); mv.Toast(T("Теперь тебя зовут ") + n); mv.AfterChange();
        }, true);
        var (have, need) = Account.Progress(); int lv = Account.Level;
        var next = lv < Account.MAX_LV ? Col(4, Txt(T("Награда за ур. ") + (lv + 1) + ":", 12, FontWeight.ExtraBold, B("#cc2b1d14")), MetaUi.Rewards(Account.RewardFor(lv + 1), 12)) : null;
        var info = Col(8, Txt(T("Имя в игре"), 12, FontWeight.ExtraBold, B("#cc2b1d14")), Row(8, nameBox, save), MetaUi.LevelBar(220),
            Txt(T("PTS — опыт аккаунта: дают за бои, задания и достижения. Всего: ") + L.Fmt(SAVE.pts), 12, FontWeight.SemiBold, B("#cc2b1d14")), next);
        var head = new DockPanel(); DockPanel.SetDock(ava, Dock.Left); ava.Margin = new Thickness(0, 0, 16, 0); ava.VerticalAlignment = VerticalAlignment.Top; head.Children.Add(ava); head.Children.Add(info);
        body.Children.Add(Panels.Item(new Control[] { head }, Array.Empty<Control>()));

        // статистика
        var s2 = Sect(T("Статистика")); s2.Margin = new Thickness(0, 18, 0, 10); body.Children.Add(s2);
        var sg = new AutoGrid { Min = 150, Gap = 10 };
        foreach (var (label, v) in new[]
        {
            (T("Врагов побеждено"), Stats.Get("kills")), (T("Боссов побеждено"), Stats.Get("bosses")), (T("Лучшая волна"), Math.Max(Stats.Get("wave"), SAVE.best.wave)),
            (T("Уровней сюжета"), (long)SAVE.story.done.Count), (T("Комнат Soul Knight"), Stats.Get("sk_rooms")), (T("Забегов пройдено"), Stats.Get("sk_runs")),
            (T("Сундуков открыто"), Stats.Get("chests")), (T("Ульт применено"), Stats.Get("ults")), (T("Боёв сыграно"), Stats.Get("games")),
            (T("Пивчиков собрано"), Stats.Get("piv")), (T("Заданий выполнено"), Stats.Get("quests")), (T("Сетевых игр"), Stats.Get("mp_games")),
        })
            sg.Children.Add(Box(Col(2, Txt(L.Fmt(v), 20, FontWeight.Black), Txt(label, 12, FontWeight.Bold, B("#bf2b1d14"))), PaperB, 2.5, 14, new Thickness(12, 8), 3));
        body.Children.Add(sg);

        // аватарки
        var s3 = Sect(T("Аватарки")); s3.Margin = new Thickness(0, 18, 0, 10); body.Children.Add(s3);
        body.Children.Add(Note(T("Нажми на открытую аватарку, чтобы поставить её. Новые открываются героями, уровнем аккаунта, достижениями и боевым пропуском.")));
        var ag = new AutoGrid { Min = 96, Gap = 10 };
        foreach (var a in Avatars.LIST)
        {
            bool open = Avatars.Unlocked(a.id), on = SAVE.avatar == a.id; var id = a.id;
            var av = MetaUi.Avatar(a.id, 64, !open); av.HorizontalAlignment = HorizontalAlignment.Center;
            var nm = Txt(a.name, 11.5, FontWeight.ExtraBold, InkB, TextAlignment.Center); nm.LineHeight = 13;
            var col = Col(4, av, nm); if (!open) col.Children.Add(Txt(a.how, 10.5, FontWeight.SemiBold, B("#a62b1d14"), TextAlignment.Center));
            var p = new Press(col, on ? YolkB : open ? PaperB : B(Poor), 3, 14, new Thickness(6, 8), on ? 0 : 3);
            if (on) p.BoxShadow = BoxShadows.Parse("0 0 0 3 #C0392B");
            p.Click += () =>
            {
                if (!open) { mv.Toast(a.how); return; }
                SAVE.avatar = id; persist(); Sfx.pick(); Render(); mv.RefreshMenu();
            };
            ag.Children.Add(p);
        }
        body.Children.Add(ag);
    }

    void Achievements()
    {
        body.Children.Add(Note(T("Достижения открываются сами — награда сразу приходит на счёт.")));
        var g = new AutoGrid { Min = 240, Gap = 12 };
        foreach (var a in Ach.LIST.OrderBy(x => Ach.Done(x.id) ? 1 : 0))
        {
            bool done = Ach.Done(a.id); long v; try { v = Math.Min(a.goal, a.val()); } catch { v = 0; }
            var ic = new SkiaView((c, w, h) => Overlays.DrawPassIcon(c, a.icon, w, h)) { Width = 44, Height = 44, VerticalAlignment = VerticalAlignment.Top };
            var col = Col(4, Txt(a.name, 15, FontWeight.Black), Txt(a.desc, 12.5, FontWeight.SemiBold, B("#cc2b1d14")));
            if (!done) col.Children.Add(Col(3, ProgT(T("Прогресс"), L.Fmt(v) + " / " + L.Fmt(a.goal)), Prog((double)v / a.goal, "#ffc93a")));
            else col.Children.Add(Rar(T("Получено"), "#3fa535", 9));
            col.Children.Add(MetaUi.Rewards(a.rw, 12));
            var d = new DockPanel(); DockPanel.SetDock(ic, Dock.Left); ic.Margin = new Thickness(0, 0, 10, 0); d.Children.Add(ic); d.Children.Add(col);
            var card = Box(d, done ? B("#e9f8dc") : PaperB, 3, 16, new Thickness(12, 10), 4); if (!done) card.Opacity = .92;
            g.Children.Add(card);
        }
        body.Children.Add(g);
    }

    void Accounts()
    {
        body.Children.Add(Note(T("Аккаунты хранятся на этом устройстве: у каждого свой прогресс, герои и награды. Сетевых аккаунтов пока нет — сервер появится позже.")));
        var g = new AutoGrid { Min = 230, Gap = 12 };
        foreach (var p in Profiles())
        {
            bool cur = p.id == SET.profile; var pid = p.id;
            var ava = MetaUi.Avatar(p.avatar, 52);
            var col = Col(2, Txt(p.name, 16, FontWeight.Black), Txt(T("Уровень ") + p.lv + " · " + L.Fmt(p.pts) + " PTS", 12, FontWeight.Bold, B("#bf2b1d14")));
            if (cur) col.Children.Add(Rar(T("Сейчас играешь"), "#3fa535", 9));
            var d = new DockPanel(); DockPanel.SetDock(ava, Dock.Left); ava.Margin = new Thickness(0, 0, 10, 0); d.Children.Add(ava); d.Children.Add(col);
            var bottom = new List<Control>();
            if (!cur) bottom.Add(Panels.PBtn(T("Войти"), "go", () => { SwitchProfile(pid); Sfx.pick(); mv.Toast(T("Аккаунт: ") + p.name); G.toMenu(); mv.AfterChange(); Render(); }, true));
            if (!cur && pid != "main") bottom.Add(Panels.PBtn(T("Удалить"), "eq", () => Overlays.Confirm(mv, T("Удалить аккаунт «") + p.name + T("»? Его прогресс пропадёт навсегда."), T("Удалить"), () => { DeleteProfile(pid); Render(); }), true));
            g.Children.Add(Panels.Item(new Control[] { d }, new[] { Panels.BRow(bottom.Count > 1, bottom.ToArray()) }, cur));
        }
        body.Children.Add(g);
        var s2 = Sect(T("Новый аккаунт")); s2.Margin = new Thickness(0, 18, 0, 10); body.Children.Add(s2);
        var nameBox = new TextBox { MaxLength = 16, FontFamily = Body, FontSize = 16, FontWeight = FontWeight.Bold, MinWidth = 200, Watermark = T("Имя игрока") };
        var create = Panels.PBtn(T("Создать и войти"), "buy", () =>
        {
            var n = CleanName(nameBox.Text);
            if (n == null) { mv.Toast(T("Имя — от 2 до 16 символов")); Sfx.hurt(); return; }
            var id = CreateProfile(n); SwitchProfile(id); Sfx.buy(); mv.Toast(T("Новый аккаунт: ") + n); G.toMenu(); mv.AfterChange(); Render();
        }, true);
        body.Children.Add(Box(Row(8, nameBox, create), PaperB, 3, 16, new Thickness(12, 10), 4));
    }
}

/* ---------- задания: три на день и одно на неделю ---------- */
public sealed class QuestsPanel : BoardPanel
{
    public QuestsPanel(MainView mv) : base(mv, "Задания") { Render(); }
    Control Card(QuestRec q, bool weekly)
    {
        var d = Quests.Find(q.id); bool ready = Quests.Ready(q);
        var ic = new SkiaView((c, w, h) => Overlays.DrawPassIcon(c, d.icon, w, h)) { Width = 44, Height = 44, VerticalAlignment = VerticalAlignment.Top };
        var col = Col(5, weekly ? Rar(T("Неделя"), "#6a3fb5", 9) : null, Txt(d.Text(q.goal), 15, FontWeight.Black),
            Col(3, ProgT(T("Прогресс"), L.Fmt(Math.Min(q.prog, q.goal)) + " / " + L.Fmt(q.goal)), Prog((double)q.prog / q.goal, ready || q.claimed ? "#5fcf3f" : "#ffc93a")),
            MetaUi.Rewards(Quests.RewardOf(q), 12));
        var dp = new DockPanel(); DockPanel.SetDock(ic, Dock.Left); ic.Margin = new Thickness(0, 0, 10, 0); dp.Children.Add(ic); dp.Children.Add(col);
        Control btn = q.claimed ? Panels.PBtn(T("Получено"), "on", null) : ready ? Panels.PBtn(T("Забрать"), "buy", () =>
        {
            var r = Quests.RewardOf(q); if (Quests.Claim(q)) { Sfx.buy(); mv.Toast(r.Text()); mv.AfterChange(); }
        }) : Panels.PBtn(T("В процессе"), "poor", null);
        return Panels.Item(new Control[] { dp }, new[] { btn }, ready, weekly ? "#6a3fb5" : null);
    }
    protected override void Build()
    {
        Quests.Ensure();
        body.Children.Add(Note(T("Каждый день в 00:00 по UTC — три новых задания, каждый понедельник в 00:00 по UTC — новое недельное. За них дают зёрна, PTS и опыт боевого пропуска.")));
        var s1 = Sect(T("Задания дня") + " · " + T("новые через ") + MetaUi.Timer(Quests.DayLeft)); body.Children.Add(s1);
        var g = new AutoGrid { Min = 240, Gap = 12 };
        foreach (var q in SAVE.quests.daily) g.Children.Add(Card(q, false));
        body.Children.Add(g);
        var s2 = Sect(T("Задание недели") + " · " + T("новое через ") + MetaUi.Timer(Quests.WeekLeft)); s2.Margin = new Thickness(0, 18, 0, 10); body.Children.Add(s2);
        var g2 = new AutoGrid { Min = 240, Gap = 12 };
        if (SAVE.quests.weekly != null) g2.Children.Add(Card(SAVE.quests.weekly, true));
        body.Children.Add(g2);
    }
}

/* ---------- боевой пропуск ---------- */
public sealed class PassPanel : BoardPanel
{
    public PassPanel(MainView mv) : base(mv, "Боевой пропуск") { Render(); }
    protected override void Build()
    {
        Pass.Ensure(); var Pr = SAVE.pass; int tier = Pass.Tier;
        long inTier = Pr.xp - (long)tier * Pass.XP_PER; bool max = tier >= Pass.TIERS;
        var head = new List<Control>
        {
            Txt((T("Сезон ") + Pass.SeasonNo + " · " + T("до конца ") + MetaUi.Timer(Pass.Left)).ToUpper(L.Culture), 12, FontWeight.ExtraBold, BarnB),
            Panels.H4(T("Ступень ") + tier + " / " + Pass.TIERS, 18),
            Col(3, ProgT(max ? T("Все ступени пройдены") : T("До ступени ") + (tier + 1), max ? "" : inTier + " / " + Pass.XP_PER + T(" опыта")), Prog(max ? 1 : (double)inTier / Pass.XP_PER, "#ffc93a")),
            Panels.P(T("Опыт пропуска дают за бои и задания. Бесплатные награды — всем, премиум — после покупки пропуска на этот сезон. Сезон длится календарный месяц по UTC.")),
        };
        Control buy = Pr.premium ? Panels.PBtn(T("Премиум активен"), "on", null)
            : Panels.PBtn(T("Купить премиум · ") + Panels.priceTag(Pass.PRICE, "eggs"), SAVE.eggs >= Pass.PRICE ? "buy" : "poor", () =>
            {
                if (SAVE.eggs < Pass.PRICE) { mv.Toast(T("Не хватает ") + (Pass.PRICE - SAVE.eggs) + T(" золотых яиц")); Sfx.hurt(); return; }
                if (Pass.BuyPremium()) { Sfx.buy(); Sfx.win(); mv.Toast(T("Премиум-пропуск открыт!")); mv.AfterChange(); }
            });
        var hb = Panels.Item(head, new[] { buy }, false, Pr.premium ? "#c9a227" : null); hb.Margin = new Thickness(0, 0, 0, 14);
        body.Children.Add(hb);
        // ступени: слева бесплатная награда, справа премиум
        var list = Col(8);
        for (int t = 1; t <= Pass.TIERS; t++)
        {
            int tt = t; bool reached = t <= tier;
            var num = Box(Txt(t.ToString(), 15, FontWeight.Black, reached ? InkB : PaperB, TextAlignment.Center), reached ? YolkB : B("#6b5a48"), 3, 18);
            num.Width = 40; num.Height = 40; ((TextBlock)num.Child).VerticalAlignment = VerticalAlignment.Center;
            Control cell(Reward r, bool prem)
            {
                bool claimed = (prem ? Pr.prem : Pr.free).Contains(tt), can = Pass.CanClaim(tt, prem);
                var rw = MetaUi.Rewards(r, 12);
                Control b = claimed ? Rar(T("Получено"), "#3fa535", 9) : can ? Panels.PBtn(T("Забрать"), "buy", () =>
                {
                    if (Pass.Claim(tt, prem)) { Sfx.buy(); mv.Toast(r.Text()); mv.AfterChange(); }
                }, true) : prem && !Pr.premium ? Rar(T("Премиум"), "#c9a227", 9) : Rar(T("Ступень ") + tt, "#6b5a48", 9);
                b.HorizontalAlignment = HorizontalAlignment.Left;
                var bx = Box(Col(6, rw, b), prem ? B("#fff1c9") : PaperB, 2.5, 14, new Thickness(10, 8), 2, prem ? B("#c9a227") : null);
                if (!reached || (prem && !Pr.premium)) bx.Opacity = .7;
                return bx;
            }
            var row = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,*"), ColumnSpacing = 8 };
            var f = cell(Pass.Free(t), false); var p = cell(Pass.Prem(t), true);
            Grid.SetColumn(f, 1); Grid.SetColumn(p, 2); num.VerticalAlignment = VerticalAlignment.Center;
            row.Children.Add(num); row.Children.Add(f); row.Children.Add(p);
            list.Children.Add(row);
        }
        var hdr = new Grid { ColumnDefinitions = new ColumnDefinitions("40,*,*"), ColumnSpacing = 8, Margin = new Thickness(0, 0, 0, 6) };
        var h1 = Sect(T("Бесплатно")); var h2 = Sect(T("Премиум")); Grid.SetColumn(h1, 1); Grid.SetColumn(h2, 2); hdr.Children.Add(h1); hdr.Children.Add(h2);
        body.Children.Add(hdr); body.Children.Add(list);
    }
}
