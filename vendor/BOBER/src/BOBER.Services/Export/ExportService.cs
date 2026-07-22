using ClosedXML.Excel;
using BOBER.Core.Constants;
using BOBER.Core.Models;
using BOBER.Core.Rules;
using System.Linq;

namespace BOBER.Services.Export;

public sealed class ExportService
{
    /// <summary>Eksportuje grafik jednego miesiąca do pliku Excel.</summary>
    public void ExportMonth(
        string filePath,
        int rok,
        int miesiac,
        IReadOnlyList<Funkcjonariusz> funkcjonariusze,
        IReadOnlyList<GrafikWpis> wpisy,
        int stanZmiany,
        int stanMinimalny,
        IReadOnlyDictionary<string, string> kolory,
        IReadOnlyCollection<int>? workDays = null)
    {
        using var workbook = new XLWorkbook();
        AddMonthWorksheet(workbook, rok, miesiac, funkcjonariusze, wpisy, stanZmiany, stanMinimalny, kolory, workDays);
        workbook.SaveAs(filePath);
    }

    /// <summary>Eksportuje wszystkie miesiące roku do jednego pliku Excel (osobny arkusz na miesiąc).</summary>
    public void ExportYear(
        string filePath,
        int rok,
        IReadOnlyList<Funkcjonariusz> funkcjonariusze,
        IReadOnlyDictionary<int, IReadOnlyList<GrafikWpis>> wpisyByMonth,
        IReadOnlyDictionary<int, IReadOnlyCollection<int>> workDaysByMonth,
        int stanZmiany,
        int stanMinimalny,
        IReadOnlyDictionary<string, string> kolory)
    {
        using var workbook = new XLWorkbook();
        for (var miesiac = 1; miesiac <= 12; miesiac++)
        {
            wpisyByMonth.TryGetValue(miesiac, out var wpisy);
            workDaysByMonth.TryGetValue(miesiac, out var workDays);
            AddMonthWorksheet(
                workbook, rok, miesiac,
                funkcjonariusze,
                wpisy ?? [],
                stanZmiany,
                stanMinimalny,
                kolory,
                workDays);
        }

        workbook.SaveAs(filePath);
    }

