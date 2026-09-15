using BOBER.Core.Enums;
using BOBER.Core.Models;

namespace BOBER.Core.Rules;

/// <summary>Ocena wierszy zliczania i poziomów na liście obecnych.</summary>
public static class GrafikZliczanieEvaluator
{
    public const string WolneMiejscaNazwa = "Wolne miejsca";
    public const string BrakPoziomu = "-";

    public static bool JestZarezerwowanaNazwa(string? nazwa) =>
        string.Equals(nazwa?.Trim(), WolneMiejscaNazwa, StringComparison.OrdinalIgnoreCase);

    public static bool OsobaSpelnia(
        Funkcjonariusz osoba,
        GrafikZliczanieZrodlo zrodlo,
        IReadOnlyList<GrafikZliczanieGrupa> grupy)
    {
        if (grupy.Count == 0)
            return false;

        foreach (var grupa in grupy.OrderBy(g => g.Kolejnosc))
        {
            if (grupa.RefIds.Count == 0)
                return false;

            var trafia = zrodlo == GrafikZliczanieZrodlo.Stanowiska
                ? grupa.RefIds.Contains(osoba.StanowiskoId)
                : grupa.RefIds.Any(id => osoba.IdsUprawnien.Contains(id));
            if (!trafia)
                return false;
        }

        return true;
    }

    public static int Policz(IEnumerable<Funkcjonariusz> obecni, GrafikZliczanieWiersz wiersz) =>
        obecni.Count(f => OsobaSpelnia(f, wiersz.Zrodlo, wiersz.Grupy));

    public static string OcenaPoziomu(IReadOnlyList<Funkcjonariusz> obecni, GrafikZliczanieWiersz wiersz)
    {
        foreach (var poziom in wiersz.Poziomy.OrderBy(p => p.Kolejnosc))
        {
            if (SpelniaPoziom(obecni, poziom))
                return string.IsNullOrWhiteSpace(poziom.Kod) ? BrakPoziomu : poziom.Kod.Trim();
        }

        return BrakPoziomu;
    }

    public static string FormatWartosc(IReadOnlyList<Funkcjonariusz> obecni, GrafikZliczanieWiersz wiersz) =>
        wiersz.Typ == GrafikZliczanieTyp.Poziom
            ? OcenaPoziomu(obecni, wiersz)
            : Policz(obecni, wiersz).ToString();

    public static string FormatEtykiety(IReadOnlyList<GrafikZliczanieWiersz> wiersze)
    {
        var linie = new List<string> { WolneMiejscaNazwa };
        linie.AddRange(wiersze.OrderBy(w => w.Kolejnosc).Select(w => w.Nazwa));
        return string.Join('\n', linie);
    }

    private static bool SpelniaPoziom(IReadOnlyList<Funkcjonariusz> obecni, GrafikZliczaniePoziom poziom)
    {
        var sloty = poziom.Sloty.OrderBy(s => s.Kolejnosc).ToList();
        if (sloty.Count == 0)
            return false;

        var przypisani = new Dictionary<short, HashSet<int>>();
        foreach (var slot in sloty)
        {
            var zabronieni = new HashSet<int>();
            foreach (var (kolejnosc, osoby) in przypisani)
            {
                if (slot.WspoldzielSlotKolejnosc == kolejnosc)
                    continue;
                zabronieni.UnionWith(osoby);
            }

            var kandydaci = obecni
                .Where(f => OsobaSpelnia(f, slot.Zrodlo, slot.Grupy))
                .Where(f => !zabronieni.Contains(f.Id))
                .ToList();

            if (kandydaci.Count < slot.Liczba)
                return false;

            var wspoldzieleni = slot.WspoldzielSlotKolejnosc is short klucz
                && przypisani.TryGetValue(klucz, out var pula)
                    ? pula
                    : [];

            var wybrane = kandydaci
                .OrderBy(f => wspoldzieleni.Contains(f.Id) ? 1 : 0)
                .ThenBy(f => f.Id)
                .Take(slot.Liczba)
                .Select(f => f.Id)
                .ToHashSet();

            przypisani[slot.Kolejnosc] = wybrane;
        }

        return true;
    }
}
