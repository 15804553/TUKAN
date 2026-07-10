namespace BOBER.Core.Constants;

/// <summary>Domyślne kolory interfejsu aplikacji (zgodne z motywem piaskowym).</summary>
public static class AppColors
{
    public const string ForegroundHex = "#2C2818";
    public const string ForegroundLightHex = "#E0E0E0";

    public static string ContrastTextHex(string backgroundHex)
    {
        if (!TryParseRgb(backgroundHex, out var r, out var g, out var b))
            return ForegroundHex;

        var luminance = (0.299 * r + 0.587 * g + 0.114 * b) / 255;
        return luminance > 0.55 ? ForegroundHex : ForegroundLightHex;
    }

    private static bool TryParseRgb(string hex, out int r, out int g, out int b)
    {
        r = g = b = 0;
        if (string.IsNullOrWhiteSpace(hex))
            return false;

        hex = hex.TrimStart('#');
        if (hex.Length == 8)
            hex = hex[2..];

        if (hex.Length != 6)
            return false;

        try
        {
            r = Convert.ToInt32(hex[..2], 16);
            g = Convert.ToInt32(hex.Substring(2, 2), 16);
            b = Convert.ToInt32(hex.Substring(4, 2), 16);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
