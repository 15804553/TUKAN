using System.Text.Json;
using BOBER.Data.Repositories;

namespace Tukan.App.Services.GuestAudit;

/// <summary>Konfiguracja audytu Gościa, blokady Planu urlopów i dostępu do zarządzania grafikiem — tabela Ustawienia (BOBER).</summary>
public sealed class GuestAuditSettingsService(IUstawieniaRepository ustawienia)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static string AuditKey(int shiftNumber) => $"AudytGosc.Zmiana{shiftNumber}";
    public static string UrlopLockKey(int shiftNumber) => $"BlokadaPlanUrlopow.Zmiana{shiftNumber}";
    public static string GrafikManageKey(int shiftNumber) => $"DostepZarzadzanieGrafikiem.Zmiana{shiftNumber}";

    public async Task<GuestAuditScope> GetScopeAsync(
        int shiftNumber,
        CancellationToken cancellationToken = default)
    {
        var raw = await ustawienia.GetAsync(AuditKey(shiftNumber), cancellationToken);
        if (string.IsNullOrWhiteSpace(raw))
            return new GuestAuditScope();

        try
        {
            return JsonSerializer.Deserialize<GuestAuditScope>(raw, JsonOptions) ?? new GuestAuditScope();
        }
        catch (JsonException)
        {
            return new GuestAuditScope();
        }
    }

    public Task SaveScopeAsync(
        int shiftNumber,
        GuestAuditScope scope,
        CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(scope, JsonOptions);
        return ustawienia.SetAsync(AuditKey(shiftNumber), json, cancellationToken);
    }

    public async Task<bool> GetUrlopPlanLockedAsync(
        int shiftNumber,
        CancellationToken cancellationToken = default)
    {
        var raw = await ustawienia.GetAsync(UrlopLockKey(shiftNumber), cancellationToken);
        return string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase)
            || raw == "1";
    }

    public Task SetUrlopPlanLockedAsync(
        int shiftNumber,
        bool locked,
        CancellationToken cancellationToken = default) =>
        ustawienia.SetAsync(UrlopLockKey(shiftNumber), locked ? "true" : "false", cancellationToken);

    /// <summary>
    /// Czy Gość może używać sekcji „Zarządzanie grafikiem”. Brak klucza / false = wyłączone.
    /// </summary>
    public async Task<bool> GetGuestCanManageGrafikAsync(
        int shiftNumber,
        CancellationToken cancellationToken = default)
    {
        var raw = await ustawienia.GetAsync(GrafikManageKey(shiftNumber), cancellationToken);
        return string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase)
            || raw == "1";
    }

    public Task SetGuestCanManageGrafikAsync(
        int shiftNumber,
        bool allowed,
        CancellationToken cancellationToken = default) =>
        ustawienia.SetAsync(GrafikManageKey(shiftNumber), allowed ? "true" : "false", cancellationToken);
}
