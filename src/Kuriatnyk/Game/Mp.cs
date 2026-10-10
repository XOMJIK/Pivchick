using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using static Kuriatnyk.D;
using static Kuriatnyk.Store;

namespace Kuriatnyk;

/// <summary>Игрок в сетевом лобби: имя, аватарка, герой и его уровень, ствол класса.</summary>
public sealed class MpPlayer { public int pid; public string name, avatar, hero, gun; public int lv; public bool host; public Net.Conn conn; }

/// <summary>Сетевая игра: лобби, запуск боя, рассылка снимков (хост) и ввода (клиент).
/// Хост считает весь бой; клиенты сами двигают своего героя, присылают прицел и нажатия, а получают картинку мира.</summary>
public static class Mp
{
    public const int PROTO = 1;
    public const byte T_CONN = 0, T_HELLO = 1, T_WELCOME = 2, T_ROSTER = 3, T_REJECT = 4, T_START = 5, T_WORLD = 6, T_SNAP = 7, T_INPUT = 8, T_STAT = 9, T_END = 10, T_LOBBY = 11, T_BYE = 12, T_TOAST = 13;
    public static readonly List<MpPlayer> roster = new();
    /// <summary>Состояние: idle, connecting, lobby (ждём начала), game (идёт бой).</summary>
    public static string state = "idle";
    public static string status = ""; // строка для окна лобби: «Подключение…», «Хост вышел» и т. п.
    public static int myPid;
    /// <summary>Что выбрал хост: режим и уровень сюжета — показывается всем в лобби.</summary>
    public static string mode = "endless"; public static int modeIdx;
    public static event Action Changed; // окно лобби перерисовывается
    static void Fire() => Changed?.Invoke();
    static readonly ConcurrentQueue<Action> posted = new();
    public static void Post(Action a) => posted.Enqueue(a);
    public static bool InMatch => Net.role != Net.Role.Off && state == "game";
    /// <summary>Множитель здоровья врагов по числу игроков: 1 — 100%, 2 — 150%, 3 — 250%, 4 — 400%.</summary>
    public static double Scale(int n) => n <= 1 ? 1 : n == 2 ? 1.5 : n == 3 ? 2.5 : 4;

    /* ---------- хост ---------- */
    public static bool Host()
    {
        Leave();
        if (!Net.StartHost(out var err)) { status = G.T("Не удалось открыть сервер: ") + err; Fire(); return false; }
        roster.Clear(); myPid = 1;
        roster.Add(new MpPlayer { pid = 1, name = SAVE.name, avatar = SAVE.avatar, hero = SAVE.hero, lv = heroLv(SAVE.hero), gun = gunOf(SAVE.hero), host = true });
        state = "lobby"; status = ""; UpdateHostInfo(); Fire(); return true;
    }
    static void UpdateHostInfo() { Net.hostName = SAVE.name; Net.hostCount = roster.Count; Net.hostOpen = state == "lobby" && roster.Count < Net.MAX_PLAYERS; }
    static byte[] RosterMsg()
    {
        var p = new Pk(T_ROSTER).S(mode).I(modeIdx).U8(roster.Count);
        foreach (var r in roster) p.I(r.pid).S(r.name).S(r.avatar).S(r.hero).I(r.lv).B(r.host);
        return p.Bytes();
    }
    public static void SetMode(string m, int idx) { mode = m; modeIdx = idx; if (Net.IsHost) Net.Broadcast(RosterMsg()); Fire(); }
    /// <summary>Хост начинает бой со всеми из лобби.</summary>
    public static void StartMatch(string heroForSk)
    {
        if (!Net.IsHost) return;
        state = "game"; UpdateHostInfo();
        Net.Broadcast(new Pk(T_START).S(mode).I(modeIdx).F(Scale(roster.Count)).Bytes());
        if (mode == "sk") G.startSK(heroForSk ?? SAVE.hero); else G.startGame(mode, modeIdx);
        sentWorld = -1; snapT = 0;
        Stats.AddLocal("mp_games", 1);
    }
    /// <summary>Бой кончился (поражение или победа) — сообщить всем итог.</summary>
    public static void SendEnd(bool won, string lead, string waveLabel, string waveText)
    {
        if (!Net.IsHost || state != "game") return;
        var S = G.S; Net.Broadcast(new Pk(T_END).B(won).S(lead).S(waveLabel).S(waveText).L(S.score).I(S.kills).L(S.coins).L(S.tokens).Bytes());
    }
    /// <summary>Хост вернулся в меню — все возвращаются в лобби.</summary>
    public static void BackToLobby()
    {
        if (Net.IsHost && state == "game") { state = "lobby"; UpdateHostInfo(); Net.Broadcast(new Pk(T_LOBBY).Bytes()); Net.Broadcast(RosterMsg()); Fire(); }
    }

