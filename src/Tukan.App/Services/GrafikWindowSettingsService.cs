using BOBER.Data.Repositories;

namespace Tukan.App.Services;

/// <summary>
/// Czy użytkownik Zmiana 1–3 otwiera grafik miesięczny służb w osobnym oknie.
/// Brak klucza oznacza wyłączenie.
/// </summary>
public sealed class GrafikWindowSettingsService(IUstawieniaRepository ustawienia)
{
    public static string OpenInNewWindowKey(int shiftNumber) =>
        $"GrafikSluzb.NoweOkno.Zmiana{shiftNumber}";

    public async Task<bool> GetOpenInNewWindowAsync(
        int shiftNumber,
        CancellationToken cancellationToken = default)
    {
        if (shiftNumber is < 1 or > 3)
            return false;

        var raw = await ustawienia.GetAsync(OpenInNewWindowKey(shiftNumber), cancellationToken);
        return string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase)
            || raw == "1";
    }

    public Task SetOpenInNewWindowAsync(
        int shiftNumber,
        bool enabled,
        CancellationToken cancellationToken = default)
    {
        if (shiftNumber is < 1 or > 3)
            return Task.CompletedTask;

        return ustawienia.SetAsync(
            OpenInNewWindowKey(shiftNumber),
            enabled ? "true" : "false",
            cancellationToken);
    }
}
