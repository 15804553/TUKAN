using SKRYBEK.Core.Enums;
using SKRYBEK.Core.Models;
using SKRYBEK.Core.Rules;

namespace SKRYBEK.App.Tests;

public sealed class StanowiskoSluzbyRulesTests
{
    private static PozycjaSluzby Pozycja(StanowiskoSluzby stanowisko, int funkcjonariuszId, string nazwisko = "Kowalski")
        => new()
        {
            Stanowisko = stanowisko,
            FunkcjonariuszId = funkcjonariuszId,
            Nazwisko = nazwisko
        };

    [Fact]
    public void Wyjatek_PaIDowodcaDzialanSgrwn_SaDozwolone()
    {
        Assert.True(StanowiskoSluzbyRules.CzyDozwolonyWyjatekWylacznosci(
            StanowiskoSluzby.DyzurnyPAJRG,
            StanowiskoSluzby.DowodcaDzialanRatowniczychSGRWN));
        Assert.True(StanowiskoSluzbyRules.CzyDozwolonyWyjatekWylacznosci(
            StanowiskoSluzby.DowodcaDzialanRatowniczychSGRWN,
            StanowiskoSluzby.DyzurnyPAJRG));
    }

    [Fact]
    public void Wyjatek_PaIInneStanowisko_Niedozwolone()
    {
        Assert.False(StanowiskoSluzbyRules.CzyDozwolonyWyjatekWylacznosci(
            StanowiskoSluzby.DyzurnyPAJRG,
            StanowiskoSluzby.SzefZmiany));
        Assert.False(StanowiskoSluzbyRules.CzyDozwolonyWyjatekWylacznosci(
            StanowiskoSluzby.DyzurnyPAJRG,
            StanowiskoSluzby.DowodcaZmiany));
    }

    [Fact]
    public void Konflikt_PaZeSzefemZmiany_Wykrywany()
    {
        var sluzba = new[]
        {
            Pozycja(StanowiskoSluzby.DyzurnyPAJRG, 10),
            Pozycja(StanowiskoSluzby.SzefZmiany, 10)
        };

        var msg = StanowiskoSluzbyRules.ZnajdzKonfliktWylacznosciWSluzbie(sluzba);
        Assert.NotNull(msg);
        Assert.Contains("Dyżurny PA JRG", msg);
    }

    [Fact]
    public void Konflikt_PaZDowodcaDzialanSgrwn_Brak()
    {
        var sluzba = new[]
        {
            Pozycja(StanowiskoSluzby.DyzurnyPAJRG, 10),
            Pozycja(StanowiskoSluzby.DowodcaDzialanRatowniczychSGRWN, 10)
        };

        Assert.Null(StanowiskoSluzbyRules.ZnajdzKonfliktWylacznosciWSluzbie(sluzba));
    }

    [Fact]
    public void Konflikt_DowodcaZmianyZDowodcaDzialanSgrwn_Wykrywany()
    {
        var sluzba = new[]
        {
            Pozycja(StanowiskoSluzby.DowodcaZmiany, 10),
            Pozycja(StanowiskoSluzby.DowodcaDzialanRatowniczychSGRWN, 10)
        };

        Assert.NotNull(StanowiskoSluzbyRules.ZnajdzKonfliktWylacznosciWSluzbie(sluzba));
    }

    [Fact]
    public void ZachowajPrzyCzyszczeniu_TylkoParePaSgrwn()
    {
        Assert.True(StanowiskoSluzbyRules.CzyZachowacPrzyCzyszczeniuWylacznosci(
            StanowiskoSluzby.DyzurnyPAJRG,
            StanowiskoSluzby.DowodcaDzialanRatowniczychSGRWN));
        Assert.False(StanowiskoSluzbyRules.CzyZachowacPrzyCzyszczeniuWylacznosci(
            StanowiskoSluzby.DyzurnyPAJRG,
            StanowiskoSluzby.Bosman));
        Assert.False(StanowiskoSluzbyRules.CzyZachowacPrzyCzyszczeniuWylacznosci(
            StanowiskoSluzby.DowodcaZmiany,
            StanowiskoSluzby.DowodcaDzialanRatowniczychSGRWN));
    }
}
