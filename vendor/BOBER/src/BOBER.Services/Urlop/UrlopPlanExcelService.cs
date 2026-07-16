using ClosedXML.Excel;
using BOBER.Core.Constants;
using BOBER.Core.Models;

namespace BOBER.Services.Urlop;

public sealed class UrlopPlanExcelService
{
  private static readonly string[] MonthNames =
  [
      "Styczeń", "Luty", "Marzec", "Kwiecień", "Maj", "Czerwiec",
      "Lipiec", "Sierpień", "Wrzesień", "Październik", "Listopad", "Grudzień"
  ];

    private const int FirstDataRow = 6;
    private const int LastDataRow = 25;
    private const int FirstDayCol = 3;
    private const int MaxDayCol = 33;

    public void Export(
        int zmianaId,
        int rok,
        IReadOnlyList<Funkcjonariusz> funkcjonariusze,
        IReadOnlyList<UrlopPlanWpis> wpisy,
        string filePath)
    {
        using var workbook = new XLWorkbook();

        for (var miesiac = 1; miesiac <= 12; miesiac++)
        {
            var monthWpisy = wpisy.Where(w => w.Miesiac == miesiac).ToList();
            AddMonthSheet(workbook, rok, miesiac, funkcjonariusze, monthWpisy);
        }

        AddInstructionSheet(workbook);
        workbook.SaveAs(filePath);
    }

    public IReadOnlyList<UrlopPlanWpis> Import(
        string filePath,
        int rok,
        IReadOnlyList<Funkcjonariusz> funkcjonariusze)
    {
        using var workbook = new XLWorkbook(filePath);
        var lookup = UrlopNameMatcher.BuildLookup(
            funkcjonariusze.Select(f => (f.Id, f.Imie, f.Nazwisko)));

        var result = new List<UrlopPlanWpis>();
        var unknownNames = new List<string>();

        foreach (var sheetName in MonthNames)
        {
            if (!workbook.Worksheets.TryGetWorksheet(sheetName, out var ws))
                continue;

            var miesiac = Array.IndexOf(MonthNames, sheetName) + 1;
            var daysInMonth = DateTime.DaysInMonth(rok, miesiac);

            for (var row = FirstDataRow; row <= LastDataRow; row++)
            {
                var name = ws.Cell(row, 2).GetString().Trim();
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                if (!UrlopNameMatcher.TryMatch(name, lookup, out var fid))
                {
                    unknownNames.Add(name);
                    continue;
                }

                for (var day = 1; day <= daysInMonth; day++)
                {
                    var col = FirstDayCol + day - 1;
                    var code = ws.Cell(row, col).GetString().Trim().ToLowerInvariant();
                    if (!UrlopTypy.IsValid(code))
                        continue;

                    result.Add(new UrlopPlanWpis
                    {
                        FunkcjonariuszId = fid,
                        Rok = rok,
                        Miesiac = miesiac,
                        Dzien = day,
                        TypUrlopu = UrlopTypy.Normalize(code)
                    });
                }
            }
        }

        if (unknownNames.Count > 0)
        {
            throw new InvalidOperationException(
                "Nie rozpoznano następujących osób w pliku Excel:\n"
                + string.Join("\n", unknownNames.Distinct().OrderBy(n => n)));
        }

        return result;
    }

    private static void AddMonthSheet(
        XLWorkbook workbook,
        int rok,
        int miesiac,
        IReadOnlyList<Funkcjonariusz> funkcjonariusze,
        IReadOnlyList<UrlopPlanWpis> wpisy)
    {
        var ws = workbook.Worksheets.Add(MonthNames[miesiac - 1]);
        var daysInMonth = DateTime.DaysInMonth(rok, miesiac);

        ws.Cell(1, 3).Value = $"{MonthNames[miesiac - 1]} {rok}";
        ws.Cell(4, 1).Value = "L.p.";
        ws.Cell(4, 2).Value = "Nazwisko i imię";
        ws.Cell(1, 34).Value = "Wypoczynkowy";
        ws.Cell(1, 35).Value = "Dodatkowy";

        for (var day = 1; day <= daysInMonth; day++)
            ws.Cell(5, FirstDayCol + day - 1).Value = day;

        var wpisyLookup = wpisy
            .GroupBy(w => (w.FunkcjonariuszId, w.Dzien))
            .ToDictionary(g => g.Key, g => g.Last().TypUrlopu);

        for (var i = 0; i < funkcjonariusze.Count && i < LastDataRow - FirstDataRow + 1; i++)
        {
            var row = FirstDataRow + i;
            var f = funkcjonariusze[i];
            ws.Cell(row, 1).Value = $"{i + 1}.";
            ws.Cell(row, 2).Value = UrlopNameMatcher.ToExcelFormat(f.Imie, f.Nazwisko);

            for (var day = 1; day <= daysInMonth; day++)
            {
                if (!wpisyLookup.TryGetValue((f.Id, day), out var typ))
                    continue;

                ws.Cell(row, FirstDayCol + day - 1).Value = typ;
            }

            var dayRange = GetDayColumnRange(daysInMonth);
            ws.Cell(row, 34).FormulaA1 = $"COUNTIF({dayRange}{row},{ws.Cell(27, 2).Address})";
            ws.Cell(row, 35).FormulaA1 = $"COUNTIF({dayRange}{row},{ws.Cell(28, 2).Address})";
        }

        ws.Cell(27, 2).Value = UrlopTypy.Wypoczynkowy;
        ws.Cell(28, 2).Value = UrlopTypy.Dodatkowy;
        ws.Cell(27, 3).Value = "urlop wypoczynkowy, planujemy 20 dni z 26";

        for (var day = 1; day <= daysInMonth; day++)
        {
            var col = FirstDayCol + day - 1;
            var colLetter = ws.Column(col).ColumnLetter();
            ws.Cell(26, col).FormulaA1 =
                $"SUMPRODUCT(COUNTIF({colLetter}{FirstDataRow}:{colLetter}{LastDataRow},$B$27:$B$28))";
        }

        ws.SheetView.FreezeRows(5);
        ws.SheetView.FreezeColumns(2);
        ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
    }

    private static void AddInstructionSheet(XLWorkbook workbook)
    {
        var ws = workbook.Worksheets.Add("Instrukcja");
        var rules = new[]
        {
            "urlopy planujemy \"służbami\" czyli w interwale 3 dniowym",
            "urlop zaczyna się na służbie a kończy w dniu przed służbą",
            "planujemy 20 z 26 dni wypoczynkowego",
            "planujemy 13 dni dodatkowego,UWAGA nie możemy podzielić go więcej niż na dwie części z którzych jedna nie może być krótsza niż 6 dni.",
            "nie wpisujemy w plan urlopowy urlopów ZALEGŁYCH",
            "urlop w sezonie wakacyjnym czerwiec wrzesień planujemy max. 15 dni",
            $"max. na danej służbie na urlopie {UrlopPlanInstructions.DefaultMaxUrlopowNaSluzbie} osoby (wartość w Ustawieniach)",
            "nie planujemy urlopów w święta Wielkanocne oraz Bożego Narodzenia"
        };

        for (var i = 0; i < rules.Length; i++)
            ws.Cell(i + 3, 3).Value = rules[i];
    }

    private static string GetDayColumnRange(int daysInMonth)
    {
        var lastCol = FirstDayCol + daysInMonth - 1;
        var first = XLHelper.GetColumnLetterFromNumber(FirstDayCol);
        var last = XLHelper.GetColumnLetterFromNumber(lastCol);
        return $"{first}:{last}";
    }
}
