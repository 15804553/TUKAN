using Serilog;
using Serilog.Events;

namespace BOBER.Services.Logging;

/// <summary>
/// Wspólny dziennik aplikacji BOBER (plik BOBER.log obok exe).
/// Konfiguracja w <see cref="BOBER.App.App"/> przy starcie.
/// </summary>
public static class BoberLog
{
    public static void Information(string messageTemplate, params object?[] propertyValues) =>
        Log.Information(messageTemplate, propertyValues);

    public static void Warning(Exception? ex, string messageTemplate, params object?[] propertyValues)
    {
        if (ex is null)
            Log.Warning(messageTemplate, propertyValues);
        else
            Log.Warning(ex, messageTemplate, propertyValues);
    }

    public static void Error(Exception ex, string messageTemplate, params object?[] propertyValues) =>
        Log.Error(ex, messageTemplate, propertyValues);

    public static void Error(string messageTemplate, params object?[] propertyValues) =>
        Log.Error(messageTemplate, propertyValues);

    /// <summary>Rejestruje wyjątek i zwraca komunikat dla użytkownika (bez stack trace).</summary>
    public static string FormatUserMessage(Exception ex, string context) =>
        $"{context}\n\n{ex.Message}";

    public static bool IsConfigured => Log.Logger is not null &&
                                       Log.IsEnabled(LogEventLevel.Information);
}
