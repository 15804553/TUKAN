namespace BOBER.Core.Constants;

/// <summary>Kody typów urlopu w planie urlopowym (zgodne ze wzorcem Excel).</summary>
public static class UrlopTypy
{
    public const string Wypoczynkowy = "w";
    public const string Dodatkowy = "d";
    public const string Rodzicielski = "r";

    public static bool IsValid(string? value) =>
        string.Equals(value, Wypoczynkowy, StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, Dodatkowy, StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, Rodzicielski, StringComparison.OrdinalIgnoreCase);

    public static string Normalize(string value)
    {
        if (string.Equals(value, Dodatkowy, StringComparison.OrdinalIgnoreCase))
            return Dodatkowy;
        if (string.Equals(value, Rodzicielski, StringComparison.OrdinalIgnoreCase))
            return Rodzicielski;
        return Wypoczynkowy;
    }
}
