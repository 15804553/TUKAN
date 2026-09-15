using System.Windows.Media;
using BOBER.App.Helpers;
using BOBER.App.ViewModels;
using BOBER.Core.Constants;
using BOBER.Core.Models;
using BOBER.Core.Rules;
using BOBER.Services;
using BOBER.Services.Logging;

namespace BOBER.App.Controllers;

public sealed record GrafikCellChange(
    int FunkcjonariuszId,
    int Rok,
    int Miesiac,
    int Dzien,
    string PreviousTyp,
    string NewTyp,
    bool IsAuto = false);

/// <summary>Grafik roczny: wiersze DataGrid, kolory ról, podsumowania dzienne, eksport Excel.</summary>
public sealed class MainController(AppServices services)
{
    internal AppServices Services => services;

    public SettingsController CreateSettingsController() => new(services);

    private IReadOnlyList<Funkcjonariusz>? _funkcjonariusze;
    private IReadOnlyList<GrafikZliczanieWiersz> _zliczanie = [];
    private IReadOnlyDictionary<string, KolorStanowiska>? _koloryMap;
    private int _stanZmiany = 10;
    private int _stanMinimalny = 6;
    private readonly Dictionary<int, IReadOnlyDictionary<int, HashSet<int>>> _workDaysByYear = new();

