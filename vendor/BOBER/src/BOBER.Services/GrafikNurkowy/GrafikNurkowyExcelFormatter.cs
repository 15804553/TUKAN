using ClosedXML.Excel;
using BOBER.Core.Constants;
using BOBER.Core.Models;
using BOBER.Services.Urlop;

namespace BOBER.Services.GrafikNurkowy;

/// <summary>Kolory, scalenia i wyrównania grafiku nurkowego zgodne z plikiem wzorcowym.</summary>
internal static class GrafikNurkowyExcelFormatter
{
    public static void Apply(
        IXLWorksheet ws,
        int rok,
        int miesiac,
        IReadOnlyList<Funkcjonariusz> wszyscyNurkowie)
    {
        var daysInMonth = DateTime.DaysInMonth(rok, miesiac);
        var lastDayCol = GrafikNurkowyConstants.FirstDayCol + daysInMonth - 1;
        var lastDataRow = FindLastDataRow(ws);
        var summaryRow = FindSummaryRow(ws);
        if (summaryRow < 0)
            summaryRow = lastDataRow + 1;

        var nameToZmiana = wszyscyNurkowie
            .GroupBy(f => UrlopNameMatcher.Normalize(UrlopNameMatcher.ToExcelFormat(f.Imie, f.Nazwisko)))
            .ToDictionary(g => g.Key, g => g.First().NumerZmiany, StringComparer.Ordinal);

        StyleTitle(ws, lastDayCol);
        StyleHeaderRow(ws, daysInMonth);
        StyleUnitColumns(ws, lastDataRow, summaryRow);
        StylePersonRows(ws, lastDataRow, daysInMonth, nameToZmiana);
        StyleSummaryRow(ws, summaryRow, daysInMonth);
        ApplyConditionalFormats(ws, lastDataRow, summaryRow, daysInMonth);
        StyleLegend(ws, summaryRow);
        ApplyColumnWidths(ws);
        ApplyRowHeights(ws, lastDataRow, summaryRow);
    }

    /// <summary>
    /// Formatowanie warunkowe jak we wzorcu:
    /// - funkcja „KPP” → czerwona czcionka;
    /// - suma dnia &lt; 2 → czerwone tło (brak deklarowanego poziomu gotowości).
    /// </summary>
    private static void ApplyConditionalFormats(
        IXLWorksheet ws,
        int lastDataRow,
        int summaryRow,
        int daysInMonth)
    {
        ws.ConditionalFormats.RemoveAll();

        if (lastDataRow >= GrafikNurkowyConstants.FirstDataRow)
        {
            var funkcjaRange = ws.Range(
                GrafikNurkowyConstants.FirstDataRow,
                GrafikNurkowyConstants.ColFunkcja,
                lastDataRow,
                GrafikNurkowyConstants.ColFunkcja);

            funkcjaRange.AddConditionalFormat()
                .WhenEquals(GrafikNurkowyConstants.FunkcjaKpp)
                .Font.SetFontColor(XLColor.FromHtml(GrafikNurkowyConstants.ColorWartoscCzcionka));

            funkcjaRange.AddConditionalFormat()
                .WhenEquals(GrafikNurkowyConstants.FunkcjaNurek)
                .Font.SetFontColor(XLColor.Black);

            funkcjaRange.AddConditionalFormat()
                .WhenEquals(GrafikNurkowyConstants.FunkcjaMlodszyNurek)
                .Font.SetFontColor(XLColor.Black);
        }

        var lastDayCol = GrafikNurkowyConstants.FirstDayCol + daysInMonth - 1;
        var summaryDays = ws.Range(
            summaryRow,
            GrafikNurkowyConstants.FirstDayCol,
            summaryRow,
            lastDayCol);

        // Próg jak we wzorcu: wartość &lt; 2 → czerwone tło.
        summaryDays.AddConditionalFormat()
            .WhenLessThan(2)
            .Fill.SetBackgroundColor(XLColor.FromHtml(GrafikNurkowyConstants.ColorBrakGotowosci));
    }

