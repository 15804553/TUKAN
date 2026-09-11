namespace BOBER.Core.Oznaczenia;

/// <summary>
/// Skróty w bazie bywają zapisane jako nazwa WPF (<c>Oem2</c>) albo znak (<c>/</c>).
/// Ta klasa ujednolica wyświetlanie i dopasowanie przy naciskaniu klawisza.
/// </summary>
public static class SkrotKlawiszowyRules
{
    public static string FormatForDisplay(string? skrot)
    {
        if (string.IsNullOrWhiteSpace(skrot))
            return string.Empty;

        var token = skrot.Trim();
        if (IsSlashFamily(token))
            return "/";
        if (IsPeriodFamily(token))
            return ".";
        return token;
    }

    /// <summary>Czy zapisany skrót i naciśnięty klawisz (nazwa WPF lub znak) to ten sam skrót.</summary>
    public static bool Matches(string? stored, string? pressed)
    {
        if (string.IsNullOrWhiteSpace(stored) || string.IsNullOrWhiteSpace(pressed))
            return false;

        var a = Expand(stored.Trim());
        var b = Expand(pressed.Trim());
        return a.Overlaps(b);
    }

    public static HashSet<string> Expand(string token)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { token };

        if (IsSlashFamily(token))
        {
            set.Add("/");
            set.Add("?");
            set.Add("Oem2");
            set.Add("OemQuestion");
            set.Add("Divide");
        }
        else if (IsPeriodFamily(token))
        {
            set.Add(".");
            set.Add(",");
            set.Add("OemPeriod");
            set.Add("Decimal");
        }

        return set;
    }

    private static bool IsSlashFamily(string token) =>
        token is "/" or "?" or "Oem2" or "OemQuestion" or "Divide";

    private static bool IsPeriodFamily(string token) =>
        token is "." or "," or "OemPeriod" or "Decimal";
}