    private static void AddMonthWorksheet(
        XLWorkbook workbook,
        int rok,
        int miesiac,
        IReadOnlyList<Funkcjonariusz> funkcjonariusze,
        IReadOnlyList<GrafikWpis> wpisy,
        int stanZmiany,
        int stanMinimalny,
        IReadOnlyDictionary<string, string> kolory,
        IReadOnlyCollection<int>? workDays)
    {
        var ws = workbook.Worksheets.Add(GetMonthName(miesiac));

        var nieobecnoscBg = ToXl(ResolveHex(kolory, RoleKeys.WolnaSluzba, RoleKeys.DomyslneKoloryWpisow));
        var appText = ToXl(AppColors.ForegroundHex);
        var bandBg = ToXl(ResolveHex(kolory, RoleKeys.EksportNaglowekStopkaTlo, RoleKeys.DomyslneKoloryEksportu));
        var bandFg = ToXl(ResolveHex(kolory, RoleKeys.EksportNaglowekStopkaCzcionka, RoleKeys.DomyslneKoloryEksportu));

        var daysInMonth = DateTime.DaysInMonth(rok, miesiac);
        var workDaysList = (workDays is { Count: > 0 }
            ? workDays.OrderBy(d => d)
            : Enumerable.Range(1, daysInMonth)).ToList();

        // Kolumna 1: Lp., kolumna 2: Imię i Nazwisko, kolumny 3+: dni
        const int colLp = 1;
        const int colName = 2;
        const int firstDayCol = 3;
        var lastCol = workDaysList.Count + 2;

        var gridLineColor = XLColor.FromHtml("#505050");

        // Nagłówek "Lp."
        var lpHeader = ws.Range(1, colLp, 2, colLp);
        lpHeader.Merge();
        lpHeader.Value = "Lp.";
        StyleBandCell(lpHeader.FirstCell(), bandBg, bandFg);
        lpHeader.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        lpHeader.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

        // Nagłówek "Imię i Nazwisko"
        var nameHeader = ws.Range(1, colName, 2, colName);
        nameHeader.Merge();
        nameHeader.Value = "Imię i Nazwisko";
        StyleBandCell(nameHeader.FirstCell(), bandBg, bandFg);
        nameHeader.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        nameHeader.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

        for (int i = 0; i < workDaysList.Count; i++)
        {
            var day = workDaysList[i];
            var date = new DateTime(rok, miesiac, day);
            var col = i + firstDayCol;

            ws.Cell(1, col).Value = day;
            ws.Cell(2, col).Value = date.ToString("ddd");
            StyleBandCell(ws.Cell(1, col), bandBg, bandFg);
            StyleBandCell(ws.Cell(2, col), bandBg, bandFg);

            ws.Column(col).Width = 9;
        }

        ws.Row(1).Height = 22;
        ws.Row(2).Height = 18;
        ws.Column(colLp).Width = 7;
        ws.Column(colName).Width = 30;
        ws.Style.Font.FontSize = 14;

        var wpisyLookup = wpisy
            .GroupBy(w => (w.FunkcjonariuszId, w.Dzien))
            .ToDictionary(g => g.Key, g => g.Last().TypWpisu);

        var funcLookup = funkcjonariusze.ToDictionary(f => f.Id);

        for (int i = 0; i < funkcjonariusze.Count; i++)
        {
            var f = funkcjonariusze[i];
            var row = i + 3;
            var role = RoleClassifier.DetermineBackgroundRole(f);
            var rowBgHex = ResolveHex(kolory, role, RoleKeys.DomyslneKolory);
            var rowBg = ToXl(rowBgHex);
            var nameText = RoleClassifier.IsNurek(f)
                ? ToXl(ResolveHex(kolory, RoleKeys.NurekCzcionka, RoleKeys.DomyslneKoloryWpisow))
                : ToXl(AppColors.ContrastTextHex(rowBgHex));

            // Kolumna Lp.
            ws.Cell(row, colLp).Value = i + 1;
            ws.Cell(row, colLp).Style.Fill.BackgroundColor = rowBg;
            ws.Cell(row, colLp).Style.Font.FontColor = nameText;
            ws.Cell(row, colLp).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Kolumna Imię i Nazwisko
            ws.Cell(row, colName).Value = f.PelneImieNazwisko;
            ws.Cell(row, colName).Style.Fill.BackgroundColor = rowBg;
            ws.Cell(row, colName).Style.Font.FontColor = nameText;

            for (int d = 0; d < workDaysList.Count; d++)
            {
                var day = workDaysList[d];
                var cell = ws.Cell(row, d + firstDayCol);
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Fill.BackgroundColor = rowBg;
                cell.Style.Font.FontColor = appText;

                if (!wpisyLookup.TryGetValue((f.Id, day), out var wpis)) continue;

                var bazowy = GrafikWpisTypy.BazowyKod(wpis);

                if (bazowy.Equals(GrafikWpisTypy.WolnaSluzba, StringComparison.OrdinalIgnoreCase))
                {
                    cell.Style.Fill.BackgroundColor = nieobecnoscBg;
                    cell.Value = GrafikWpisTypy.TekstWyswietlany(wpis);
                    continue;
                }

                if (bazowy == GrafikWpisTypy.PotrzebujeWolne)
                {
                    cell.Value = GrafikWpisTypy.PotrzebujeWolne;
                    continue;
                }

                cell.Value = GrafikWpisTypy.TekstWyswietlany(wpis);
                cell.Style.Fill.BackgroundColor = bazowy switch
                {
                    GrafikWpisTypy.Dyzur => nieobecnoscBg,
                    GrafikWpisTypy.Delegacja => nieobecnoscBg,
                    _ => rowBg
                };
                cell.Style.Font.FontColor = appText;
            }
        }

        int sumBase = funkcjonariusze.Count + 3;
        var sumLabels = new[] { "Wolne miejsca", "Dowódcy", "Nurkowie", "Kierowcy", "Poziom A/AB" };

        for (int s = 0; s < sumLabels.Length; s++)
        {
            var row = sumBase + s;
            for (int col = 1; col <= lastCol; col++)
                StyleBandCell(ws.Cell(row, col), bandBg, bandFg);

            ws.Cell(row, colName).Value = sumLabels[s];
            ws.Cell(row, colName).Style.Font.Bold = true;
        }

        for (int i = 0; i < workDaysList.Count; i++)
        {
            var day = workDaysList[i];
            var col = i + firstDayCol;

            var nieobecniIds = wpisyLookup
                .Where(kv => kv.Key.Dzien == day && GrafikWpisTypy.JestNieobecnoscia(kv.Value))
                .Select(kv => kv.Key.FunkcjonariuszId)
                .ToHashSet();

            var obecniIds = funkcjonariusze
                .Where(f => !nieobecniIds.Contains(f.Id))
                .Select(f => f.Id)
                .ToHashSet();

            var stanEfektywny = funkcjonariusze.Count > 0 ? funkcjonariusze.Count : stanZmiany;
            int wolne = stanEfektywny - stanMinimalny - nieobecniIds.Count;
            int kierowcy = obecniIds.Count(id => funcLookup.TryGetValue(id, out var f) && f.MaUprawnieniaKierowca);
            int nurkowie = obecniIds.Count(id => funcLookup.TryGetValue(id, out var f) && RoleClassifier.IsNurek(f));
            int dowodcy = obecniIds.Count(id => funcLookup.TryGetValue(id, out var f) && RoleClassifier.IsDowodca(f));
            var obecni = obecniIds
                .Where(id => funcLookup.ContainsKey(id))
                .Select(id => funcLookup[id]);
            var poziom = PoziomGotowosciNurkowejRules.Format(PoziomGotowosciNurkowejRules.Ocena(obecni));

            SetSummaryCell(ws.Cell(sumBase, col), wolne, bandBg, bandFg);
            SetSummaryCell(ws.Cell(sumBase + 1, col), dowodcy, bandBg, bandFg);
            SetSummaryCell(ws.Cell(sumBase + 2, col), nurkowie, bandBg, bandFg);
            SetSummaryCell(ws.Cell(sumBase + 3, col), kierowcy, bandBg, bandFg);
            SetSummaryCell(ws.Cell(sumBase + 4, col), poziom, bandBg, bandFg);
        }

        int lastRow = sumBase + sumLabels.Length - 1;
        var dataRange = ws.Range(1, 1, lastRow, lastCol);
        dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        dataRange.Style.Border.InsideBorderColor = gridLineColor;
        dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
        dataRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#707070");

        ws.SheetView.FreezeRows(2);
        ws.SheetView.FreezeColumns(2);

        ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        ws.PageSetup.PaperSize = XLPaperSize.A4Paper;
    }

