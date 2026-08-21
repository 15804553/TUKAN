namespace SKRYBEK.Core.Chomik;

/// <summary>
/// Identyfikatory rekordów w słownikach bazy CHOMIK (StanowiskaSlownik, TypyUprawnien).
/// Kolejność zgodna z aktualną bazą TUKAN (słownik stanowisk).
/// </summary>
public static class ChomikSlowniki
{
    /// <summary>Stanowiska z CHOMIK uprawniające do miejsca 1.D w pojeździe.</summary>
    public static readonly HashSet<int> StanowiskaMiejsca1D =
    [
        9,  // Dowódca zastępu
        11, // Dowódca sekcji
        12, // Zastępca dowódcy zmiany
        13  // Dowódca zmiany
    ];

    /// <summary>
    /// Grupy stanowisk do koloru kafelka — jak w grafiku BOBER, wyłącznie stanowiska dowódcze.
    /// Ratownik specjalista (10) nie jest stanowiskiem dowódczym.
    /// </summary>
    public static readonly HashSet<int> StanowiskaDowodcyZmiany = [13]; // Dowódca zmiany
    public static readonly HashSet<int> StanowiskaDowodcySekcji = [11, 12]; // Dowódca sekcji + zastępca dowódcy zmiany
    public const int StanowiskoDowodcaZastepu = 9;

    /// <summary>
    /// Stanowiska z CHOMIK uprawniające do pełnienia funkcji Dowódcy zmiany w rozkazie.
    /// </summary>
    public static readonly HashSet<int> StanowiskaUprawnioneNaDowodceZmiany =
    [
        9,  // Dowódca zastępu
        11, // Dowódca sekcji
        12, // Zastępca dowódcy zmiany
        13  // Dowódca zmiany
    ];

    public static bool CzyMozeNaMiejsce1DPojazdu(int stanowiskoId) =>
        StanowiskaMiejsca1D.Contains(stanowiskoId);

    /// <summary>
    /// Nazwa uprawnienia z TypyUprawnien — osoba z tym uprawnieniem jest traktowana jak dowódca
    /// (miejsce 1.D w pojeździe oraz combo Dowódca zmiany).
    /// </summary>
    public const string UprawnienieDowodzeniePrzyAkcjiNazwa = "Dowodzenie przy akcji";

    public static bool CzyUprawnienieDowodzeniePrzyAkcji(string? etykieta) =>
        !string.IsNullOrWhiteSpace(etykieta)
        && etykieta.Contains(UprawnienieDowodzeniePrzyAkcjiNazwa, StringComparison.OrdinalIgnoreCase);

    public const int UprawnienieKierowcaKatC  = 2;
    public const int UprawnienieKierowcaKatCE = 3;
    public const int UprawnienieNurek          = 9;
    public const int UprawnienieKPP            = 10;

    /// <summary>ID uprawnienia sonarzysty w CHOMIK. Weryfikować w bazie TypyUprawnien.</summary>
    public const int UprawnienieSonarzysta = 11;

    public static string FormatUprawnienie(string nazwa, string? podtyp)
        => string.IsNullOrWhiteSpace(podtyp) ? nazwa.Trim() : $"{nazwa.Trim()} {podtyp.Trim()}";
}
