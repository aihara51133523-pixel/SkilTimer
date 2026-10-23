using System;
using System.IO;
using System.Media;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace BuffTimer;
internal sealed class MainWindow : Window
{
    private readonly TextBox hotkeyBox = new() { IsReadOnly = true, Height = 34, VerticalContentAlignment = VerticalAlignment.Center };
    private readonly TextBox secondsBox = new() { Height = 34, VerticalContentAlignment = VerticalAlignment.Center };
    private readonly Button toggle = new() { Content = "動作開始", Height = 42, Margin = new Thickness(0, 22, 0, 0) };
    private readonly Countdown countdown = new();
    private readonly OverlayWindow overlay;
    private readonly KeyboardHook? hook;
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(10) };
    private readonly SoundPlayer sound;
    private readonly MemoryStream soundData;
    private Settings settings;
    private bool enabled;
    private static readonly string SettingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BuffTimer", "settings.json");
    public MainWindow()
    {
        settings = LoadSettings();
        Title = "スキルバフタイマー"; Width = 340; Height = 270;
        ResizeMode = ResizeMode.NoResize; WindowStartupLocation = WindowStartupLocation.CenterScreen;
        FontFamily = new System.Windows.Media.FontFamily("Yu Gothic UI"); FontSize = 14;
        var panel = new StackPanel { Margin = new Thickness(24) };
        panel.Children.Add(new TextBlock { Text = "ホットキー（クリックしてキーを押す）", Margin = new Thickness(0, 0, 0, 5) });
        panel.Children.Add(hotkeyBox);
        panel.Children.Add(new TextBlock { Text = "効果時間（秒）", Margin = new Thickness(0, 14, 0, 5) });
        panel.Children.Add(secondsBox); panel.Children.Add(toggle); Content = panel;
        hotkeyBox.Text = KeyInterop.KeyFromVirtualKey(settings.Key).ToString();
        secondsBox.Text = settings.Seconds.ToString();
        overlay = new OverlayWindow();
        if (settings.Left is double left && settings.Top is double top && double.IsFinite(left) && double.IsFinite(top))
        {
            var area = SystemParameters.VirtualScreenWidth;
            if (left >= SystemParameters.VirtualScreenLeft && left + overlay.Width <= SystemParameters.VirtualScreenLeft + area &&
                top >= SystemParameters.VirtualScreenTop && top + overlay.Height <= SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight)
            { overlay.Left = left; overlay.Top = top; }
        }
        countdown.Reset(settings.Seconds); overlay.Display(countdown.Seconds, false);
        soundData = MakeSound(); sound = new SoundPlayer(soundData); sound.Load();
        try
        {
            hook = new KeyboardHook();
            hook.Pressed += key =>
            {
                if (enabled && key == settings.Key)
                    Dispatcher.BeginInvoke(new Action(() => { if (enabled) { countdown.Start(settings.Seconds); Render(); } }));
            };
        }
        catch (Exception ex) { toggle.IsEnabled = false; MessageBox.Show("ホットキーの監視を開始できませんでした。\n" + ex.Message, Title); }
        hotkeyBox.PreviewKeyDown += (_, e) =>
        {
            e.Handled = true;
            var key = e.Key == Key.System ? e.SystemKey : e.Key;
            if (key is Key.None or Key.ImeProcessed or Key.DeadCharProcessed) return;
            settings.Key = KeyInterop.VirtualKeyFromKey(key); hotkeyBox.Text = key.ToString(); SaveSettings();
        };
        toggle.Click += (_, _) =>
        {
            if (!enabled)
            {
                if (!int.TryParse(secondsBox.Text, out int seconds) || seconds < 1 || seconds > 9999)
                { MessageBox.Show("秒数は1〜9999の整数で入力してください。", Title); secondsBox.Focus(); return; }
                settings.Seconds = seconds; SaveSettings();
            }
            enabled = !enabled;
            hotkeyBox.IsEnabled = secondsBox.IsEnabled = !enabled;
            toggle.Content = enabled ? "動作終了" : "動作開始";
            countdown.Reset(settings.Seconds); Render();
        };
        timer.Tick += (_, _) => { if (countdown.Tick()) { try { sound.Play(); } catch { SystemSounds.Exclamation.Play(); } } Render(); };
        Loaded += (_, _) => { overlay.Owner = this; overlay.Show(); timer.Start(); };
        Closed += (_, _) => { timer.Stop(); hook?.Dispose(); settings.Left = overlay.Left; settings.Top = overlay.Top; SaveSettings(); sound.Dispose(); soundData.Dispose(); };
    }
    private void Render() => overlay.Display(countdown.Seconds, countdown.Expired);
    private static Settings LoadSettings()
    {
        try { var s = JsonSerializer.Deserialize<Settings>(File.ReadAllText(SettingsPath)); if (s != null && s.Seconds is >= 1 and <= 9999 && s.Key is >= 1 and <= 254) return s; } catch { }
        return new Settings();
    }
    private void SaveSettings()
    {
        try { Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!); File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings)); } catch { /* Persistence is optional; timing remains available. */ }
    }
    private static MemoryStream MakeSound()
    {
        const int rate = 22050, samples = rate / 2;
        var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.ASCII, true))
        {
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + samples * 2);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16); writer.Write((short)1); writer.Write((short)1);
            writer.Write(rate); writer.Write(rate * 2); writer.Write((short)2); writer.Write((short)16);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(samples * 2);
            for (int i = 0; i < samples; i++)
            {
                double t = i / (double)rate, envelope = Math.Min(1, t * 100) * Math.Min(1, (.5 - t) * 15);
                writer.Write((short)(Math.Sin(2 * Math.PI * (t < .22 ? 880 : 1174.66) * t) * envelope * 10000));
            }
        }
        stream.Position = 0; return stream;
    }
    public sealed class Settings
    {
        public int Key { get; set; } = 0x51;
        public int Seconds { get; set; } = 30;
        public double? Left { get; set; }
        public double? Top { get; set; }
    }
}
