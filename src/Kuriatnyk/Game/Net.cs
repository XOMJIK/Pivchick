using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Kuriatnyk;

/// <summary>Сеть для игры по локальной сети (в том числе через Radmin VPN, Hamachi и т. п.): TCP-соединения и поиск серверов.
/// Хост принимает до 3 игроков, все сообщения складываются в очередь и разбираются в потоке игры (Mp.Poll).
/// Сообщение — 4 байта длины и тело, первый байт тела — тип.</summary>
public static class Net
{
    public const int PORT = 27015, DISC_PORT = 27016, MAX_PLAYERS = 4;
    public enum Role { Off, Host, Client }
    public static Role role = Role.Off;
    public static bool IsHost => role == Role.Host;
    public static bool IsClient => role == Role.Client;

    public sealed class Conn
    {
        public int id; public string ip; public TcpClient tcp; public NetworkStream ns; public volatile bool dead;
        public readonly BlockingCollection<byte[]> outq = new(new ConcurrentQueue<byte[]>(), 512);
    }
    /// <summary>Входящие: (соединение, тело). Тело null — соединение разорвано.</summary>
    public static readonly ConcurrentQueue<(Conn c, byte[] msg)> inbox = new();
    public static readonly List<Conn> conns = new();
    static TcpListener listener; static UdpClient disc; static int nextId = 1;
    static volatile bool running;
    /// <summary>Имя сервера для поиска в сети и сколько сейчас игроков (обновляет Mp).</summary>
    public static volatile string hostName = "";
    public static volatile int hostCount = 1;
    public static volatile bool hostOpen = true;

