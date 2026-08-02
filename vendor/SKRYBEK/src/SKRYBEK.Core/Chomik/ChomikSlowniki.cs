namespace SKRYBEK.Core.Chomik;

/// <summary>
/// Identyfikatory rekordów w słownikach bazy CHOMIK (StanowiskaSlownik, TypyUprawnien).
/// </summary>
public static class ChomikSlowniki
{
    /// <summary>Stanowiska z CHOMIK uprawniające do miejsca 1.D w pojeździe.</summary>
    public static readonly HashSet<int> StanowiskaMiejsca1D =
    [
        9,  // Dowódca zastępu
        11, // Dowódca sekcji
        12, // Zastępca dowódcy zmiany
        13, // Dowódca zmiany
        14  // Dowódca zmiany
    ];

    /// <summary>Grupy stanowisk do mapowania kolorów ról w TUKAN.</summary>
    public static readonly HashSet<int> StanowiskaDowodcyZmiany  = [13, 14];
    public static readonly HashSet<int> StanowiskaDowodcySekcji  = [11, 12];
    public const int StanowiskoDowodcaZastepu = 9;

    /// <summary>
    /// Stanowiska z CHOMIK uprawniające do pełnienia funkcji Dowódcy zmiany w rozkazie.
    /// </summary>
    public static readonly HashSet<int> StanowiskaUprawnioneNaDowodceZmiany =
    [
        9,  // Dowódca zastępu
        11, // Dowódca sekcji
        12, // Zastępca dowódcy zmiany
        13, // Dowódca zmiany
        14  // Dowódca zmiany
    ];

    public static bool CzyMozeNaMiejsce1DPojazdu(int stanowiskoId) =>
        StanowiskaMiejsca1D.Contains(stanowiskoId);

    public const int UprawnienieKierowcaKatC  = 2;
    public const int UprawnienieKierowcaKatCE = 3;
    public const int UprawnienieNurek          = 9;
    public const int UprawnienieKPP            = 10;

    /// <summary>ID uprawnienia sonarzysty w CHOMIK. Weryfikować w bazie TypyUprawnien.</summary>
    public const int UprawnienieSonarzysta = 11;

    public static string FormatUprawnienie(string nazwa, string? podtyp)
        => string.IsNullOrWhiteSpace(podtyp) ? nazwa.Trim() : $"{nazwa.Trim()} {podtyp.Trim()}";
}
