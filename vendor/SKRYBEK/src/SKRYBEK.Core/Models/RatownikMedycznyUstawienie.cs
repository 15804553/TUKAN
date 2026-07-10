using SKRYBEK.Core.Rules;

namespace SKRYBEK.Core.Models;

/// <summary>
/// Konfiguracja źródła osoby dla jednej pozycji dyżurnego ratownika medycznego.
/// </summary>
public sealed class RatownikMedycznyPozycjaUstawienie
{
    /// <summary>Pozycja ratownika w rozkazie (1 lub 2).</summary>
    public int RatownikPozycja { get; set; }

    /// <summary>Kolejność pojazdu (1 = pierwszy samochód, 2 = drugi samochód).</summary>
    public int SamochodKolejnosc { get; set; }

    /// <summary>Numer pozycji na pojeździe, z której pobierane jest nazwisko.</summary>
    public int PozycjaPojazdu { get; set; }
}

public static class RatownikMedycznyUstawieniaKlucze
{
    public static string DlaZmiany(int zmianaId) => $"RatownikMedyczny.Zmiana{zmianaId}";
}

public static class RatownikMedycznyUstawieniaDomyslne
{
    public static List<RatownikMedycznyPozycjaUstawienie> Utworz(IReadOnlyList<Samochod> samochody)
    {
        var poKolejnosci = samochody.ToDictionary(s => s.Kolejnosc);
        poKolejnosci.TryGetValue(1, out var samochod1);
        poKolejnosci.TryGetValue(2, out var samochod2);

        return
        [
            new RatownikMedycznyPozycjaUstawienie
            {
                RatownikPozycja = 1,
                SamochodKolejnosc = 1,
                PozycjaPojazdu = OstatniaPozycja(samochod1)
            },
            new RatownikMedycznyPozycjaUstawienie
            {
                RatownikPozycja = 2,
                SamochodKolejnosc = 2,
                PozycjaPojazdu = OstatniaPozycja(samochod2)
            }
        ];
    }

    public static int OstatniaPozycja(Samochod? samochod)
    {
        var liczba = samochod is { LiczbaPozycji: > 0 } ? samochod.LiczbaPozycji : 6;
        return PozycjaSamochoduRules.DomyslnaPozycjaRatownika(liczba);
    }
}
