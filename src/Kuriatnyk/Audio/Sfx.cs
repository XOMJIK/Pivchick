using System;
using static Kuriatnyk.Store;

namespace Kuriatnyk;

/// <summary>Запуск звука на устройстве (initAudio) и громкость шин (applyVolume).</summary>
public static class Audio
{
    public static Func<IAudioOut> Factory;
    public static void Init()
    {
        if (!Synth.Running && Factory != null) { Synth.Start(Factory()); ApplyVolume(); }
    }
    public static void ApplyVolume()
    {
        if (SET == null) return;
        Synth.SetBusTargets(SET.muted ? 0 : SET.sfx, SET.muted ? 0 : SET.music * .55 * Music.duck);
    }
}

/// <summary>Звуки игры: те же формулы, что SFX в web/js/audio.js.</summary>
public static class Sfx
{
    static bool On => Synth.Running && SET != null && !SET.muted && SET.sfx > 0;
    static void tone(double f, double d, string type, double v, double slide = 0, double delay = 0) { if (On) Synth.Tone(false, f, d, type, v, slide, Synth.Time + delay); }
    static void noise(double d, double v, double freq) { if (On) Synth.Noise(false, d, v, freq, Synth.Time); }
    static double Ms => Environment.TickCount64;
    static double lastSmg;

    public static void Play(string name)
    {
        switch (name)
        {
            case "shot": shot(); break; case "shotgun": shotgun(); break; case "smg": smg(); break; case "pop": pop(); break;
            case "throw": throw_(); break; case "flame": flame(); break; case "ice": ice(); break; case "rail": rail(); break; case "swish": swish(); break;
        }
    }
    public static void shot() { noise(.09, .22, 2600); tone(260, .06, "square", .05, 110); }
    public static void shotgun() { noise(.22, .38, 1500); tone(140, .12, "square", .06, 60); }
    public static void smg() { var n = Ms; if (n - lastSmg < 60) return; lastSmg = n; noise(.05, .14, 3800); }
    public static void pop() { tone(900, .05, "square", .05, 300); noise(.05, .12, 2400); }
    public static void meow() { tone(700, .09, "triangle", .06, 1100); tone(1100, .16, "triangle", .05, 520, .08); }
    public static void egg() => tone(520, .08, "triangle", .05, 260);
    public static void hit() => tone(880, .04, "square", .035, 520);
    public static void cluck() { tone(620, .06, "square", .06, 980); tone(760, .08, "square", .05, 1150, .08); }
    public static void caw() => tone(420, .14, "sawtooth", .06, 300);
    public static void squeak() => tone(1600, .06, "square", .03, 2200);
    public static void yelp() => tone(900, .12, "sawtooth", .05, 500);
    public static void gobble() { tone(300, .05, "square", .06, 420); tone(280, .05, "square", .06, 420, .06); tone(260, .07, "square", .06, 400, .12); }
    public static void hurt() => tone(340, .16, "sawtooth", .07, 130);
    public static void boom() { noise(.7, .5, 520); tone(95, .45, "sine", .3, 38); }
    public static void pick() { tone(660, .07, "triangle", .08, 990); tone(990, .09, "triangle", .07, 1320, .07); }
    public static void buy() { tone(784, .08, "square", .05); tone(1047, .08, "square", .05, 0, .08); tone(1568, .14, "triangle", .06, 0, .16); }
    public static void wave() { tone(523, .12, "square", .05); tone(659, .12, "square", .05, 0, .12); tone(784, .2, "square", .05, 0, .24); }
    public static void win() { var f = new[] { 523, 659, 784, 1047 }; for (int i = 0; i < f.Length; i++) tone(f[i], .18, "square", .05, 0, i * .13); }
    public static void ult() { tone(220, .4, "sawtooth", .07, 880); noise(.3, .2, 1800); }
    public static void crow() { tone(500, .12, "square", .07, 900); tone(700, .3, "square", .07, 1400, .12); }
    public static void dead() => tone(500, .5, "sawtooth", .07, 90);
    public static void throw_() => tone(400, .12, "triangle", .06, 800);
    public static void fuse() { noise(.4, .12, 5000); tone(900, .12, "square", .04, 1400); }
    public static void beep() => tone(1500, .05, "square", .04);
    public static void creak() { tone(190, .45, "sawtooth", .05, 105); tone(260, .3, "square", .025, 150, .15); }
    public static void crack() { noise(.45, .35, 900); tone(130, .4, "sawtooth", .07, 45); noise(.6, .22, 2600); }
    public static void hoot() { tone(380, .12, "sine", .07, 300); tone(330, .18, "sine", .07, 260, .14); }
    public static void zap() => tone(1200, .1, "sawtooth", .05, 300);
    public static void flame() { var n = Ms; if (n - lastSmg < 70) return; lastSmg = n; noise(.09, .12, 900); }
    public static void ice() { var n = Ms; if (n - lastSmg < 70) return; lastSmg = n; tone(1500, .05, "triangle", .03, 2300); }
    public static void rail() { tone(2000, .28, "sawtooth", .06, 180); noise(.22, .25, 5000); }
    /// <summary>Нажатие кнопки интерфейса: короткий мягкий «тык» (в веб-версии кнопки были беззвучными).</summary>
    public static void click() { var n = Ms; if (n - lastClick < 40) return; lastClick = n; tone(1150, .035, "triangle", .09, 760); tone(1700, .02, "sine", .03, 0, .012); }
    static double lastClick;
    public static void swish() { noise(.09, .2, 1600); tone(260, .09, "triangle", .05, 620); }
    public static void chest() { noise(.15, .2, 900); tone(392, .1, "square", .05, 0, .05); }
}

