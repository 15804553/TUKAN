namespace BOBER.Core.Constants;

/// <summary>
/// Kody wpisów w komórkach grafiku i reguły ich interpretacji przy podsumowaniach.
/// </summary>
public static class GrafikWpisTypy
{
    public const string Dyzur = "D";
    public const string WolnaSluzba = "WS";
    public const string Urlop = "U";
    public const string Delegacja = "Del";

    /// <summary>
    /// Czy wpis oznacza nieobecność funkcjonariusza w pracy zmiany w danym dniu.
    /// Pusta komórka = w pracy. D (dyżur), WS, U i Del = nieobecny w składzie operacyjnym.
    /// </summary>
    public static bool JestNieobecnoscia(string? typWpisu)
    {
        if (string.IsNullOrWhiteSpace(typWpisu))
            return false;

        var kod = typWpisu.Trim();
        return kod.Equals(Dyzur, StringComparison.OrdinalIgnoreCase)
            || kod.Equals(WolnaSluzba, StringComparison.OrdinalIgnoreCase)
            || kod.Equals(Urlop, StringComparison.OrdinalIgnoreCase)
            || kod.Equals(Delegacja, StringComparison.OrdinalIgnoreCase)
            || kod.Equals("DEL", StringComparison.OrdinalIgnoreCase)
            || kod.Equals("DD", StringComparison.OrdinalIgnoreCase);
    }
}