    public static int FindLastDataRow(IXLWorksheet ws)
    {
        var last = ws.LastRowUsed()?.RowNumber() ?? GrafikNurkowyConstants.FirstDataRow - 1;
        for (var row = last; row >= GrafikNurkowyConstants.FirstDataRow; row--)
        {
            var name = ws.Cell(row, GrafikNurkowyConstants.ColImieNazwisko).GetString().Trim();
            if (string.IsNullOrWhiteSpace(name))
                continue;
            if (name.Contains("JRG", StringComparison.OrdinalIgnoreCase))
                continue;
            if (name.StartsWith("-", StringComparison.Ordinal))
                continue;
            return row;
        }

        return GrafikNurkowyConstants.FirstDataRow - 1;
    }

    public static int FindSummaryRow(IXLWorksheet ws)
    {
        var last = ws.LastRowUsed()?.RowNumber() ?? 0;
        for (var row = GrafikNurkowyConstants.FirstDataRow; row <= last; row++)
        {
            var name = ws.Cell(row, GrafikNurkowyConstants.ColImieNazwisko).GetString().Trim();
            if (name.Contains("JRG", StringComparison.OrdinalIgnoreCase))
                return row;
        }

        return -1;
    }

    private static void StyleTitle(IXLWorksheet ws, int lastDayCol)
    {
        var titleCell = ws.Cell(GrafikNurkowyConstants.TitleRow, GrafikNurkowyConstants.ColJednostkaPsp);
        var titleRange = ws.Range(
            GrafikNurkowyConstants.TitleRow,
            GrafikNurkowyConstants.ColJednostkaPsp,
            GrafikNurkowyConstants.TitleRow,
            lastDayCol);

        if (!titleCell.IsMerged())
            titleRange.Merge();

        titleRange.Style.Font.Bold = true;
        titleRange.Style.Font.FontSize = 18;
        titleRange.Style.Font.FontColor = XLColor.Black;
        titleRange.Style.Fill.BackgroundColor = XLColor.FromHtml(GrafikNurkowyConstants.ColorBiale);
        titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        titleRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Bottom;
    }

    private static void StyleHeaderRow(IXLWorksheet ws, int daysInMonth)
    {
        var headerLabel = ws.Range(
            GrafikNurkowyConstants.HeaderRow,
            GrafikNurkowyConstants.ColJednostkaPsp,
            GrafikNurkowyConstants.HeaderRow,
            GrafikNurkowyConstants.ColFunkcja);
        headerLabel.Style.Font.Bold = true;
        headerLabel.Style.Font.FontSize = 11;
        headerLabel.Style.Fill.BackgroundColor = XLColor.FromHtml(GrafikNurkowyConstants.ColorBiale);
        headerLabel.Style.Alignment.Vertical = XLAlignmentVerticalValues.Bottom;

        ws.Cell(GrafikNurkowyConstants.HeaderRow, GrafikNurkowyConstants.ColJednostkaPsp)
            .Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell(GrafikNurkowyConstants.HeaderRow, GrafikNurkowyConstants.ColImieNazwisko)
            .Style.Alignment.Horizontal = XLAlignmentHorizontalValues.General;
        ws.Cell(GrafikNurkowyConstants.HeaderRow, GrafikNurkowyConstants.ColFunkcja)
            .Style.Alignment.Horizontal = XLAlignmentHorizontalValues.General;

        for (var day = 1; day <= daysInMonth; day++)
        {
            var cell = ws.Cell(GrafikNurkowyConstants.HeaderRow, GrafikNurkowyConstants.FirstDayCol + day - 1);
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontSize = 12;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml(GrafikNurkowyConstants.ColorForDayHeader(day));
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Bottom;
            ApplyThinBorder(cell);
        }

        for (var col = GrafikNurkowyConstants.ColJednostkaPsp; col <= GrafikNurkowyConstants.ColFunkcja; col++)
            ApplyThinBorder(ws.Cell(GrafikNurkowyConstants.HeaderRow, col));
    }

