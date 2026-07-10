using Chomik.Core.Slowniki;

namespace Chomik.Core.Constants;

/// <summary>
/// Stopnie służbowe w kolejności rosnących Id (1–17) w tabeli StopnieSlownik.
/// Preferowany plik: <see cref="SlownikTextFiles.StopnieFileName"/> (jedna pozycja na wiersz).
/// </summary>
public static class StopnieSlownikDefaults
{
    private static readonly IReadOnlyList<string> EmbeddedNazwyPoKolei =
    [
        "str.",       // 1
        "st.str.",    // 2
        "sexc.",      // 3
        "st.sexc.",   // 4
        "mł.ogn.",    // 5
        "ogn.",       // 6
        "st.ogn.",    // 7
        "mł.asp.",    // 8
        "asp.",       // 9
        "st.asp.",    // 10
        "asp.sztab.", // 11
        "mł.kpt.",    // 12
        "kpt.",       // 13
        "st.kpt.",    // 14
        "mł.bryg.",   // 15
        "bryg.",      // 16
        "st.bryg."    // 17
    ];

    public static IReadOnlyList<string> NazwyPoKolei
    {
        get
        {
            var fromFile = SlownikTextFiles.ReadStopnie();
            return fromFile.Count > 0 ? fromFile : EmbeddedNazwyPoKolei;
        }
    }
}
