using SKRYBEK.Core.Models;

namespace SKRYBEK.Core.Rules;

public static class PozycjaSamochoduRules
{
    public static string OznaczeniePozycji(int pozycja) => pozycja switch
    {
        1 => "D",
        2 => "K",
        _ => string.Empty
    };

    public static string EtykietaPozycji(int pozycja)
    {
        var ozn = OznaczeniePozycji(pozycja);
        return string.IsNullOrEmpty(ozn) ? $"{pozycja}." : $"{pozycja}.{ozn}";
    }

    public static bool CzyOsobaDozwolonaNaPozycji(Funkcjonariusz osoba, int pozycja) => pozycja switch
    {
        1 => osoba.CzyMozeNaMiejsce1DPojazdu,
        2 => osoba.MaUprawnieniaKierowca,
        _ => true
    };

    /// <summary>
    /// Sprawdza, czy dana osoba posiada wszystkie kursy wymagane na pojeździe.
    /// </summary>
    public static bool CzyOsobaSpelniaWymaganiaPojazdu(Funkcjonariusz osoba, Samochod samochod) =>
        samochod.WymaganeUprawnieniaIds.All(id => osoba.IdUprawnien.Contains(id));

    /// <summary>
    /// Sprawdza, czy obsada pojazdu łącznie posiada wszystkie wymagane kursy.
    /// Wystarczy, że kurs ma dowolna osoba z obsady — pozostali nie muszą go mieć.
    /// Pojazdy bez ustawionych wymagań zawsze przechodzą walidację.
    /// </summary>
    public static bool CzyObsadaSpelniaWymaganiaPojazdu(IEnumerable<Funkcjonariusz> obsada, Samochod samochod) =>
        !samochod.CzyWymagaKursow ||
        samochod.WymaganeUprawnieniaIds.All(id => obsada.Any(o => o.IdUprawnien.Contains(id)));

    public static bool CzyOsobaDozwolonaNaPozycjiIPojezdzie(
        Funkcjonariusz osoba,
        int pozycja,
        Samochod samochod,
        IEnumerable<Funkcjonariusz>? inniNaPojezdzie = null) =>
        CzyOsobaDozwolonaNaPozycji(osoba, pozycja) &&
        (!samochod.CzyWymagaKursow ||
         CzyObsadaSpelniaWymaganiaPojazdu((inniNaPojezdzie ?? []).Append(osoba), samochod));

    /// <summary>
    /// Zwraca true gdy osoba spełnia wymagania pozycji i posiada wszystkie kursy pojazdu — do grupy „Zalecani”.
    /// </summary>
    public static bool CzyOsobaMaSugerowaneKwalifikacje(Funkcjonariusz osoba, int pozycja, Samochod samochod) =>
        CzyOsobaDozwolonaNaPozycji(osoba, pozycja) &&
        CzyOsobaSpelniaWymaganiaPojazdu(osoba, samochod);

    public static IEnumerable<int> BrakujaceWymaganiaPojazdu(Funkcjonariusz osoba, Samochod samochod) =>
        samochod.WymaganeUprawnieniaIds.Where(id => !osoba.IdUprawnien.Contains(id));

    public static IEnumerable<int> BrakujaceWymaganiaObsadyPojazdu(
        IEnumerable<Funkcjonariusz> obsada,
        Samochod samochod) =>
        samochod.CzyWymagaKursow
            ? samochod.WymaganeUprawnieniaIds.Where(id => !obsada.Any(o => o.IdUprawnien.Contains(id)))
            : [];

    public static string OpisBrakujacychWymaganPojazdu(
        Funkcjonariusz osoba,
        Samochod samochod,
        Func<int, string?>? nazwaTypu = null)
    {
        var brakujace = BrakujaceWymaganiaPojazdu(osoba, samochod).ToList();
        if (brakujace.Count == 0)
            return string.Empty;

        var nazwy = brakujace
            .Select(id => nazwaTypu?.Invoke(id) ?? $"uprawnienie #{id}")
            .ToList();

        return $"Brak wymaganych uprawnień/kursów do obsady pojazdu „{samochod.Nazwa}”: {string.Join(", ", nazwy)}.";
    }

    public static string OpisBrakujacychWymaganObsadyPojazdu(
        IEnumerable<Funkcjonariusz> obsada,
        Samochod samochod,
        Func<int, string?>? nazwaTypu = null)
    {
        var brakujace = BrakujaceWymaganiaObsadyPojazdu(obsada, samochod).ToList();
        if (brakujace.Count == 0)
            return string.Empty;

        var nazwy = brakujace
            .Select(id => nazwaTypu?.Invoke(id) ?? $"uprawnienie #{id}")
            .ToList();

        return $"Obsada pojazdu „{samochod.Nazwa}” nie posiada wymaganych uprawnień/kursów: {string.Join(", ", nazwy)}. " +
               "Wystarczy, że kurs ma dowolna osoba z obsady pojazdu.";
    }

    public static string OpisWymagania(int pozycja) => pozycja switch
    {
        1 => "Miejsce 1.D — dowódca zmiany, zastępca dowódcy zmiany, dowódca zastępu lub dowódca sekcji.",
        2 => "Miejsce 2.K — tylko kierowca kat. C lub C+E.",
        _ => string.Empty
    };
}
