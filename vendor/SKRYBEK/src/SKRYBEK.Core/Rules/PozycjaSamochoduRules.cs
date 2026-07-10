using SKRYBEK.Core.Models;

namespace SKRYBEK.Core.Rules;

public static class PozycjaSamochoduRules
{
    public const int PozycjaDowodca = 1;
    public const int PozycjaKierowca = 2;
    public const string OznaczenieRatownika = "R";

    /// <summary>Pozycje 1.D i 2.K są zarezerwowane — ratownik medyczny tylko od pozycji 3 wzwyż.</summary>
    public static bool CzyPozycjaDozwolonaDlaRatownika(int pozycja) =>
        pozycja > PozycjaKierowca;

    public static int DomyslnaPozycjaRatownika(int liczbaPozycjiPojazdu) =>
        liczbaPozycjiPojazdu > PozycjaKierowca
            ? liczbaPozycjiPojazdu
            : PozycjaKierowca + 1;

    public static int NormalizujPozycjeRatownika(int pozycja, int liczbaPozycjiPojazdu)
    {
        if (CzyPozycjaDozwolonaDlaRatownika(pozycja) && pozycja <= liczbaPozycjiPojazdu)
            return pozycja;

        return DomyslnaPozycjaRatownika(liczbaPozycjiPojazdu);
    }

    public static string OznaczeniePozycji(int pozycja) => pozycja switch
    {
        PozycjaDowodca => "D",
        PozycjaKierowca => "K",
        _ => string.Empty
    };

    /// <summary>Etykieta pozycji źródłowej ratownika medycznego (np. 6.R).</summary>
    public static string EtykietaPozycjiRatownika(int pozycja) =>
        $"{pozycja}.{OznaczenieRatownika}";

    public static string OznaczenieWyswietlane(int pozycja, bool czyPozycjaRatownika)
    {
        var dk = OznaczeniePozycji(pozycja);
        if (!string.IsNullOrEmpty(dk))
            return dk;

        return czyPozycjaRatownika ? OznaczenieRatownika : string.Empty;
    }

    public static string EtykietaPozycji(int pozycja)
    {
        var ozn = OznaczeniePozycji(pozycja);
        return string.IsNullOrEmpty(ozn) ? $"{pozycja}." : $"{pozycja}.{ozn}";
    }

    /// <summary>
    /// Obowiązkowe wymaganie pozycji: 1.D — dowódca, 2.K — prawo jazdy kat. C/C+E.
    /// </summary>
    public static bool CzyOsobaDozwolonaNaPozycji(Funkcjonariusz osoba, int pozycja) => pozycja switch
    {
        PozycjaDowodca => osoba.CzyMozeNaMiejsce1DPojazdu,
        PozycjaKierowca => osoba.MaUprawnieniaKierowca,
        _ => true
    };

    public static bool CzyPozycja1DSpelniona(Funkcjonariusz? osoba, int liczbaPozycjiPojazdu)
    {
        if (liczbaPozycjiPojazdu < PozycjaDowodca)
            return true;

        return osoba is not null && CzyOsobaDozwolonaNaPozycji(osoba, PozycjaDowodca);
    }

    public static bool CzyPozycja2KSpelniona(Funkcjonariusz? osoba, int liczbaPozycjiPojazdu)
    {
        if (liczbaPozycjiPojazdu < PozycjaKierowca)
            return true;

        return osoba is not null && CzyOsobaDozwolonaNaPozycji(osoba, PozycjaKierowca);
    }

    public static IEnumerable<Funkcjonariusz> OsobyNaPozycjach1D2K(
        IEnumerable<(int Pozycja, Funkcjonariusz? Osoba)> pozycje) =>
        pozycje
            .Where(p => p.Pozycja is PozycjaDowodca or PozycjaKierowca && p.Osoba is not null)
            .Select(p => p.Osoba!);

    /// <summary>
    /// Dodatkowe kursy/uprawnienia ustawione przez DCA JRG — tylko osoby na 1.D lub 2.K.
    /// </summary>
    public static bool CzyDodatkoweWymaganiaPojazduSpelnione(
        IEnumerable<Funkcjonariusz> osoby1D2K,
        Samochod samochod) =>
        !samochod.CzyWymagaKursow ||
        samochod.WymaganeUprawnieniaIds.All(id => osoby1D2K.Any(o => o.IdUprawnien.Contains(id)));

