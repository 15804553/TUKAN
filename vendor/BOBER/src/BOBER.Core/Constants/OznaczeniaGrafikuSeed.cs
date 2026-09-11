using BOBER.Core.Enums;
using BOBER.Core.Models;

namespace BOBER.Core.Constants;

/// <summary>Domyślne oznaczenia grafiku (odwzorowanie historycznej logiki).</summary>
public static class OznaczeniaGrafikuSeed
{
    public const string KodOddaje = "ODDAJE";

    /// <summary>Symbol w grafiku — ta sama kropka • (\u2022) co wcześniej w komórce.</summary>
    public const string KodChceOddac = "\u2022";

    /// <summary>Stary kod wiersza sprzed zmiany symbolu na •.</summary>
    public const string KodChceOddacLegacy = "CHCEODDAC";

    public static bool IsChceOddacKod(string? kod) =>
        !string.IsNullOrEmpty(kod)
        && (kod.Equals(KodChceOddac, StringComparison.Ordinal)
            || kod.Equals(KodChceOddacLegacy, StringComparison.OrdinalIgnoreCase));

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
            Row(GrafikWpisTypy.Dyzur, "Dyżur", ws, false, SekcjaRozkazuGrafiku.DyzurDomowy, "D",
                moznaOddac: true, moznaKropke: true, dodatkowa: SekcjaRozkazuGrafiku.CzasWolny,
                kolejnosc: ++i),
            Row(GrafikWpisTypy.WolnaSluzba, "Wolna służba", ws, false, SekcjaRozkazuGrafiku.CzasWolny, "W",
                moznaOddac: true, moznaKropke: true,
                rola: RolaNalozaniaOznaczenia.WolnaSluzba, kolejnosc: ++i),
            Row(GrafikWpisTypy.Urlop, "Urlop", RoleKeys.BrakWypelnienia, false, SekcjaRozkazuGrafiku.Urlop, "U",
                moznaOddac: true, moznaKropke: true, rola: RolaNalozaniaOznaczenia.Urlop, kolejnosc: ++i),
            Row(GrafikWpisTypy.UrlopZWolnaSluzba, "Urlop z wolną służbą", ws, false, SekcjaRozkazuGrafiku.CzasWolny, "",
                tekst: GrafikWpisTypy.Urlop, moznaOddac: true, moznaKropke: true,
                rola: RolaNalozaniaOznaczenia.UrlopZWolnaSluzba, kolejnosc: ++i),
            Row(GrafikWpisTypy.UrlopRodzicielski, "Urlop rodzicielski", RoleKeys.BrakWypelnienia, false,
                SekcjaRozkazuGrafiku.Urlop, "", moznaOddac: true, moznaKropke: true,
                rola: RolaNalozaniaOznaczenia.Urlop, kolejnosc: ++i),
            Row(GrafikWpisTypy.Delegacja, "Delegacja", del, false, SekcjaRozkazuGrafiku.Delegowany, "E",
                zachowajTlo: true, kolejnosc: ++i),
            Row(GrafikWpisTypy.Szkolenie, "Szkolenie", s, false, SekcjaRozkazuGrafiku.Delegowany, "S",
                zachowajTlo: true, kolejnosc: ++i),
            Row(GrafikWpisTypy.Chory, "Chory", RoleKeys.BrakWypelnienia, false, SekcjaRozkazuGrafiku.Chory, "C",
                kolejnosc: ++i),
            Row(GrafikWpisTypy.PotrzebujeWolne, "Potrzebuje wolne", OznaczenieGrafiku.DomyslnyKolorCzcionkiFlagi, true, null, "/",
                flaga: FlagaPozycjaOznaczenia.Prawa, kolejnosc: ++i),
            Row(KodChceOddac, "Chce oddać", OznaczenieGrafiku.DomyslnyKolorCzcionkiFlagi, true, null, ".",
                flaga: FlagaPozycjaOznaczenia.Prawa,
                kolejnosc: ++i),
            Row(KodOddaje, "Oddaje", OznaczenieGrafiku.DomyslnyKolorCzcionkiFlagi, true, null, "O",
                styl: StylWyswietlaniaOznaczenia.Przekreslenie,
                flaga: FlagaPozycjaOznaczenia.Centrum,
                kolejnosc: ++i)
        ];
    }

    private static OznaczenieGrafiku Row(
        string kod,
        string nazwa,
        string kolor,
        bool wPracy,
        SekcjaRozkazuGrafiku? sekcja,
        string skrot,
        string? tekst = null,
        bool moznaOddac = false,
        bool moznaKropke = false,
        bool zachowajTlo = false,
        SekcjaRozkazuGrafiku? dodatkowa = null,
        RolaNalozaniaOznaczenia rola = RolaNalozaniaOznaczenia.Brak,
        StylWyswietlaniaOznaczenia styl = StylWyswietlaniaOznaczenia.Normalny,
        FlagaPozycjaOznaczenia flaga = FlagaPozycjaOznaczenia.Nie,
        short kolejnosc = 0) =>
        new()
        {
            Kod = kod,
            Nazwa = nazwa,
            KolorHex = kolor,
            KolorExcelHex = kolor,
            WPracy = wPracy,
            SekcjaRozkazu = sekcja,
            SkrotKlawiszowy = skrot,
            TekstWyswietlany = tekst,
            MoznaOddac = moznaOddac,
            MoznaKropke = moznaKropke,
            ZachowajTloWsPrzyBraku = zachowajTlo,
            DodatkowaSekcjaRozkazu = dodatkowa,
            RolaNalozania = rola,
            Kolejnosc = kolejnosc,
            EksportDoExcela = true,
            StylWyswietlania = styl,
            FlagaPozycja = flaga
        };
}
