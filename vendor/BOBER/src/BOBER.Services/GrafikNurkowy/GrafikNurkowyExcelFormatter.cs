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
        IReadOnlyList<Funkcjonariusz> wszyscyNurkowie,
        IReadOnlyDictionary<int, int>? dzienDoZmiany = null)
    {
        RemoveObsoleteSgrwnColumn(ws);

        var daysInMonth = DateTime.DaysInMonth(rok, miesiac);
        var lastDayCol = GrafikNurkowyConstants.FirstDayCol + daysInMonth - 1;
        var lastDataRow = FindLastDataRow(ws);
        var summaryRow = FindSummaryRow(ws);
        if (summaryRow < 0)
            summaryRow = lastDataRow + 1;

        var nameToZmiana = wszyscyNurkowie
            .GroupBy(f => UrlopNameMatcher.Normalize(UrlopNameMatcher.ToExcelFormat(f.Imie, f.Nazwisko)))
            .ToDictionary(g => g.Key, g => g.First().NumerZmiany, StringComparer.Ordinal);

        StyleTitle(ws, lastDayCol, miesiac, rok);
        StyleHeaderRow(ws, daysInMonth, dzienDoZmiany);
        StyleUnitColumn(ws, lastDataRow, summaryRow);
        StylePersonRows(ws, lastDataRow, daysInMonth, nameToZmiana);
        StyleSummaryRow(ws, summaryRow, daysInMonth);
        ApplyConditionalFormats(ws, lastDataRow, summaryRow, daysInMonth);
        StyleLegend(ws, summaryRow);
        ApplyColumnWidths(ws);
        ApplyRowHeights(ws, lastDataRow, summaryRow);
    }

    /// <summary>
    /// Usuwa dawną kolumnę A (SGRW-N), jeśli plik powstał w starszym układzie.
    /// </summary>
    public static void RemoveObsoleteSgrwnColumn(IXLWorksheet ws)
    {
        var probe = ws.Cell(GrafikNurkowyConstants.FirstDataRow, 1);
        var text = probe.IsMerged()
            ? probe.MergedRange().FirstCell().GetString().Trim()
            : probe.GetString().Trim();

        if (!text.Contains("SGRW", StringComparison.OrdinalIgnoreCase))
            return;

        if (probe.IsMerged())
            probe.MergedRange().Unmerge();

        ws.Column(1).Delete();
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

    private static void StyleTitle(IXLWorksheet ws, int lastDayCol, int miesiac, int rok)
    {
        var titleCell = ws.Cell(GrafikNurkowyConstants.TitleRow, GrafikNurkowyConstants.ColJednostkaPsp);
        var titleRange = ws.Range(
            GrafikNurkowyConstants.TitleRow,
            GrafikNurkowyConstants.ColJednostkaPsp,
            GrafikNurkowyConstants.TitleRow,
            lastDayCol);

        if (!titleCell.IsMerged())
            titleRange.Merge();

        titleCell.Value = GrafikNurkowyConstants.BuildTitle(miesiac, rok);
        titleRange.Style.Font.Bold = true;
        titleRange.Style.Font.FontSize = 18;
        titleRange.Style.Font.FontColor = XLColor.Black;
        titleRange.Style.Fill.BackgroundColor = XLColor.FromHtml(GrafikNurkowyConstants.ColorBiale);
        titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        titleRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Bottom;
    }

    private static void StyleHeaderRow(
        IXLWorksheet ws,
        int daysInMonth,
        IReadOnlyDictionary<int, int>? dzienDoZmiany)
    {
        // Bez etykiety „Jednostka PSP” w A2 — zostaje tylko scalona kolumna jednostki w danych.
        ws.Cell(GrafikNurkowyConstants.HeaderRow, GrafikNurkowyConstants.ColJednostkaPsp).Clear(XLClearOptions.Contents);

        var headerLabel = ws.Range(
            GrafikNurkowyConstants.HeaderRow,
            GrafikNurkowyConstants.ColImieNazwisko,
            GrafikNurkowyConstants.HeaderRow,
            GrafikNurkowyConstants.ColFunkcja);
        headerLabel.Style.Font.Bold = true;
        headerLabel.Style.Font.FontSize = 11;
        headerLabel.Style.Fill.BackgroundColor = XLColor.FromHtml(GrafikNurkowyConstants.ColorBiale);
        headerLabel.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        headerLabel.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

        for (var day = 1; day <= daysInMonth; day++)
        {
            var cell = ws.Cell(GrafikNurkowyConstants.HeaderRow, GrafikNurkowyConstants.FirstDayCol + day - 1);
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontSize = 12;
            var zmianaId = dzienDoZmiany is not null && dzienDoZmiany.TryGetValue(day, out var z) ? z : 0;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml(GrafikNurkowyConstants.ColorForDayHeader(zmianaId));
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Bottom;
            ApplyThinBorder(cell);
        }

        for (var col = GrafikNurkowyConstants.ColImieNazwisko; col <= GrafikNurkowyConstants.ColFunkcja; col++)
            ApplyThinBorder(ws.Cell(GrafikNurkowyConstants.HeaderRow, col));
        ApplyThinBorder(ws.Cell(GrafikNurkowyConstants.HeaderRow, GrafikNurkowyConstants.ColJednostkaPsp));
    }

    private static void StyleUnitColumn(IXLWorksheet ws, int lastDataRow, int summaryRow)
    {
        if (lastDataRow < GrafikNurkowyConstants.FirstDataRow)
            return;

        var endRow = Math.Max(lastDataRow, summaryRow);
        UnmergeColumnIfNeeded(ws, GrafikNurkowyConstants.ColJednostkaPsp, GrafikNurkowyConstants.FirstDataRow, endRow);

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
            var funkcja = funkcjaCell.GetString().Trim();
            funkcjaCell.Style.Font.Bold = true;
            funkcjaCell.Style.Font.FontSize = 14;
            funkcjaCell.Style.Font.FontColor = funkcja.Equals(
                    GrafikNurkowyConstants.FunkcjaKpp, StringComparison.OrdinalIgnoreCase)
                ? XLColor.FromHtml(GrafikNurkowyConstants.ColorWartoscCzcionka)
                : XLColor.Black;
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
        if (!labelRange.FirstCell().IsMerged())
            labelRange.Merge();
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
        ClearLegendRow(ws, legendRow);

        // Wąskie kolumny dni — każdy wpis: kratka + scalony opis, z odstępem między wpisami.
        PlaceLegendEntry(ws, legendRow, swatchCol: 4, labelCols: 3,
            GrafikNurkowyConstants.ColorZmiana1, "- zm. I");
        PlaceLegendEntry(ws, legendRow, swatchCol: 9, labelCols: 3,
            GrafikNurkowyConstants.ColorZmiana2, "- zm. II");
        PlaceLegendEntry(ws, legendRow, swatchCol: 14, labelCols: 3,
            GrafikNurkowyConstants.ColorZmiana3, "- zm. III");
        PlaceLegendEntry(ws, legendRow, swatchCol: 19, labelCols: 8,
            GrafikNurkowyConstants.ColorBrakGotowosci, "- BRAK DEKLAROWANEGO POZIOMU GOTOWOŚCI");

        ws.Row(legendRow).Height = 18;
    }

    private static void ClearLegendRow(IXLWorksheet ws, int legendRow)
    {
        var lastCol = GrafikNurkowyConstants.FirstDayCol + 30;
        var merges = ws.MergedRanges
            .Where(r => r.FirstRow().RowNumber() <= legendRow && r.LastRow().RowNumber() >= legendRow)
            .ToList();
        foreach (var merge in merges)
            merge.Unmerge();

        for (var col = 1; col <= lastCol; col++)
        {
            var cell = ws.Cell(legendRow, col);
            cell.Clear();
            cell.Style.Fill.BackgroundColor = XLColor.NoColor;
            cell.Style.Border.SetOutsideBorder(XLBorderStyleValues.None);
        }
    }

    private static void PlaceLegendEntry(
        IXLWorksheet ws,
        int row,
        int swatchCol,
        int labelCols,
        string colorHex,
        string label)
    {
        StyleLegendSwatch(ws.Cell(row, swatchCol), colorHex);

        var labelStart = swatchCol + 1;
        var labelEnd = labelStart + labelCols - 1;
        var labelRange = ws.Range(row, labelStart, row, labelEnd);
        if (labelCols > 1)
            labelRange.Merge();
        labelRange.FirstCell().Value = label;
        StyleLegendLabel(labelRange);
    }

    private static void StyleLegendSwatch(IXLCell cell, string colorHex)
    {
        cell.Style.Fill.BackgroundColor = XLColor.FromHtml(colorHex);
        ApplyThinBorder(cell);
    }

    private static void StyleLegendLabel(IXLRange range)
    {
        range.Style.Font.FontSize = 11;
        range.Style.Font.FontColor = XLColor.Black;
        range.Style.Fill.BackgroundColor = XLColor.FromHtml(GrafikNurkowyConstants.ColorBiale);
        range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
        range.Style.Alignment.Indent = 1;
    }

    private static void ApplyColumnWidths(IXLWorksheet ws)
    {
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
