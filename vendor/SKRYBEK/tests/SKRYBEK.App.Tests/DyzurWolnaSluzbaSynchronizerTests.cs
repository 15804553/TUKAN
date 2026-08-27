using System.Collections.ObjectModel;
using SKRYBEK.App.Helpers;
using SKRYBEK.App.ViewModels;
using SKRYBEK.Core.Enums;
using SKRYBEK.Core.Models;

namespace SKRYBEK.App.Tests;

public sealed class DyzurWolnaSluzbaSynchronizerTests
{
    private static Funkcjonariusz Kowalski() => new()
    {
        Id = 7,
        Stopien = "st. ogn.",
        Imie = "Jan",
        Nazwisko = "Kowalski"
    };

    private static NieobecnyViewModel Dyzur(string nazwisko, int? id = null, IEnumerable<Funkcjonariusz>? personel = null)
        => new(new NieobecnyWSluzbie
        {
            Nazwisko = nazwisko,
            FunkcjonariuszId = id,
            TypNieobecnosci = TypNieobecnosci.DyzurDomowy
        }, personel);

    private static NieobecnyViewModel Wolna(string nazwisko, int? id = null, IEnumerable<Funkcjonariusz>? personel = null)
        => new(new NieobecnyWSluzbie
        {
            Nazwisko = nazwisko,
            FunkcjonariuszId = id,
            TypNieobecnosci = TypNieobecnosci.CzasWolny
        }, personel);

    [Fact]
    public void PisanieLiteraPoLiterze_NieTworzyKolejnychWierszyWolnejSluzby()
    {
        var sync = new DyzurWolnaSluzbaSynchronizer();
        var personel = new[] { Kowalski() };
        var dyzur = new ObservableCollection<NieobecnyViewModel> { Dyzur("", personel: personel) };
        var wolna = new ObservableCollection<NieobecnyViewModel>();

        foreach (var fragment in new[] { "K", "Ko", "Kow", "Kowa", "Kowal", "Kowals", "Kowalski" })
        {
            dyzur[0].Nazwisko = fragment;
            sync.Synchronizuj(dyzur, wolna, personel, wymusNoweWpisy: false);
        }

        Assert.Single(wolna);
        Assert.Equal(personel[0].StopienINazwisko, wolna[0].Nazwisko);
    }

    [Fact]
    public void NiekompletneNazwisko_NieDodajeWierszaDopokiNieMaDokladnegoDopasowania()
    {
        var sync = new DyzurWolnaSluzbaSynchronizer();
        var personel = new[] { Kowalski() };
        var dyzur = new ObservableCollection<NieobecnyViewModel> { Dyzur("Kow", personel: personel) };
        var wolna = new ObservableCollection<NieobecnyViewModel>();

        sync.Synchronizuj(dyzur, wolna, personel, wymusNoweWpisy: false);

        Assert.Empty(wolna);
    }

    [Fact]
    public void Zapis_DopisujeRecznieWpisaneNazwiskoSpozaPersonelu()
    {
        var sync = new DyzurWolnaSluzbaSynchronizer();
        var dyzur = new ObservableCollection<NieobecnyViewModel> { Dyzur("Nowak") };
        var wolna = new ObservableCollection<NieobecnyViewModel>();

        sync.Synchronizuj(dyzur, wolna, personel: [], wymusNoweWpisy: false);
        Assert.Empty(wolna);

        sync.Synchronizuj(dyzur, wolna, personel: [], wymusNoweWpisy: true);

        Assert.Single(wolna);
        Assert.Equal("Nowak", wolna[0].Nazwisko);
    }

    [Fact]
    public void IstniejacyWpisWolnej_NieJestDuplikowanyAniNadpisywanyPrzyZmianieDyzuru()
    {
        var sync = new DyzurWolnaSluzbaSynchronizer();
        var personel = new[] { Kowalski() };
        var dyzur = new ObservableCollection<NieobecnyViewModel>
        {
            Dyzur(personel[0].StopienINazwisko, personel[0].Id, personel)
        };
        var wolna = new ObservableCollection<NieobecnyViewModel>
        {
            Wolna(personel[0].StopienINazwisko, personel[0].Id, personel)
        };

        sync.Synchronizuj(dyzur, wolna, personel, wymusNoweWpisy: false);
        Assert.Single(wolna);

        dyzur[0].Nazwisko = "Nowak";
        sync.Synchronizuj(dyzur, wolna, personel, wymusNoweWpisy: true);

        Assert.Equal(2, wolna.Count);
        Assert.Contains(wolna, w => w.Nazwisko == personel[0].StopienINazwisko);
        Assert.Contains(wolna, w => w.Nazwisko == "Nowak");
    }

    [Fact]
    public void UsuniecieDyzuru_UsuwaTylkoAutoKopie()
    {
        var sync = new DyzurWolnaSluzbaSynchronizer();
        var dyzur = new ObservableCollection<NieobecnyViewModel> { Dyzur("Nowak") };
        var wolna = new ObservableCollection<NieobecnyViewModel> { Wolna("Kowalski", 7) };

        sync.Synchronizuj(dyzur, wolna, personel: [], wymusNoweWpisy: true);
        Assert.Equal(2, wolna.Count);

        dyzur.Clear();
        sync.Synchronizuj(dyzur, wolna, personel: [], wymusNoweWpisy: false);

        Assert.Single(wolna);
        Assert.Equal("Kowalski", wolna[0].Nazwisko);
    }
}
