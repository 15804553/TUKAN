using System.Windows;
using BOBER.App.Views.Chrome;
using BOBER.Services.Logging;

namespace BOBER.App.Logging;

/// <summary>Pokazuje błąd użytkownikowi i zapisuje pełny wyjątek do BOBER.log.</summary>
public static class UiErrorReporter
{
    public static void Show(Window? owner, Exception ex, string context)
    {
        BoberLog.Error(ex, "{Context}", context);
        BoberMessageBox.Show(owner, BoberLog.FormatUserMessage(ex, context), "BOBER");
    }

    public static void Show(Window? owner, string context, string detail)
    {
        BoberLog.Error("{Context}: {Detail}", context, detail);
        BoberMessageBox.Show(owner, $"{context}\n\n{detail}", "BOBER");
    }
}
