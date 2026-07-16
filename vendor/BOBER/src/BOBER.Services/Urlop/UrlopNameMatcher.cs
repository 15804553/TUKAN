using System.Globalization;
using System.Text;

namespace BOBER.Services.Urlop;

public static class UrlopNameMatcher
{
    public static string Normalize(string name)
    {
        var parts = name
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(p => p.ToUpperInvariant())
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToArray();
        return string.Join(' ', parts);
    }

    public static string ToExcelFormat(string imie, string nazwisko) =>
        $"{nazwisko} {imie}".Trim();

    public static IReadOnlyDictionary<string, int> BuildLookup(IEnumerable<(int Id, string Imie, string Nazwisko)> osoby)
    {
        var lookup = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var (id, imie, nazwisko) in osoby)
        {
            lookup[Normalize(ToExcelFormat(imie, nazwisko))] = id;
            lookup[Normalize($"{imie} {nazwisko}".Trim())] = id;
        }

        return lookup;
    }

    public static bool TryMatch(string excelName, IReadOnlyDictionary<string, int> lookup, out int funkcjonariuszId)
    {
        funkcjonariuszId = 0;
        if (string.IsNullOrWhiteSpace(excelName))
            return false;

        return lookup.TryGetValue(Normalize(excelName), out funkcjonariuszId);
    }
}