    /* ---------- клиент ---------- */
    public static void Join(string ip)
    {
        Leave(); state = "connecting"; status = G.T("Подключение к ") + ip + "…"; SET.lastIp = ip; saveSet();
        Net.Connect(ip); Fire();
    }
    public static void Leave()
    {
        if (Net.role != Net.Role.Off)
        {
            try { Net.Broadcast(new Pk(T_BYE).Bytes()); } catch { }
            System.Threading.Thread.Sleep(30);
        }
        bool wasClientGame = Net.IsClient && state == "game";
        Net.Stop(); roster.Clear(); state = "idle"; myPid = 0; G.MpHp = 1; Stats.Route = null;
        if (wasClientGame || G.S.mode is "play" or "paused" or "pick" or "over" or "result" && G.IsClientWorld) G.toMenu();
        G.IsClientWorld = false;
        Fire();
    }

    /* ---------- каждый кадр ---------- */
    static double snapT, inT; static int sentWorld = -1;
    public static void Poll(double dt)
    {
        while (posted.TryDequeue(out var a)) { try { a(); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); } }
        if (Net.role == Net.Role.Off) return;
        while (Net.inbox.TryDequeue(out var m))
        {
            try { if (Net.IsHost) HostMsg(m.c, m.msg); else ClientMsg(m.c, m.msg); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
        }
        if (Net.IsHost && state == "game")
        {
            Stats.Route = RouteStat;
            if (G.worldVer != sentWorld) { sentWorld = G.worldVer; Net.Broadcast(G.WorldMsg()); }
            snapT -= dt; if (snapT <= 0) { snapT = 1 / 30.0; Net.Broadcast(G.SnapMsg()); }
        }
        if (Net.IsClient && state == "game" && G.Me != null && G.IsClientWorld)
        {
            inT -= dt; if (inT <= 0) { inT = 1 / 30.0; Net.Broadcast(G.InputMsg()); }
        }
    }
    /// <summary>Хост: удар, убийство или подбор сделал чужой герой — засчитать ему (событие уходит на его компьютер).</summary>
    static bool RouteStat(string key, long n, bool max)
    {
        var p = G.P; if (p == null || !p.remote) return false;
        var r = roster.FirstOrDefault(x => x.pid == p.pid); if (r?.conn != null) Net.Send(r.conn, new Pk(T_STAT).S(key).L(n).B(max).Bytes());
        return true;
    }
    /// <summary>Командная статистика (волна, уровень, комната): всем игрокам.</summary>
    public static void TeamStat(string key, long n, bool max)
    {
        if (!Net.IsHost || state != "game") return;
        Net.Broadcast(new Pk(T_STAT).S(key).L(n).B(max).Bytes());
    }

