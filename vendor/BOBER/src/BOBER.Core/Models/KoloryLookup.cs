namespace BOBER.Core.Models;

/// <summary>
/// Odczyt flagi <see cref="KolorStanowiska.Aktywny"/> i hexów kolorów stanowisk.
/// </summary>
public static class KoloryLookup
{
    public static IReadOnlyDictionary<string, KolorStanowiska> Index(
        IEnumerable<KolorStanowiska> kolory) =>
        kolory
            .GroupBy(k => k.KluczRoli, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

    /// <summary>Brak rekordu = kolor aktywny (zachowanie sprzed flagi).</summary>
    public static bool IsAktywny(IReadOnlyDictionary<string, KolorStanowiska>? kolory, string klucz)
    {
        if (kolory is null)
            return true;

        return !kolory.TryGetValue(klucz, out var kolor) || kolor.Aktywny;
    }

    /// <summary>Hex gdy klucz jest aktywny; w przeciwnym razie <c>null</c> (nie maluj).</summary>
    public static string? GetHexIfAktywny(
        IReadOnlyDictionary<string, KolorStanowiska>? kolory,
        string klucz)
    {
        if (!IsAktywny(kolory, klucz))
            return null;

        if (kolory is not null
            && kolory.TryGetValue(klucz, out var kolor)
            && !string.IsNullOrWhiteSpace(kolor.KolorHex))
            return kolor.KolorHex;

        return null;
    }

    public static IReadOnlyDictionary<string, string> ToHexDictionary(
        IEnumerable<KolorStanowiska> kolory) =>
        kolory
            .GroupBy(k => k.KluczRoli, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().KolorHex, StringComparer.OrdinalIgnoreCase);

    public static IReadOnlySet<string> NieaktywneKlucze(IEnumerable<KolorStanowiska> kolory) =>
        kolory
            .Where(k => !k.Aktywny)
            .Select(k => k.KluczRoli)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
}
