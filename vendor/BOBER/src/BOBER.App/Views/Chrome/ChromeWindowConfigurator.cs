using System.Windows;

namespace BOBER.App.Views.Chrome;

public static class ChromeWindowConfigurator
{
    public static void Apply(Window window, bool canResize = true)
    {
        window.WindowStyle = WindowStyle.None;
        window.AllowsTransparency = true;
        window.Background = System.Windows.Media.Brushes.Transparent;
        window.ResizeMode = canResize ? ResizeMode.CanResize : ResizeMode.NoResize;
    }
}
