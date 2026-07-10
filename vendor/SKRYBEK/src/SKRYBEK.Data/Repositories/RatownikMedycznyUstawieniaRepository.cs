using System.Text.Json;
using SKRYBEK.Core.Models;
using SKRYBEK.Core.Rules;

namespace SKRYBEK.Data.Repositories;

public sealed class RatownikMedycznyUstawieniaRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly UstawieniaRepository _ustawienia;
    private readonly SamochodyRepository _samochody;

    public RatownikMedycznyUstawieniaRepository(
        UstawieniaRepository ustawienia,
        SamochodyRepository samochody)
    {
        _ustawienia = ustawienia;
        _samochody = samochody;
    }

    public async Task<List<RatownikMedycznyPozycjaUstawienie>> GetDlaZmianyAsync(int zmianaId)
    {
        if (zmianaId is < 1 or > 3)
            return [];

        var samochody = await _samochody.GetAktywneAsync();
        var klucz = RatownikMedycznyUstawieniaKlucze.DlaZmiany(zmianaId);
        var json = await _ustawienia.GetAsync(klucz);
        if (!string.IsNullOrWhiteSpace(json))
        {
            try
            {
                var zapisane = JsonSerializer.Deserialize<List<RatownikMedycznyPozycjaUstawienie>>(json, JsonOptions);
                if (zapisane is { Count: > 0 })
                    return Normalizuj(zapisane, samochody);
            }
            catch
            {
                // Uszkodzony JSON — zwróć domyślne.
            }
        }

        return RatownikMedycznyUstawieniaDomyslne.Utworz(samochody);
    }

    public async Task SaveDlaZmianyAsync(int zmianaId, IReadOnlyList<RatownikMedycznyPozycjaUstawienie> ustawienia)
    {
        if (zmianaId is < 1 or > 3)
            throw new ArgumentOutOfRangeException(nameof(zmianaId), "Ustawienia dostępne tylko dla zmian 1–3.");

        var samochody = await _samochody.GetAktywneAsync();
        var klucz = RatownikMedycznyUstawieniaKlucze.DlaZmiany(zmianaId);
        var json = JsonSerializer.Serialize(Normalizuj(ustawienia.ToList(), samochody), JsonOptions);
        await _ustawienia.SetAsync(klucz, json);
    }

    public async Task EnsureDefaultsAsync()
    {
        var samochody = await _samochody.GetAktywneAsync();
        for (var zmiana = 1; zmiana <= 3; zmiana++)
        {
            var klucz = RatownikMedycznyUstawieniaKlucze.DlaZmiany(zmiana);
            var istniejace = await _ustawienia.GetAsync(klucz);
            if (!string.IsNullOrWhiteSpace(istniejace))
                continue;

            var domyslne = RatownikMedycznyUstawieniaDomyslne.Utworz(samochody);
            await SaveDlaZmianyAsync(zmiana, domyslne);
        }
    }

    private static List<RatownikMedycznyPozycjaUstawienie> Normalizuj(
        List<RatownikMedycznyPozycjaUstawienie> lista,
        IReadOnlyList<Samochod> samochody)
    {
        var poKolejnosci = samochody.ToDictionary(s => s.Kolejnosc);
        var wynik = new List<RatownikMedycznyPozycjaUstawienie>();
        foreach (var pozycja in new[] { 1, 2 })
        {
            var wpis = lista.FirstOrDefault(u => u.RatownikPozycja == pozycja);
            var kolejnosc = Ogranicz(wpis?.SamochodKolejnosc ?? pozycja, 1, 2);
            poKolejnosci.TryGetValue(kolejnosc, out var samochod);
            var liczbaPozycji = samochod?.LiczbaPozycji ?? 6;

            wynik.Add(new RatownikMedycznyPozycjaUstawienie
            {
                RatownikPozycja = pozycja,
                SamochodKolejnosc = kolejnosc,
                PozycjaPojazdu = PozycjaSamochoduRules.NormalizujPozycjeRatownika(
                    wpis?.PozycjaPojazdu ?? RatownikMedycznyUstawieniaDomyslne.OstatniaPozycja(samochod),
                    liczbaPozycji)
            });
        }

        return wynik;
    }

    private static int Ogranicz(int wartosc, int min, int max) =>
        wartosc < min ? min : wartosc > max ? max : wartosc;
}
