using ClosedXML.Excel;
using BOBER.Core.Constants;
using BOBER.Core.Models;
using BOBER.Services.Urlop;

namespace BOBER.Services.GrafikNurkowy;

/// <summary>Tworzenie, aktualizacja i odczyt dokumentu Excel grafiku nurkowego.</summary>
public sealed class GrafikNurkowyExcelService
{
    public void CreateOrUpdate(
        string filePath,
        int rok,
        int miesiac,
        int zmianaId,
        IReadOnlyList<Funkcjonariusz> nurkowieZmiany,
        IReadOnlyList<Funkcjonariusz> wszyscyNurkowie,
        IReadOnlyDictionary<(int FunkcjonariuszId, int Dzien), string?> wartosciDni,
        IReadOnlyCollection<int> workDays)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        var createdNew = !File.Exists(filePath);
        using var workbook = createdNew ? new XLWorkbook() : new XLWorkbook(filePath);

        IXLWorksheet ws;
        if (createdNew || !TryGetMonthSheet(workbook, miesiac, rok, out ws!))
        {
            ws = CreateSheet(workbook, rok, miesiac, wszyscyNurkowie);
        }

        UpdateShiftRows(ws, rok, miesiac, zmianaId, nurkowieZmiany, wartosciDni, workDays);
        RemoveBlankRowsBetweenDataAndSummary(ws);
        RecalculateSummary(ws, rok, miesiac);
        GrafikNurkowyExcelFormatter.Apply(ws, rok, miesiac, wszyscyNurkowie);

