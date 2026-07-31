using System.Text;
using BOBER.Core.Models;

namespace BOBER.Core.Constants;

public static class RoleClassifier
{
    public static bool IsNurek(Funkcjonariusz f) =>
        f.MaUprawnieniaKPP || f.MaUprawnieniaNumek;

    /// <summary>
    /// Zwraca true dla dowódcy zastępu, dowódcy sekcji, dowódcy zmiany oraz zastępcy dowódcy zmiany.
    /// </summary>
    public static bool IsDowodca(Funkcjonariusz f)
    {
        var role = DetermineBackgroundRole(f);
        return role is RoleKeys.DowodcaZmiany or RoleKeys.DowodcaSekcji or RoleKeys.DowodcaZastepu;
    }

    /// <summary>
    /// Kierowca: uprawnienia kat. C / C+E albo stanowisko zawierające „kierowca”.
    /// </summary>
    public static bool IsKierowca(Funkcjonariusz f) =>
        f.MaUprawnieniaKierowca || MatchesStanowisko(f, "kierowca");

    /// <summary>
    /// Piktogramy ról do eksportu (D = dowódca, N = nurek, K = kierowca), kolejność stała.
    /// </summary>
    public static string FormatExportRoleMarks(Funkcjonariusz f)
    {
        var sb = new StringBuilder(3);
        if (IsDowodca(f)) sb.Append('D');
        if (IsNurek(f)) sb.Append('N');
        if (IsKierowca(f)) sb.Append('K');
        return sb.ToString();
    }

    /// <summary>
    /// Określa rolę funkcjonariusza na podstawie stanowiska.
    /// Priorytet: dowódca zmiany → dowódca sekcji / z-ca dcy zmiany → dowódca zastępu → kierowca → zwykły.
    /// Status nureka nie wpływa na rolę — jest obsługiwany wyłącznie przez kolor obramowania (NurekCzcionka).
    /// </summary>
    public static string DetermineRole(Funkcjonariusz f) => DetermineBackgroundRole(f);

    /// <summary>
    /// Zwraca rolę używaną do koloru tła wiersza oraz kolejności sortowania.
    /// Nurek nie ma własnej roli — otrzymuje kolor tła i sort wynikający ze stanowiska,
    /// a wyróżnienie obramowaniem jest ustawiane oddzielnie jako NurekCzcionka.
    /// </summary>
    public static string DetermineBackgroundRole(Funkcjonariusz f)
    {
        if (MatchesStanowisko(f, "dowódca zmiany", "dowodca zmiany"))
            return RoleKeys.DowodcaZmiany;
        if (MatchesStanowisko(f, "dowódca sekcji", "dowodca sekcji", "zastępca dowódcy zmiany", "zastepca dowodcy zmiany"))
            return RoleKeys.DowodcaSekcji;
        if (MatchesStanowisko(f, "dowódca zastępu", "dowodca zastepu", "dowódca zastępcy", "dowodca zastepcy"))
            return RoleKeys.DowodcaZastepu;
        if (f.MaUprawnieniaKierowca || MatchesStanowisko(f, "kierowca"))
            return RoleKeys.Kierowca;
        return RoleKeys.Zwykly;
    }

    /// <summary>
    /// Domyślna kolejność sortowania wierszy w grafiku.
    /// Nurek nie ma własnej pozycji — jest sortowany według stanowiska (DetermineRole).
    /// </summary>
    public static int GetDefaultSortOrder(string roleKey) => roleKey switch
    {
        RoleKeys.DowodcaZmiany => 0,
        RoleKeys.DowodcaSekcji => 1,
        RoleKeys.DowodcaZastepu => 2,
        RoleKeys.Kierowca => 3,
        RoleKeys.Zwykly => 4,
        _ => 99
    };

    private static bool MatchesStanowisko(Funkcjonariusz f, params string[] needles)
    {
        if (string.IsNullOrWhiteSpace(f.Stanowisko))
            return false;

        var lower = f.Stanowisko.ToLowerInvariant();
        foreach (var needle in needles)
        {
            if (lower.Contains(needle))
                return true;
        }

        return false;
    }
}

