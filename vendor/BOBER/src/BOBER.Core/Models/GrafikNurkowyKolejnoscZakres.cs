namespace BOBER.Core.Models;

/// <summary>
/// Zakres sortowania listy grafiku nurkowego.
/// Ustawienie jest wspólne dla wszystkich zmian.
/// </summary>
public enum GrafikNurkowyKolejnoscZakres
{
    /// <summary>Każda zmiana osobno: kierownicy prac podwodnych na górze tej zmiany, reszta poniżej.</summary>
    WObrebieZmian = 0,

    /// <summary>Jedna lista dla wszystkich zmian.</summary>
    WObrebieWszystkichZmian = 1
}
