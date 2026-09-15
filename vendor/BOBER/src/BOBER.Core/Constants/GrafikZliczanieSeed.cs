using BOBER.Core.Enums;
using BOBER.Core.Models;

namespace BOBER.Core.Constants;

/// <summary>Domyślne wiersze podsumowania — odpowiednik dotychczasowego twardego zliczania.</summary>
public static class GrafikZliczanieSeed
{
    public static IReadOnlyList<GrafikZliczanieWiersz> CreateDefaults(
        IReadOnlyDictionary<(string Nazwa, string Podtyp), int> uprawnienia,
        IReadOnlyDictionary<string, int> stanowiska,
        int zmianaId)
    {
        var dowodcyIds = IdList(stanowiska,
            "Dowódca zastępu",
            "Dowódca sekcji",
            "Zastępca dowódcy zmiany",
            "Dowódca zmiany");
        var nurekIds = IdList(uprawnienia,
            ("Nurek", ""),
            ("Mł.nurek", ""),
            ("Kierownik prac podwodnych", ""));
        var kierowcaIds = IdList(uprawnienia,
            ("Prawo jazdy", "kat. C"),
            ("Prawo jazdy", "kat. C+E"));
        var kppIds = IdList(uprawnienia, ("Kierownik prac podwodnych", ""));
        var nurekBezKppIds = IdList(uprawnienia, ("Nurek", ""), ("Mł.nurek", ""));
        var lodzIds = IdList(uprawnienia, ("Stermotorzysta / obsługa łodzi", ""));

        return
        [
            Zliczanie(zmianaId, 1, "Dowódcy", GrafikZliczanieZrodlo.Stanowiska, dowodcyIds),
            Zliczanie(zmianaId, 2, "Nurkowie", GrafikZliczanieZrodlo.Uprawnienia, nurekIds),
            Zliczanie(zmianaId, 3, "Kierowcy", GrafikZliczanieZrodlo.Uprawnienia, kierowcaIds),
            PoziomAb(zmianaId, 4, kppIds, nurekBezKppIds, lodzIds)
        ];
    }

    private static GrafikZliczanieWiersz Zliczanie(
        int zmianaId,
        short kolejnosc,
        string nazwa,
        GrafikZliczanieZrodlo zrodlo,
        IReadOnlyList<int> refIds) =>
        new()
        {
            ZmianaId = zmianaId,
            Nazwa = nazwa,
            Typ = GrafikZliczanieTyp.Zliczanie,
            Zrodlo = zrodlo,
            Kolejnosc = kolejnosc,
            Grupy = [Grupa(0, refIds)]
        };

    private static GrafikZliczanieWiersz PoziomAb(
        int zmianaId,
        short kolejnosc,
        IReadOnlyList<int> kppIds,
        IReadOnlyList<int> nurekIds,
        IReadOnlyList<int> lodzIds) =>
        new()
        {
            ZmianaId = zmianaId,
            Nazwa = "Poziom A/AB",
            Typ = GrafikZliczanieTyp.Poziom,
            Kolejnosc = kolejnosc,
            Poziomy =
            [
                new GrafikZliczaniePoziom
                {
                    Kod = "AB",
                    Kolejnosc = 0,
                    Sloty =
                    [
                        Slot("KPP", 0, 1, kppIds),
                        Slot("Nurkowie", 1, 2, nurekIds),
                        Slot("Łódź", 2, 1, lodzIds, wspoldziel: 0)
                    ]
                },
                new GrafikZliczaniePoziom
                {
                    Kod = "A",
                    Kolejnosc = 1,
                    Sloty =
                    [
                        Slot("Nurkowie", 0, 2, nurekIds),
                        Slot("Łódź", 1, 1, lodzIds)
                    ]
                }
            ]
        };

    private static GrafikZliczanieSlot Slot(
        string nazwa,
        short kolejnosc,
        short liczba,
        IReadOnlyList<int> refIds,
        short? wspoldziel = null) =>
        new()
        {
            Nazwa = nazwa,
            Zrodlo = GrafikZliczanieZrodlo.Uprawnienia,
            Liczba = liczba,
            Kolejnosc = kolejnosc,
            WspoldzielSlotKolejnosc = wspoldziel,
            Grupy = [Grupa(0, refIds)]
        };

    private static GrafikZliczanieGrupa Grupa(short kolejnosc, IReadOnlyList<int> refIds) =>
        new() { Kolejnosc = kolejnosc, RefIds = [.. refIds] };

    private static List<int> IdList(
        IReadOnlyDictionary<string, int> slownik,
        params string[] nazwy)
    {
        var ids = new List<int>();
        foreach (var nazwa in nazwy)
        {
            if (TryGetIgnoreCase(slownik, nazwa, out var id))
                ids.Add(id);
        }

        return ids;
    }

    private static List<int> IdList(
        IReadOnlyDictionary<(string Nazwa, string Podtyp), int> slownik,
        params (string Nazwa, string Podtyp)[] klucze)
    {
        var ids = new List<int>();
        foreach (var klucz in klucze)
        {
            if (TryGetIgnoreCase(slownik, klucz, out var id))
                ids.Add(id);
        }

        return ids;
    }

    private static bool TryGetIgnoreCase(
        IReadOnlyDictionary<string, int> slownik,
        string nazwa,
        out int id)
    {
        foreach (var kv in slownik)
        {
            if (kv.Key.Equals(nazwa, StringComparison.OrdinalIgnoreCase))
            {
                id = kv.Value;
                return true;
            }
        }

        id = 0;
        return false;
    }

    private static bool TryGetIgnoreCase(
        IReadOnlyDictionary<(string Nazwa, string Podtyp), int> slownik,
        (string Nazwa, string Podtyp) klucz,
        out int id)
    {
        foreach (var kv in slownik)
        {
            if (kv.Key.Nazwa.Equals(klucz.Nazwa, StringComparison.OrdinalIgnoreCase)
                && kv.Key.Podtyp.Equals(klucz.Podtyp, StringComparison.OrdinalIgnoreCase))
            {
                id = kv.Value;
                return true;
            }
        }

        id = 0;
        return false;
    }
}
