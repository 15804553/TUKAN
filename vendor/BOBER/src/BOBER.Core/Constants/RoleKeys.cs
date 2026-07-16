namespace BOBER.Core.Constants;

/// <summary>
/// Klucze identyfikujące role/funkcje funkcjonariuszy używane do mapowania kolorów wierszy w grafiku.
/// </summary>
public static class RoleKeys
{
    public const string DowodcaZmiany = "DowodcaZmiany";
    public const string DowodcaZastepu = "DowodcaZastepu";
    public const string DowodcaSekcji = "DowodcaSekcji";
    public const string Nurek = "Nurek";
    public const string NurekCzcionka = "NurekCzcionka";
    /// <summary>Legacy — usuwany migracją; KPP scalone z Nurkiem.</summary>
    public const string KierownikPracPodwodnych = "KierownikPracPodwodnych";
    public const string Kierowca = "Kierowca";
    public const string Zwykly = "Zwykly";
    /// <summary>Legacy — usuwany migracją; D i WS używają <see cref="WolnaSluzba"/>.</summary>
    public const string Dyzur = "Dyzur";
    public const string WolnaSluzba = "WolnaSluzba";
    public const string DzienSluzby = "DzienSluzby";
    public const string EksportNaglowekStopkaTlo = "EksportNaglowekStopkaTlo";
    public const string EksportNaglowekStopkaCzcionka = "EksportNaglowekStopkaCzcionka";

    public static IReadOnlyList<string> WszystkieRole =>
    [
        DowodcaZmiany,
        DowodcaSekcji,
        DowodcaZastepu,
        Nurek,
        Kierowca,
        Zwykly
    ];

    public static IReadOnlyList<string> WszystkieKolory =>
        [
            .. WszystkieRole,
            WolnaSluzba,
            DzienSluzby,
            NurekCzcionka,
            EksportNaglowekStopkaTlo,
            EksportNaglowekStopkaCzcionka
        ];

    public static IReadOnlyDictionary<string, string> DomyslneEtykiety =>
        new Dictionary<string, string>
        {
            { DowodcaZmiany, "Dowódca zmiany" },
            { DowodcaSekcji, "Dowódca sekcji" },
            { DowodcaZastepu, "Dowódca zastępu" },
            { Nurek, "Nurek" },
            { NurekCzcionka, "Nurek — czcionka (imię i nazwisko)" },
            { Kierowca, "Kierowca (C/C+E/D)" },
            { Zwykly, "Zwykły strażak" },
            { WolnaSluzba, "D / WS — tło komórki" },
            { DzienSluzby, "Dzień służby — plan urlopów" },
            { EksportNaglowekStopkaTlo, "Eksport — tło nagłówka i stopki" },
            { EksportNaglowekStopkaCzcionka, "Eksport — czcionka nagłówka i stopki" }
        };

    /// <summary>Kolory tła wiersza w grafiku.</summary>
    public static IReadOnlyDictionary<string, string> DomyslneKolory =>
        new Dictionary<string, string>
        {
            { DowodcaZmiany, "#F79646" },
            { DowodcaSekcji, "#B8CCE4" },
            { DowodcaZastepu, "#92D050" },
            { Nurek, "#FFFFFF" },
            { Kierowca, "#BFBFBF" },
            { Zwykly, "#FFFFFF" }
        };

    /// <summary>Domyślne kolory wpisów w komórkach grafiku i czcionek ról.</summary>
    public static IReadOnlyDictionary<string, string> DomyslneKoloryWpisow =>
        new Dictionary<string, string>
        {
            { WolnaSluzba, "#6A5C00" },
            { DzienSluzby, "#FFD700" },
            { NurekCzcionka, "#F80808" }
        };

    /// <summary>Domyślne kolory eksportu Excel (nagłówek i stopka).</summary>
    public static IReadOnlyDictionary<string, string> DomyslneKoloryEksportu =>
        new Dictionary<string, string>
        {
            { EksportNaglowekStopkaTlo, "#BFBFBF" },
            { EksportNaglowekStopkaCzcionka, "#FFFFFF" }
        };

    public static string GetDefaultKolorHex(string klucz)
    {
        if (DomyslneKolory.TryGetValue(klucz, out var rola))
            return rola;
        if (DomyslneKoloryWpisow.TryGetValue(klucz, out var wpis))
            return wpis;
        if (DomyslneKoloryEksportu.TryGetValue(klucz, out var eksport))
            return eksport;
        return "#2D2D2D";
    }
}
