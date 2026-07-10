using System.Windows;

namespace Chomik.App.Views.Chrome;

public enum ChomikMessageButtons
{
    Ok,
    YesNo
}

public static class ChomikMessageBox
{
    public static void Show(Window? owner, string message, string title = "Chomik") =>
        Show(owner, message, title, ChomikMessageButtons.Ok);

    public static MessageBoxResult Show(
        Window? owner,
        string message,
        string title,
        ChomikMessageButtons buttons)
    {
        var window = new ChomikMessageWindow();
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