    static void HostMsg(Net.Conn c, byte[] msg)
    {
        var r0 = roster.FirstOrDefault(x => x.conn == c);
        if (msg == null)
        { // игрок ушёл
            if (r0 == null) return;
            roster.Remove(r0); UpdateHostInfo();
            if (state == "game") { var pl = G.players.FirstOrDefault(p => p.pid == r0.pid); if (pl != null) { G.players.Remove(pl); G.ftext(pl.x, pl.y - 56, r0.name + G.T(" вышел"), "#ffb3a3", 14); } }
            G.Ui.Toast(r0.name + G.T(" вышел из игры"));
            Net.Broadcast(RosterMsg()); Fire(); return;
        }
        var d = new Rd(msg);
        switch (d.type)
        {
            case T_HELLO:
                {
                    int proto = d.I(); string name = d.S() ?? G.T("Игрок"), ava = d.S() ?? "egg", hero = d.S() ?? "hen", gun = null; int lv = d.I(); gun = d.S();
                    string why = proto != PROTO ? G.T("Другая версия игры — обновитесь до одной версии") : state != "lobby" ? G.T("Игра уже идёт — дождитесь конца боя") : roster.Count >= Net.MAX_PLAYERS ? G.T("Нет свободных мест (до 4 игроков)") : null;
                    if (why != null) { Net.Send(c, new Pk(T_REJECT).S(why).Bytes()); Post(() => Net.Kick(c)); return; }
                    if (!HEROES.ContainsKey(hero)) hero = "hen";
                    var r = new MpPlayer { pid = c.id, name = CleanName(name) ?? G.T("Игрок"), avatar = Avatars.Has(ava) ? ava : "egg", hero = hero, lv = Math.Clamp(lv, 1, 10), gun = gun, conn = c };
                    roster.Add(r); UpdateHostInfo();
                    Net.Send(c, new Pk(T_WELCOME).I(r.pid).Bytes());
                    Net.Broadcast(RosterMsg()); Sfx.pick(); G.Ui.Toast(r.name + G.T(" подключился")); Fire();
                    break;
                }
            case T_INPUT:
                {
                    if (r0 == null || state != "game") return;
                    var pl = G.players.FirstOrDefault(p => p.pid == r0.pid); if (pl == null) return;
                    G.ReadInput(d, pl);
                    break;
                }
            case T_BYE: Net.Kick(c); break;
        }
    }

    static void ClientMsg(Net.Conn c, byte[] msg)
    {
        if (msg == null)
        { // связь с хостом пропала
            bool inGame = state == "game";
            Net.Stop(); roster.Clear(); state = "idle"; G.MpHp = 1;
            status = G.T("Связь с хостом потеряна");
            if (inGame || G.IsClientWorld) { G.IsClientWorld = false; G.toMenu(); G.Ui.Toast(G.T("Хост завершил игру или связь потеряна")); }
            Fire(); return;
        }
        var d = new Rd(msg);
        switch (d.type)
        {
            case T_CONN:
                {
                    bool ok = d.U8() == 1;
                    if (!ok) { var err = System.Text.Encoding.UTF8.GetString(msg, 2, msg.Length - 2); Net.Stop(); state = "idle"; status = G.T("Не удалось подключиться: ") + err; Fire(); return; }
                    var h = gunOf(SAVE.hero);
                    Net.Broadcast(new Pk(T_HELLO).I(PROTO).S(SAVE.name).S(SAVE.avatar).S(SAVE.hero).I(heroLv(SAVE.hero)).S(h).Bytes());
                    status = G.T("Подключено, ждём хоста…"); Fire(); break;
                }
            case T_WELCOME: myPid = d.I(); state = "lobby"; status = G.T("Ты в лобби — хост скоро начнёт"); Sfx.pick(); Fire(); break;
            case T_REJECT: status = d.S(); Net.Stop(); state = "idle"; Fire(); break;
            case T_ROSTER:
                {
                    mode = d.S() ?? "endless"; modeIdx = d.I(); int n = d.U8(); roster.Clear();
                    for (int i = 0; i < n; i++) roster.Add(new MpPlayer { pid = d.I(), name = d.S(), avatar = d.S() ?? "egg", hero = d.S() ?? "hen", lv = d.I(), host = d.B() });
                    Fire(); break;
                }
            case T_START:
                {
                    mode = d.S() ?? "endless"; modeIdx = d.I(); G.MpHp = d.F();
                    state = "game"; G.ClientStart(mode, modeIdx); Stats.AddLocal("mp_games", 1); Fire(); break;
                }
            case T_WORLD: G.ReadWorld(d); break;
            case T_SNAP: if (state == "game") G.ReadSnap(d); break;
            case T_STAT: { var k = d.S(); var n = d.L(); bool mx = d.B(); if (k != null) { if (mx) Stats.MaxLocal(k, n); else Stats.AddLocal(k, n); } break; }
            case T_END:
                {
                    bool won = d.B(); string lead = d.S(), wl = d.S(), wt = d.S(); long score = d.L(); int kills = d.I(); long coins = d.L(), tokens = d.L();
                    G.ClientEnd(won, lead, wl, wt, score, kills, coins, tokens); break;
                }
            case T_LOBBY: state = "lobby"; status = G.T("Ты в лобби — хост скоро начнёт"); G.IsClientWorld = false; G.toMenu(); G.Ui.ShowMpLobby(); Fire(); break;
            case T_TOAST: G.Ui.Toast(d.S()); break;
            case T_BYE: break;
        }
    }
}
