using System.Windows;

namespace BOBER.App.Views.Chrome;

public enum BoberMessageButtons { Ok, YesNo }

public static class BoberMessageBox
{
    /// <summary>
    /// Gdy ustawione (np. przez hosta TUKAN), zastępuje tytuł okienka komunikatu.
    /// </summary>
    public static string? ApplicationTitleOverride { get; set; }

    public static void Show(Window? owner, string message, string title = "BOBER") =>
        Show(owner, message, title, BoberMessageButtons.Ok);

    public static MessageBoxResult Show(Window? owner, string message, string title, BoberMessageButtons buttons)
    {
        var window = new BoberMessageWindow();
        window.Configure(message, ResolveTitle(title), buttons);

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

    private static string ResolveTitle(string title) =>
        string.IsNullOrWhiteSpace(ApplicationTitleOverride) ? title : ApplicationTitleOverride;
}