        workbook.SaveAs(filePath);
    }

    /// <summary>Usuwa zbędne puste wiersze między osobami a wierszem podsumowania.</summary>
    private static void RemoveBlankRowsBetweenDataAndSummary(IXLWorksheet ws)
    {
        var summaryRow = GrafikNurkowyExcelFormatter.FindSummaryRow(ws);
        if (summaryRow < 0)
            return;

        for (var row = summaryRow - 1; row >= GrafikNurkowyConstants.FirstDataRow; row--)
        {
            var name = ws.Cell(row, GrafikNurkowyConstants.ColImieNazwisko).GetString().Trim();
            if (!string.IsNullOrWhiteSpace(name))
                break;

            ws.Row(row).Delete();
        }
    }

    public IReadOnlyList<GrafikNurkowyWiersz> ReadPreview(string filePath, int rok, int miesiac)
    {
        if (!File.Exists(filePath))
            return [];

        using var workbook = new XLWorkbook(filePath);
        if (!TryGetMonthSheet(workbook, miesiac, rok, out var ws) || ws is null)
            return [];

        var daysInMonth = DateTime.DaysInMonth(rok, miesiac);
        var result = new List<GrafikNurkowyWiersz>();
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? GrafikNurkowyConstants.FirstDataRow;

        for (var row = GrafikNurkowyConstants.FirstDataRow; row <= lastRow; row++)
        {
            var name = ws.Cell(row, GrafikNurkowyConstants.ColImieNazwisko).GetString().Trim();
            if (string.IsNullOrWhiteSpace(name))
                continue;

            if (name.Contains("JRG", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("-", StringComparison.Ordinal))
                continue;

            var funkcja = ws.Cell(row, GrafikNurkowyConstants.ColFunkcja).GetString().Trim();
            var dni = new Dictionary<int, string>();
            for (var day = 1; day <= daysInMonth; day++)
            {
                var cell = ws.Cell(row, GrafikNurkowyConstants.FirstDayCol + day - 1);
                var value = ReadDayCellValue(cell);
                if (!string.IsNullOrEmpty(value))
                    dni[day] = value;
            }

            result.Add(new GrafikNurkowyWiersz
            {
                ImieNazwisko = name,
                Funkcja = funkcja,
                Dni = dni
            });
        }

        return result;
    }

    private static bool TryGetMonthSheet(XLWorkbook workbook, int miesiac, int rok, out IXLWorksheet? ws)
    {
        var expected = GrafikNurkowyConstants.BuildSheetName(miesiac, rok);
        if (workbook.Worksheets.TryGetWorksheet(expected, out ws))
            return true;

        var monthToken = GrafikNurkowyConstants.MonthNames[miesiac];
        foreach (var sheet in workbook.Worksheets)
        {
            if (sheet.Name.Contains(monthToken, StringComparison.OrdinalIgnoreCase)
                || sheet.Name.Contains(RemoveDiacritics(monthToken), StringComparison.OrdinalIgnoreCase))
            {
                ws = sheet;
                return true;
            }
        }

        ws = null;
        return false;
    }

    private static IXLWorksheet CreateSheet(
        XLWorkbook workbook,
        int rok,
        int miesiac,
        IReadOnlyList<Funkcjonariusz> wszyscyNurkowie)
    {
        var sheetName = GrafikNurkowyConstants.BuildSheetName(miesiac, rok);
        if (workbook.Worksheets.TryGetWorksheet(sheetName, out var existing))
            existing.Delete();

        var ws = workbook.Worksheets.Add(sheetName);
        var daysInMonth = DateTime.DaysInMonth(rok, miesiac);
        var lastDayCol = GrafikNurkowyConstants.FirstDayCol + daysInMonth - 1;

        ws.Cell(GrafikNurkowyConstants.TitleRow, GrafikNurkowyConstants.ColJednostkaPsp).Value =
            GrafikNurkowyConstants.BuildTitle(miesiac, rok);
        ws.Range(
            GrafikNurkowyConstants.TitleRow,
            GrafikNurkowyConstants.ColJednostkaPsp,
            GrafikNurkowyConstants.TitleRow,
            lastDayCol).Merge();

        ws.Cell(GrafikNurkowyConstants.HeaderRow, GrafikNurkowyConstants.ColJednostkaPsp).Value = "Jednostka PSP";
        ws.Cell(GrafikNurkowyConstants.HeaderRow, GrafikNurkowyConstants.ColImieNazwisko).Value = "Imię i nazwisko";
        ws.Cell(GrafikNurkowyConstants.HeaderRow, GrafikNurkowyConstants.ColFunkcja).Value = "funkcja";
        for (var day = 1; day <= daysInMonth; day++)
            ws.Cell(GrafikNurkowyConstants.HeaderRow, GrafikNurkowyConstants.FirstDayCol + day - 1).Value = day;

        var row = GrafikNurkowyConstants.FirstDataRow;
        foreach (var nurek in wszyscyNurkowie)
        {
            WritePersonRow(ws, row, nurek);
            row++;
        }

        ws.Cell(row, GrafikNurkowyConstants.ColImieNazwisko).Value =
            GrafikNurkowyConstants.PodsumowanieEtykieta;

        return ws;
    }

    private static void WritePersonRow(IXLWorksheet ws, int row, Funkcjonariusz nurek)
    {
        ws.Cell(row, GrafikNurkowyConstants.ColImieNazwisko).Value =
            UrlopNameMatcher.ToExcelFormat(nurek.Imie, nurek.Nazwisko);
        ws.Cell(row, GrafikNurkowyConstants.ColFunkcja).Value =
            GrafikNurkowyConstants.ResolveFunkcja(nurek);
    }

    private static void UpdateShiftRows(
        IXLWorksheet ws,
        int rok,
        int miesiac,
        int zmianaId,
        IReadOnlyList<Funkcjonariusz> nurkowieZmiany,
        IReadOnlyDictionary<(int FunkcjonariuszId, int Dzien), string?> wartosciDni,
        IReadOnlyCollection<int> workDays)
    {
        var daysInMonth = DateTime.DaysInMonth(rok, miesiac);
        var lookup = UrlopNameMatcher.BuildLookup(
            nurkowieZmiany.Select(f => (f.Id, f.Imie, f.Nazwisko)));
        var byId = nurkowieZmiany.ToDictionary(f => f.Id);
        var matchedIds = new HashSet<int>();

        var lastRow = GrafikNurkowyExcelFormatter.FindLastDataRow(ws);
        for (var row = GrafikNurkowyConstants.FirstDataRow; row <= lastRow; row++)
        {
            var name = ws.Cell(row, GrafikNurkowyConstants.ColImieNazwisko).GetString().Trim();
            if (string.IsNullOrWhiteSpace(name))
                continue;
            if (!UrlopNameMatcher.TryMatch(name, lookup, out var fid) || !byId.ContainsKey(fid))
                continue;

            matchedIds.Add(fid);
            ApplyDayValues(ws, row, fid, daysInMonth, workDays, wartosciDni);
            ws.Cell(row, GrafikNurkowyConstants.ColFunkcja).Value =
                GrafikNurkowyConstants.ResolveFunkcja(byId[fid]);
        }

        var insertAt = GrafikNurkowyExcelFormatter.FindSummaryRow(ws);
        if (insertAt < GrafikNurkowyConstants.FirstDataRow)
            insertAt = lastRow + 1;

        foreach (var nurek in nurkowieZmiany.Where(n => !matchedIds.Contains(n.Id)))
        {
            ws.Row(insertAt).InsertRowsAbove(1);
            WritePersonRow(ws, insertAt, nurek);
            ApplyDayValues(ws, insertAt, nurek.Id, daysInMonth, workDays, wartosciDni);
            insertAt++;
        }

        _ = zmianaId;
    }

    private static void ApplyDayValues(
        IXLWorksheet ws,
        int row,
        int funkcjonariuszId,
        int daysInMonth,
        IReadOnlyCollection<int> workDays,
        IReadOnlyDictionary<(int FunkcjonariuszId, int Dzien), string?> wartosciDni)
    {
        var workSet = workDays as HashSet<int> ?? workDays.ToHashSet();
        for (var day = 1; day <= daysInMonth; day++)
        {
            var cell = ws.Cell(row, GrafikNurkowyConstants.FirstDayCol + day - 1);
            if (!workSet.Contains(day))
            {
                cell.Clear(XLClearOptions.Contents);
                continue;
            }

            wartosciDni.TryGetValue((funkcjonariuszId, day), out var value);
            if (string.IsNullOrEmpty(value))
            {
                cell.Clear(XLClearOptions.Contents);
                continue;
            }

            // Jak we wzorcu: „1” jako liczba (SUM w podsumowaniu), „U” jako tekst.
            if (value == GrafikNurkowyConstants.WartoscWPracy)
                cell.Value = 1;
            else
                cell.Value = value;
        }
    }

    private static void RecalculateSummary(IXLWorksheet ws, int rok, int miesiac)
    {
        var daysInMonth = DateTime.DaysInMonth(rok, miesiac);
        var lastDataRow = GrafikNurkowyExcelFormatter.FindLastDataRow(ws);
        var summaryRow = GrafikNurkowyExcelFormatter.FindSummaryRow(ws);
        if (summaryRow < 0)
        {
            summaryRow = lastDataRow + 1;
            ws.Cell(summaryRow, GrafikNurkowyConstants.ColImieNazwisko).Value =
                GrafikNurkowyConstants.PodsumowanieEtykieta;
        }

        if (lastDataRow < GrafikNurkowyConstants.FirstDataRow)
            return;

        for (var day = 1; day <= daysInMonth; day++)
        {
            var col = GrafikNurkowyConstants.FirstDayCol + day - 1;
            var top = ws.Cell(GrafikNurkowyConstants.FirstDataRow, col).Address.ToStringRelative();
            var bottom = ws.Cell(lastDataRow, col).Address.ToStringRelative();
            ws.Cell(summaryRow, col).FormulaA1 = $"SUM({top}:{bottom})";
        }
    }

    private static string ReadDayCellValue(IXLCell cell)
    {
        if (cell.TryGetValue(out double number))
            return ((int)number).ToString();

        return cell.GetString().Trim();
    }

    private static string RemoveDiacritics(string text)
    {
        var formD = text.Normalize(System.Text.NormalizationForm.FormD);
        var sb = new System.Text.StringBuilder(formD.Length);
        foreach (var c in formD)
        {
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
                != System.Globalization.UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        return sb.ToString().Normalize(System.Text.NormalizationForm.FormC);
    }
}
