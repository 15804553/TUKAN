namespace BOBER.Core.Constants;

/// <summary>Kody typów urlopu w planie urlopowym (zgodne ze wzorcem Excel).</summary>
public static class UrlopTypy
{
    public const string Wypoczynkowy = "w";
    public const string Dodatkowy = "d";

    public static bool IsValid(string? value) =>
        string.Equals(value, Wypoczynkowy, StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, Dodatkowy, StringComparison.OrdinalIgnoreCase);

    public static string Normalize(string value) =>
        string.Equals(value, Dodatkowy, StringComparison.OrdinalIgnoreCase) ? Dodatkowy : Wypoczynkowy;
}
