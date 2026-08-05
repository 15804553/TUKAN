using BOBER.Core.Constants;

namespace BOBER.Services.Tests.Rules;

public sealed class GrafikWpisTypyTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("?", false)]
    [InlineData("D", true)]
    [InlineData("WS", true)]
    [InlineData("U", true)]
    [InlineData("UWS", true)]
    [InlineData("U.", true)]
    [InlineData("UWS.", true)]
    [InlineData("WS.", true)]
    [InlineData("Del", true)]
    [InlineData("S", true)]
    [InlineData("C", true)]
    [InlineData("D/", false)]
    [InlineData("WS/", false)]
    [InlineData("U/", false)]
    [InlineData("UWS/", false)]
    [InlineData("S/", true)]
    public void JestNieobecnoscia_UwzgledniaFlagi(string? typ, bool expected)
    {
        Assert.Equal(expected, GrafikWpisTypy.JestNieobecnoscia(typ));
    }

    [Theory]
    [InlineData("U", "U.")]
    [InlineData("U.", "U")]
    [InlineData("UWS", "UWS.")]
    [InlineData("UWS.", "UWS")]
    [InlineData("WS", "WS.")]
    [InlineData("WS/", "WS.")]
    [InlineData("D", "D.")]
    [InlineData("D.", "D")]
    [InlineData("D/", "D.")]
    public void PrzelaczKropke_TylkoDUorazWS(string wejscie, string oczekiwane)
    {
        Assert.Equal(oczekiwane, GrafikWpisTypy.PrzelaczKropke(wejscie));
    }

    [Theory]
    [InlineData("")]
    [InlineData("S")]
    [InlineData("?")]
    [InlineData("Del")]
    public void PrzelaczKropke_Niedozwolone_ZwracaNull(string? typ)
    {
        Assert.Null(GrafikWpisTypy.PrzelaczKropke(typ));
    }

    [Theory]
    [InlineData("", "?")]
    [InlineData("?", "")]
    public void PrzelaczPytajnik_TylkoWPracy(string wejscie, string oczekiwane)
    {
        Assert.Equal(oczekiwane, GrafikWpisTypy.PrzelaczPytajnik(wejscie));
    }

    [Theory]
    [InlineData("U")]
    [InlineData("WS")]
    [InlineData("S")]
    public void PrzelaczPytajnik_Niedozwolone_ZwracaNull(string? typ)
    {
        Assert.Null(GrafikWpisTypy.PrzelaczPytajnik(typ));
    }

    [Theory]
    [InlineData("S", true)]
    [InlineData("C", true)]
    [InlineData("Del", true)]
    [InlineData("U", false)]
    [InlineData("WS", false)]
    public void NieMoznaOddacBoZakazanyTyp(string? typ, bool expected)
    {
        Assert.Equal(expected, GrafikWpisTypy.NieMoznaOddacBoZakazanyTyp(typ));
    }

    [Theory]
    [InlineData("U.", "U")]
    [InlineData("WS.", "")]
    [InlineData("?", "")]
    [InlineData("U", "U")]
    [InlineData("UWS", "U")]
    [InlineData("UWS.", "U")]
    public void TekstGlowny_BezZnaczka(string? typ, string expected)
    {
        Assert.Equal(expected, GrafikWpisTypy.TekstGlowny(typ));
    }

    [Theory]
    [InlineData("U.", "\u2022")]
    [InlineData("D.", "\u2022")]
    [InlineData("?", "?")]
    [InlineData("U", "")]
    public void TekstZnaczka(string? typ, string expected)
    {
        Assert.Equal(expected, GrafikWpisTypy.TekstZnaczka(typ));
    }

    [Theory]
    [InlineData("U", "WS", "UWS")]
    [InlineData("UWS", "WS", "U")]
    [InlineData("WS", "U", "UWS")]
    [InlineData("", "WS", "WS")]
    [InlineData("", "U", "U")]
    [InlineData("D", "WS", "WS")]
    [InlineData("U.", "WS", "UWS")]
    public void ResolvePoNalozeniu_LaczyUrlopZWs(string? aktualny, string nowy, string oczekiwane)
    {
        Assert.Equal(oczekiwane, GrafikWpisTypy.ResolvePoNalozeniu(aktualny, nowy));
    }

    [Theory]
    [InlineData("UWS", true)]
    [InlineData("WS", true)]
    [InlineData("D", true)]
    [InlineData("Del", false)]
    [InlineData("S", false)]
    [InlineData("U", false)]
    public void MaTloWolnejSluzby(string? typ, bool expected)
    {
        Assert.Equal(expected, GrafikWpisTypy.MaTloWolnejSluzby(typ));
    }

    [Theory]
    [InlineData("Del*", true)]
    [InlineData("S*", true)]
    [InlineData("Del", false)]
    [InlineData("S", false)]
    [InlineData("WS", false)]
    public void MaZachowaneTloWs(string? typ, bool expected)
    {
        Assert.Equal(expected, GrafikWpisTypy.MaZachowaneTloWs(typ));
    }

    [Theory]
    [InlineData("Del*", "Del")]
    [InlineData("S*", "S")]
    [InlineData("Del", "Del")]
    public void BazowyKod_UsuwaSufiksZachowanegoTla(string typ, string expected)
    {
        Assert.Equal(expected, GrafikWpisTypy.BazowyKod(typ));
    }

    [Theory]
    [InlineData("WS", "Del", "Del*")]
    [InlineData("D", "S", "S*")]
    [InlineData("UWS", "Del", "Del*")]
    [InlineData("Del*", "S", "S*")]
    [InlineData("", "Del", "Del")]
    [InlineData("U", "Del", "Del")]
    [InlineData("Del", "Del", "Del")]
    [InlineData("WS", "U", "U")]
    public void ResolveDelSDlaZapisu(string? poprzedni, string nowy, string expected)
    {
        Assert.Equal(expected, GrafikWpisTypy.ResolveDelSDlaZapisu(poprzedni, nowy));
    }

    [Fact]
    public void TekstGlowny_DelZSufiksem_PokazujeDel()
    {
        Assert.Equal("Del", GrafikWpisTypy.TekstGlowny("Del*"));
        Assert.Equal("S", GrafikWpisTypy.TekstGlowny("S*"));
    }
}
