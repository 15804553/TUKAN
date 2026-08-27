using SKRYBEK.Core.Enums;
using SKRYBEK.Core.Models;
using SKRYBEK.Core.Rules;

namespace SKRYBEK.App.Tests;

public sealed class PodzialBojowyRulesTests
{
    private static Samochod Podstawowy(int id, string nazwa = "GCBA") => new()
    {
        Id = id,
        Nazwa = nazwa,
        Typ = TypSamochodu.Podstawowy,
        LiczbaPozycji = 6
    };

    private static Samochod Dodatkowy(int id, string nazwa = "SLRt") => new()
    {
        Id = id,
        Nazwa = nazwa,
        Typ = TypSamochodu.Dodatkowy,
        LiczbaPozycji = 4
    };

    private static PozycjaSamochodu Miejsce(int samochodId, int pozycja, int funkcjonariuszId, string nazwisko = "Kowalski")
        => new()
        {
            SamochodId = samochodId,
            Pozycja = pozycja,
            FunkcjonariuszId = funkcjonariuszId,
            Nazwisko = nazwisko
        };

    [Fact]
    public void Konflikt_TejSamejOsobyNaDwochMiejscachTegoSamegoPojazduPodstawowego()
    {
        var samochody = new[] { Podstawowy(1) };
        var obsadaJednegoMiejsca = new[] { Miejsce(1, 1, 10) };
        var obsadaDuplikatu = new[]
        {
            Miejsce(1, 1, 10),
            Miejsce(1, 3, 10)
        };

        // Próba wpisania tej samej osoby na inne miejsce tego samego pojazdu.
        Assert.True(PodzialBojowyRules.CzyKonfliktPodstawowy(obsadaJednegoMiejsca, samochody, 10, 1, 3));
        // Ponowny wybór na już zajętym przez nią miejscu — bez konfliktu.
        Assert.False(PodzialBojowyRules.CzyKonfliktPodstawowy(obsadaJednegoMiejsca, samochody, 10, 1, 1));

        var komunikat = PodzialBojowyRules.ZnajdzKomunikatDuplikatuNaPodstawowych(obsadaDuplikatu, samochody);
        Assert.NotNull(komunikat);
        Assert.Contains("tym samym pojeździe podstawowym", komunikat);
    }

    [Fact]
    public void Konflikt_TejSamejOsobyNaDwochRoznychPojazdachPodstawowych()
    {
        var samochody = new[] { Podstawowy(1, "GCBA"), Podstawowy(2, "GBA") };
        var podzial = new[]
        {
            Miejsce(1, 1, 10),
            Miejsce(2, 2, 10)
        };

        Assert.True(PodzialBojowyRules.CzyKonfliktPodstawowy(podzial, samochody, 10, 2, 2));

        var komunikat = PodzialBojowyRules.ZnajdzKomunikatDuplikatuNaPodstawowych(podzial, samochody);
        Assert.NotNull(komunikat);
        Assert.Contains("więcej niż jednego pojazdu podstawowego", komunikat);
    }

    [Fact]
    public void BrakKonfliktu_GdyOsobaNaPojedzieDodatkowym()
    {
        var samochody = new[] { Podstawowy(1), Dodatkowy(2) };
        var podzial = new[]
        {
            Miejsce(2, 1, 10),
            Miejsce(1, 1, 10)
        };

        // Osoba jest na dodatkowym — przypisanie na podstawowy jest dozwolone.
        Assert.False(PodzialBojowyRules.CzyKonfliktPodstawowy(podzial, samochody, 10, 1, 1));
        Assert.Null(PodzialBojowyRules.ZnajdzKomunikatDuplikatuNaPodstawowych(podzial, samochody));
    }

    [Fact]
    public void BrakKonfliktu_GdyDocelowyPojazdNieJestPodstawowy()
    {
        var samochody = new[] { Podstawowy(1), Dodatkowy(2) };
        var podzial = new[] { Miejsce(1, 1, 10) };

        Assert.False(PodzialBojowyRules.CzyKonfliktPodstawowy(podzial, samochody, 10, 2, 1));
    }
}
