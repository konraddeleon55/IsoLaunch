using System.Windows;
using System.Windows.Navigation;

namespace IsoLaunch.App;

public partial class SplashWindow : Window
{
    public SplashWindow()
    {
        InitializeComponent();
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        => App.OpenExternalUri(e);
}