    private static void StyleUnitColumns(IXLWorksheet ws, int lastDataRow, int summaryRow)
    {
        if (lastDataRow < GrafikNurkowyConstants.FirstDataRow)
            return;

        var endRow = Math.Max(lastDataRow, summaryRow);
        UnmergeColumnIfNeeded(ws, GrafikNurkowyConstants.ColJednostkaSgrwn, GrafikNurkowyConstants.FirstDataRow, endRow);
        UnmergeColumnIfNeeded(ws, GrafikNurkowyConstants.ColJednostkaPsp, GrafikNurkowyConstants.FirstDataRow, endRow);

        var sgrwnRange = ws.Range(
            GrafikNurkowyConstants.FirstDataRow,
            GrafikNurkowyConstants.ColJednostkaSgrwn,
            endRow,
            GrafikNurkowyConstants.ColJednostkaSgrwn);
        sgrwnRange.Merge();
        sgrwnRange.Value = GrafikNurkowyConstants.JednostkaSgrwn;
        sgrwnRange.Style.Font.Bold = true;
        sgrwnRange.Style.Font.FontSize = 26;
        sgrwnRange.Style.Fill.BackgroundColor = XLColor.FromHtml(GrafikNurkowyConstants.ColorSgrwn);
        sgrwnRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        sgrwnRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        sgrwnRange.Style.Alignment.TextRotation = 90;
        ApplyThinBorder(sgrwnRange);

        var pspRange = ws.Range(
            GrafikNurkowyConstants.FirstDataRow,
            GrafikNurkowyConstants.ColJednostkaPsp,
            endRow,
            GrafikNurkowyConstants.ColJednostkaPsp);
        pspRange.Merge();
        pspRange.Value = GrafikNurkowyConstants.JednostkaPsp;
        pspRange.Style.Font.Bold = true;
        pspRange.Style.Font.FontSize = 20;
        pspRange.Style.Fill.BackgroundColor = XLColor.FromHtml(GrafikNurkowyConstants.ColorJednostkaPsp);
        pspRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        pspRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        pspRange.Style.Alignment.TextRotation = 90;
        ApplyThinBorder(pspRange);
    }

    private static void StylePersonRows(
        IXLWorksheet ws,
        int lastDataRow,
        int daysInMonth,
        IReadOnlyDictionary<string, int> nameToZmiana)
    {
        for (var row = GrafikNurkowyConstants.FirstDataRow; row <= lastDataRow; row++)
        {
            var name = ws.Cell(row, GrafikNurkowyConstants.ColImieNazwisko).GetString().Trim();
            if (string.IsNullOrWhiteSpace(name))
                continue;

            var zmianaId = nameToZmiana.TryGetValue(UrlopNameMatcher.Normalize(name), out var z) ? z : 1;
            var rowColor = XLColor.FromHtml(GrafikNurkowyConstants.ColorForZmiana(zmianaId));

            var nameCell = ws.Cell(row, GrafikNurkowyConstants.ColImieNazwisko);
            nameCell.Style.Font.Bold = true;
            nameCell.Style.Font.FontSize = 14;
            nameCell.Style.Font.FontColor = XLColor.Black;
            nameCell.Style.Fill.BackgroundColor = rowColor;
            nameCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            nameCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ApplyThinBorder(nameCell);

            var funkcjaCell = ws.Cell(row, GrafikNurkowyConstants.ColFunkcja);
            funkcjaCell.Style.Font.Bold = true;
            funkcjaCell.Style.Font.FontSize = 14;
            // Kolor czcionki funkcji ustala formatowanie warunkowe (KPP → czerwony).
            funkcjaCell.Style.Font.FontColor = XLColor.Black;
            funkcjaCell.Style.Fill.BackgroundColor = rowColor;
            funkcjaCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            funkcjaCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ApplyThinBorder(funkcjaCell);

            for (var day = 1; day <= daysInMonth; day++)
            {
                var cell = ws.Cell(row, GrafikNurkowyConstants.FirstDayCol + day - 1);
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml(GrafikNurkowyConstants.ColorBiale);
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontSize = 16;
                cell.Style.Font.FontColor = XLColor.FromHtml(GrafikNurkowyConstants.ColorWartoscCzcionka);
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                ApplyThinBorder(cell);
            }
        }
    }

