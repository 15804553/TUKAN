using BOBER.Core.Models;
using BOBER.Core.Rules;

namespace BOBER.Services.Tests.Rules;

public sealed class PoziomGotowosciNurkowejRulesTests
{
    [Fact]
    public void Ocena_Brak_GdyZaMaloOsob()
    {
        var osoby = new[]
        {
            Osoba(1, nurek: true),
            Osoba(2, lodz: true)
        };

        Assert.Equal(PoziomGotowosciNurkowej.Brak, PoziomGotowosciNurkowejRules.Ocena(osoby));
    }

    [Fact]
    public void Ocena_A_DlaDwochNurkowIOsobnejLodzi()
    {
        var osoby = new[]
        {
            Osoba(1, nurek: true),
            Osoba(2, mlodszy: true),
            Osoba(3, lodz: true)
        };

        Assert.Equal(PoziomGotowosciNurkowej.A, PoziomGotowosciNurkowejRules.Ocena(osoby));
        Assert.Equal("A", PoziomGotowosciNurkowejRules.Format(PoziomGotowosciNurkowej.A));
    }

    [Fact]
    public void Ocena_Brak_GdyLodzJestJednymZDwochNurkow()
    {
        var osoby = new[]
        {
            Osoba(1, nurek: true, lodz: true),
            Osoba(2, nurek: true)
        };

        Assert.Equal(PoziomGotowosciNurkowej.Brak, PoziomGotowosciNurkowejRules.Ocena(osoby));
    }

    [Fact]
    public void Ocena_AB_GdyKppOsobnoOdDwochNurkowIMaLodz()
    {
        var osoby = new[]
        {
            Osoba(1, nurek: true, kpp: true, lodz: true),
            Osoba(2, nurek: true),
            Osoba(3, mlodszy: true)
        };

        Assert.Equal(PoziomGotowosciNurkowej.AB, PoziomGotowosciNurkowejRules.Ocena(osoby));
    }

    [Fact]
    public void Ocena_AB_GdyLodzJestOsobnaOdKpp()
    {
        var osoby = new[]
        {
            Osoba(1, nurek: true, kpp: true),
            Osoba(2, nurek: true),
            Osoba(3, mlodszy: true),
            Osoba(4, lodz: true)
        };

        Assert.Equal(PoziomGotowosciNurkowej.AB, PoziomGotowosciNurkowejRules.Ocena(osoby));
    }

    [Fact]
    public void Ocena_A_GdyKppZastępujeTylkoJednegoNurkaIBrakujeSlotuKppDlaAb()
    {
        // 1 KPP (bez łodzi) + 1 nurek + 1 łódź → nie AB (brak 2 nurków poza KPP), nie A (tylko 1 nurek jeśli KPP nie ma „Nurek”)
        // KPP z nurek: KPP liczy się jako nurek dla A, ale łódź musi być spoza 2 nurków.
        var osoby = new[]
        {
            Osoba(1, nurek: true, kpp: true),
            Osoba(2, nurek: true),
            Osoba(3, lodz: true)
        };

        // AB: KPP + 1 nurek poza nim → za mało 2 nurków poza KPP → nie AB
        // A: 2 nurków (KPP+nurek jako nurek) + osobna łódź → A
        Assert.Equal(PoziomGotowosciNurkowej.A, PoziomGotowosciNurkowejRules.Ocena(osoby));
    }

    [Fact]
    public void Ocena_ZFunkcjonariusza_RozpoznajeObslugeLodzi()
    {
        var f = new Funkcjonariusz
        {
            Id = 1,
            NazwyUprawnien = ["Stermotorzysta / obsługa łodzi"]
        };
        Assert.True(f.MaUprawnieniaObslugaLodzi);
    }

    private static OsobaDoOcenyPoziomu Osoba(
        int id,
        bool nurek = false,
        bool mlodszy = false,
        bool kpp = false,
        bool lodz = false) =>
        new(id, MaKwalifikacjeNurka: nurek || mlodszy, MaKpp: kpp, MaObslugeLodzi: lodz);
}
