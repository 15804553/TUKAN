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
    /// <summary>Tło komórki Del w grafiku służb. Wartość <see cref="BrakWypelnienia"/> = bez własnego koloru (zachowuje tło komórki).</summary>
    public const string Delegacja = "Delegacja";
    /// <summary>Tło komórki S w grafiku służb. Wartość <see cref="BrakWypelnienia"/> = bez własnego koloru (zachowuje tło komórki).</summary>
    public const string Szkolenie = "Szkolenie";
    public const string EksportNaglowekStopkaTlo = "EksportNaglowekStopkaTlo";
    public const string EksportNaglowekStopkaCzcionka = "EksportNaglowekStopkaCzcionka";

    /// <summary>Sentinel w KolorHex — brak własnego koloru Del/S (tło jak WS).</summary>
    public const string BrakWypelnienia = "None";

    public const string KalendarzZmiana1 = "KalendarzZmiana1";
    public const string KalendarzZmiana2 = "KalendarzZmiana2";
    public const string KalendarzZmiana3 = "KalendarzZmiana3";

    public static IReadOnlyList<string> WszystkieRole =>
    [
        DowodcaZmiany,
        DowodcaSekcji,
        DowodcaZastepu,
        Nurek,
        Kierowca,
        Zwykly
    ];

    public static IReadOnlyList<string> KoloryEksportu =>
    [
        EksportNaglowekStopkaTlo,
        EksportNaglowekStopkaCzcionka
    ];

    public static IReadOnlyList<string> KalendarzKolory =>
    [
        KalendarzZmiana1,
        KalendarzZmiana2,
        KalendarzZmiana3
    ];

    /// <summary>Kolory opcjonalne (kolor albo brak wypełnienia) — Del, S.</summary>
    public static IReadOnlyList<string> KoloryOpcjonalneWypelnienia =>
    [
        Delegacja,
        Szkolenie
    ];

    public static IReadOnlyList<string> WszystkieKolory =>
        [
            DowodcaZmiany,
            DowodcaSekcji,
            DowodcaZastepu,
            Kierowca,
            Zwykly,
            WolnaSluzba,
            Nurek,
            NurekCzcionka,
            Delegacja,
            Szkolenie,
            .. KalendarzKolory,
            .. KoloryEksportu
        ];

    public static IReadOnlyDictionary<string, string> DomyslneEtykiety =>
        new Dictionary<string, string>
        {
            { DowodcaZmiany, "Dowódca zmiany" },
            { DowodcaSekcji, "Dowódca sekcji" },
            { DowodcaZastepu, "Dowódca zastępu" },
            { Nurek, "Nurek" },
            { NurekCzcionka, "Nurek — obramowanie (imię i nazwisko)" },
            { Kierowca, "Kierowca (C/C+E/D)" },
            { Zwykly, "Zwykły strażak" },
            { WolnaSluzba, "D / WS — tło komórki" },
            { Delegacja, "Del — tło (Brak = bez zmiany tła: służba bez koloru, WS zostaje żółte)" },
            { Szkolenie, "S — tło (Brak = bez zmiany tła: służba bez koloru, WS zostaje żółte)" },
            { DzienSluzby, "Dzień służby — plan urlopów (legacy)" },
            { EksportNaglowekStopkaTlo, "Eksport — tło nagłówka i stopki" },
            { EksportNaglowekStopkaCzcionka, "Eksport — czcionka nagłówka i stopki" },
            { KalendarzZmiana1, "Zmiana I — dzień służby (plan urlopów / kalendarz)" },
            { KalendarzZmiana2, "Zmiana II — dzień służby (plan urlopów / kalendarz)" },
            { KalendarzZmiana3, "Zmiana III — dzień służby (plan urlopów / kalendarz)" }
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
            { NurekCzcionka, "#F80808" },
            { Delegacja, BrakWypelnienia },
            { Szkolenie, BrakWypelnienia }
        };

    /// <summary>Domyślne kolory eksportu Excel (nagłówek i stopka).</summary>
    public static IReadOnlyDictionary<string, string> DomyslneKoloryEksportu =>
        new Dictionary<string, string>
        {
            { EksportNaglowekStopkaTlo, "#BFBFBF" },
            { EksportNaglowekStopkaCzcionka, "#FFFFFF" }
        };

    /// <summary>Domyślne kolory zmian w kalendarzu (jak grafik nurkowy).</summary>
    public static IReadOnlyDictionary<string, string> DomyslneKoloryKalendarza =>
        new Dictionary<string, string>
        {
            { KalendarzZmiana1, GrafikNurkowyConstants.ColorZmiana1 },
            { KalendarzZmiana2, GrafikNurkowyConstants.ColorZmiana2 },
            { KalendarzZmiana3, GrafikNurkowyConstants.ColorZmiana3 }
        };

    public static string KalendarzKluczForZmiana(int zmianaId) => zmianaId switch
    {
        1 => KalendarzZmiana1,
        2 => KalendarzZmiana2,
        3 => KalendarzZmiana3,
        _ => KalendarzZmiana1
    };

    public static string GetDefaultKolorHex(string klucz)
    {
        if (DomyslneKolory.TryGetValue(klucz, out var rola))
            return rola;
        if (DomyslneKoloryWpisow.TryGetValue(klucz, out var wpis))
            return wpis;
        if (DomyslneKoloryEksportu.TryGetValue(klucz, out var eksport))
            return eksport;
        if (DomyslneKoloryKalendarza.TryGetValue(klucz, out var kalendarz))
            return kalendarz;
        return "#2D2D2D";
    }

    /// <summary>Czy wartość koloru oznacza brak wypełnienia tła komórki.</summary>
    public static bool IsBrakWypelnienia(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
            return true;

        var trimmed = hex.Trim();
        return trimmed.Equals(BrakWypelnienia, StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("Brak", StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("Transparent", StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("#00000000", StringComparison.OrdinalIgnoreCase);
    }

    public static string NormalizeKolorHex(string? hex, string klucz)
    {
        if (KoloryOpcjonalneWypelnienia.Contains(klucz) && IsBrakWypelnienia(hex))
            return BrakWypelnienia;

        if (string.IsNullOrWhiteSpace(hex))
            return GetDefaultKolorHex(klucz);

        var trimmed = hex.Trim();
        if (!trimmed.StartsWith('#'))
            trimmed = "#" + trimmed;

        return trimmed.Length is 7 or 9 ? trimmed.ToUpperInvariant() : GetDefaultKolorHex(klucz);
    }
}
