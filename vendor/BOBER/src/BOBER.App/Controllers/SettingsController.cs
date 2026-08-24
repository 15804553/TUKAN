using BOBER.Core.Audit;
using BOBER.Core.Constants;
using BOBER.Core.Models;
using BOBER.Services;

namespace BOBER.App.Controllers;

public sealed class SettingsController(AppServices services)
{
    public int ZmianaId => services.Auth.CurrentSession?.ZmianaId ?? 1;
    public string NazwaZmiany => services.Auth.CurrentSession?.NazwaZmiany ?? $"Zmiana {ZmianaId}";

    public async Task<IReadOnlyList<Funkcjonariusz>> GetFunkcjonariuszeAsync(CancellationToken ct = default) =>
        await services.Funkcjonariusze.GetByZmianaAsync(ZmianaId, ct);

    public async Task SaveKolejnoscAsync(IReadOnlyList<int> ids, CancellationToken ct = default) =>
        await services.Kolejnosc.SaveAsync(ZmianaId, ids, ct);

    /// <summary>
    /// Aktualizuje kolumnę Nr w tabeli Funkcjonariusze ChomikDatabase zgodnie z bieżącą kolejnością listy.
    /// Pozycja na liście (1-bazowana) staje się wartością Nr.
    /// Nie rzuca wyjątku — błąd jest przekazywany jako zwracany komunikat.
    /// </summary>
    public async Task<string?> TrySyncNrToChomikAsync(IReadOnlyList<int> ids, CancellationToken ct = default)
    {
        try
        {
            var idToNr = ids
                .Select((id, index) => (id, nr: index + 1))
                .ToDictionary(x => x.id, x => x.nr);
            await services.Chomik.UpdateNrAsync(idToNr, ct);
            return null;
        }
        catch (Exception ex)
        {
            return $"Nr w ChomikDatabase nie zostało zaktualizowane:\n{ex.Message}";
        }
    }

    public async Task<IReadOnlyList<KolorStanowiska>> GetKoloryAsync(CancellationToken ct = default) =>
        await services.Kolory.GetAllAsync(ct);

    public async Task SaveKoloryAsync(IReadOnlyList<KolorStanowiska> kolory, CancellationToken ct = default)
    {
        // SaveAsync nadpisuje całą tabelę — dołącz klucze spoza listy UI (np. legacy DzienSluzby, Nurek).
        var existing = await services.Kolory.GetAllAsync(ct);
        var submittedKeys = kolory.Select(c => c.KluczRoli).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var preserved = existing.Where(k => !submittedKeys.Contains(k.KluczRoli));
        var merged = kolory.Concat(preserved).ToList();
        await services.Kolory.SaveAsync(merged, ct);
    }

    public Task<int> GetStanZmianyAsync(int zmianaId, CancellationToken ct = default) =>
        services.Settings.GetStanZmianyAsync(zmianaId, ct);

    public Task SetStanZmianyAsync(int zmianaId, int stan, CancellationToken ct = default) =>
        services.Settings.SetStanZmianyAsync(zmianaId, stan, ct);

    public Task<int> GetStanMinimalnyAsync(int zmianaId, CancellationToken ct = default) =>
        services.Settings.GetStanMinimalnyAsync(zmianaId, ct);

    public Task SetStanMinimalnyAsync(int zmianaId, int stan, CancellationToken ct = default) =>
        services.Settings.SetStanMinimalnyAsync(zmianaId, stan, ct);

    public Task<int> GetMaxUrlopowNaSluzbieAsync(int zmianaId, CancellationToken ct = default) =>
        services.Settings.GetMaxUrlopowNaSluzbieAsync(zmianaId, ct);

    public Task SetMaxUrlopowNaSluzbieAsync(int zmianaId, int max, CancellationToken ct = default) =>
        services.Settings.SetMaxUrlopowNaSluzbieAsync(zmianaId, max, ct);

    public Task<bool> GetLessColorAsync(CancellationToken ct = default) =>
        services.Settings.GetLessColorAsync(ct);

    public Task SetLessColorAsync(bool enabled, CancellationToken ct = default) =>
        services.Settings.SetLessColorAsync(enabled, ct);

    public Task<GrafikRowColorSettings> GetGrafikRowColorSettingsAsync(CancellationToken ct = default) =>
        services.Settings.GetGrafikRowColorSettingsAsync(ct);

    public Task SetGrafikRowColorSettingsAsync(GrafikRowColorSettings settings, CancellationToken ct = default) =>
        services.Settings.SetGrafikRowColorSettingsAsync(settings, ct);

    public Task<GrafikExportAlternatingSettings> GetGrafikExportAlternatingSettingsAsync(CancellationToken ct = default) =>
        services.Settings.GetGrafikExportAlternatingSettingsAsync(ct);

    public Task SetGrafikExportAlternatingSettingsAsync(
        GrafikExportAlternatingSettings settings,
        CancellationToken ct = default) =>
        services.Settings.SetGrafikExportAlternatingSettingsAsync(settings, ct);

    public async Task ClearHalfYearAsync(int polrocze, bool alsoClearUrlopPlan = false, CancellationToken ct = default)
    {
        await EnsureGuestCanManageGrafikAsync(ct);
        await services.Grafik.ClearHalfYearAsync(ZmianaId, DateTime.Today.Year, polrocze, ct);
        if (alsoClearUrlopPlan)
            await services.UrlopPlan.ClearHalfYearAsync(ZmianaId, DateTime.Today.Year, polrocze, ct);
    }

    public async Task GenerateBaseScheduleAsync(int year, CancellationToken ct = default)
    {
        await EnsureGuestCanManageGrafikAsync(ct);
        var funkcjonariusze = await GetFunkcjonariuszeAsync(ct);
        var ids = funkcjonariusze.Select(f => f.Id).ToList();
        await services.Grafik.GenerateBaseScheduleAsync(ZmianaId, year, ids, ct);
    }

    /// <summary>
    /// Czy pokazać sekcję zarządzania grafikiem. Dla Gościa — tylko gdy Zmiana włączyła dostęp.
    /// </summary>
    public async Task<bool> CanShowGrafikManagementAsync(CancellationToken ct = default)
    {
        if (!GuestChangeAudit.IsGuestSession)
            return true;

        var checker = GuestChangeAudit.CanManageGrafikAsync;
        return checker is not null && await checker(ZmianaId);
    }

    private async Task EnsureGuestCanManageGrafikAsync(CancellationToken ct)
    {
        if (!GuestChangeAudit.IsGuestSession)
            return;

        if (await CanShowGrafikManagementAsync(ct))
            return;

        throw new InvalidOperationException(
            "Zarządzanie grafikiem jest wyłączone dla użytkownika Gość — włącz je w Konto Gość (użytkownik Zmiana).");
    }

    public IReadOnlyList<(string Klucz, string Etykieta)> GetKolorKeys() =>
        RoleKeys.WszystkieKolory
            .Where(k => k != RoleKeys.Nurek)
            .Select(k => (k, RoleKeys.DomyslneEtykiety.TryGetValue(k, out var e) ? e : k))
            .ToList();
}
