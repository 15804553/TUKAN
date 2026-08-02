using System.Windows;

namespace Tukan.App.Views.Chrome;

public static class TukanMessageBox
{
    public static void Show(Window? owner, string message, string title = "TUKAN")
    {
        var window = new TukanMessageWindow();
        window.Configure(message, title);

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
    }
}
