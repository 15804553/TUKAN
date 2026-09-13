using SKRYBEK.Core.Enums;

namespace SKRYBEK.Core.Rules;

/// <summary>Most do katalogu oznaczeń BOBER (konfigurowany przez TUKAN).</summary>
public static class BoberOznaczeniaBridge
{
    /// <summary>
    /// true = obsłużono (sekcja null = w pracy / pomiń); false = użyj mapowania legacy.
    /// </summary>
    public static TryMapOznaczenie? TryMap { get; set; }

    /// <summary>Dopisek do nazwiska w grupie rozkazu (np. „-odb”); null/pusty = bez dopisku.</summary>
    public static Func<string, TypNieobecnosci, string?>? MapAdnotacja { get; set; }

    public static void Clear()
    {
        TryMap = null;
        MapAdnotacja = null;
    }
}

public delegate bool TryMapOznaczenie(string? typWpisu, out TypNieobecnosci? sekcja);
