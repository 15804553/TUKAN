using SKRYBEK.Core.Chomik;
using SKRYBEK.Core.Enums;
using SKRYBEK.Core.Models;

namespace SKRYBEK.Core.Rules;

public static class StanowiskoSluzbyRules
{
    /// <summary>
    /// Sprawdza czy osoba spełnia wymagania danego stanowiska służby:
    /// <list type="bullet">
    ///   <item>DowodcaZmiany — stanowisko dowódcze albo uprawnienie „Dowodzenie przy akcji”</item>
    ///   <item>Garazomistrz — uprawnienia kierowcy kat. C lub C+E</item>
    ///   <item>Bosman, DowodcaDzialanRatowniczychSGRWN — uprawnienia nurka</item>
    ///   <item>Sonarzysta — uprawnienie sonarzysty</item>
    ///   <item>Pozostałe — brak ograniczeń (każda osoba)</item>
    /// </list>
    /// </summary>
    public static bool CzyOsobaDozwolonaNaStanowisko(Funkcjonariusz osoba, StanowiskoSluzby stanowisko)
        => stanowisko switch
        {
            StanowiskoSluzby.DowodcaZmiany =>
                ChomikSlowniki.StanowiskaUprawnioneNaDowodceZmiany.Contains(osoba.StanowiskoId)
                || osoba.MaUprawnienieDowodzeniePrzyAkcji,
            StanowiskoSluzby.Garazomistrz =>
                osoba.MaUprawnieniaKierowca,
            StanowiskoSluzby.Bosman =>
                osoba.MaUprawnieniaNumek,
            StanowiskoSluzby.DowodcaDzialanRatowniczychSGRWN =>
                osoba.MaUprawnieniaNumek,
            StanowiskoSluzby.Sonarzysta =>
                osoba.IdUprawnien.Contains(ChomikSlowniki.UprawnienieSonarzysta),
            _ => true
        };

    public static string OpisWymagania(StanowiskoSluzby stanowisko) => stanowisko switch
    {
        StanowiskoSluzby.DowodcaZmiany =>
            "Dowódca zmiany — wymagane stanowisko dowódcze albo uprawnienie „Dowodzenie przy akcji”.",
        StanowiskoSluzby.Garazomistrz =>
            "Garażomistrz — wymagane uprawnienia kierowcy (kat. C lub C+E).",
        StanowiskoSluzby.Bosman =>
            "Bosman — wymagane uprawnienia nurka.",
        StanowiskoSluzby.DowodcaDzialanRatowniczychSGRWN =>
            "Dowódca działań ratowniczych SGRW-N — wymagane uprawnienia nurka.",
        StanowiskoSluzby.Sonarzysta =>
            "Sonarzysta \u2014 wymagane uprawnienie sonarzysty.",
        _ => string.Empty
    };

    public static bool MaWymagania(StanowiskoSluzby stanowisko) => stanowisko switch
    {
        StanowiskoSluzby.DowodcaZmiany => true,
        StanowiskoSluzby.Garazomistrz => true,
        StanowiskoSluzby.Bosman => true,
        StanowiskoSluzby.DowodcaDzialanRatowniczychSGRWN => true,
        StanowiskoSluzby.Sonarzysta => true,
        _ => false
    };

    /// <summary>
    /// Dowódca zmiany i dyżurny PA JRG nie mogą pełnić innych funkcji w dziale Służba
    /// (z wyjątkiem pary PA + Dowódca działań ratowniczych SGRW-N).
    /// Dowódca zmiany może być na pojeździe (podstawowym lub dodatkowym);
    /// dyżurny PA — tylko na dodatkowym.
    /// </summary>
    public static bool CzyStanowiskoWylaczaInneWSluzbie(StanowiskoSluzby stanowisko)
        => stanowisko is StanowiskoSluzby.DowodcaZmiany or StanowiskoSluzby.DyzurnyPAJRG;

    /// <summary>
    /// Wyjątek od wyłączności: dyżurny PA JRG może jednocześnie być
    /// dowódcą działań ratowniczych SGRW-N.
    /// </summary>
    public static bool CzyDozwolonyWyjatekWylacznosci(
        StanowiskoSluzby stanowiskoA,
        StanowiskoSluzby stanowiskoB)
    {
        var para = (stanowiskoA, stanowiskoB);
        return para is
            (StanowiskoSluzby.DyzurnyPAJRG, StanowiskoSluzby.DowodcaDzialanRatowniczychSGRWN) or
            (StanowiskoSluzby.DowodcaDzialanRatowniczychSGRWN, StanowiskoSluzby.DyzurnyPAJRG);
    }

    /// <summary>
    /// True, gdy przy czyszczeniu obsady po wpisaniu na stanowisko wyłączające
    /// nie wolno zdejmować wskazanego drugiego stanowiska (wyjątek PA + SGRW-N).
    /// </summary>
    public static bool CzyZachowacPrzyCzyszczeniuWylacznosci(
        StanowiskoSluzby stanowiskoWylaczajace,
        StanowiskoSluzby stanowiskoDoSprawdzenia)
        => CzyDozwolonyWyjatekWylacznosci(stanowiskoWylaczajace, stanowiskoDoSprawdzenia);

    public static string OpisKonfliktuWylacznosciWSluzbie(string nazwisko, string nazwaStanowiskaWylaczajacego)
        => $"{nazwisko} jest na stanowisku {nazwaStanowiskaWylaczajacego}.\n" +
           "Osoba na tym stanowisku nie może pełnić innych funkcji w dziale Służba.";

    /// <summary>
    /// Zwraca komunikat konfliktu, gdy osoba z DZ lub PA jest też na innym stanowisku służby
    /// (z wyjątkiem dozwolonej pary PA + Dowódca działań SGRW-N).
    /// </summary>
    public static string? ZnajdzKonfliktWylacznosciWSluzbie(IEnumerable<PozycjaSluzby> sluzba)
    {
        var obsadzone = sluzba.Where(s => s.FunkcjonariuszId.HasValue).ToList();
        foreach (var wylaczajace in obsadzone.Where(s => CzyStanowiskoWylaczaInneWSluzbie(s.Stanowisko)))
        {
            var inne = obsadzone.FirstOrDefault(s =>
                s.Stanowisko != wylaczajace.Stanowisko
                && s.FunkcjonariuszId == wylaczajace.FunkcjonariuszId
                && !CzyDozwolonyWyjatekWylacznosci(wylaczajace.Stanowisko, s.Stanowisko));

            if (inne is null)
                continue;

            return $"{wylaczajace.Nazwisko} jest na stanowisku {wylaczajace.NazwaStanowiska} " +
                   $"i nie może pełnić innych funkcji w dziale Służba " +
                   $"(jest też na stanowisku {inne.NazwaStanowiska}).";
        }

        return null;
    }
}