    /// <summary>
    /// Pełna ocena wymagań pojazdu: obowiązkowe 1.D/2.K oraz dodatkowe kursy DCA (wyłącznie 1.D/2.K).
    /// </summary>
    public static bool CzySpelniaWymaganiaPojazdu(
        IEnumerable<(int Pozycja, Funkcjonariusz? Osoba)> pozycje,
        Samochod samochod)
    {
        var lista = pozycje.ToList();
        var dowodca = lista.FirstOrDefault(p => p.Pozycja == PozycjaDowodca).Osoba;
        var kierowca = lista.FirstOrDefault(p => p.Pozycja == PozycjaKierowca).Osoba;
        var osoby1D2K = OsobyNaPozycjach1D2K(lista).ToList();

        return CzyPozycja1DSpelniona(dowodca, samochod.LiczbaPozycji)
            && CzyPozycja2KSpelniona(kierowca, samochod.LiczbaPozycji)
            && CzyDodatkoweWymaganiaPojazduSpelnione(osoby1D2K, samochod);
    }

    public static IEnumerable<int> BrakujaceDodatkoweWymaganiaPojazdu(
        IEnumerable<Funkcjonariusz> osoby1D2K,
        Samochod samochod) =>
        samochod.CzyWymagaKursow
            ? samochod.WymaganeUprawnieniaIds.Where(id => !osoby1D2K.Any(o => o.IdUprawnien.Contains(id)))
            : [];

    public static string OpisWymagania(int pozycja) => pozycja switch
    {
        PozycjaDowodca => "Miejsce 1.D — dowódca zmiany, zastępca dowódcy zmiany, dowódca zastępu lub dowódca sekcji.",
        PozycjaKierowca => "Miejsce 2.K — tylko kierowca z prawem jazdy kat. C lub C+E.",
        _ => string.Empty
    };

    public static string OpisObowiazkowychWymaganPojazdu(int liczbaPozycjiPojazdu)
    {
        var czesci = new List<string>();
        if (liczbaPozycjiPojazdu >= PozycjaDowodca)
            czesci.Add("1.D — dowódca zmiany, zastępca dowódcy zmiany, dowódca zastępu lub dowódca sekcji");
        if (liczbaPozycjiPojazdu >= PozycjaKierowca)
            czesci.Add("2.K — prawo jazdy kat. C lub C+E");
        return string.Join("; ", czesci);
    }

    public static string BudujTooltipWymaganPojazdu(
        Samochod samochod,
        IEnumerable<(int Pozycja, Funkcjonariusz? Osoba)> pozycje,
        Func<int, string?>? nazwaTypu = null)
    {
        var lista = pozycje.ToList();
        var dowodca = lista.FirstOrDefault(p => p.Pozycja == PozycjaDowodca).Osoba;
        var kierowca = lista.FirstOrDefault(p => p.Pozycja == PozycjaKierowca).Osoba;
        var osoby1D2K = OsobyNaPozycjach1D2K(lista).ToList();

        var linie = new List<string> { $"Wymagania pojazdu „{samochod.Nazwa}”:", string.Empty };

        if (samochod.LiczbaPozycji >= PozycjaDowodca)
        {
            linie.Add(CzyPozycja1DSpelniona(dowodca, samochod.LiczbaPozycji)
                ? "✓ 1.D — dowódca (spełnione)"
                : "✗ 1.D — dowódca zmiany, zastępca, zastępu lub sekcji (brak lub niespełnione)");
        }

        if (samochod.LiczbaPozycji >= PozycjaKierowca)
        {
            linie.Add(CzyPozycja2KSpelniona(kierowca, samochod.LiczbaPozycji)
                ? "✓ 2.K — prawo jazdy kat. C/C+E (spełnione)"
                : "✗ 2.K — prawo jazdy kat. C lub C+E (brak lub niespełnione)");
        }

        if (samochod.CzyWymagaKursow)
        {
            linie.Add(string.Empty);
            linie.Add("Dodatkowe uprawnienia/kursy (tylko 1.D lub 2.K, nie pozycje 3+):");

            foreach (var id in samochod.WymaganeUprawnieniaIds)
            {
                var nazwa = nazwaTypu?.Invoke(id) ?? $"uprawnienie #{id}";
                var ma = osoby1D2K.Any(o => o.IdUprawnien.Contains(id));
                linie.Add(ma ? $"✓ {nazwa}" : $"✗ {nazwa}");
            }
        }

        linie.Add(string.Empty);
        linie.Add(CzySpelniaWymaganiaPojazdu(lista, samochod)
            ? "Status: wszystkie wymagania spełnione."
            : "Status: brakuje wymaganych uprawnień.");

        return string.Join("\n", linie);
    }
}
