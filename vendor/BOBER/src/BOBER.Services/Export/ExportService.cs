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
        IReadOnlyCollection<int>? workDays = null,
        bool lessColor = true,
        bool alternatingRows = false,
        string? alternatingColorA = null,
        string? alternatingColorB = null,
        string? nazwaZmiany = null,
        int zmianaId = 1)
    {
        using var workbook = new XLWorkbook();
        AddMonthWorksheet(
            workbook, rok, miesiac, funkcjonariusze, wpisy, stanZmiany, stanMinimalny, kolory, workDays,
            lessColor, alternatingRows, alternatingColorA, alternatingColorB, nazwaZmiany, zmianaId);
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
        IReadOnlyDictionary<string, string> kolory,
        bool lessColor = true,
        bool alternatingRows = false,
        string? alternatingColorA = null,
        string? alternatingColorB = null,
        string? nazwaZmiany = null,
        int zmianaId = 1)
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
                workDays,
                lessColor,
                alternatingRows,
                alternatingColorA,
                alternatingColorB,
                nazwaZmiany,
                zmianaId);
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
        IReadOnlyCollection<int>? workDays,
        bool lessColor,
        bool alternatingRows,
        string? alternatingColorA,
        string? alternatingColorB,
        string? nazwaZmiany,
        int zmianaId)
    {
        var ws = workbook.Worksheets.Add(GetMonthName(miesiac));
        var altA = ResolveAlternatingHex(alternatingColorA, GrafikRowColorSettings.DefaultColorA);
        var altB = ResolveAlternatingHex(alternatingColorB, GrafikRowColorSettings.DefaultColorB);

        var nieobecnoscBg = ToXl(ResolveHex(kolory, RoleKeys.WolnaSluzba, RoleKeys.DomyslneKoloryWpisow));
        var appText = ToXl(AppColors.ForegroundHex);
        var bandBg = ToXl(ResolveHex(kolory, RoleKeys.EksportNaglowekStopkaTlo, RoleKeys.DomyslneKoloryEksportu));
        var lessColorText = ToXl("#000000");
        var bandFg = lessColor
            ? lessColorText
            : ToXl(ResolveHex(kolory, RoleKeys.EksportNaglowekStopkaCzcionka, RoleKeys.DomyslneKoloryEksportu));

        var daysInMonth = DateTime.DaysInMonth(rok, miesiac);
        var workDaysList = (workDays is { Count: > 0 }
            ? workDays.OrderBy(d => d)
            : Enumerable.Range(1, daysInMonth)).ToList();

        // Kolumna 1: oznaczenia D/N/K, 2: Imię i Nazwisko, 3+: dni
        const int colMarks = 1;
        const int colName = 2;
        const int firstDayCol = 3;
        var lastCol = workDaysList.Count + 2;

        var gridLineColor = XLColor.FromHtml("#505050");

        if (lessColor)
            ws.Style.Font.FontColor = lessColorText;

        var titleRange = ws.Range(TitleRow, colMarks, TitleRow, lastCol);
        titleRange.Merge();
        titleRange.Value = FormatSheetTitle(nazwaZmiany, zmianaId, miesiac, rok);
        StyleBandCell(titleRange.FirstCell(), bandBg, bandFg);
        titleRange.Style.Font.FontSize = TitleFontSize;
        titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Row(TitleRow).Height = TitleRowHeight;

        // Wąska kolumna oznaczeń ról (D/N/K) — pusty nagłówek
        var marksHeader = ws.Range(HeaderRow1, colMarks, HeaderRow2, colMarks);
        marksHeader.Merge();
        StyleBandCell(marksHeader.FirstCell(), bandBg, bandFg);

        // Nagłówek "Imię i Nazwisko"
        var nameHeader = ws.Range(HeaderRow1, colName, HeaderRow2, colName);
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

            ws.Cell(HeaderRow1, col).Value = day;
            ws.Cell(HeaderRow2, col).Value = date.ToString("ddd");
            StyleBandCell(ws.Cell(HeaderRow1, col), bandBg, bandFg);
            StyleBandCell(ws.Cell(HeaderRow2, col), bandBg, bandFg);
        }

        ws.Row(HeaderRow1).Height = 22;
        ws.Row(HeaderRow2).Height = 18;
        ws.Column(colMarks).Width = MarksColumnWidth;
        ws.Style.Font.FontSize = 14;

        var wpisyLookup = wpisy
            .GroupBy(w => (w.FunkcjonariuszId, w.Dzien))
            .ToDictionary(g => g.Key, g => g.Last());

        var funcLookup = funkcjonariusze.ToDictionary(f => f.Id);

        var lessColorRowBg = ToXl("#FFFFFF");

        for (int i = 0; i < funkcjonariusze.Count; i++)
        {
            var f = funkcjonariusze[i];
            var row = i + FirstDataRow;
            string rowBgHex;
            XLColor rowBg;
            XLColor nameText;

            if (alternatingRows)
            {
                rowBgHex = i % 2 == 0 ? altA : altB;
                rowBg = ToXl(rowBgHex);
                nameText = lessColor
                    ? lessColorText
                    : ToXl(AppColors.ContrastTextHex(rowBgHex));
            }
            else if (lessColor)
            {
                rowBgHex = "#FFFFFF";
                rowBg = lessColorRowBg;
                nameText = lessColorText;
            }
            else
            {
                var role = RoleClassifier.DetermineBackgroundRole(f);
                rowBgHex = ResolveHex(kolory, role, RoleKeys.DomyslneKolory);
                rowBg = ToXl(rowBgHex);
                nameText = RoleClassifier.IsNurek(f)
                    ? ToXl(ResolveHex(kolory, RoleKeys.NurekCzcionka, RoleKeys.DomyslneKoloryWpisow))
                    : ToXl(AppColors.ContrastTextHex(rowBgHex));
            }

            // Kolumna oznaczeń D/N/K
            var marksCell = ws.Cell(row, colMarks);
            var marks = RoleClassifier.FormatExportRoleMarks(f);
            if (marks.Length > 0)
                marksCell.Value = marks;
            marksCell.Style.Fill.BackgroundColor = rowBg;
            marksCell.Style.Font.FontColor = nameText;
            marksCell.Style.Font.FontSize = RoleMarkFontSize;
            marksCell.Style.Font.Bold = true;
            marksCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            marksCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

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
                cell.Style.Font.FontColor = lessColor ? lessColorText : appText;

                if (!wpisyLookup.TryGetValue((f.Id, day), out var wpis)) continue;

                var typ = wpis.TypWpisu;
                var bazowy = GrafikWpisTypy.BazowyKod(typ);

                // IsAuto = urlop z planu → Uₚ; ręczny → U
                var fromUrlopPlan = wpis.IsAuto && GrafikWpisTypy.JestUrlopem(typ);

                if (bazowy.Equals(GrafikWpisTypy.WolnaSluzba, StringComparison.OrdinalIgnoreCase)
                    || bazowy.Equals(GrafikWpisTypy.UrlopZWolnaSluzba, StringComparison.OrdinalIgnoreCase)
                    || bazowy.Equals(GrafikWpisTypy.Dyzur, StringComparison.OrdinalIgnoreCase))
                {
                    cell.Style.Fill.BackgroundColor = nieobecnoscBg;
                    cell.Value = GrafikWpisTypy.TekstWyswietlany(typ, fromUrlopPlan);
                    if (lessColor)
                        cell.Style.Font.FontColor = lessColorText;
                    ApplyOddajeStrikethrough(cell, typ);
                    continue;
                }

                if (bazowy == GrafikWpisTypy.PotrzebujeWolne)
                {
                    cell.Value = GrafikWpisTypy.PotrzebujeWolne;
                    continue;
                }

                cell.Value = GrafikWpisTypy.TekstWyswietlany(typ, fromUrlopPlan);
                // LessColor: tylko WS/UWS/D żółte (powyżej). Del/S — własny kolor lub żółte tylko przy zachowanym tle WS.
                cell.Style.Fill.BackgroundColor = lessColor
                    ? rowBg
                    : ResolveOptionalWpisBg(typ, bazowy, kolory, rowBg, nieobecnoscBg);
                cell.Style.Font.FontColor = lessColor ? lessColorText : appText;
                ApplyOddajeStrikethrough(cell, typ);
            }
        }

        int sumBase = funkcjonariusze.Count + FirstDataRow;
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
                .Where(kv => kv.Key.Dzien == day && GrafikWpisTypy.JestNieobecnoscia(kv.Value.TypWpisu))
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

        // Szerokość nazwiska wg treści, z limitem tak by dni zmieściły się na 1 stronie A4 (poziom).
        var nameTexts = funkcjonariusze
            .Select(f => f.PelneImieNazwisko)
            .Append("Imię i Nazwisko")
            .Concat(sumLabels);
        ApplyNameAndDayColumnWidths(ws, colName, firstDayCol, workDaysList.Count, nameTexts);

        var dataRange = ws.Range(1, 1, lastRow, lastCol);
        dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        dataRange.Style.Border.InsideBorderColor = gridLineColor;
        dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
        dataRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#707070");

        ws.SheetView.FreezeRows(HeaderRow2);
        ws.SheetView.FreezeColumns(2);

        ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        ws.PageSetup.PaperSize = XLPaperSize.A4Paper;
        ws.PageSetup.SetRowsToRepeatAtTop(TitleRow, HeaderRow2);
        ws.PageSetup.Margins.Left = 0.25;
        ws.PageSetup.Margins.Right = 0.25;
        ws.PageSetup.Margins.Top = 0.4;
        ws.PageSetup.Margins.Bottom = 0.7;
        ws.PageSetup.Margins.Footer = 0.25;
        ws.PageSetup.FitToPages(1, 0);
        ws.PageSetup.ScaleHFWithDocument = false;
        ws.PageSetup.AlignHFWithMargins = true;
        AddPageFooterLegend(ws);
    }

    private static readonly string LegendLine1 =
        "Legenda: puste=praca | D=Dyżur (żółte) | żółte=WS | U=Urlop | Uₚ=z planu | Uᵣ=rodz. | U na żółtym=U z WS | Del=Delegacja | S=Szkolenie | C=Chory";

    private static readonly string LegendLine2 =
        "?=potrzebuje wolne | •=chętna oddać | przekreśl./—=Oddaje | D=Dowódca | N=Nurek | K=Kierowca";

    private static void AddPageFooterLegend(IXLWorksheet ws)
    {
        // Excel: cały footer razem z kodami &L/&C max 255 znaków — bez SetFontSize/Color.
        ws.PageSetup.Footer.Left.AddText(LegendLine1 + "\n" + LegendLine2, XLHFOccurrence.AllPages);
    }

    private const int TitleRow = 1;
    private const int HeaderRow1 = 2;
    private const int HeaderRow2 = 3;
    private const int FirstDataRow = 4;
    private const double TitleFontSize = 16;
    private const double TitleRowHeight = 24;
    private const double RoleMarkFontSize = 8;
    private const double MarksColumnWidth = 4.5;
    private const double PreferredDayColumnWidth = 9;
    private const double MinDayColumnWidth = 5.5;
    private const double MinNameColumnWidth = 12;
    private const double MaxNameColumnWidth = 28;
    /// <summary>Przybliżona suma szerokości kolumn Excel na A4 w poziomie (marginesy 0,25").</summary>
    private const double A4LandscapeUsableWidth = 132;

    /// <summary>
    /// Dobiera szerokość nazwiska do najdłuższego tekstu; w razie braku miejsca
    /// najpierw zwęża kolumny dni (do minimum), potem ewentualnie nazwisko —
    /// żeby ostatnia kolumna nie wychodziła na drugą stronę.
    /// </summary>
    private static (double NameWidth, double DayWidth) ResolveNameAndDayColumnWidths(
        int dayCount,
        IEnumerable<string> nameColumnTexts)
    {
        var needed = nameColumnTexts
            .Select(EstimateTextColumnWidth)
            .DefaultIfEmpty(MinNameColumnWidth)
            .Max();
        needed = Math.Clamp(needed, MinNameColumnWidth, MaxNameColumnWidth);

        if (dayCount <= 0)
            return (needed, PreferredDayColumnWidth);

        var remainingForDays = A4LandscapeUsableWidth - MarksColumnWidth - needed;
        var preferredDaysTotal = dayCount * PreferredDayColumnWidth;

        if (preferredDaysTotal <= remainingForDays)
            return (needed, PreferredDayColumnWidth);

        var dayWidth = remainingForDays / dayCount;
        if (dayWidth >= MinDayColumnWidth)
            return (needed, dayWidth);

        dayWidth = MinDayColumnWidth;
        var nameWidth = Math.Max(
            MinNameColumnWidth,
            A4LandscapeUsableWidth - MarksColumnWidth - dayCount * dayWidth);
        return (Math.Min(nameWidth, needed), dayWidth);
    }

    private static double EstimateTextColumnWidth(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return MinNameColumnWidth;

        // Czcionka 14: nieco więcej niż domyślna jednostka Excel (~11 pt).
        return text.Length * 1.15 + 2.0;
    }

    private static void ApplyNameAndDayColumnWidths(
        IXLWorksheet ws,
        int colName,
        int firstDayCol,
        int dayCount,
        IEnumerable<string> nameColumnTexts)
    {
        var (nameWidth, dayWidth) = ResolveNameAndDayColumnWidths(dayCount, nameColumnTexts);
        ws.Column(colName).Width = nameWidth;
        for (var i = 0; i < dayCount; i++)
            ws.Column(firstDayCol + i).Width = dayWidth;
    }

    private static XLColor ToXl(string hex) => XLColor.FromHtml(hex);

    private static string ResolveAlternatingHex(string? hex, string fallback)
    {
        if (string.IsNullOrWhiteSpace(hex))
            return fallback;

        var trimmed = hex.Trim();
        if (!trimmed.StartsWith('#'))
            trimmed = "#" + trimmed;

        return trimmed.Length is 7 or 9 ? trimmed : fallback;
    }

    private static void ApplyOddajeStrikethrough(IXLCell cell, string? typWpisu)
    {
        if (!GrafikWpisTypy.MaOddal(typWpisu))
            return;

        // Przy WS Oddaje w komórce zostaje „—” (bez przekreślenia).
        var bazowy = GrafikWpisTypy.BazowyKod(typWpisu);
        if (bazowy.Equals(GrafikWpisTypy.WolnaSluzba, StringComparison.OrdinalIgnoreCase))
            return;

        cell.Style.Font.Strikethrough = true;
    }

    private static string ResolveHex(
        IReadOnlyDictionary<string, string> kolory,
        string key,
        IReadOnlyDictionary<string, string> defaults)
    {
        if (kolory.TryGetValue(key, out var hex) && !string.IsNullOrWhiteSpace(hex))
            return hex;
        return defaults.TryGetValue(key, out var fallback) ? fallback : "#FFFFFF";
    }

    private static XLColor ResolveOptionalWpisBg(
        string typWpisu,
        string bazowy,
        IReadOnlyDictionary<string, string> kolory,
        XLColor rowBg,
        XLColor wsYellowBg)
    {
        string? klucz = null;
        if (bazowy.Equals(GrafikWpisTypy.Delegacja, StringComparison.OrdinalIgnoreCase))
            klucz = RoleKeys.Delegacja;
        else if (bazowy.Equals(GrafikWpisTypy.Szkolenie, StringComparison.OrdinalIgnoreCase))
            klucz = RoleKeys.Szkolenie;

        if (klucz is null)
            return rowBg;

        var hex = ResolveHex(kolory, klucz, RoleKeys.DomyslneKoloryWpisow);
        // Brak własnego koloru: żółte tylko gdy zachowano tło WS (Del*/S*), inaczej tło wiersza.
        if (RoleKeys.IsBrakWypelnienia(hex))
            return GrafikWpisTypy.MaZachowaneTloWs(typWpisu) ? wsYellowBg : rowBg;

        return ToXl(hex);
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

    private static string FormatSheetTitle(string? nazwaZmiany, int zmianaId, int miesiac, int rok)
    {
        var zmiana = string.IsNullOrWhiteSpace(nazwaZmiany)
            ? $"Zmiana {zmianaId}"
            : nazwaZmiany.Trim();
        return $"{zmiana} — {GetMonthName(miesiac)} {rok}";
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
