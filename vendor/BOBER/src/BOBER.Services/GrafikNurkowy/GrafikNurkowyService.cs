using BOBER.Core.Constants;
using BOBER.Core.Models;
using BOBER.Data.Repositories;
using BOBER.Services.Grafik;
using BOBER.Services.Personnel;
using BOBER.Services.Settings;
using BOBER.Services.Urlop;

namespace BOBER.Services.GrafikNurkowy;

public sealed class GrafikNurkowyService(
    IGrafikRepository grafikRepository,
    IGrafikNurkowyRepository zatwierdzeniaRepository,
    ShiftCalendarEngine calendar,
    IFunkcjonariuszService funkcjonariusze,
    ISettingsService settings,
    GrafikNurkowyExcelService excel) : IGrafikNurkowyService
{
    public async Task<string> ResolveFilePathAsync(
        int rok,
        int miesiac,
        CancellationToken cancellationToken = default)
    {
        var dir = await settings.GetExportPathGrafikNurkowyAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(dir))
        {
            throw new InvalidOperationException(
                "Nie ustawiono katalogu eksportu grafiku nurkowego. "
                + "Administrator musi wskazać ścieżkę w Ustawieniach → Ścieżki eksportu.");
        }

        return Path.Combine(dir.Trim(), GrafikNurkowyConstants.BuildFileName(miesiac, rok));
    }

    public async Task<GrafikNurkowySyncResult> GenerateOrUpdateAsync(
        int zmianaId,
        int rok,
        int miesiac,
        CancellationToken cancellationToken = default)
    {
        if (await IsZatwierdzonyAsync(rok, miesiac, cancellationToken))
        {
            throw new InvalidOperationException(
                $"Grafik nurkowy za {GrafikNurkowyConstants.MonthNames[miesiac]} {rok} "
                + "jest zatwierdzony i zablokowany przed modyfikacjami.");
        }

        var filePath = await ResolveFilePathAsync(rok, miesiac, cancellationToken);
        var createdNew = !File.Exists(filePath);

        var nurkowieZmiany = await GetNurkowieZmianyAsync(zmianaId, cancellationToken);
        if (nurkowieZmiany.Count == 0)
        {
            throw new InvalidOperationException(
                $"Brak osób z uprawnieniami nurka na zmianie {zmianaId}.");
        }

        var wszyscyNurkowie = await GetWszyscyNurkowieAsync(cancellationToken);
        var workDays = await GetWorkDaysForMonthAsync(zmianaId, rok, miesiac, cancellationToken);
        var wpisy = await grafikRepository.GetByZmianaAndMonthAsync(zmianaId, rok, miesiac, cancellationToken);
        var wpisyByPersonDay = wpisy
            .GroupBy(w => (w.FunkcjonariuszId, w.Dzien))
            .ToDictionary(g => g.Key, g => g.Last().TypWpisu);

        var wartosci = new Dictionary<(int FunkcjonariuszId, int Dzien), string?>();
        foreach (var nurek in nurkowieZmiany)
        {
            foreach (var day in workDays)
            {
                wpisyByPersonDay.TryGetValue((nurek.Id, day), out var typ);
                wartosci[(nurek.Id, day)] = GrafikNurkowyConstants.MapFromGrafikWpis(typ);
            }
        }

        excel.CreateOrUpdate(
            filePath,
            rok,
            miesiac,
            zmianaId,
            nurkowieZmiany,
            wszyscyNurkowie,
            wartosci,
            workDays,
            await calendar.GetWorkingShiftsForMonthAsync(rok, miesiac, cancellationToken));

        return new GrafikNurkowySyncResult
        {
            FilePath = filePath,
            CreatedNew = createdNew,
            UpdatedPeople = nurkowieZmiany.Count,
            Message = createdNew
                ? $"Utworzono grafik nurkowy ({nurkowieZmiany.Count} os. ze zmiany {zmianaId})."
                : $"Zaktualizowano grafik nurkowy ({nurkowieZmiany.Count} os. ze zmiany {zmianaId})."
        };
    }

    public async Task<IReadOnlyList<GrafikNurkowyWiersz>> LoadPreviewAsync(
        int rok,
        int miesiac,
        CancellationToken cancellationToken = default)
    {
        var dir = await settings.GetExportPathGrafikNurkowyAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(dir))
            return [];

        var filePath = Path.Combine(dir.Trim(), GrafikNurkowyConstants.BuildFileName(miesiac, rok));
        var rows = excel.ReadPreview(filePath, rok, miesiac);
        if (rows.Count == 0)
            return rows;

        var wszyscy = await GetWszyscyNurkowieAsync(cancellationToken);
        var nameToZmiana = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var f in wszyscy)
        {
            nameToZmiana[UrlopNameMatcher.Normalize(UrlopNameMatcher.ToExcelFormat(f.Imie, f.Nazwisko))] =
                f.NumerZmiany;
            nameToZmiana[UrlopNameMatcher.Normalize($"{f.Imie} {f.Nazwisko}".Trim())] = f.NumerZmiany;
        }

        foreach (var row in rows)
        {
            if (nameToZmiana.TryGetValue(UrlopNameMatcher.Normalize(row.ImieNazwisko), out var zmiana))
                row.ZmianaId = zmiana;
        }

        return rows;
    }

    public Task<GrafikNurkowyZatwierdzenie?> GetZatwierdzenieAsync(
        int rok,
        int miesiac,
        CancellationToken cancellationToken = default) =>
        zatwierdzeniaRepository.GetAsync(rok, miesiac, cancellationToken);

    public async Task ZatwierdzAsync(
        int rok,
        int miesiac,
        string zatwierdzonyPrzez,
        CancellationToken cancellationToken = default)
    {
        var dir = await settings.GetExportPathGrafikNurkowyAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(dir))
        {
            throw new InvalidOperationException(
                "Brak katalogu eksportu grafiku nurkowego — nie można zatwierdzić nieistniejącego pliku.");
        }

        var filePath = Path.Combine(dir.Trim(), GrafikNurkowyConstants.BuildFileName(miesiac, rok));
        if (!File.Exists(filePath))
        {
            throw new InvalidOperationException(
                $"Brak pliku grafiku nurkowego za {GrafikNurkowyConstants.MonthNames[miesiac]} {rok}. "
                + "Najpierw wygeneruj dokument ze zmian.");
        }

        await zatwierdzeniaRepository.SetZatwierdzenieAsync(
            rok, miesiac, true, zatwierdzonyPrzez, cancellationToken);
    }

    public Task CofnijZatwierdzenieAsync(
        int rok,
        int miesiac,
        CancellationToken cancellationToken = default) =>
        zatwierdzeniaRepository.SetZatwierdzenieAsync(rok, miesiac, false, null, cancellationToken);

    public async Task<bool> IsZatwierdzonyAsync(
        int rok,
        int miesiac,
        CancellationToken cancellationToken = default)
    {
        var status = await zatwierdzeniaRepository.GetAsync(rok, miesiac, cancellationToken);
        return status?.Zatwierdzony == true;
    }

    private async Task<IReadOnlyList<Funkcjonariusz>> GetNurkowieZmianyAsync(
        int zmianaId,
        CancellationToken cancellationToken)
    {
        var lista = await funkcjonariusze.GetByZmianaAsync(zmianaId, cancellationToken);
        return lista.Where(RoleClassifier.IsNurek).ToList();
    }

    private async Task<IReadOnlyList<Funkcjonariusz>> GetWszyscyNurkowieAsync(
        CancellationToken cancellationToken)
    {
        var result = new List<Funkcjonariusz>();
        for (var zmiana = 1; zmiana <= 3; zmiana++)
        {
            var lista = await funkcjonariusze.GetByZmianaAsync(zmiana, cancellationToken);
            result.AddRange(lista.Where(RoleClassifier.IsNurek));
        }

        return result;
    }

    private async Task<HashSet<int>> GetWorkDaysForMonthAsync(
        int zmianaId,
        int rok,
        int miesiac,
        CancellationToken cancellationToken)
    {
        var all = await calendar.GetWorkDaysAsync(zmianaId, rok, cancellationToken);
        return all.Where(d => d.Month == miesiac).Select(d => d.Day).ToHashSet();
    }
}
