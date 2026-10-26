namespace SKRYBEK.Core.Rules;

/// <summary>Łączenie treści notatki kalendarza z działem Uwagi rozkazu dziennego.</summary>
public static class RozkazUwagiRules
{
    public static string DopiszNotatke(string? istniejaceUwagi, string trescNotatki)
    {
        var notatka = (trescNotatki ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(notatka))
            return (istniejaceUwagi ?? string.Empty).TrimEnd();

        var uwagi = (istniejaceUwagi ?? string.Empty).TrimEnd();
        if (string.IsNullOrWhiteSpace(uwagi))
            return notatka;

        if (ZawieraNotatke(uwagi, notatka))
            return uwagi;

        return uwagi + Environment.NewLine + notatka;
    }

    public static bool ZawieraNotatke(string uwagi, string trescNotatki)
    {
        var notatka = Normalize(trescNotatki);
        if (string.IsNullOrWhiteSpace(notatka))
            return true;

        var uwagiNorm = Normalize(uwagi);
        if (uwagiNorm.Equals(notatka, StringComparison.Ordinal))
            return true;

        return uwagiNorm.StartsWith(notatka + "\n", StringComparison.Ordinal)
            || uwagiNorm.EndsWith("\n" + notatka, StringComparison.Ordinal)
            || uwagiNorm.Contains("\n" + notatka + "\n", StringComparison.Ordinal);
    }

    private static string Normalize(string? text) =>
        (text ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n').Trim();
}
