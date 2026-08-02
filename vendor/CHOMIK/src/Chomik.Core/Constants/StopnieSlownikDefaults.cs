namespace Chomik.Core.Constants;

/// <summary>
/// Stopnie służbowe w kolejności rosnących Id — seed nowej bazy.
/// Bieżąca edycja: Ustawienia (DCA).
/// </summary>
public static class StopnieSlownikDefaults
{
    public static IReadOnlyList<string> NazwyPoKolei { get; } =
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
}
