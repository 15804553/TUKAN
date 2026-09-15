using BOBER.Core.Constants;
using BOBER.Core.Enums;
using BOBER.Core.Models;
using BOBER.Core.Rules;

namespace BOBER.Services.Tests.Rules;

public sealed class GrafikZliczanieEvaluatorTests
{
    private const int KatC = 2;
    private const int KatCe = 3;
    private const int Nurek = 9;
    private const int Kpp = 10;
    private const int Lodz = 12;
    private const int Mlodszy = 17;
    private const int DowodcaZastepu = 101;
    private const int DowodcaZmiany = 113;

    [Fact]
    public void Policz_OrUprawnien_KierowcyCLubCe()
    {
        var wiersz = Zliczanie(GrafikZliczanieZrodlo.Uprawnienia, [KatC, KatCe]);
        var obecni = new[]
        {
            Osoba(1, uprawnienia: [KatC]),
            Osoba(2, uprawnienia: [KatCe]),
            Osoba(3, uprawnienia: [Nurek])
        };

        Assert.Equal(2, GrafikZliczanieEvaluator.Policz(obecni, wiersz));
    }

    [Fact]
    public void Policz_OrStanowisk_Dowodcy()
    {
        var wiersz = Zliczanie(GrafikZliczanieZrodlo.Stanowiska, [DowodcaZastepu, DowodcaZmiany]);
        var obecni = new[]
        {
            Osoba(1, stanowiskoId: DowodcaZmiany),
            Osoba(2, stanowiskoId: DowodcaZastepu),
            Osoba(3, stanowiskoId: 2)
        };

        Assert.Equal(2, GrafikZliczanieEvaluator.Policz(obecni, wiersz));
    }

    [Fact]
    public void Policz_OrNurkow_WliczaKpp()
    {
        var wiersz = Zliczanie(GrafikZliczanieZrodlo.Uprawnienia, [Nurek, Mlodszy, Kpp]);
        var obecni = new[]
        {
            Osoba(1, uprawnienia: [Nurek]),
            Osoba(2, uprawnienia: [Kpp]),
            Osoba(3, uprawnienia: [KatC])
        };

        Assert.Equal(2, GrafikZliczanieEvaluator.Policz(obecni, wiersz));
    }

    [Fact]
    public void Policz_AndDwochGrup_WymagaObu()
    {
        var wiersz = new GrafikZliczanieWiersz
        {
            Typ = GrafikZliczanieTyp.Zliczanie,
            Zrodlo = GrafikZliczanieZrodlo.Uprawnienia,
            Grupy =
            [
                new GrafikZliczanieGrupa { Kolejnosc = 0, RefIds = [Nurek, Mlodszy] },
                new GrafikZliczanieGrupa { Kolejnosc = 1, RefIds = [Lodz] }
            ]
        };
        var obecni = new[]
        {
            Osoba(1, uprawnienia: [Nurek, Lodz]),
            Osoba(2, uprawnienia: [Nurek]),
            Osoba(3, uprawnienia: [Lodz])
        };

        Assert.Equal(1, GrafikZliczanieEvaluator.Policz(obecni, wiersz));
    }

    [Fact]
    public void OcenaPoziomu_A_DlaDwochNurkowIOsobnejLodzi()
    {
        var wiersz = PoziomAb();
        var obecni = new[]
        {
            Osoba(1, uprawnienia: [Nurek]),
            Osoba(2, uprawnienia: [Mlodszy]),
            Osoba(3, uprawnienia: [Lodz])
        };

        Assert.Equal("A", GrafikZliczanieEvaluator.OcenaPoziomu(obecni, wiersz));
    }

    [Fact]
    public void OcenaPoziomu_Brak_GdyLodzJestJednymZDwochNurkow()
    {
        var wiersz = PoziomAb();
        var obecni = new[]
        {
            Osoba(1, uprawnienia: [Nurek, Lodz]),
            Osoba(2, uprawnienia: [Nurek])
        };

        Assert.Equal("-", GrafikZliczanieEvaluator.OcenaPoziomu(obecni, wiersz));
    }

    [Fact]
    public void OcenaPoziomu_Ab_GdyKppMaLodzIDwochNurkow()
    {
        var wiersz = PoziomAb();
        var obecni = new[]
        {
            Osoba(1, uprawnienia: [Kpp, Nurek, Lodz]),
            Osoba(2, uprawnienia: [Nurek]),
            Osoba(3, uprawnienia: [Mlodszy])
        };

        Assert.Equal("AB", GrafikZliczanieEvaluator.OcenaPoziomu(obecni, wiersz));
    }

