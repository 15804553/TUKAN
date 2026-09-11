using BOBER.Core.Enums;
using BOBER.Core.Models;

namespace BOBER.Core.Oznaczenia;

/// <summary>
/// Cache oznaczeń grafiku (ustawiany przez BOBER.Services po załadowaniu z DB).
/// Gdy pusty — reguły w <see cref="Constants.GrafikWpisTypy"/> używają fallbacku seed.
/// </summary>
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

    public static OznaczenieGrafiku? FindByRola(RolaNalozaniaOznaczenia rola) =>
        _items.FirstOrDefault(o => o.RolaNalozania == rola);

    /// <summary>Oznaczenie z flagą centrum (konfiguracja Oddaje).</summary>
    public static OznaczenieGrafiku? FindCentrumFlaga() =>
        _items.FirstOrDefault(o => o.FlagaPozycja == Enums.FlagaPozycjaOznaczenia.Centrum);

    /// <summary>Konfiguracja znaczka „chce oddać” (kropka •).</summary>
    public static OznaczenieGrafiku? FindChceOddac() =>
        FindByKod(Constants.OznaczeniaGrafikuSeed.KodChceOddac)
        ?? FindByKod(Constants.OznaczeniaGrafikuSeed.KodChceOddacLegacy);

    public static string? KolorWsHex()
    {
        var ws = FindByRola(RolaNalozaniaOznaczenia.WolnaSluzba)
            ?? FindByKod(Constants.GrafikWpisTypy.WolnaSluzba);
        return ws?.KolorHex;
    }
}
