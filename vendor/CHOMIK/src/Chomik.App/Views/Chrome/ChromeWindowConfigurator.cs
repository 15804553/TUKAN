using System.Windows;
using Chomik.App.Views.Branding;

namespace Chomik.App.Views.Chrome;

public static class ChromeWindowConfigurator
{
    public static void Apply(Window window, bool canResize = true)
    {
        window.Icon = ChomikAppIcon.GetIcon();
        window.WindowStyle = WindowStyle.None;
        window.AllowsTransparency = true;
        window.Background = System.Windows.Media.Brushes.Transparent;
        window.ResizeMode = canResize ? ResizeMode.CanResize : ResizeMode.NoResize;
    }
}
