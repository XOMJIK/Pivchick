using System;
using Avalonia;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace Kuriatnyk.Desktop;

/// <summary>Запуск на Windows.</summary>
static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        Audio.Factory = () => new WasapiAudio();
        App.WindowOpened = AutoShots.Run;
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // кэш текстур видеокарты побольше: поле 3000×3000 (36 МБ) не влезало в стандартные ~28 МБ и грузилось в GPU заново каждый кадр
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>().UsePlatformDetect()
        .With(new Avalonia.SkiaOptions { MaxGpuResourceSizeBytes = 256L * 1024 * 1024 }).LogToTrace();
}

/// <summary>Вывод звука через WASAPI (общий режим, ~40 мс задержки).</summary>
sealed class WasapiAudio : IAudioOut
{
    WasapiOut outp;
    sealed class Provider : ISampleProvider
    {
        readonly Action<float[], int> fill; float[] mono = new float[4096];
        public Provider(int sr, Action<float[], int> f) { fill = f; WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sr, 2); }
        public WaveFormat WaveFormat { get; }
        public int Read(float[] buffer, int offset, int count)
        {
            int frames = count / 2; if (mono.Length < frames) mono = new float[frames];
            try { fill(mono, frames); } catch { Array.Clear(mono, 0, frames); }
            for (int i = 0; i < frames; i++) { buffer[offset + 2 * i] = mono[i]; buffer[offset + 2 * i + 1] = mono[i]; }
            return count;
        }
    }
    public void Start(int sampleRate, Action<float[], int> fill)
    {
        outp = new WasapiOut(AudioClientShareMode.Shared, true, 40);
        outp.Init(new Provider(sampleRate, fill));
        outp.Play();
    }
    public void Stop() { try { outp?.Stop(); outp?.Dispose(); } catch { } outp = null; }
}
