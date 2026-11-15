using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace IsoLaunch.App;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var splash = new SplashWindow();
        splash.Show();

        var mainWindow = new MainWindow();
        MainWindow = mainWindow;

        await Task.Delay(TimeSpan.FromMilliseconds(1600));

        mainWindow.Show();
        splash.Close();
    }

    internal static void OpenExternalUri(RequestNavigateEventArgs e)
    {
        using var _ = Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }
}
