using BOBER.Core.Constants;
using BOBER.Core.Models;
using BOBER.Data.Repositories;
using BOBER.Services.Grafik;
using BOBER.Services.Settings;

namespace BOBER.Services.Kalendarz;

public sealed class KalendarzService(
    IKalendarzRepository repository,
    IKoloryRepository koloryRepository,
    ShiftCalendarEngine calendar,
    ISettingsService settings) : IKalendarzService
{
    public Task<IReadOnlyList<KalendarzWpis>> GetMonthAsync(
        int rok,
        int miesiac,
        int? viewerShiftId = null,
        bool includePrivateEntries = false,
        CancellationToken cancellationToken = default) =>
        repository.GetByMonthAsync(rok, miesiac, viewerShiftId, includePrivateEntries, cancellationToken);

    public async Task UpsertAsync(
        DateOnly data,
        IReadOnlyList<int> zmianaIds,
        string tresc,
        string autorLogin,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tresc);
        ArgumentException.ThrowIfNullOrWhiteSpace(autorLogin);

        foreach (var zmianaId in NormalizeZmianaIds(zmianaIds))
        {
            await repository.UpsertAsync(
                new KalendarzWpis
                {
                    Data = data,
                    ZmianaId = zmianaId,
                    TypWpisu = KalendarzTypWpisu.Dca,
                    Tresc = tresc.Trim(),
                    AutorLogin = autorLogin.Trim()
                },
                cancellationToken);
        }
    }

    public async Task AddShiftNoteAsync(
        DateOnly data,
        int authorShiftId,
        IReadOnlyList<int> recipientShiftIds,
        string tresc,
        string autorLogin,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tresc);
        ArgumentException.ThrowIfNullOrWhiteSpace(autorLogin);
        if (authorShiftId is < 1 or > 3)
            throw new ArgumentOutOfRangeException(nameof(authorShiftId));

        foreach (var recipientShiftId in NormalizeZmianaIds(recipientShiftIds))
        {
            await repository.AddAsync(
                new KalendarzWpis
                {
                    Data = data,
                    ZmianaId = recipientShiftId,
                    TypWpisu = KalendarzTypWpisu.MiedzyZmianami,
                    AutorZmianaId = authorShiftId,
                    Tresc = tresc.Trim(),
                    AutorLogin = autorLogin.Trim()
                },
                cancellationToken);
        }
    }

    public async Task DeleteAsync(
        DateOnly data,
        IReadOnlyList<int> zmianaIds,
        CancellationToken cancellationToken = default)
    {
        foreach (var zmianaId in NormalizeZmianaIds(zmianaIds))
            await repository.DeleteByDateAndZmianaAsync(data, zmianaId, cancellationToken);
    }

    public Task MarkAsReadAsync(
        int wpisId,
        int zmianaId,
        string login,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(login);
        // 0 = odczyt przez DCA (odpowiedzi na notatki DCA); 1–3 = odczyt przez zmianę
        if (zmianaId is < 0 or > 3)
            throw new ArgumentOutOfRangeException(nameof(zmianaId));

        return repository.MarkAsReadAsync(wpisId, zmianaId, login.Trim(), cancellationToken);
    }

    public async Task AddDcaReplyAsync(
        DateOnly data,
        int authorShiftId,
        string tresc,
        string autorLogin,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tresc);
        ArgumentException.ThrowIfNullOrWhiteSpace(autorLogin);
        if (authorShiftId is < 1 or > 3)
            throw new ArgumentOutOfRangeException(nameof(authorShiftId));

        await repository.AddAsync(
            new KalendarzWpis
            {
                Data = data,
                ZmianaId = 0,
                TypWpisu = KalendarzTypWpisu.OdpowiedzDca,
                AutorZmianaId = authorShiftId,
                Tresc = tresc.Trim(),
                AutorLogin = autorLogin.Trim()
            },
            cancellationToken);
    }

    public async Task DeleteManyAsync(
        IReadOnlyList<int> wpisIds,
        CancellationToken cancellationToken = default)
    {
        var normalizedIds = wpisIds
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        foreach (var wpisId in normalizedIds)
            await repository.DeleteAsync(wpisId, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<int, string>> GetKoloryZmianAsync(
        CancellationToken cancellationToken = default)
    {
        var all = await koloryRepository.GetAllAsync(cancellationToken);
        var dict = all
            .GroupBy(k => k.KluczRoli)
            .ToDictionary(g => g.Key, g => g.First().KolorHex);

        var result = new Dictionary<int, string>();
        for (var zmiana = 1; zmiana <= 3; zmiana++)
        {
            var klucz = RoleKeys.KalendarzKluczForZmiana(zmiana);
            result[zmiana] = dict.TryGetValue(klucz, out var hex)
                ? hex
                : RoleKeys.GetDefaultKolorHex(klucz);
        }

        return result;
    }

    public async Task SaveKoloryZmianAsync(
        IReadOnlyDictionary<int, string> kolory,
        CancellationToken cancellationToken = default)
    {
        var existing = await koloryRepository.GetAllAsync(cancellationToken);
        var byKey = existing
            .GroupBy(k => k.KluczRoli)
            .ToDictionary(g => g.Key, g => g.First());

        foreach (var (zmianaId, hex) in kolory)
        {
            if (zmianaId is < 1 or > 3)
                continue;

            var klucz = RoleKeys.KalendarzKluczForZmiana(zmianaId);
            byKey[klucz] = new KolorStanowiska
            {
                KluczRoli = klucz,
                KolorHex = NormalizeHex(hex)
            };
        }

        await koloryRepository.SaveAsync(byKey.Values.ToList(), cancellationToken);
    }

    public Task<KalendarzAutoDeleteMode> GetAutoDeleteModeAsync(
        int? shiftNumber,
        CancellationToken cancellationToken = default) =>
        settings.GetKalendarzAutoDeleteModeAsync(shiftNumber, cancellationToken);

    public Task SaveAutoDeleteModeAsync(
        int? shiftNumber,
        KalendarzAutoDeleteMode mode,
        CancellationToken cancellationToken = default) =>
        settings.SetKalendarzAutoDeleteModeAsync(shiftNumber, mode, cancellationToken);

    public Task<bool> HasUnreadForRecipientAsync(
        int zmianaId,
        CancellationToken cancellationToken = default)
    {
        if (zmianaId is < 1 or > 3)
            throw new ArgumentOutOfRangeException(nameof(zmianaId));

        return repository.HasUnreadForRecipientAsync(zmianaId, cancellationToken);
    }

    public async Task ApplyAutoDeleteAsync(
        int? shiftNumber,
        bool canEditDcaEntries,
        CancellationToken cancellationToken = default)
    {
        var mode = await GetAutoDeleteModeAsync(shiftNumber, cancellationToken);
        var cutoff = GetCutoffDate(mode);
        if (cutoff is null)
            return;

        if (canEditDcaEntries)
        {
            await repository.DeleteOlderThanAsync(cutoff.Value, KalendarzTypWpisu.Dca, null, cancellationToken);
            return;
        }

        if (shiftNumber is >= 1 and <= 3)
        {
            await repository.DeleteOlderThanAsync(
                cutoff.Value,
                KalendarzTypWpisu.Dca,
                shiftNumber.Value,
                cancellationToken);
            await repository.DeleteOlderThanAsync(
                cutoff.Value,
                KalendarzTypWpisu.MiedzyZmianami,
                shiftNumber.Value,
                cancellationToken);
        }
    }

    public Task<IReadOnlyDictionary<int, int>> GetWorkingShiftsForMonthAsync(
        int rok,
        int miesiac,
        CancellationToken cancellationToken = default) =>
        calendar.GetWorkingShiftsForMonthAsync(rok, miesiac, cancellationToken);

    public Task<int> GetWorkingShiftAsync(DateOnly data, CancellationToken cancellationToken = default) =>
        calendar.GetWorkingShiftAsync(data, cancellationToken);

    private static DateOnly? GetCutoffDate(KalendarzAutoDeleteMode mode)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        return mode switch
        {
            KalendarzAutoDeleteMode.Nigdy => null,
            KalendarzAutoDeleteMode.RazNaMiesiac => today.AddMonths(-1),
            KalendarzAutoDeleteMode.RazNaPolRoku => today.AddMonths(-6),
            _ => null
        };
    }

    private static IEnumerable<int> NormalizeZmianaIds(IReadOnlyList<int> zmianaIds)
    {
        var normalized = zmianaIds
            .Where(z => z is >= 1 and <= 3)
            .Distinct()
            .OrderBy(z => z)
            .ToList();

        if (normalized.Count == 0)
            throw new ArgumentException("Wymagana jest co najmniej jedna zmiana (1–3).", nameof(zmianaIds));

        return normalized;
    }

    private static string NormalizeHex(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
            return "#FFFFFF";

        var value = hex.Trim();
        if (!value.StartsWith('#'))
            value = "#" + value;

        return value.ToUpperInvariant();
    }
}
