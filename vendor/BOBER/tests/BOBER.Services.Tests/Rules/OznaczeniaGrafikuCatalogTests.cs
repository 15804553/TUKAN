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
    [InlineData("S", true)]
    [InlineData("C", true)]
    [InlineData("D", true)]
    [InlineData("?", false)]
    [InlineData("WS/", false)]
    [InlineData("Del*", true)]
    public void JestNieobecnoscia_ZeSeeda(string typ, bool expected) =>
        Assert.Equal(expected, GrafikWpisTypy.JestNieobecnoscia(typ));

    [Theory]
    [InlineData("U", SekcjaRozkazuGrafiku.Urlop)]
    [InlineData("WS", SekcjaRozkazuGrafiku.CzasWolny)]
    [InlineData("UWS", SekcjaRozkazuGrafiku.CzasWolny)]
    [InlineData("C", SekcjaRozkazuGrafiku.Chory)]
    [InlineData("Del", SekcjaRozkazuGrafiku.Delegowany)]
    [InlineData("Del*", SekcjaRozkazuGrafiku.Delegowany)]
    [InlineData("S", SekcjaRozkazuGrafiku.Delegowany)]
    [InlineData("D", SekcjaRozkazuGrafiku.DyzurDomowy)]
    public void SekcjaRozkazu_ZeSeeda(string typ, SekcjaRozkazuGrafiku expected)
    {
        var ozn = OznaczeniaLookup.FindByKod(GrafikWpisTypy.BazowyKod(typ));
        Assert.NotNull(ozn);
        Assert.Equal(expected, ozn!.SekcjaRozkazu);
    }

    [Fact]
    public void D_MaDodatkowaSekcjeWolnejSluzby()
    {
        var d = OznaczeniaLookup.FindByKod("D");
        Assert.Equal(SekcjaRozkazuGrafiku.CzasWolny, d!.DodatkowaSekcjaRozkazu);
    }

    [Fact]
    public void ResolvePoNalozeniu_U_plus_WS()
    {
        Assert.Equal("UWS", GrafikWpisTypy.ResolvePoNalozeniu("U", "WS"));
        Assert.Equal("U", GrafikWpisTypy.ResolvePoNalozeniu("UWS", "WS"));
    }

    [Fact]
    public void FindBySkrot_D()
    {
        var ozn = OznaczeniaLookup.FindBySkrot("D");
        Assert.NotNull(ozn);
        Assert.Equal("D", ozn!.Kod);
    }

    [Fact]
    public void ResolveDelS_ZachowujeGwiazdkeNaWs()
    {
        Assert.Equal("Del*", GrafikWpisTypy.ResolveDelSDlaZapisu("WS", "Del"));
        Assert.Equal("Del", GrafikWpisTypy.ResolveDelSDlaZapisu("", "Del"));
    }

    [Fact]
    public void Seed_MaPolaEksportuExcel()
    {
        var ws = OznaczeniaLookup.FindByKod("WS");
        Assert.NotNull(ws);
        Assert.True(ws!.EksportDoExcela);
        Assert.False(string.IsNullOrWhiteSpace(ws.KolorExcelHex));
        Assert.True(ws.MaKolorExcel);
    }
}