    private static void StyleSummaryRow(IXLWorksheet ws, int summaryRow, int daysInMonth)
    {
        ws.Cell(summaryRow, GrafikNurkowyConstants.ColImieNazwisko).Value =
            GrafikNurkowyConstants.PodsumowanieEtykieta;

        var labelRange = ws.Range(
            summaryRow,
            GrafikNurkowyConstants.ColImieNazwisko,
            summaryRow,
            GrafikNurkowyConstants.ColFunkcja);
        labelRange.Style.Font.Bold = true;
        labelRange.Style.Font.FontSize = 16;
        labelRange.Style.Font.FontColor = XLColor.Black;
        labelRange.Style.Fill.BackgroundColor = XLColor.FromHtml(GrafikNurkowyConstants.ColorPodsumowanie);
        labelRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        labelRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ApplyThinBorder(labelRange);

        for (var day = 1; day <= daysInMonth; day++)
        {
            var cell = ws.Cell(summaryRow, GrafikNurkowyConstants.FirstDayCol + day - 1);
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontSize = 16;
            cell.Style.Font.FontColor = XLColor.Black;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml(GrafikNurkowyConstants.ColorBiale);
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ApplyThinBorder(cell);
        }
    }

    private static void StyleLegend(IXLWorksheet ws, int summaryRow)
    {
        var legendRow = summaryRow + 2;

        StyleLegendSwatch(ws.Cell(legendRow, 12), GrafikNurkowyConstants.ColorZmiana1);
        ws.Cell(legendRow, 13).Value = "- zm. I";
        StyleLegendLabel(ws.Cell(legendRow, 13));

        StyleLegendSwatch(ws.Cell(legendRow, 14), GrafikNurkowyConstants.ColorZmiana2);
        ws.Cell(legendRow, 15).Value = "- zm. II";
        StyleLegendLabel(ws.Cell(legendRow, 15));

        StyleLegendSwatch(ws.Cell(legendRow, 16), GrafikNurkowyConstants.ColorZmiana3);
        ws.Cell(legendRow, 17).Value = "- zm. III";
        StyleLegendLabel(ws.Cell(legendRow, 17));

        StyleLegendSwatch(ws.Cell(legendRow, 19), GrafikNurkowyConstants.ColorBrakGotowosci);
        ws.Cell(legendRow, 20).Value = "- BRAK DEKLAROWANEGO POZIOMU GOTOWOŚCI";
        StyleLegendLabel(ws.Cell(legendRow, 20));
    }

    private static void StyleLegendSwatch(IXLCell cell, string colorHex)
    {
        cell.Style.Fill.BackgroundColor = XLColor.FromHtml(colorHex);
        ApplyThinBorder(cell);
    }

    private static void StyleLegendLabel(IXLCell cell)
    {
        cell.Style.Font.FontSize = 11;
        cell.Style.Font.FontColor = XLColor.Black;
        cell.Style.Fill.BackgroundColor = XLColor.FromHtml(GrafikNurkowyConstants.ColorBiale);
        cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
    }

    private static void ApplyColumnWidths(IXLWorksheet ws)
    {
        ws.Column(GrafikNurkowyConstants.ColJednostkaSgrwn).Width = 8.43;
        ws.Column(GrafikNurkowyConstants.ColJednostkaPsp).Width = 8.43;
        ws.Column(GrafikNurkowyConstants.ColImieNazwisko).Width = 24.71;
        ws.Column(GrafikNurkowyConstants.ColFunkcja).Width = 12;
        for (var col = GrafikNurkowyConstants.FirstDayCol; col <= GrafikNurkowyConstants.FirstDayCol + 30; col++)
            ws.Column(col).Width = 4.5;
    }

    private static void ApplyRowHeights(IXLWorksheet ws, int lastDataRow, int summaryRow)
    {
        ws.Row(GrafikNurkowyConstants.TitleRow).Height = 24;
        ws.Row(GrafikNurkowyConstants.HeaderRow).Height = 16.5;
        for (var row = GrafikNurkowyConstants.FirstDataRow; row <= Math.Max(lastDataRow, summaryRow); row++)
            ws.Row(row).Height = 20.25;
    }

    private static void UnmergeColumnIfNeeded(IXLWorksheet ws, int col, int firstRow, int lastRow)
    {
        for (var row = firstRow; row <= lastRow; row++)
        {
            var cell = ws.Cell(row, col);
            if (cell.IsMerged())
                cell.MergedRange().Unmerge();
        }
    }

    private static void ApplyThinBorder(IXLRange range)
    {
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.OutsideBorderColor = XLColor.Black;
        range.Style.Border.InsideBorderColor = XLColor.Black;
    }

    private static void ApplyThinBorder(IXLCell cell)
    {
        cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        cell.Style.Border.OutsideBorderColor = XLColor.Black;
    }
}
