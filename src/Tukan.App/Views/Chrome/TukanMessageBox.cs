using System.Windows;

namespace Tukan.App.Views.Chrome;

public enum TukanMessageButtons
{
    Ok,
    YesNo
}

public static class TukanMessageBox
{
    public static void Show(Window? owner, string message, string title = "TUKAN") =>
        Show(owner, message, title, TukanMessageButtons.Ok);

    public static MessageBoxResult Show(
        Window? owner,
        string message,
        string title,
        TukanMessageButtons buttons)
    {
        var window = new TukanMessageWindow();
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
