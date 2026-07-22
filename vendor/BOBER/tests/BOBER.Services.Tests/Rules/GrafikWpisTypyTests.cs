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
    [InlineData("U.", true)]
    [InlineData("WS.", true)]
    [InlineData("Del", true)]
    [InlineData("S", true)]
    [InlineData("C", true)]
    [InlineData("D/", false)]
    [InlineData("WS/", false)]
    [InlineData("U/", false)]
    [InlineData("S/", true)]
    public void JestNieobecnoscia_UwzgledniaFlagi(string? typ, bool expected)
    {
        Assert.Equal(expected, GrafikWpisTypy.JestNieobecnoscia(typ));
    }

    [Theory]
    [InlineData("U", "U.")]
    [InlineData("U.", "U")]
    [InlineData("WS", "WS.")]
    [InlineData("WS/", "WS.")]
    public void PrzelaczKropke_TylkoUorazWS(string wejscie, string oczekiwane)
    {
        Assert.Equal(oczekiwane, GrafikWpisTypy.PrzelaczKropke(wejscie));
    }

    [Theory]
    [InlineData("")]
    [InlineData("D")]
    [InlineData("S")]
    [InlineData("?")]
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
    public void TekstGlowny_BezZnaczka(string? typ, string expected)
    {
        Assert.Equal(expected, GrafikWpisTypy.TekstGlowny(typ));
    }

    [Theory]
    [InlineData("U.", "\u2022")]
    [InlineData("?", "?")]
    [InlineData("U", "")]
    public void TekstZnaczka(string? typ, string expected)
    {
        Assert.Equal(expected, GrafikWpisTypy.TekstZnaczka(typ));
    }
}
