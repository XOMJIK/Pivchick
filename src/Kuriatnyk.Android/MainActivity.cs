using System;
using Android.App;
using Android.Content.PM;
using Android.Media;
using Android.OS;
using Android.Views;
using Avalonia;
using Avalonia.Android;

namespace Kuriatnyk.Android;

/// <summary>Главное окно Android: настраивает Avalonia и платформенные мелочи (звук, вибрация, сенсорный ввод).</summary>
[Activity(Label = "Боевой Курятник", Theme = "@style/KurTheme", Icon = "@drawable/icon", MainLauncher = true,
    ScreenOrientation = ScreenOrientation.SensorLandscape, // только горизонтально (обе стороны)
    LaunchMode = LaunchMode.SingleTask,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden)]
public class MainActivity : AvaloniaMainActivity<App>
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        G.COARSE = true; // палец, а не мышь: стики и кнопки на экране
        Audio.Factory = () => AndroidAudio.Instance;
        Platform.Vibrate = ms =>
        {
            if (!OperatingSystem.IsAndroidVersionAtLeast(26)) return;
            var v = (Vibrator)GetSystemService(VibratorService);
            if (v == null || !v.HasVibrator) return;
            v.Vibrate(VibrationEffect.CreateOneShot(ms, VibrationEffect.DefaultAmplitude));
        };
        return base.CustomizeAppBuilder(builder);
    }
    protected override void OnCreate(Bundle savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        Window?.AddFlags(WindowManagerFlags.KeepScreenOn); // экран не гаснет во время боя
    }
    protected override void OnPause()
    {
        base.OnPause();
        try { (G.Ui as Kuriatnyk.UI.MainView)?.OnBackground(); Store.persist(); } catch { }
        AndroidAudio.Instance.Pause();
    }
    protected override void OnResume()
    {
        base.OnResume();
        AndroidAudio.Instance.Resume();
    }
}

/// <summary>Звук через AudioTrack: поток пишет моно-сэмплы float, которые готовит Synth.</summary>
public sealed class AndroidAudio : IAudioOut
{
    public static readonly AndroidAudio Instance = new();
    AudioTrack track; System.Threading.Thread thread; volatile bool run, paused; Action<float[], int> fill;

    public void Start(int sampleRate, Action<float[], int> f)
    {
        fill = f;
        int minBuf = AudioTrack.GetMinBufferSize(sampleRate, ChannelOut.Mono, Encoding.PcmFloat);
        var b = new AudioTrack.Builder()
            .SetAudioAttributes(new AudioAttributes.Builder().SetUsage(AudioUsageKind.Game).SetContentType(AudioContentType.Music).Build())
            .SetAudioFormat(new AudioFormat.Builder().SetEncoding(Encoding.PcmFloat).SetSampleRate(sampleRate).SetChannelMask(ChannelOut.Mono).Build())
            .SetBufferSizeInBytes(Math.Max(minBuf, sampleRate / 25 * 4))
            .SetTransferMode(AudioTrackMode.Stream);
        if (OperatingSystem.IsAndroidVersionAtLeast(26)) b.SetPerformanceMode(AudioTrackPerformanceMode.LowLatency);
        track = b.Build();
        track.Play();
        run = true;
        thread = new System.Threading.Thread(Loop) { IsBackground = true, Priority = System.Threading.ThreadPriority.Highest, Name = "audio" };
        thread.Start();
    }
    void Loop()
    {
        var buf = new float[441];
        while (run)
        {
            if (paused) { System.Threading.Thread.Sleep(30); continue; }
            try { fill(buf, buf.Length); track.Write(buf, 0, buf.Length, WriteMode.Blocking); }
            catch { System.Threading.Thread.Sleep(10); }
        }
    }
    public void Pause() { paused = true; try { track?.Pause(); } catch { } }
    public void Resume() { if (track == null) return; try { track.Play(); } catch { } paused = false; }
    public void Stop() { run = false; try { track?.Stop(); track?.Release(); } catch { } track = null; }
}
