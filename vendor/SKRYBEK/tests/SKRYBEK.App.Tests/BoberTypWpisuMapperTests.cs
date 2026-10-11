using SKRYBEK.Core.Enums;
using SKRYBEK.Core.Rules;

namespace SKRYBEK.App.Tests;

public sealed class BoberTypWpisuMapperTests
{
    [Fact]
    public void MapWszystkie_ZwracaKazdaZaznaczonaSekcje()
    {
        BoberOznaczeniaBridge.TryMapWszystkie = _ =>
            [TypNieobecnosci.CzasWolny, TypNieobecnosci.DyzurDomowy];

        try
        {
            Assert.Equal(
                [TypNieobecnosci.CzasWolny, TypNieobecnosci.DyzurDomowy],
                BoberTypWpisuMapper.MapWszystkie("X"));
            Assert.Equal(TypNieobecnosci.CzasWolny, BoberTypWpisuMapper.MapLubPominOddal("X"));
        }
        finally
        {
            BoberOznaczeniaBridge.Clear();
        }
    }

    [Fact]
    public void MapWszystkie_PustaListaOznaczaOsobeDostepna()
    {
        BoberOznaczeniaBridge.TryMapWszystkie = _ => [];

        try
        {
            Assert.Empty(BoberTypWpisuMapper.MapWszystkie("WS"));
            Assert.Null(BoberTypWpisuMapper.MapLubPominOddal("WS"));
        }
        finally
        {
            BoberOznaczeniaBridge.Clear();
        }
    }
}
