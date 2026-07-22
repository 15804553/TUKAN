using SKRYBEK.Core.Enums;

namespace SKRYBEK.Core.Rules;

/// <summary>
/// Mapuje kody TypWpisu z grafiku BOBER na sekcje nieobecności w rozkazie dziennym.
/// </summary>
public static class BoberTypWpisuMapper
{
    /// <summary>
    /// Zwraca typ nieobecności albo null, gdy osoba jest dostępna (Oddaje, „?”, brak wpisu).
    /// </summary>
    public static TypNieobecnosci? MapLubPominOddal(string? typWpisu)
    {
        if (string.IsNullOrWhiteSpace(typWpisu))
            return null;

        if (JestOddany(typWpisu))
            return null;

        var bazowy = BazowyKod(typWpisu);
        if (bazowy is "?" or "")
            return null;

        return Map(typWpisu);
    }

    /// <summary>Oddaje: sufiks „/” na WS, D lub U — osoba wraca do pracy.</summary>
    public static bool JestOddany(string? typWpisu)
    {
        if (string.IsNullOrWhiteSpace(typWpisu))
            return false;

        var trimmed = typWpisu.Trim();
        if (trimmed.Length > 0 && trimmed[^1] == '.')
            trimmed = trimmed[..^1];

        if (trimmed.Length < 2 || trimmed[^1] != '/')
            return false;

        var bazowy = trimmed[..^1].Trim().ToUpperInvariant();
        return bazowy is "D" or "WS" or "U";
    }

    public static string BazowyKod(string? typWpisu)
    {
        if (string.IsNullOrWhiteSpace(typWpisu))
            return string.Empty;

        var trimmed = typWpisu.Trim();
        if (trimmed.Length > 1 && trimmed[^1] == '/')
            trimmed = trimmed[..^1];
        if (trimmed.Length > 0 && trimmed[^1] == '.')
            trimmed = trimmed[..^1];

        return trimmed;
    }

    /// <summary>
    /// S (szkolenie) → Delegowany; C (chory) → Chory; Del → Delegowany; itd.
    /// </summary>
    public static TypNieobecnosci Map(string? kod)
    {
        if (string.IsNullOrWhiteSpace(kod))
            return TypNieobecnosci.CzasWolny;

        var normalizowany = BazowyKod(kod).ToUpperInvariant();

        return normalizowany switch
        {
            "U" or "URL" or "URLOP" or "UR"
                => TypNieobecnosci.Urlop,

            "DEL" or "DELEGACJA" or "DELEG" or "DG" or "S" or "SZKOLENIE" or "SZK"
                => TypNieobecnosci.Delegowany,

            "C" or "CH" or "CHORY" or "CHORA" or "L4" or "ZW" or "ZWOLNIENIE"
                => TypNieobecnosci.Chory,

            "D" or "DD" or "DYZ" or "DYZUR" or "DYZURD" or "DYŻ" or "DYŻUR" or "DYŻURD"
                => TypNieobecnosci.DyzurDomowy,

            "WS" or "W" or "WOL" or "WOLNY" or "WOLNA" or "CW" or "CWASLUZBY"
                => TypNieobecnosci.CzasWolny,

            "1" => TypNieobecnosci.Urlop,
            "2" => TypNieobecnosci.CzasWolny,
            "3" => TypNieobecnosci.Chory,
            "4" => TypNieobecnosci.Delegowany,
            "5" => TypNieobecnosci.DyzurDomowy,

            _ => TypNieobecnosci.CzasWolny
        };
    }
}