    public int CurrentYear { get; } = DateTime.Today.Year;
    public int ZmianaId => services.Auth.CurrentSession?.ZmianaId ?? 1;
    public string NazwaZmiany => services.Auth.CurrentSession?.NazwaZmiany ?? string.Empty;
    public bool IsShiftScoped =>
        ZmianaId is >= 1 and <= 3
        && NazwaZmiany.StartsWith("Zmiana ", StringComparison.OrdinalIgnoreCase);

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        _workDaysByYear.Clear();
        _funkcjonariusze = await services.Funkcjonariusze.GetByZmianaAsync(ZmianaId, cancellationToken);
        var kolory = await services.Kolory.GetAllAsync(cancellationToken);
        _koloryMap = KoloryLookup.Index(kolory);
        _stanZmiany = await services.Settings.GetStanZmianyAsync(ZmianaId, cancellationToken);
        _stanMinimalny = await services.Settings.GetStanMinimalnyAsync(ZmianaId, cancellationToken);
        _zliczanie = await services.GrafikZliczanie.GetAllAsync(ZmianaId, cancellationToken);
        await ReloadOznaczeniaAsync(cancellationToken);
    }

    public Task ReloadOznaczeniaAsync(CancellationToken cancellationToken = default) =>
        services.Oznaczenia.ReloadAsync(ZmianaId, cancellationToken);

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
            .ToDictionary(g => g.Key, g => g.Last());

        var uwagi = await services.Grafik.GetUwagiMonthAsync(ZmianaId, rok, miesiac, cancellationToken);
        var uwagiLookup = uwagi
            .GroupBy(u => u.FunkcjonariuszId)
            .ToDictionary(g => g.Key, g => g.Last().Tresc);

        var rowColors = await services.Settings.GetGrafikRowColorSettingsAsync(cancellationToken);
        var useAlternating = rowColors.Mode == GrafikRowColorMode.Alternating;
        var altBrushA = ParseBrush(rowColors.ColorA, Color.FromRgb(0xFF, 0xFF, 0xFF));
        var altBrushB = ParseBrush(rowColors.ColorB, Color.FromRgb(0xD9, 0xE2, 0xF3));

        var daysInMonth = DateTime.DaysInMonth(rok, miesiac);
        var rows = new List<GrafikRowViewModel>();

        for (int i = 0; i < _funkcjonariusze!.Count; i++)
        {
            var f = _funkcjonariusze[i];
            var rowBackground = useAlternating
                ? (i % 2 == 0 ? altBrushA : altBrushB)
                : GetRoleBrush(f);
            var row = new GrafikRowViewModel
            {
                FunkcjonariuszId = f.Id,
                Numer = i + 1,
                ImieNazwisko = f.PelneImieNazwisko,
                Stanowisko = f.Stanowisko,
                KluczRoli = RoleClassifier.DetermineRole(f),
                IsNurek = RoleClassifier.IsNurek(f),
                RowBackground = rowBackground,
                RowForeground = GetForegroundForBackground(rowBackground),
                NameBorderBrush = GetNurekBorderBrush(f),
                UwagaMiesieczna = uwagiLookup.TryGetValue(f.Id, out var uwaga) ? uwaga : string.Empty
            };

            for (var day = 1; day <= daysInMonth; day++)
            {
                if (wpisyLookup.TryGetValue((f.Id, day), out var wpis))
                    row.SetCell(
                        day,
                        wpis.TypWpisu,
                        fromUrlopPlan: wpis.IsAuto && GrafikWpisTypy.JestUrlopem(wpis.TypWpisu));
            }

            rows.Add(row);
        }

        // Wiersz sumaryczny: Wolne miejsca + definicje z ustawień zmiany
        var summaryRow = new GrafikRowViewModel
        {
            IsSummaryRow = true,
            SummaryLineCount = 1 + _zliczanie.Count,
            ImieNazwisko = GrafikZliczanieEvaluator.FormatEtykiety(_zliczanie),
            RowBackground = UrlopPlanPalette.SurfaceVariantBrush,
            RowForeground = UrlopPlanPalette.ForegroundBrush
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
            RowForeground = UrlopPlanPalette.ForegroundBrush
        };

        foreach (var notatka in notatki)
        {
            if (!string.IsNullOrWhiteSpace(notatka.Tresc))
                notesRow.SetCell(notatka.Dzien, notatka.Tresc);
        }

        // Notatki kalendarza DCA dla tej zmiany — niebieska ikona obok zielonej „N”.
        var kalendarzWpisy = await services.Kalendarz.GetMonthAsync(
            rok, miesiac, viewerShiftId: ZmianaId, cancellationToken: cancellationToken);
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

    public async Task SetUwagaMiesiecznaAsync(
        int funkcjonariuszId,
        int rok,
        int miesiac,
        string tresc,
        CancellationToken cancellationToken = default)
    {
        var trimmed = tresc?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(trimmed))
        {
            await services.Grafik.ClearUwagaMiesiecznaAsync(
                funkcjonariuszId, ZmianaId, rok, miesiac, cancellationToken);
            return;
        }

        await services.Grafik.SetUwagaMiesiecznaAsync(
            funkcjonariuszId, ZmianaId, rok, miesiac, trimmed, cancellationToken);
    }

    public GrafikCellColors GetCellColors()
    {
        var transparent = new SolidColorBrush(Colors.Transparent);
        if (!KoloryLookup.IsAktywny(_koloryMap, RoleKeys.WolnaSluzba))
        {
            return new GrafikCellColors
            {
                DyzurTlo = transparent,
                WsTlo = transparent,
                DelTlo = TryParseOptionalFillBrush(RoleKeys.Delegacja),
                STlo = TryParseOptionalFillBrush(RoleKeys.Szkolenie)
            };
        }

        var wsHex = BOBER.Core.Oznaczenia.OznaczeniaLookup.KolorWsHex();
        if (string.IsNullOrWhiteSpace(wsHex) || RoleKeys.IsBrakWypelnienia(wsHex))
            wsHex = GetKolorHex(RoleKeys.WolnaSluzba, RoleKeys.DomyslneKoloryWpisow);

        var nieobecnosc = ParseBrush(wsHex!);
        return new GrafikCellColors
        {
            DyzurTlo = nieobecnosc,
            WsTlo = nieobecnosc,
            DelTlo = TryParseOptionalFillBrush(RoleKeys.Delegacja),
            STlo = TryParseOptionalFillBrush(RoleKeys.Szkolenie)
        };
    }

    private SolidColorBrush? TryParseOptionalFillBrush(string klucz)
    {
        if (!KoloryLookup.IsAktywny(_koloryMap, klucz))
            return null;
        var hex = GetKolorHex(klucz, RoleKeys.DomyslneKoloryWpisow);
        if (RoleKeys.IsBrakWypelnienia(hex))
            return null;

        try
        {
            var color = (Color)ColorConverter.ConvertFromString(hex)!;
            return new SolidColorBrush(color);
        }
        catch
        {
            return null;
        }
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
        string previousTyp,
        bool isAuto = false,
        CancellationToken cancellationToken = default)
    {
        await ApplyChangesAsync(
            [new GrafikCellChange(
                funkcjonariuszId,
                rok,
                miesiac,
                dzien,
                previousTyp,
                typWpisu,
                isAuto)],
            cancellationToken);
    }

    public async Task ClearWpisAsync(
        int funkcjonariuszId,
        int rok,
        int miesiac,
        int dzien,
        string previousTyp,
        CancellationToken cancellationToken = default)
    {
        await ApplyChangesAsync(
            [new GrafikCellChange(funkcjonariuszId, rok, miesiac, dzien, previousTyp, string.Empty)],
            cancellationToken);
    }

    public async Task ApplyChangesAsync(
        IReadOnlyList<GrafikCellChange> changes,
        CancellationToken cancellationToken = default)
    {
        var upserts = changes
            .Where(change => !string.IsNullOrEmpty(change.NewTyp))
            .Select(change => new GrafikWpis
            {
                FunkcjonariuszId = change.FunkcjonariuszId,
                ZmianaId = ZmianaId,
                Rok = change.Rok,
                Miesiac = change.Miesiac,
                Dzien = change.Dzien,
                TypWpisu = change.NewTyp,
                IsAuto = change.IsAuto
            })
            .ToList();
        var deletes = changes
            .Where(change => string.IsNullOrEmpty(change.NewTyp))
            .Select(change => new GrafikWpis
            {
                FunkcjonariuszId = change.FunkcjonariuszId,
                ZmianaId = ZmianaId,
                Rok = change.Rok,
                Miesiac = change.Miesiac,
                Dzien = change.Dzien
            })
            .ToList();

        await services.Grafik.ApplyBatchAsync(upserts, deletes, cancellationToken);

        foreach (var change in changes)
        {
            var oldTyp = string.IsNullOrWhiteSpace(change.PreviousTyp) ? "—" : change.PreviousTyp;
            var newTyp = string.IsNullOrWhiteSpace(change.NewTyp) ? "—" : change.NewTyp;
            await TryAuditGrafikAsync(change.FunkcjonariuszId, oldTyp, newTyp);
        }
    }

    private async Task TryAuditGrafikAsync(int funkcjonariuszId, string oldTyp, string newTyp)
    {
        try
        {
            if (string.Equals(oldTyp, newTyp, StringComparison.OrdinalIgnoreCase))
                return;

            var append = BOBER.Core.Audit.GuestChangeAudit.TryAppendAsync;
            if (append is null)
                return;

            var osoba = (_funkcjonariusze ?? []).FirstOrDefault(f => f.Id == funkcjonariuszId);
            var name = osoba?.PelneImieNazwisko ?? $"ID {funkcjonariuszId}";
            await append("Grafik", $"Grafik służb [{name}] {oldTyp} na {newTyp}");
        }
        catch (Exception ex)
        {
            BoberLog.Warning(ex, "Nie udało się zapisać audytu zmiany grafiku.");
        }
    }

    public async Task ExportMonthAsync(
        string filePath,
        int rok,
        int miesiac,
        CancellationToken cancellationToken = default)
    {
        if (_koloryMap is null)
            await LoadAsync(cancellationToken);

        var wpisy = await services.Grafik.GetMonthAsync(ZmianaId, rok, miesiac, cancellationToken);
        var workDays = await GetWorkDaysForMonthAsync(rok, miesiac, cancellationToken);
        var lessColor = await services.Settings.GetLessColorAsync(cancellationToken);
        var exportAlt = await services.Settings.GetGrafikExportAlternatingSettingsAsync(cancellationToken);
        var koloryHex = KoloryHex();
        var wylaczone = KoloryLookup.NieaktywneKlucze(_koloryMap?.Values ?? []);

        // Generowanie pliku Excel jest synchroniczne i długie — poza wątkiem UI okno nie zamarza.
        await Task.Run(() => services.Export.ExportMonth(
            filePath, rok, miesiac,
            _funkcjonariusze ?? [],
            wpisy,
            _stanZmiany,
            _stanMinimalny,
            koloryHex,
            workDays,
            lessColor,
            exportAlt.Enabled,
            exportAlt.ColorA,
            exportAlt.ColorB,
            NazwaZmiany,
            ZmianaId,
            wylaczone,
            _zliczanie), cancellationToken);
    }

    public async Task ExportYearAsync(
        string filePath,
        int rok,
        CancellationToken cancellationToken = default)
    {
        if (_koloryMap is null || _funkcjonariusze is null)
            await LoadAsync(cancellationToken);

        var wpisyByMonth = new Dictionary<int, IReadOnlyList<GrafikWpis>>();
        var workDaysByMonth = new Dictionary<int, IReadOnlyCollection<int>>();
        var yearEntries = await services.Grafik.GetYearAsync(ZmianaId, rok, cancellationToken);
        var entriesByMonth = yearEntries
            .GroupBy(entry => entry.Miesiac)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<GrafikWpis>)group.ToList());

        for (var miesiac = 1; miesiac <= 12; miesiac++)
        {
            wpisyByMonth[miesiac] = entriesByMonth.GetValueOrDefault(miesiac) ?? [];
            workDaysByMonth[miesiac] = await GetWorkDaysForMonthAsync(rok, miesiac, cancellationToken);
        }

        var lessColor = await services.Settings.GetLessColorAsync(cancellationToken);
        var exportAlt = await services.Settings.GetGrafikExportAlternatingSettingsAsync(cancellationToken);
        var koloryHex = KoloryHex();
        var wylaczone = KoloryLookup.NieaktywneKlucze(_koloryMap?.Values ?? []);

        // Generowanie pliku Excel jest synchroniczne i długie — poza wątkiem UI okno nie zamarza.
        await Task.Run(() => services.Export.ExportYear(
            filePath, rok,
            _funkcjonariusze ?? [],
            wpisyByMonth,
            workDaysByMonth,
            _stanZmiany,
            _stanMinimalny,
            koloryHex,
            lessColor,
            exportAlt.Enabled,
            exportAlt.ColorA,
            exportAlt.ColorB,
            NazwaZmiany,
            ZmianaId,
            wylaczone,
            _zliczanie), cancellationToken);
    }

    public Task<string> GetExportPathGrafikSluzbAsync(CancellationToken cancellationToken = default) =>
        services.Settings.GetExportPathGrafikSluzbAsync(cancellationToken);

    public Task<GrafikNurkowySyncResult> GenerateGrafikNurkowyAsync(
        int rok,
        int miesiac,
        CancellationToken cancellationToken = default) =>
        services.GrafikNurkowy.GenerateOrUpdateAsync(ZmianaId, rok, miesiac, cancellationToken);

    public Task<bool> IsGrafikNurkowyZatwierdzonyAsync(
        int rok,
        int miesiac,
        CancellationToken cancellationToken = default) =>
        services.GrafikNurkowy.IsZatwierdzonyAsync(rok, miesiac, cancellationToken);

    private SolidColorBrush GetRoleBrush(Funkcjonariusz f)
    {
        var role = RoleClassifier.DetermineBackgroundRole(f);
        if (!KoloryLookup.IsAktywny(_koloryMap, role))
            return new SolidColorBrush(Colors.White);

        if (_koloryMap is not null && _koloryMap.TryGetValue(role, out var kolor)
            && !string.IsNullOrWhiteSpace(kolor.KolorHex))
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(kolor.KolorHex);
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

    private static SolidColorBrush GetForegroundForBackground(SolidColorBrush background)
    {
        return IsLightColor(background.Color)
            ? new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E))
            : new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0));
    }

    private Brush GetNurekBorderBrush(Funkcjonariusz f)
    {
        if (!RoleClassifier.IsNurek(f) || !KoloryLookup.IsAktywny(_koloryMap, RoleKeys.NurekCzcionka))
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
        if (!_workDaysByYear.TryGetValue(rok, out var months))
        {
            var allWorkDays = await services.Calendar.GetWorkDaysAsync(ZmianaId, rok, cancellationToken);
            months = allWorkDays
                .GroupBy(date => date.Month)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(date => date.Day).ToHashSet());
            _workDaysByYear[rok] = months;
        }

        return months.TryGetValue(miesiac, out var days)
            ? days.ToHashSet()
            : [];
    }

    public int GetStanMinimalny() => _stanMinimalny;
    public int GetStanZmiany() => _stanZmiany;

    private void UpdateSummaryForDay(
        GrafikRowViewModel summaryRow,
        IEnumerable<GrafikRowViewModel> allRows,
        int dzien)
    {
        summaryRow.SetCell(dzien, ComputeSummaryText(allRows, dzien));
    }

    private string ComputeSummaryText(IEnumerable<GrafikRowViewModel> allRows, int dzien)
    {
        var workerRows = allRows
            .Where(r => !r.IsSummaryRow && !r.IsNotesRow && r.FunkcjonariuszId.HasValue)
            .ToList();

        var nieobecni = workerRows.Count(r => GrafikWpisTypy.JestNieobecnoscia(r.GetCell(dzien)));
        var stanZmiany = workerRows.Count > 0 ? workerRows.Count : _stanZmiany;
        var stan = stanZmiany - _stanMinimalny - nieobecni;

        var funcLookup = (_funkcjonariusze ?? []).ToDictionary(f => f.Id);
        var obecni = workerRows
            .Where(r => !GrafikWpisTypy.JestNieobecnoscia(r.GetCell(dzien)))
            .Select(r => r.FunkcjonariuszId!.Value)
            .Where(funcLookup.ContainsKey)
            .Select(id => funcLookup[id])
            .ToList();

        var linie = new List<string> { stan.ToString() };
        foreach (var wiersz in _zliczanie.OrderBy(w => w.Kolejnosc))
            linie.Add(GrafikZliczanieEvaluator.FormatWartosc(obecni, wiersz));

        return string.Join('\n', linie);
    }

    private IReadOnlyDictionary<string, string> KoloryHex() =>
        _koloryMap is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : KoloryLookup.ToHexDictionary(_koloryMap.Values);

    private string GetKolorHex(string klucz, IReadOnlyDictionary<string, string> domyslne)
    {
        if (_koloryMap is not null && _koloryMap.TryGetValue(klucz, out var kolor))
        {
            var hex = kolor.KolorHex;
            if (RoleKeys.KoloryOpcjonalneWypelnienia.Contains(klucz) && RoleKeys.IsBrakWypelnienia(hex))
                return RoleKeys.BrakWypelnienia;
            if (!string.IsNullOrWhiteSpace(hex))
                return hex;
        }

        return domyslne.TryGetValue(klucz, out var defaultHex)
            ? defaultHex
            : RoleKeys.GetDefaultKolorHex(klucz);
    }

    private SolidColorBrush ParseBrush(string hex) =>
        ParseBrush(hex, Color.FromRgb(0x6A, 0x5C, 0x00));

    private static SolidColorBrush ParseBrush(string hex, Color fallback)
    {
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(hex)!;
            return new SolidColorBrush(color);
        }
        catch
        {
            return new SolidColorBrush(fallback);
        }
    }
}
