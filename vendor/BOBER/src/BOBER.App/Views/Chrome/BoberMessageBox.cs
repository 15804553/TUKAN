using System.Windows;

namespace BOBER.App.Views.Chrome;

public enum BoberMessageButtons { Ok, YesNo }

public static class BoberMessageBox
{
    public static void Show(Window? owner, string message, string title = "BOBER") =>
        Show(owner, message, title, BoberMessageButtons.Ok);

    public static MessageBoxResult Show(Window? owner, string message, string title, BoberMessageButtons buttons)
    {
        var window = new BoberMessageWindow();
        window.Configure(message, title, buttons);

        if (owner is not null)
        {
            window.Owner = owner;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }
        else
        {
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        window.ShowDialog();
        return window.Result;
    }
}
