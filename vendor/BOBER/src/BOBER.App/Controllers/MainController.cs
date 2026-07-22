using System.Windows.Media;
using BOBER.App.Helpers;
using BOBER.App.ViewModels;
using BOBER.Core.Constants;
using BOBER.Core.Models;
using BOBER.Core.Rules;
using BOBER.Services;

namespace BOBER.App.Controllers;

/// <summary>Grafik roczny: wiersze DataGrid, kolory ról, podsumowania dzienne, eksport Excel.</summary>
public sealed class MainController(AppServices services)
{
    internal AppServices Services => services;

    public SettingsController CreateSettingsController() => new(services);

    private IReadOnlyList<Funkcjonariusz>? _funkcjonariusze;
    private IReadOnlyDictionary<string, string>? _kolory;
    private int _stanZmiany = 10;
    private int _stanMinimalny = 6;

    public int CurrentYear { get; } = DateTime.Today.Year;
    public int ZmianaId => services.Auth.CurrentSession?.ZmianaId ?? 1;
    public string NazwaZmiany => services.Auth.CurrentSession?.NazwaZmiany ?? string.Empty;
    public bool IsShiftScoped =>
        ZmianaId is >= 1 and <= 3
        && NazwaZmiany.StartsWith("Zmiana ", StringComparison.OrdinalIgnoreCase);

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        _funkcjonariusze = await services.Funkcjonariusze.GetByZmianaAsync(ZmianaId, cancellationToken);
        var kolory = await services.Kolory.GetAllAsync(cancellationToken);
        _kolory = kolory.ToDictionary(k => k.KluczRoli, k => k.KolorHex);
        _stanZmiany = await services.Settings.GetStanZmianyAsync(ZmianaId, cancellationToken);
        _stanMinimalny = await services.Settings.GetStanMinimalnyAsync(ZmianaId, cancellationToken);
    }

    public IReadOnlyList<Funkcjonariusz> GetFunkcjonariusze() => _funkcjonariusze ?? [];

    public async Task<IReadOnlyList<GrafikRowViewModel>> BuildRowsAsync(
        int rok,
        int miesiac,
        CancellationToken cancellationToken = default)
    {
        if (_funkcjonariusze is null)
            await LoadAsync(cancellationToken);

        var wpisy = await services.Grafik.GetMonthAsync(ZmianaId, rok, miesiac, cancellationToken);

        // GroupBy zabezpiecza przed wyjątkiem przy zduplikowanych wpisach w DB
        var wpisyLookup = wpisy
            .GroupBy(w => (w.FunkcjonariuszId, w.Dzien))
            .ToDictionary(g => g.Key, g => g.Last().TypWpisu);

        var daysInMonth = DateTime.DaysInMonth(rok, miesiac);
        var rows = new List<GrafikRowViewModel>();

        for (int i = 0; i < _funkcjonariusze!.Count; i++)
        {
            var f = _funkcjonariusze[i];
            var row = new GrafikRowViewModel
            {
                FunkcjonariuszId = f.Id,
                Numer = i + 1,
                ImieNazwisko = f.PelneImieNazwisko,
                Stanowisko = f.Stanowisko,
                KluczRoli = RoleClassifier.DetermineRole(f),
                IsNurek = RoleClassifier.IsNurek(f),
                RowBackground = GetRoleBrush(f),
                RowForeground = GetRoleForeground(f),
                NameBorderBrush = GetNurekBorderBrush(f)
            };

            for (var day = 1; day <= daysInMonth; day++)
            {
                if (wpisyLookup.TryGetValue((f.Id, day), out var wpis))
                    row.SetCell(day, wpis);
            }

            rows.Add(row);
        }

        // Wiersz sumaryczny (Stan / Dowódcy / Nurkowie / Kierowcy / Poziom A/AB)
        var summaryRow = new GrafikRowViewModel
        {
            IsSummaryRow = true,
            ImieNazwisko = "Wolne miejsca\nDowódcy\nNurkowie\nKierowcy\nPoziom A/AB",
            RowBackground = new SolidColorBrush(Color.FromRgb(0xA8, 0x98, 0x68))
        };

        for (var day = 1; day <= daysInMonth; day++)
            UpdateSummaryForDay(summaryRow, rows, day);

        rows.Add(summaryRow);

        var notatki = await services.Grafik.GetNotatkiMonthAsync(ZmianaId, rok, miesiac, cancellationToken);
        var notesRow = new GrafikRowViewModel
        {
            IsNotesRow = true,
            ImieNazwisko = string.Empty,
            RowBackground = Brushes.Transparent,
            RowForeground = new SolidColorBrush(Color.FromRgb(0x2C, 0x28, 0x18))
        };

        foreach (var notatka in notatki)
        {
            if (!string.IsNullOrWhiteSpace(notatka.Tresc))
                notesRow.SetCell(notatka.Dzien, notatka.Tresc);
        }

        // Notatki kalendarza DCA dla tej zmiany — niebieska ikona obok zielonej „N”.
        var kalendarzWpisy = await services.Kalendarz.GetMonthAsync(
            rok, miesiac, zmianaFilter: ZmianaId, cancellationToken);
        foreach (var wpis in kalendarzWpisy)
        {
            if (!string.IsNullOrWhiteSpace(wpis.Tresc))
                notesRow.SetKalendarzNote(wpis.Data.Day, wpis.Tresc);
        }

        rows.Add(notesRow);
        return rows;
    }

    public async Task SetNotatkaAsync(
        int rok,
        int miesiac,
        int dzien,
        string tresc,
        CancellationToken cancellationToken = default)
    {
        var trimmed = tresc?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(trimmed))
        {
            await services.Grafik.ClearNotatkaAsync(ZmianaId, rok, miesiac, dzien, cancellationToken);
            return;
        }

        await services.Grafik.SetNotatkaAsync(ZmianaId, rok, miesiac, dzien, trimmed, cancellationToken);
    }

    public void UpdateNotesRowCell(GrafikRowViewModel notesRow, int dzien, string tresc)
    {
        var trimmed = tresc?.Trim() ?? string.Empty;
        notesRow.SetCell(dzien, trimmed);
    }

    public GrafikCellColors GetCellColors()
    {
        var nieobecnoscHex = GetKolorHex(RoleKeys.WolnaSluzba, RoleKeys.DomyslneKoloryWpisow);
        var nieobecnosc = ParseBrush(nieobecnoscHex);
        return new GrafikCellColors
        {
            DyzurTlo = nieobecnosc,
            WsTlo = nieobecnosc
        };
    }

    public void RefreshSummaryRow(GrafikRowViewModel summaryRow, IEnumerable<GrafikRowViewModel> allRows, int miesiac)
    {
        var daysInMonth = DateTime.DaysInMonth(CurrentYear, miesiac);
        for (var day = 1; day <= daysInMonth; day++)
            UpdateSummaryForDay(summaryRow, allRows, day);
    }

    public async Task SetWpisAsync(
        int funkcjonariuszId,
        int rok,
        int miesiac,
        int dzien,
        string typWpisu,
        CancellationToken cancellationToken = default) =>
        await services.Grafik.SetWpisAsync(funkcjonariuszId, ZmianaId, rok, miesiac, dzien, typWpisu, cancellationToken);

    public async Task ClearWpisAsync(
        int funkcjonariuszId,
        int rok,
        int miesiac,
        int dzien,
        CancellationToken cancellationToken = default) =>
        await services.Grafik.ClearWpisAsync(funkcjonariuszId, rok, miesiac, dzien, cancellationToken);

    public async Task ExportMonthAsync(
        string filePath,
        int rok,
        int miesiac,
        CancellationToken cancellationToken = default)
    {
        if (_kolory is null)
            await LoadAsync(cancellationToken);

        var wpisy = await services.Grafik.GetMonthAsync(ZmianaId, rok, miesiac, cancellationToken);
        var workDays = await GetWorkDaysForMonthAsync(rok, miesiac, cancellationToken);
        services.Export.ExportMonth(
            filePath, rok, miesiac,
            _funkcjonariusze ?? [],
            wpisy,
            _stanZmiany,
            _stanMinimalny,
            _kolory ?? new Dictionary<string, string>(),
            workDays);
    }

    public async Task ExportYearAsync(
        string filePath,
        int rok,
        CancellationToken cancellationToken = default)
    {
        if (_kolory is null || _funkcjonariusze is null)
            await LoadAsync(cancellationToken);

        var wpisyByMonth = new Dictionary<int, IReadOnlyList<GrafikWpis>>();
        var workDaysByMonth = new Dictionary<int, IReadOnlyCollection<int>>();

        for (var miesiac = 1; miesiac <= 12; miesiac++)
        {
            wpisyByMonth[miesiac] = await services.Grafik.GetMonthAsync(ZmianaId, rok, miesiac, cancellationToken);
            workDaysByMonth[miesiac] = await GetWorkDaysForMonthAsync(rok, miesiac, cancellationToken);
        }

        services.Export.ExportYear(
            filePath, rok,
            _funkcjonariusze ?? [],
            wpisyByMonth,
            workDaysByMonth,
            _stanZmiany,
            _stanMinimalny,
            _kolory ?? new Dictionary<string, string>());
    }

    public Task<string> GetExportPathGrafikSluzbAsync(CancellationToken cancellationToken = default) =>
        services.Settings.GetExportPathGrafikSluzbAsync(cancellationToken);

    public Task<GrafikNurkowySyncResult> GenerateGrafikNurkowyAsync(
        int rok,
        int miesiac,
        CancellationToken cancellationToken = default) =>
        services.GrafikNurkowy.GenerateOrUpdateAsync(ZmianaId, rok, miesiac, cancellationToken);

    private SolidColorBrush GetRoleBrush(Funkcjonariusz f)
    {
        var role = RoleClassifier.DetermineBackgroundRole(f);
        if (_kolory is not null && _kolory.TryGetValue(role, out var hex))
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(hex);
                return new SolidColorBrush(color);
            }
            catch { }
        }

        if (RoleKeys.DomyslneKolory.TryGetValue(role, out var defaultHex))
        {
            var color = (Color)ColorConverter.ConvertFromString(defaultHex);
            return new SolidColorBrush(color);
        }

        return new SolidColorBrush(Color.FromRgb(0x2D, 0x2D, 0x2D));
    }

    private SolidColorBrush GetRoleForeground(Funkcjonariusz f)
    {
        var bg = GetRoleBrush(f).Color;
        return IsLightColor(bg)
            ? new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E))
            : new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0));
    }

    private Brush GetNurekBorderBrush(Funkcjonariusz f)
    {
        if (!RoleClassifier.IsNurek(f))
            return Brushes.Transparent;

        return ParseBrush(GetKolorHex(RoleKeys.NurekCzcionka, RoleKeys.DomyslneKoloryWpisow));
    }

    private static bool IsLightColor(Color c)
    {
        var luminance = (0.299 * c.R + 0.587 * c.G + 0.114 * c.B) / 255;
        return luminance > 0.55;
    }

    public async Task<HashSet<int>> GetWorkDaysForMonthAsync(
        int rok,
        int miesiac,
        CancellationToken cancellationToken = default)
    {
        var allWorkDays = await services.Calendar.GetWorkDaysAsync(ZmianaId, rok, cancellationToken);
        return allWorkDays
            .Where(d => d.Month == miesiac)
            .Select(d => d.Day)
            .ToHashSet();
    }

    public int GetStanMinimalny() => _stanMinimalny;
    public int GetStanZmiany() => _stanZmiany;

    private void UpdateSummaryForDay(
        GrafikRowViewModel summaryRow,
        IEnumerable<GrafikRowViewModel> allRows,
        int dzien)
    {
        var (stan, kierowcy, nurkowie, dowodcy, poziom) = ComputeSummary(allRows, dzien);
        summaryRow.SetCell(dzien, $"{stan}\n{dowodcy}\n{nurkowie}\n{kierowcy}\n{poziom}");
    }

    private (int Stan, int Kierowcy, int Nurkowie, int Dowodcy, string Poziom) ComputeSummary(
        IEnumerable<GrafikRowViewModel> allRows,
        int dzien)
    {
        var workerRows = allRows
            .Where(r => !r.IsSummaryRow && !r.IsNotesRow && r.FunkcjonariuszId.HasValue)
            .ToList();

        var nieobecni = workerRows.Count(r => GrafikWpisTypy.JestNieobecnoscia(r.GetCell(dzien)));
        var stanZmiany = workerRows.Count > 0 ? workerRows.Count : _stanZmiany;
        var stan = stanZmiany - _stanMinimalny - nieobecni;

        var funcLookup = (_funkcjonariusze ?? []).ToDictionary(f => f.Id);
        var obecniIds = workerRows
            .Where(r => !GrafikWpisTypy.JestNieobecnoscia(r.GetCell(dzien)))
            .Select(r => r.FunkcjonariuszId!.Value)
            .ToHashSet();

        var kierowcy = obecniIds.Count(id => funcLookup.TryGetValue(id, out var f) && f.MaUprawnieniaKierowca);
        var nurkowie = obecniIds.Count(id => funcLookup.TryGetValue(id, out var f) && RoleClassifier.IsNurek(f));
        var dowodcy = obecniIds.Count(id => funcLookup.TryGetValue(id, out var f) && RoleClassifier.IsDowodca(f));

        var obecni = obecniIds
            .Where(id => funcLookup.ContainsKey(id))
            .Select(id => funcLookup[id]);
        var poziom = PoziomGotowosciNurkowejRules.Format(PoziomGotowosciNurkowejRules.Ocena(obecni));

        return (stan, kierowcy, nurkowie, dowodcy, poziom);
    }

    private string GetKolorHex(string klucz, IReadOnlyDictionary<string, string> domyslne)
    {
        if (_kolory is not null && _kolory.TryGetValue(klucz, out var hex))
            return hex;
        return domyslne.TryGetValue(klucz, out var defaultHex) ? defaultHex : "#6A5C00";
    }

    private static SolidColorBrush ParseBrush(string hex)
    {
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(hex)!;
            return new SolidColorBrush(color);
        }
        catch
        {
            return new SolidColorBrush(Color.FromRgb(0x6A, 0x5C, 0x00));
        }
    }
}