    [Fact]
    public void OcenaPoziomu_Ab_GdyLodzOsobnaOdKpp()
    {
        var wiersz = PoziomAb();
        var obecni = new[]
        {
            Osoba(1, uprawnienia: [Kpp, Nurek]),
            Osoba(2, uprawnienia: [Nurek]),
            Osoba(3, uprawnienia: [Mlodszy]),
            Osoba(4, uprawnienia: [Lodz])
        };

        Assert.Equal("AB", GrafikZliczanieEvaluator.OcenaPoziomu(obecni, wiersz));
    }

    [Fact]
    public void OcenaPoziomu_A_GdyBrakujeDrugiegoNurkaPozaKpp()
    {
        var wiersz = PoziomAb();
        var obecni = new[]
        {
            Osoba(1, uprawnienia: [Kpp, Nurek]),
            Osoba(2, uprawnienia: [Nurek]),
            Osoba(3, uprawnienia: [Lodz])
        };

        Assert.Equal("A", GrafikZliczanieEvaluator.OcenaPoziomu(obecni, wiersz));
    }

    [Fact]
    public void FormatEtykiety_PustaLista_TylkoWolneMiejsca()
    {
        Assert.Equal(
            GrafikZliczanieEvaluator.WolneMiejscaNazwa,
            GrafikZliczanieEvaluator.FormatEtykiety([]));
    }

    [Fact]
    public void Seed_TworzyCzteryWierszeZOczekiwanymiNazwami()
    {
        var uprawnienia = new Dictionary<(string Nazwa, string Podtyp), int>
        {
            [("Prawo jazdy", "kat. C")] = KatC,
            [("Prawo jazdy", "kat. C+E")] = KatCe,
            [("Nurek", "")] = Nurek,
            [("Mł.nurek", "")] = Mlodszy,
            [("Kierownik prac podwodnych", "")] = Kpp,
            [("Stermotorzysta / obsługa łodzi", "")] = Lodz
        };
        var stanowiska = new Dictionary<string, int>
        {
            ["Dowódca zastępu"] = DowodcaZastepu,
            ["Dowódca sekcji"] = 111,
            ["Zastępca dowódcy zmiany"] = 112,
            ["Dowódca zmiany"] = DowodcaZmiany
        };

        var seed = GrafikZliczanieSeed.CreateDefaults(uprawnienia, stanowiska, zmianaId: 1);
        Assert.Equal(new[] { "Dowódcy", "Nurkowie", "Kierowcy", "Poziom A/AB" }, seed.Select(w => w.Nazwa));
        Assert.Equal(GrafikZliczanieTyp.Poziom, seed[^1].Typ);
        Assert.Equal("AB", seed[^1].Poziomy[0].Kod);
        Assert.Equal("A", seed[^1].Poziomy[1].Kod);
    }

    [Fact]
    public void JestZarezerwowanaNazwa_WolneMiejsca()
    {
        Assert.True(GrafikZliczanieEvaluator.JestZarezerwowanaNazwa("Wolne miejsca"));
        Assert.True(GrafikZliczanieEvaluator.JestZarezerwowanaNazwa(" wolne miejsca "));
        Assert.False(GrafikZliczanieEvaluator.JestZarezerwowanaNazwa("Dowódcy"));
    }

    private static GrafikZliczanieWiersz Zliczanie(GrafikZliczanieZrodlo zrodlo, IReadOnlyList<int> ids) =>
        new()
        {
            Typ = GrafikZliczanieTyp.Zliczanie,
            Zrodlo = zrodlo,
            Grupy = [new GrafikZliczanieGrupa { Kolejnosc = 0, RefIds = [.. ids] }]
        };

    private static GrafikZliczanieWiersz PoziomAb() =>
        GrafikZliczanieSeed.CreateDefaults(
            new Dictionary<(string Nazwa, string Podtyp), int>
            {
                [("Nurek", "")] = Nurek,
                [("Mł.nurek", "")] = Mlodszy,
                [("Kierownik prac podwodnych", "")] = Kpp,
                [("Stermotorzysta / obsługa łodzi", "")] = Lodz,
                [("Prawo jazdy", "kat. C")] = KatC,
                [("Prawo jazdy", "kat. C+E")] = KatCe
            },
            new Dictionary<string, int>(),
            zmianaId: 1)[^1];

    private static Funkcjonariusz Osoba(
        int id,
        int stanowiskoId = 1,
        int[]? uprawnienia = null) =>
        new()
        {
            Id = id,
            StanowiskoId = stanowiskoId,
            IdsUprawnien = uprawnienia is null ? [] : [.. uprawnienia]
        };
}
