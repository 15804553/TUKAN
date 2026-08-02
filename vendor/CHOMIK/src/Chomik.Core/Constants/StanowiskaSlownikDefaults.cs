namespace Chomik.Core.Constants;

/// <summary>
/// Stanowiska służbowe w kolejności rosnących Id — seed nowej bazy.
/// Bieżąca edycja: Ustawienia (DCA). Nowe pozycje dopisuj przez UI (zachowuje Id).
/// </summary>
public static class StanowiskaSlownikDefaults
{
    public static IReadOnlyList<string> NazwyPoKolei { get; } =
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
        "Ratownik specjalista",        // 10
        "Dowódca sekcji",              // 11
        "Zastępca dowódcy zmiany",     // 12
        "Dowódca zmiany",              // 13
        "Ratownik kierowca"            // 14
    ];
}
