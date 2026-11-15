using System;
using System.Threading.Tasks;
using System.Windows;

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
}
