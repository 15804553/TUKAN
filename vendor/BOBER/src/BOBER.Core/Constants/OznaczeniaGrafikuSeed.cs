using BOBER.Core.Enums;
using BOBER.Core.Models;

namespace BOBER.Core.Constants;

/// <summary>Domyślne oznaczenia grafiku (bootstrap pustej bazy).</summary>
public static class OznaczeniaGrafikuSeed
{
    public static IReadOnlyList<OznaczenieGrafiku> CreateDefaults(
        string? kolorWs = null,
        string? kolorDel = null,
        string? kolorS = null)
    {
        var ws = string.IsNullOrWhiteSpace(kolorWs)
            ? RoleKeys.DomyslneKoloryWpisow[RoleKeys.WolnaSluzba]
            : kolorWs;
        var del = string.IsNullOrWhiteSpace(kolorDel) ? RoleKeys.BrakWypelnienia : kolorDel;
        var s = string.IsNullOrWhiteSpace(kolorS) ? RoleKeys.BrakWypelnienia : kolorS;

        short i = 0;
        return
        [
            Row(GrafikWpisTypy.Dyzur, "Dyżur", ws, false, SekcjaRozkazuGrafiku.DyzurDomowy, "D", ++i),
            Row(GrafikWpisTypy.WolnaSluzba, "Wolna służba", ws, false, SekcjaRozkazuGrafiku.CzasWolny, "W", ++i),
            Row(GrafikWpisTypy.Urlop, "Urlop", RoleKeys.BrakWypelnienia, false, SekcjaRozkazuGrafiku.Urlop, "U", ++i),
            Row(GrafikWpisTypy.UrlopZWolnaSluzba, "Urlop z wolną służbą", ws, false, SekcjaRozkazuGrafiku.CzasWolny, "", ++i,
                tekst: GrafikWpisTypy.Urlop),
            Row(GrafikWpisTypy.UrlopRodzicielski, "Urlop rodzicielski", RoleKeys.BrakWypelnienia, false,
                SekcjaRozkazuGrafiku.Urlop, "", ++i),
            Row(GrafikWpisTypy.Delegacja, "Delegacja", del, false, SekcjaRozkazuGrafiku.Delegowany, "E", ++i),
            Row(GrafikWpisTypy.Szkolenie, "Szkolenie", s, false, SekcjaRozkazuGrafiku.Delegowany, "S", ++i),
            Row(GrafikWpisTypy.Chory, "Chory", RoleKeys.BrakWypelnienia, false, SekcjaRozkazuGrafiku.Chory, "C", ++i),
            Row(GrafikWpisTypy.PotrzebujeWolne, "Potrzebuje wolne", OznaczenieGrafiku.DomyslnyKolorCzcionkiFlagi, true, null, "/", ++i,
                flaga: FlagaPozycjaOznaczenia.Prawa),
            Row("\u2022", "Chce oddać", OznaczenieGrafiku.DomyslnyKolorCzcionkiFlagi, true, null, ".", ++i,
                flaga: FlagaPozycjaOznaczenia.Prawa),
            Row("ODDAJE", "Oddaje", OznaczenieGrafiku.DomyslnyKolorCzcionkiFlagi, true, null, "O", ++i,
                styl: StylWyswietlaniaOznaczenia.Przekreslenie,
                flaga: FlagaPozycjaOznaczenia.Centrum)
        ];
    }

    private static OznaczenieGrafiku Row(
        string kod,
        string nazwa,
        string kolor,
        bool wPracy,
        SekcjaRozkazuGrafiku? sekcja,
        string skrot,
        short kolejnosc,
        string? tekst = null,
        StylWyswietlaniaOznaczenia styl = StylWyswietlaniaOznaczenia.Normalny,
        FlagaPozycjaOznaczenia flaga = FlagaPozycjaOznaczenia.Nie) =>
        new()
        {
            Kod = kod,
            Nazwa = nazwa,
            KolorHex = kolor,
            KolorExcelHex = flaga == FlagaPozycjaOznaczenia.Nie ? kolor : RoleKeys.BrakWypelnienia,
            WPracy = wPracy,
            SekcjaRozkazu = sekcja,
            SkrotKlawiszowy = skrot,
            TekstWyswietlany = tekst,
            Kolejnosc = kolejnosc,
            EksportDoExcela = true,
            StylWyswietlania = styl,
            FlagaPozycja = flaga
        };
}