/// <summary>Музыка: короткие чиптюн-петли для меню, боя и босса (как TRACKS в audio.js).</summary>
public static class Music
{
    sealed class Track { public double bpm, lv; public string lead; public int[] roots, mel, bass, kick, snare, hat; }
    static readonly System.Collections.Generic.Dictionary<string, Track> TRACKS = new()
    {
        ["menu"] = new() { bpm = 104, lead = "triangle", lv = .05, roots = new[] { 48, 41, 45, 43 },
            mel = new[] { 76, 79, 84, 79, 76, 79, 81, 79, 77, 81, 84, 81, 77, 76, 74, 72, 76, 81, 84, 88, 86, 84, 83, 81, 79, 83, 86, 83, 79, 0, 74, 0 },
            bass = new[] { 0, 0, 7, 0, 12, 0, 7, 0 }, kick = new[] { 1, 0, 0, 0, 0, 0, 0, 0 }, snare = new[] { 0, 0, 0, 0, 1, 0, 0, 0 }, hat = new[] { 0, 0, 1, 0, 0, 0, 1, 0 } },
        ["battle"] = new() { bpm = 138, lead = "square", lv = .028, roots = new[] { 45, 41, 48, 43 },
            mel = new[] { 69, 72, 76, 81, 79, 76, 72, 76, 77, 81, 84, 81, 79, 77, 76, 77, 76, 79, 84, 79, 76, 79, 76, 74, 74, 79, 83, 86, 83, 79, 74, 71 },
            bass = new[] { 0, 12, 0, 12, 0, 12, 0, 12 }, kick = new[] { 1, 0, 0, 0, 1, 0, 0, 0 }, snare = new[] { 0, 0, 1, 0, 0, 0, 1, 0 }, hat = new[] { 0, 1, 0, 1, 0, 1, 0, 1 } },
        ["boss"] = new() { bpm = 152, lead = "square", lv = .03, roots = new[] { 45, 45, 41, 40 },
            mel = new[] { 69, 72, 76, 81, 76, 72, 69, 72, 69, 72, 77, 81, 77, 72, 69, 72, 77, 81, 84, 81, 77, 74, 72, 74, 76, 80, 83, 88, 83, 80, 76, 71 },
            bass = new[] { 0, 12, 0, 12, 0, 12, 7, 12 }, kick = new[] { 1, 0, 1, 0, 1, 0, 1, 0 }, snare = new[] { 0, 0, 1, 0, 0, 0, 1, 1 }, hat = new[] { 1, 1, 1, 1, 1, 1, 1, 1 } },
    };
    static volatile string want; static string cur; static double next; static int step;
    public static double duck = 1;
    static double mf(int m) => 440 * Math.Pow(2, (m - 69) / 12.0);
    public static void Play(string name) => want = name;
    public static void Duck(double k) { duck = k; Audio.ApplyVolume(); }

    /// <summary>Вызывает аудиопоток перед каждым буфером: расписать ноты на 0,25 с вперёд (musicTick).</summary>
    internal static void Schedule(long untilSample)
    {
        double nowT = Synth.Time;
        var w = want;
        if (w != cur) { cur = w; step = 0; next = nowT + .08; }
        if (cur == null || !TRACKS.TryGetValue(cur, out var T) || SET == null || SET.muted || SET.music <= 0) { next = nowT + .05; return; }
        double e8 = 60 / T.bpm / 2, horizon = untilSample / (double)Synth.SR + .25;
        if (next < nowT - .5) next = nowT + .05; // после паузы не догоняем пропущенное
        while (next < horizon)
        {
            int s = step % 32, bar = s / 8, b = s % 8; double t = next; int r = T.roots[bar];
            int m = T.mel[s]; if (m != 0) Synth.Tone(true, mf(m), e8 * .9, T.lead, T.lv, 0, t);
            if (b % 2 == 0 || T.bass[b] != T.bass[(b + 7) % 8]) Synth.Tone(true, mf(r + T.bass[b]), e8 * .95, "triangle", .11, 0, t);
            if (T.kick[b] != 0) Synth.Tone(true, 120, .14, "sine", .22, 40, t);
            if (T.snare[b] != 0) Synth.Noise(true, .1, .09, 1900, t);
            if (T.hat[b] != 0) Synth.Noise(true, .03, .025, 8000, t);
            next += e8; step++;
        }
    }
}

/// <summary>Платформенные мелочи: вибрация.</summary>
public static class Platform
{
    public static Action<int> Vibrate;
    public static void Buzz(int ms) { try { Vibrate?.Invoke(ms); } catch { } }
}
