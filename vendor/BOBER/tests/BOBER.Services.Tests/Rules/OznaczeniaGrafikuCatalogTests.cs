using BOBER.Core.Constants;
using BOBER.Core.Enums;
using BOBER.Core.Oznaczenia;

namespace BOBER.Services.Tests.Rules;

public sealed class OznaczeniaGrafikuCatalogTests : IDisposable
{
    public OznaczeniaGrafikuCatalogTests()
    {
        OznaczeniaLookup.Set(OznaczeniaGrafikuSeed.CreateDefaults());
    }

    public void Dispose() => OznaczeniaLookup.Clear();

    [Theory]
    [InlineData("U", true)]
    [InlineData("WS", true)]
    [InlineData("Del", true)]
    [InlineData("?", false)]
    [InlineData("D", true)]
    public void JestNieobecnoscia_ZeSeeda(string typ, bool expected) =>
        Assert.Equal(expected, GrafikWpisTypy.JestNieobecnoscia(typ));

    [Fact]
    public void Seed_MaFlagiOddajeIChce()
    {
        var oddaje = OznaczeniaLookup.FindByFlaga(FlagaPozycjaOznaczenia.Centrum);
        Assert.NotNull(oddaje);
        Assert.Equal(StylWyswietlaniaOznaczenia.Przekreslenie, oddaje!.StylWyswietlania);

        var chce = OznaczeniaLookup.FindByKod("\u2022");
        Assert.NotNull(chce);
        Assert.Equal(FlagaPozycjaOznaczenia.Prawa, chce!.FlagaPozycja);
    }

    [Fact]
    public void FindBySkrot_D()
    {
        var ozn = OznaczeniaLookup.FindBySkrot("D");
        Assert.NotNull(ozn);
        Assert.Equal("D", ozn!.Kod);
    }
}