    private static XLColor ToXl(string hex) => XLColor.FromHtml(hex);

    private static string ResolveHex(
        IReadOnlyDictionary<string, string> kolory,
        string key,
        IReadOnlyDictionary<string, string> defaults)
    {
        if (kolory.TryGetValue(key, out var hex) && !string.IsNullOrWhiteSpace(hex))
            return hex;
        return defaults.TryGetValue(key, out var fallback) ? fallback : "#FFFFFF";
    }

    private static void StyleBandCell(IXLCell cell, XLColor bg, XLColor fg)
    {
        cell.Style.Fill.BackgroundColor = bg;
        cell.Style.Font.FontColor = fg;
        cell.Style.Font.Bold = true;
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
    }

    private static void SetSummaryCell(IXLCell cell, int value, XLColor bg, XLColor fg)
    {
        cell.Value = value;
        StyleSummaryValue(cell, bg, fg);
    }

    private static void SetSummaryCell(IXLCell cell, string value, XLColor bg, XLColor fg)
    {
        cell.Value = value;
        StyleSummaryValue(cell, bg, fg);
    }

    private static void StyleSummaryValue(IXLCell cell, XLColor bg, XLColor fg)
    {
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        cell.Style.Fill.BackgroundColor = bg;
        cell.Style.Font.FontColor = fg;
        cell.Style.Font.Bold = true;
    }

    private static string GetMonthName(int month) => month switch
    {
        1 => "Styczeń",
        2 => "Luty",
        3 => "Marzec",
        4 => "Kwiecień",
        5 => "Maj",
        6 => "Czerwiec",
        7 => "Lipiec",
        8 => "Sierpień",
        9 => "Wrzesień",
        10 => "Październik",
        11 => "Listopad",
        12 => "Grudzień",
        _ => month.ToString()
    };
}
