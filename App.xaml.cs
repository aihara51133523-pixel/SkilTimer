using System;
using System.Windows;

namespace BuffTimer;
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if (Array.IndexOf(e.Args, "--self-test") >= 0)
        {
            try { Countdown.Verify(); Shutdown(0); }
            catch { Shutdown(1); }
            return;
        }
        MainWindow = new MainWindow();
        MainWindow.Show();
    }
}
