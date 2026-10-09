using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Kuriatnyk;

/// <summary>Вывод звука на платформе: раз за разом просит заполнить буфер моно-сэмплами float.</summary>
public interface IAudioOut
{
    void Start(int sampleRate, Action<float[], int> fill);
    void Stop();
}

/// <summary>
/// Синтезатор, повторяющий то, что делала веб-версия через Web Audio: осцилляторы с экспоненциальным
/// скольжением частоты и затуханием, шум через фильтр низких частот, две шины (звуки и музыка).
/// </summary>
public static class Synth
{
    public const int SR = 44100;
    static long now;                       // текущий сэмпл микшера (меняет аудиопоток)
    public static double Time => now / (double)SR;

    enum Wave { Sine, Square, Saw, Triangle }
    sealed class Voice
    {
        public bool music, noise; public Wave wave; public long start, end;
        public double phase, freq, fMul, fEnd, gain, gMul, gFloor, dur;
        public long rampEnd;
        // фильтр шума
        public double b0, b1, b2, a1, a2, x1, x2, y1, y2; public int nIdx;
    }
    static readonly List<Voice> voices = new();
    static readonly ConcurrentQueue<Voice> incoming = new();
    static float[] noiseBuf;
    static readonly Random rng = new();

    // громкость шин сглаживается, как setTargetAtTime
    static double sfxGain, musGain, sfxTarget = .8, musTarget = .33;
    public static void SetBusTargets(double sfx, double mus) { sfxTarget = sfx; musTarget = mus; }

    static IAudioOut outp;
    public static bool Running => outp != null;
    public static void Start(IAudioOut o)
    {
        if (outp != null || o == null) return;
        noiseBuf = new float[SR];
        for (int i = 0; i < noiseBuf.Length; i++) noiseBuf[i] = (float)(rng.NextDouble() * 2 - 1);
        outp = o;
        try { o.Start(SR, Fill); } catch { outp = null; }
    }
    public static void Stop() { try { outp?.Stop(); } catch { } outp = null; }

    static Wave W(string t) => t switch { "square" => Wave.Square, "sawtooth" => Wave.Saw, "triangle" => Wave.Triangle, _ => Wave.Sine };

    /// <summary>_tone(bus,f,d,type,v,slide,t) из audio.js; t — абсолютное время в секундах микшера.</summary>
    public static void Tone(bool music, double f, double d, string type, double v, double slide, double t)
    {
        var vc = new Voice { music = music, wave = W(type), freq = f, gain = v, dur = d };
        vc.start = (long)(t * SR); vc.end = vc.start + (long)((d + .03) * SR); vc.rampEnd = vc.start + (long)(d * SR);
        int n = Math.Max(1, (int)(d * SR));
        vc.fMul = slide > 0 ? Math.Pow(slide / f, 1.0 / n) : 1; vc.fEnd = slide > 0 ? slide : f;
        vc.gMul = Math.Pow(.001 / v, 1.0 / n); vc.gFloor = .001;
        incoming.Enqueue(vc);
    }
    /// <summary>_noise(bus,d,v,freq,t): белый шум через lowpass (Q как у BiquadFilterNode по умолчанию).</summary>
    public static void Noise(bool music, double d, double v, double freq, double t)
    {
        var vc = new Voice { music = music, noise = true, gain = v, dur = d };
        vc.start = (long)(t * SR); vc.end = vc.start + (long)((d + .02) * SR); vc.rampEnd = vc.start + (long)(d * SR);
        int n = Math.Max(1, (int)(d * SR));
        vc.gMul = Math.Pow(.001 / v, 1.0 / n); vc.gFloor = .001;
        vc.nIdx = rng.Next(SR / 2);
        // RBJ lowpass, Q задан в дБ (1 дБ), как в спецификации Web Audio
        double w0 = 2 * Math.PI * Math.Min(freq, SR * .49) / SR, q = Math.Pow(10, 1 / 20.0), alpha = Math.Sin(w0) / (2 * q), cs = Math.Cos(w0), a0 = 1 + alpha;
        vc.b0 = (1 - cs) / 2 / a0; vc.b1 = (1 - cs) / a0; vc.b2 = (1 - cs) / 2 / a0; vc.a1 = -2 * cs / a0; vc.a2 = (1 - alpha) / a0;
        incoming.Enqueue(vc);
    }

    /// <summary>Вызывается из аудиопотока: дописать count сэмплов в buf.</summary>
    static void Take()
    {
        while (incoming.TryDequeue(out var v))
        {   // звук, заказанный «на сейчас», пока шёл прошлый буфер, сдвигаем — чтобы не обрезать его начало
            if (v.start < now) { long sh = now - v.start; v.start += sh; v.end += sh; v.rampEnd += sh; }
            voices.Add(v);
        }
    }
    static void Fill(float[] buf, int count)
    {
        Take();
        Music.Schedule(now + count);
        Take();
        Array.Clear(buf, 0, count);
        double ks = 1 - Math.Exp(-1.0 / (.03 * SR)), km = 1 - Math.Exp(-1.0 / (.08 * SR));
        // шины: сначала сумма голосов каждой шины, потом общий выход
        for (int i = 0; i < count; i++)
        {
            sfxGain += (sfxTarget - sfxGain) * ks; musGain += (musTarget - musGain) * km;
            double s = 0, m = 0; long t = now + i;
            for (int j = 0; j < voices.Count; j++)
            {
                var v = voices[j];
                if (t < v.start || t >= v.end) continue;
                double x;
                if (v.noise)
                {
                    double inp = noiseBuf[v.nIdx]; v.nIdx = (v.nIdx + 1) % noiseBuf.Length;
                    x = v.b0 * inp + v.b1 * v.x1 + v.b2 * v.x2 - v.a1 * v.y1 - v.a2 * v.y2;
                    v.x2 = v.x1; v.x1 = inp; v.y2 = v.y1; v.y1 = x;
                }
                else
                {
                    double dt = v.freq / SR; double p = v.phase;
                    switch (v.wave)
                    {
                        case Wave.Sine: x = Math.Sin(2 * Math.PI * p); break;
                        case Wave.Square: x = (p < .5 ? 1 : -1) + PolyBlep(p, dt) - PolyBlep((p + .5) % 1, dt); break;
                        case Wave.Saw: x = 2 * p - 1 - PolyBlep(p, dt); break;
                        default: x = 1 - 4 * Math.Abs(p - .5); break;
                    }
                    v.phase = p + dt; if (v.phase >= 1) v.phase -= Math.Floor(v.phase);
                    if (t < v.rampEnd) v.freq *= v.fMul; else v.freq = v.fEnd;
                }
                x *= v.gain;
                if (t < v.rampEnd) v.gain *= v.gMul; else v.gain = v.gFloor;
                if (v.music) m += x; else s += x;
            }
            double o = s * sfxGain + m * musGain;
            buf[i] = (float)(o > 1 ? 1 : o < -1 ? -1 : o);
        }
        now += count;
        voices.RemoveAll(v => v.end <= now);
    }
    static double PolyBlep(double t, double dt)
    {
        if (dt <= 0) return 0;
        if (t < dt) { t /= dt; return t + t - t * t - 1; }
        if (t > 1 - dt) { t = (t - 1) / dt; return t * t + t + t + 1; }
        return 0;
    }
}
