using ClosedXML.Excel;
using BOBER.Core.Constants;
using BOBER.Core.Models;
using BOBER.Services.Urlop;

namespace BOBER.Services.GrafikNurkowy;

/// <summary>
/// Przestawia wiersze osób według ustawienia kolejności.
/// Kolumna jednostki zostaje nietknięta — jest scalona na całą listę.
/// </summary>
internal static class GrafikNurkowyRowOrder
{
    public static void Apply(
        IXLWorksheet ws,
        int rok,
        int miesiac,
        GrafikNurkowyKolejnoscTryb tryb,
        GrafikNurkowyKolejnoscZakres zakres,
        IReadOnlyList<Funkcjonariusz> wszyscyNurkowie)
    {
        var summaryRow = GrafikNurkowyExcelFormatter.FindSummaryRow(ws);
        var lastDataRow = GrafikNurkowyExcelFormatter.FindLastDataRow(ws);
        if (lastDataRow < GrafikNurkowyConstants.FirstDataRow)
            return;

        var end = summaryRow > GrafikNurkowyConstants.FirstDataRow
            ? Math.Min(lastDataRow, summaryRow - 1)
            : lastDataRow;

        var daysInMonth = DateTime.DaysInMonth(rok, miesiac);
        var snapshots = new List<PersonRowSnapshot>();
        for (var row = GrafikNurkowyConstants.FirstDataRow; row <= end; row++)
        {
            var name = ws.Cell(row, GrafikNurkowyConstants.ColImieNazwisko).GetString().Trim();
            if (string.IsNullOrWhiteSpace(name))
                continue;

            snapshots.Add(ReadPersonRow(ws, row, name, daysInMonth));
        }

        if (snapshots.Count < 2)
            return;

        var nameToZmiana = BuildNameToZmiana(wszyscyNurkowie);
        var ordered = GrafikNurkowyConstants.OrderByKolejnosc(
            snapshots,
            s => s.Funkcja,
            tryb,
            zakres,
            s => ResolveZmiana(s.ImieNazwisko, nameToZmiana));
        if (ordered.Select((item, index) => ReferenceEquals(item, snapshots[index])).All(same => same))
            return;

        var targetRows = snapshots.Select(s => s.Row).ToArray();
        for (var i = 0; i < ordered.Count; i++)
            WritePersonSnapshot(ws, targetRows[i], ordered[i], daysInMonth);
    }

    private static Dictionary<string, int> BuildNameToZmiana(IReadOnlyList<Funkcjonariusz> nurkowie)
    {
        var map = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var nurek in nurkowie)
        {
            var key = UrlopNameMatcher.Normalize(UrlopNameMatcher.ToExcelFormat(nurek.Imie, nurek.Nazwisko));
            map.TryAdd(key, nurek.NumerZmiany);
        }

        return map;
    }

    private static int ResolveZmiana(string name, IReadOnlyDictionary<string, int> nameToZmiana) =>
        nameToZmiana.TryGetValue(UrlopNameMatcher.Normalize(name), out var zmiana)
            ? zmiana
            : int.MaxValue;

    private static PersonRowSnapshot ReadPersonRow(
        IXLWorksheet ws,
        int row,
        string name,
        int daysInMonth)
    {
        var days = new XLCellValue?[daysInMonth];
        for (var day = 1; day <= daysInMonth; day++)
        {
            var cell = ws.Cell(row, GrafikNurkowyConstants.FirstDayCol + day - 1);
            days[day - 1] = cell.IsEmpty() ? (XLCellValue?)null : cell.Value;
        }

        return new PersonRowSnapshot
        {
            Row = row,
            ImieNazwisko = name,
            Funkcja = ws.Cell(row, GrafikNurkowyConstants.ColFunkcja).GetString().Trim(),
            Dni = days
        };
    }

    private static void WritePersonSnapshot(
        IXLWorksheet ws,
        int row,
        PersonRowSnapshot snapshot,
        int daysInMonth)
    {
        ws.Cell(row, GrafikNurkowyConstants.ColImieNazwisko).Value = snapshot.ImieNazwisko;
        ws.Cell(row, GrafikNurkowyConstants.ColFunkcja).Value = snapshot.Funkcja;
        for (var day = 1; day <= daysInMonth; day++)
        {
            var cell = ws.Cell(row, GrafikNurkowyConstants.FirstDayCol + day - 1);
            var value = snapshot.Dni[day - 1];
            if (value is null)
                cell.Clear(XLClearOptions.Contents);
            else
                cell.Value = value.Value;
        }
    }

    private sealed class PersonRowSnapshot
    {
        public int Row { get; init; }
        public required string ImieNazwisko { get; init; }
        public required string Funkcja { get; init; }
        public required XLCellValue?[] Dni { get; init; }
    }
}
