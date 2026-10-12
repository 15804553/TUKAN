using BOBER.Data.Repositories;

namespace Tukan.App.Services;

/// <summary>
/// Czy użytkownik Zmiana 1–3 otwiera grafik służb albo obsadę funkcji w osobnym oknie.
/// Brak klucza grafiku oznacza widok w oknie głównym.
/// Brak klucza obsady oznacza osobne okno — tak działało przed przełącznikiem.
/// </summary>
public sealed class GrafikWindowSettingsService(IUstawieniaRepository ustawienia)
{
    public static string OpenInNewWindowKey(int shiftNumber) =>
        $"GrafikSluzb.NoweOkno.Zmiana{shiftNumber}";

    public static string ObsadaOpenInNewWindowKey(int shiftNumber) =>
        $"ObsadaFunkcji.NoweOkno.Zmiana{shiftNumber}";

    public Task<bool> GetOpenInNewWindowAsync(
        int shiftNumber,
        CancellationToken cancellationToken = default) =>
        ReadFlagAsync(OpenInNewWindowKey(shiftNumber), shiftNumber, defaultWhenMissing: false, cancellationToken);

    public Task SetOpenInNewWindowAsync(
        int shiftNumber,
        bool enabled,
        CancellationToken cancellationToken = default) =>
        WriteFlagAsync(OpenInNewWindowKey(shiftNumber), shiftNumber, enabled, cancellationToken);

    public Task<bool> GetObsadaOpenInNewWindowAsync(
        int shiftNumber,
        CancellationToken cancellationToken = default) =>
        ReadFlagAsync(ObsadaOpenInNewWindowKey(shiftNumber), shiftNumber, defaultWhenMissing: true, cancellationToken);

    public Task SetObsadaOpenInNewWindowAsync(
        int shiftNumber,
        bool enabled,
        CancellationToken cancellationToken = default) =>
        WriteFlagAsync(ObsadaOpenInNewWindowKey(shiftNumber), shiftNumber, enabled, cancellationToken);

    private async Task<bool> ReadFlagAsync(
        string key,
        int shiftNumber,
        bool defaultWhenMissing,
        CancellationToken cancellationToken)
    {
        if (shiftNumber is < 1 or > 3)
            return defaultWhenMissing;

        var raw = await ustawienia.GetAsync(key, cancellationToken);
        if (string.IsNullOrWhiteSpace(raw))
            return defaultWhenMissing;

        return string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase) || raw == "1";
    }

    private Task WriteFlagAsync(
        string key,
        int shiftNumber,
        bool enabled,
        CancellationToken cancellationToken)
    {
        if (shiftNumber is < 1 or > 3)
            return Task.CompletedTask;

        return ustawienia.SetAsync(key, enabled ? "true" : "false", cancellationToken);
    }
}
