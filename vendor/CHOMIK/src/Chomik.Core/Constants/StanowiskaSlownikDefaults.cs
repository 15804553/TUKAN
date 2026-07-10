using Chomik.Core.Slowniki;

namespace Chomik.Core.Constants;

/// <summary>
/// Stanowiska służbowe w kolejności rosnących Id (1–14) w tabeli StanowiskaSlownik.
/// Preferowany plik: <see cref="SlownikTextFiles.StanowiskaFileName"/> (jedna pozycja na wiersz).
/// </summary>
public static class StanowiskaSlownikDefaults
{
    private static readonly IReadOnlyList<string> EmbeddedNazwyPoKolei =
    [
        "Młodszy ratownik-kierowca",   // 1
        "Ratownik",                    // 2
        "Młodszy ratownik specjalista",// 3
        "Młodszy operator sprzętu",    // 4
        "Starszy ratownik",            // 5
        "Starszy ratownik-kierowca",   // 6
        "Operator sprzętu",            // 7
        "Starszy operator sprzętu",    // 8
        "Dowódca zastępu",             // 9
        "Dowódca zastępu",             // 10
        "Ratownik specjalista",        // 11
        "Dowódca sekcji",              // 12
        "Zastępca dowódcy zmiany",     // 13
        "Dowódca zmiany"               // 14
    ];

    public static IReadOnlyList<string> NazwyPoKolei
    {
        get
        {
            var fromFile = SlownikTextFiles.ReadStanowiska();
            return fromFile.Count > 0 ? fromFile : EmbeddedNazwyPoKolei;
        }
    }
}