    /* ---------- хост ---------- */
    public static bool StartHost(out string err)
    {
        Stop(); err = null;
        try
        {
            bool loop = Environment.GetEnvironmentVariable("KUR_NET_LOOPBACK") == "1"; // проверки (Snap): только этот компьютер, без окна брандмауэра
            listener = new TcpListener(loop ? IPAddress.Loopback : IPAddress.Any, PORT); listener.Start();
            running = true; role = Role.Host;
            new Thread(AcceptLoop) { IsBackground = true, Name = "net-accept" }.Start();
            if (!loop) try { disc = new UdpClient(); disc.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true); disc.Client.Bind(new IPEndPoint(IPAddress.Any, DISC_PORT)); new Thread(DiscLoop) { IsBackground = true, Name = "net-disc" }.Start(); }
            catch { disc = null; } // поиск в сети не обязателен — можно ввести адрес вручную
            return true;
        }
        catch (Exception ex) { err = ex.Message; Stop(); return false; }
    }
    static void AcceptLoop()
    {
        while (running)
        {
            try
            {
                var t = listener.AcceptTcpClient(); t.NoDelay = true;
                var c = new Conn { id = Interlocked.Increment(ref nextId), tcp = t, ns = t.GetStream(), ip = (t.Client.RemoteEndPoint as IPEndPoint)?.Address.ToString() };
                lock (conns) conns.Add(c);
                StartIo(c);
            }
            catch { if (!running) return; Thread.Sleep(50); }
        }
    }
    static void DiscLoop()
    {
        while (running && disc != null)
        {
            try
            {
                var ep = new IPEndPoint(IPAddress.Any, 0); var data = disc.Receive(ref ep);
                if (Encoding.UTF8.GetString(data) != "KURQ") continue;
                var reply = Encoding.UTF8.GetBytes("KURA|" + Mp.PROTO + "|" + hostCount + "|" + (hostOpen ? 1 : 0) + "|" + hostName);
                disc.Send(reply, reply.Length, ep);
            }
            catch { if (!running) return; Thread.Sleep(50); }
        }
    }

    /* ---------- клиент ---------- */
    /// <summary>Подключиться к хосту; ответ — в потоке игры через inbox (тип 0: ok или ошибка).</summary>
    public static void Connect(string host)
    {
        Stop(); running = true; role = Role.Client;
        new Thread(() =>
        {
            try
            {
                var t = new TcpClient { NoDelay = true };
                var ar = t.BeginConnect(host, PORT, null, null);
                if (!ar.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(5))) { t.Close(); throw new TimeoutException("timeout"); }
                t.EndConnect(ar);
                var c = new Conn { id = 1, tcp = t, ns = t.GetStream(), ip = host };
                lock (conns) conns.Add(c);
                StartIo(c);
                inbox.Enqueue((c, new byte[] { 0, 1 }));
            }
            catch (Exception ex) { inbox.Enqueue((null, Encoding.UTF8.GetBytes("\0" + "\0" + ex.Message))); }
        }) { IsBackground = true, Name = "net-connect" }.Start();
    }

    /* ---------- общее ---------- */
    static void StartIo(Conn c)
    {
        new Thread(() =>
        {
            try
            {
                var br = new BinaryReader(c.ns);
                while (running && !c.dead)
                {
                    int len = br.ReadInt32(); if (len <= 0 || len > 8 << 20) break;
                    var buf = br.ReadBytes(len); if (buf.Length < len) break;
                    inbox.Enqueue((c, buf));
                }
            }
            catch { }
            Drop(c);
        }) { IsBackground = true, Name = "net-read" }.Start();
        new Thread(() =>
        {
            try
            {
                foreach (var m in c.outq.GetConsumingEnumerable())
                {
                    if (c.dead) break;
                    c.ns.Write(BitConverter.GetBytes(m.Length)); c.ns.Write(m);
                }
            }
            catch { }
            Drop(c);
        }) { IsBackground = true, Name = "net-write" }.Start();
    }
    static void Drop(Conn c)
    {
        if (c.dead) return; c.dead = true;
        try { c.outq.CompleteAdding(); } catch { }
        try { c.tcp.Close(); } catch { }
        inbox.Enqueue((c, null));
    }
    public static void Send(Conn c, byte[] data)
    {
        if (c == null || c.dead) return;
        try { if (!c.outq.TryAdd(data)) { /* очередь забита — клиент не успевает, пропускаем кадр */ } } catch { }
    }
    public static void Broadcast(byte[] data) { lock (conns) foreach (var c in conns) Send(c, data); }
    public static void Kick(Conn c) => Drop(c);
    public static void Stop()
    {
        running = false; role = Role.Off;
        try { listener?.Stop(); } catch { } listener = null;
        try { disc?.Close(); } catch { } disc = null;
        lock (conns) { foreach (var c in conns) { try { c.dead = true; c.outq.CompleteAdding(); c.tcp.Close(); } catch { } } conns.Clear(); }
        while (inbox.TryDequeue(out _)) { }
    }

    /// <summary>Адреса этого компьютера в локальных сетях — их называют друзьям. Radmin VPN — обычно 26.x.x.x.</summary>
    public static List<(string ip, string name)> LocalIPs()
    {
        var r = new List<(string, string)>();
        try
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up || ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                var nm = (ni.Name + " " + ni.Description).ToLowerInvariant();
                if (nm.Contains("wsl") || nm.Contains("hyper-v") || nm.Contains("virtualbox") || nm.Contains("vmware") || nm.Contains("docker")) continue; // виртуальные адаптеры — друзьям не нужны
                foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                    if (ua.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ua.Address))
                        r.Add((ua.Address.ToString(), ni.Name));
            }
        }
        catch { }
        // Radmin и Hamachi — первыми: через них обычно играют по интернету
        return r.OrderByDescending(a => a.Item2.Contains("Radmin", StringComparison.OrdinalIgnoreCase) || a.Item1.StartsWith("26.") || a.Item1.StartsWith("25.")).ToList();
    }

    public sealed class Found { public string ip, name; public int count, proto; public bool open; }
    /// <summary>Найти серверы в локальных сетях: широковещательный запрос на все сети этого компьютера, ответы за ~1,2 с.</summary>
    public static void Discover(Action<List<Found>> done)
    {
        new Thread(() =>
        {
            var found = new List<Found>();
            try
            {
                using var u = new UdpClient(0) { EnableBroadcast = true }; u.Client.ReceiveTimeout = 300;
                var q = Encoding.UTF8.GetBytes("KURQ");
                var targets = new HashSet<string> { "255.255.255.255" };
                try
                {
                    foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                    {
                        if (ni.OperationalStatus != OperationalStatus.Up) continue;
                        foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                        {
                            if (ua.Address.AddressFamily != AddressFamily.InterNetwork || ua.IPv4Mask == null) continue;
                            var a = ua.Address.GetAddressBytes(); var m = ua.IPv4Mask.GetAddressBytes(); var b = new byte[4];
                            for (int i = 0; i < 4; i++) b[i] = (byte)(a[i] | ~m[i]);
                            targets.Add(new IPAddress(b).ToString());
                        }
                    }
                }
                catch { }
                foreach (var t in targets) { try { u.Send(q, q.Length, new IPEndPoint(IPAddress.Parse(t), DISC_PORT)); } catch { } }
                var until = DateTime.UtcNow.AddMilliseconds(1300);
                while (DateTime.UtcNow < until)
                {
                    try
                    {
                        var ep = new IPEndPoint(IPAddress.Any, 0); var data = u.Receive(ref ep); var s = Encoding.UTF8.GetString(data).Split('|');
                        if (s.Length < 5 || s[0] != "KURA") continue;
                        var ip = ep.Address.ToString(); if (found.Any(f => f.ip == ip)) continue;
                        found.Add(new Found { ip = ip, proto = int.TryParse(s[1], out var pv) ? pv : 0, count = int.TryParse(s[2], out var n) ? n : 1, open = s[3] == "1", name = string.Join("|", s.Skip(4)) });
                    }
                    catch (SocketException) { }
                }
            }
            catch { }
            Mp.Post(() => done(found));
        }) { IsBackground = true, Name = "net-discover" }.Start();
    }
}

/// <summary>Сборка сообщения: тип и поля. Координаты и прочие дробные — float, чтобы снимок был поменьше.</summary>
public sealed class Pk
{
    readonly MemoryStream ms = new(); public readonly BinaryWriter w;
    public Pk(byte type) { w = new BinaryWriter(ms, Encoding.UTF8); w.Write(type); }
    public Pk F(double v) { w.Write((float)v); return this; }
    public Pk I(int v) { w.Write(v); return this; }
    public Pk L(long v) { w.Write(v); return this; }
    public Pk B(bool v) { w.Write(v); return this; }
    public Pk S(string v) { w.Write(v ?? ""); return this; }
    public Pk U8(int v) { w.Write((byte)v); return this; }
    public byte[] Bytes() { w.Flush(); return ms.ToArray(); }
}
public sealed class Rd
{
    readonly BinaryReader r; public readonly byte type;
    public Rd(byte[] d) { r = new BinaryReader(new MemoryStream(d), Encoding.UTF8); type = r.ReadByte(); }
    public double F() => r.ReadSingle();
    public int I() => r.ReadInt32();
    public long L() => r.ReadInt64();
    public bool B() => r.ReadBoolean();
    public string S() { var s = r.ReadString(); return s.Length == 0 ? null : s; }
    public int U8() => r.ReadByte();
}
