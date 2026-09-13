using BOBER.Core.Constants;
using BOBER.Core.Enums;
using BOBER.Core.Oznaczenia;

namespace BOBER.Services.Tests.Rules;

public sealed class GrafikWpisTypyTests
{
    [Fact]
    public void Parse_Compose_Flagi()
    {
        var composed = GrafikWpisTypy.Compose("D", "\u2022", "?", true);
        var parsed = GrafikWpisTypy.Parse(composed);
        Assert.Equal("D", parsed.Bazowy);
        Assert.Equal("\u2022", parsed.LewaKod);
        Assert.Equal("?", parsed.PrawaKod);
        Assert.True(parsed.Centrum);
        Assert.Equal("D", GrafikWpisTypy.BazowyKod(composed));
        Assert.True(GrafikWpisTypy.MaCentrumOverlay(composed));
    }

    [Fact]
    public void PrzelaczFlage_Centrum_NieZmieniaBazy()
    {
        var oddaje = new BOBER.Core.Models.OznaczenieGrafiku
        {
            Kod = "ODDAJE",
            FlagaPozycja = FlagaPozycjaOznaczenia.Centrum,
            StylWyswietlania = StylWyswietlaniaOznaczenia.Przekreslenie,
            KolorHex = "#000000",
            WPracy = true
        };

        var z = GrafikWpisTypy.PrzelaczFlage("D", oddaje)!;
        Assert.Equal("D", GrafikWpisTypy.BazowyKod(z));
        Assert.True(GrafikWpisTypy.MaCentrumOverlay(z));
        Assert.Equal("D", GrafikWpisTypy.TekstGlowny(z));

        var off = GrafikWpisTypy.PrzelaczFlage(z, oddaje)!;
        Assert.False(GrafikWpisTypy.MaCentrumOverlay(off));
        Assert.Equal("D", off);
    }

    [Fact]
    public void PrzelaczFlage_Prawa_Sufiks()
    {
        OznaczeniaLookup.Set(
        [
            new BOBER.Core.Models.OznaczenieGrafiku
            {
                Kod = "\u2022",
                Nazwa = "Chce",
                FlagaPozycja = FlagaPozycjaOznaczenia.Prawa,
                KolorHex = "#000000",
                WPracy = true
            }
        ]);

        try
        {
            var flaga = OznaczeniaLookup.FindByKod("\u2022")!;
            var z = GrafikWpisTypy.PrzelaczFlage("U", flaga)!;
            Assert.Equal("U", GrafikWpisTypy.BazowyKod(z));
            Assert.Equal("\u2022", GrafikWpisTypy.TekstZnaczkaPrawa(z));
            Assert.Equal("U", GrafikWpisTypy.TekstGlowny(z));
        }
        finally
        {
            OznaczeniaLookup.Clear();
        }
    }

    [Fact]
    public void UstawBazowy_ZachowujeFlagi()
    {
        var withFlags = GrafikWpisTypy.Compose("D", null, "?", true);
        var next = GrafikWpisTypy.UstawBazowy(withFlags, "WS");
        var p = GrafikWpisTypy.Parse(next);
        Assert.Equal("WS", p.Bazowy);
        Assert.Equal("?", p.PrawaKod);
        Assert.True(p.Centrum);
        Assert.False(p.ZachowajTloWs);
    }

    [Fact]
    public void UstawBazowy_WsNaU_BezKoloru_ZachowujeZolteTlo()
    {
        OznaczeniaLookup.Set(
        [
            new BOBER.Core.Models.OznaczenieGrafiku
            {
                Kod = "U",
                Nazwa = "Urlop",
                KolorHex = RoleKeys.BrakWypelnienia,
                WPracy = false,
                SekcjaRozkazu = SekcjaRozkazuGrafiku.Urlop
            }
        ]);

        try
        {
            var next = GrafikWpisTypy.UstawBazowy("WS", "U");
            Assert.Equal("U", GrafikWpisTypy.BazowyKod(next));
            Assert.True(GrafikWpisTypy.MaZachowaneTloWs(next));
            Assert.True(GrafikWpisTypy.MaTloWolnejSluzby(next));

            var zPustej = GrafikWpisTypy.UstawBazowy(string.Empty, "U");
            Assert.False(GrafikWpisTypy.MaZachowaneTloWs(zPustej));
            Assert.False(GrafikWpisTypy.MaTloWolnejSluzby(zPustej));
        }
        finally
        {
            OznaczeniaLookup.Clear();
        }
    }

    [Fact]
    public void UstawBazowy_WsNaOznaczenieZKolorem_NieZachowujeTla()
    {
        OznaczeniaLookup.Set(
        [
            new BOBER.Core.Models.OznaczenieGrafiku
            {
                Kod = "X",
                Nazwa = "Test",
                KolorHex = "#FF00AA",
                WPracy = false
            }
        ]);

        try
        {
            var next = GrafikWpisTypy.UstawBazowy("WS", "X");
            Assert.False(GrafikWpisTypy.MaZachowaneTloWs(next));
            Assert.Null(GrafikWpisTypy.ZachowaneTloHex(next));
            Assert.False(GrafikWpisTypy.MaTloWolnejSluzby(next));
        }
        finally
        {
            OznaczeniaLookup.Clear();
        }
    }

    [Fact]
    public void UstawBazowy_OznaczenieZKoloremNaBrak_ZachowujeTenKolor()
    {
        OznaczeniaLookup.Set(
        [
            new BOBER.Core.Models.OznaczenieGrafiku
            {
                Kod = "X",
                Nazwa = "Kolorowe",
                KolorHex = "#FF00AA",
                WPracy = false
            },
            new BOBER.Core.Models.OznaczenieGrafiku
            {
                Kod = "U",
                Nazwa = "Urlop",
                KolorHex = RoleKeys.BrakWypelnienia,
                WPracy = false,
                SekcjaRozkazu = SekcjaRozkazuGrafiku.Urlop
            },
            new BOBER.Core.Models.OznaczenieGrafiku
            {
                Kod = "C",
                Nazwa = "Chory",
                KolorHex = RoleKeys.BrakWypelnienia,
                WPracy = false,
                SekcjaRozkazu = SekcjaRozkazuGrafiku.Chory
            }
        ]);

        try
        {
            var poU = GrafikWpisTypy.UstawBazowy("X", "U");
            Assert.Equal("U", GrafikWpisTypy.BazowyKod(poU));
            Assert.False(GrafikWpisTypy.MaZachowaneTloWs(poU));
            Assert.Equal("#FF00AA", GrafikWpisTypy.ZachowaneTloHex(poU));

            // Kolejne oznaczenie z „brak” przenosi zachowane tło dalej.
            var poC = GrafikWpisTypy.UstawBazowy(poU, "C");
            Assert.Equal("C", GrafikWpisTypy.BazowyKod(poC));
            Assert.Equal("#FF00AA", GrafikWpisTypy.ZachowaneTloHex(poC));
        }
        finally
        {
            OznaczeniaLookup.Clear();
        }
    }

    [Fact]
    public void JestNieobecnoscia_Centrum_ToWPracy()
    {
        var typ = GrafikWpisTypy.Compose("U", null, null, true);
        Assert.False(GrafikWpisTypy.JestNieobecnoscia(typ));
    }
}
