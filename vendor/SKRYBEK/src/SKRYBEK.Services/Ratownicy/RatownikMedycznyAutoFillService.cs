using SKRYBEK.Core.Models;
using SKRYBEK.Core.Rules;

namespace SKRYBEK.Services.Ratownicy;

public sealed class RatownikMedycznyAutoFillService
{
    /// <summary>
    /// Uzupełnia ratowników medycznych na podstawie obsady pojazdów i ustawień zmiany.
    /// </summary>
    public void Zastosuj(
        IList<RatownikMedyczny> ratownicy,
        IReadOnlyList<PozycjaSamochodu> podzialBojowy,
        IReadOnlyList<Samochod> samochody,
        IReadOnlyList<RatownikMedycznyPozycjaUstawienie> ustawienia,
        IReadOnlyList<Funkcjonariusz> personel)
    {
        if (ratownicy.Count == 0 || ustawienia.Count == 0)
            return;

        var samochodPoKolejnosci = samochody.ToDictionary(s => s.Kolejnosc);
        var personelPoId = personel.ToDictionary(p => p.Id);

        foreach (var ustawienie in ustawienia)
        {
            if (!samochodPoKolejnosci.TryGetValue(ustawienie.SamochodKolejnosc, out var samochod))
                continue;

            var pozycjaPojazdu = podzialBojowy.FirstOrDefault(p =>
                p.SamochodId == samochod.Id && p.Pozycja == ustawienie.PozycjaPojazdu);

            var ratownik = ratownicy.FirstOrDefault(r => r.Pozycja == ustawienie.RatownikPozycja);

            if (ratownik is null)
            {
                ratownik = new RatownikMedyczny { Pozycja = ustawienie.RatownikPozycja };
                ratownicy.Add(ratownik);
            }

            if (pozycjaPojazdu?.FunkcjonariuszId is int fid &&
                personelPoId.TryGetValue(fid, out var osoba))
            {
                ratownik.FunkcjonariuszId = fid;
                ratownik.Nazwisko = osoba.StopienINazwisko;
                continue;
            }

            if (!string.IsNullOrWhiteSpace(pozycjaPojazdu?.Nazwisko))
            {
                ratownik.FunkcjonariuszId = pozycjaPojazdu.FunkcjonariuszId;
                ratownik.Nazwisko = pozycjaPojazdu.Nazwisko;
                continue;
            }

            ratownik.FunkcjonariuszId = null;
            ratownik.Nazwisko = string.Empty;
        }
    }

    /// <summary>
    /// Sprawdza, czy zmiana obsady na pojeździe wpływa na któregoś ratownika.
    /// </summary>
    public bool CzyWplywaNaRatownika(
        int samochodKolejnosc,
        int pozycjaPojazdu,
        IReadOnlyList<RatownikMedycznyPozycjaUstawienie> ustawienia) =>
        PozycjaSamochoduRules.CzyPozycjaDozwolonaDlaRatownika(pozycjaPojazdu) &&
        ustawienia.Any(u =>
            u.SamochodKolejnosc == samochodKolejnosc &&
            u.PozycjaPojazdu == pozycjaPojazdu);
}
