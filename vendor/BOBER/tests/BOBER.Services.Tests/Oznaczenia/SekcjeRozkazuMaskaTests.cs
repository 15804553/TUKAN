using BOBER.Core.Enums;
using BOBER.Core.Models;
using BOBER.Core.Oznaczenia;

namespace BOBER.Services.Tests.Oznaczenia;

public sealed class SekcjeRozkazuMaskaTests
{
    [Fact]
    public void ToMask_IFromMask_ZachowujeKilkaSekcji()
    {
        var wybrane = new[]
        {
            SekcjaRozkazuGrafiku.CzasWolny,
            SekcjaRozkazuGrafiku.DyzurDomowy
        };

        var maska = SekcjeRozkazuMaska.ToMask(wybrane);
        var odczyt = SekcjeRozkazuMaska.FromMask(maska);

        Assert.Equal(18, maska);
        Assert.Equal(
            [SekcjaRozkazuGrafiku.CzasWolny, SekcjaRozkazuGrafiku.DyzurDomowy],
            odczyt);
    }

    [Fact]
    public void EfektywneSekcje_GdyBrakListy_BierzeStaraSekcje()
    {
        var ozn = new OznaczenieGrafiku
        {
            SekcjaRozkazu = SekcjaRozkazuGrafiku.Chory
        };

        Assert.Equal([SekcjaRozkazuGrafiku.Chory], ozn.EfektywneSekcjeRozkazu);
        Assert.Equal(4, ozn.MaskaSekcjiRozkazu);
    }

    [Fact]
    public void EfektywneSekcje_ListaMaPierwszenstwoNadPojedyncza()
    {
        var ozn = new OznaczenieGrafiku
        {
            SekcjaRozkazu = SekcjaRozkazuGrafiku.Urlop,
            SekcjeRozkazu =
            [
                SekcjaRozkazuGrafiku.CzasWolny,
                SekcjaRozkazuGrafiku.DyzurDomowy
            ]
        };

        Assert.Equal(
            [SekcjaRozkazuGrafiku.CzasWolny, SekcjaRozkazuGrafiku.DyzurDomowy],
            ozn.EfektywneSekcjeRozkazu);
        Assert.Equal((short)SekcjaRozkazuGrafiku.CzasWolny, ozn.SekcjaRozkazuDoZapisu);
    }
}
