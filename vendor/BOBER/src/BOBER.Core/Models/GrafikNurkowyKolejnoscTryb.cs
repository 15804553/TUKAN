namespace BOBER.Core.Models;

/// <summary>
/// Kolejność osób na liście grafiku nurkowego.
/// Jedno ustawienie obowiązuje jednocześnie wszystkie zmiany.
/// </summary>
public enum GrafikNurkowyKolejnoscTryb
{
    /// <summary>Kierownicy prac podwodnych na górze, pozostałe osoby w dotychczasowej kolejności.</summary>
    KppNaGorze = 0,

    /// <summary>Kierownicy prac podwodnych, następnie nurkowie, na dole młodsi nurkowie.</summary>
    KppNurekMlodszy = 1
}
