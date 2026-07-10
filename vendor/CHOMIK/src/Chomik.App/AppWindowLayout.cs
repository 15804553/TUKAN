using System.Windows;

namespace Chomik.App;

public static class AppWindowLayout
{
    public const double MainWidth = 1800;
    public const double MainHeight = 900;
    public const double MinWidth = 1200;

    public static void ApplyMain(Window window)
    {
        window.Width = MainWidth;
        window.Height = MainHeight;
        window.MinWidth = MinWidth;
        window.MinHeight = 600;
        window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        window.WindowState = WindowState.Maximized;
    }

    public static void ApplyDialog(Window window, Window owner)
    {
        window.Owner = owner;
        window.Width = owner.Width;
        window.Height = owner.Height;
        window.MinWidth = MinWidth;
        window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
    }

    public static void ApplyProfileDialog(Window window, Window owner)
    {
        window.Owner = owner;
        window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        if (owner.ActualHeight > 0)
        {
            window.MaxHeight = Math.Max(480, owner.ActualHeight * 0.9);
        }
    }
}
