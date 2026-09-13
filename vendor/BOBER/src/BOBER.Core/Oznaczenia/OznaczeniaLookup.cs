using BOBER.Core.Enums;
using BOBER.Core.Models;

namespace BOBER.Core.Oznaczenia;

/// <summary>Cache oznaczeń grafiku z ustawień (ładowany przez serwis).</summary>
public static class OznaczeniaLookup
{
    private static IReadOnlyList<OznaczenieGrafiku> _items = Array.Empty<OznaczenieGrafiku>();

    public static IReadOnlyList<OznaczenieGrafiku> Items => _items;

    public static void Set(IReadOnlyList<OznaczenieGrafiku> items) =>
        _items = items ?? Array.Empty<OznaczenieGrafiku>();

    public static void Clear() => _items = Array.Empty<OznaczenieGrafiku>();

    public static bool HasItems => _items.Count > 0;

    public static OznaczenieGrafiku? FindByKod(string? kod)
    {
        if (string.IsNullOrWhiteSpace(kod) || _items.Count == 0)
            return null;

        return _items.FirstOrDefault(o =>
            o.Kod.Equals(kod, StringComparison.OrdinalIgnoreCase));
    }

    public static OznaczenieGrafiku? FindBySkrot(string keyName)
    {
        if (string.IsNullOrWhiteSpace(keyName) || _items.Count == 0)
            return null;

        return _items.FirstOrDefault(o =>
            SkrotKlawiszowyRules.Matches(o.SkrotKlawiszowy, keyName));
    }

    public static OznaczenieGrafiku? FindByFlaga(FlagaPozycjaOznaczenia flaga) =>
        _items.FirstOrDefault(o => o.FlagaPozycja == flaga);

    public static string? KolorWsHex() =>
        FindByKod(Constants.GrafikWpisTypy.WolnaSluzba)?.KolorHex;
}
