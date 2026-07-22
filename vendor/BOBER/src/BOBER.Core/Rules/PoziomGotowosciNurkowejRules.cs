using BOBER.Core.Models;

namespace BOBER.Core.Rules;

/// <summary>
/// Ocena poziomu gotowości nurkowej:
/// A — min. 2 osoby z kwalifikacjami mł.nurek/nurek + osobna osoba z obsługą łodzi;
/// AB — min. 2 nurków (osobno od KPP) + 1 KPP + obsługa łodzi (może być ta sama osoba co KPP).
/// </summary>
public static class PoziomGotowosciNurkowejRules
{
    public static PoziomGotowosciNurkowej Ocena(IEnumerable<OsobaDoOcenyPoziomu> osoby)
    {
        var lista = osoby.ToList();
        if (SpelniaAb(lista))
            return PoziomGotowosciNurkowej.AB;
        if (SpelniaA(lista))
            return PoziomGotowosciNurkowej.A;
        return PoziomGotowosciNurkowej.Brak;
    }

    public static PoziomGotowosciNurkowej Ocena(IEnumerable<Funkcjonariusz> funkcjonariusze) =>
        Ocena(funkcjonariusze.Select(ZFunkcjonariusza));

    public static OsobaDoOcenyPoziomu ZFunkcjonariusza(Funkcjonariusz f) =>
        new(f.Id, f.MaUprawnieniaNumek, f.MaUprawnieniaKPP, f.MaUprawnieniaObslugaLodzi);

    public static string Format(PoziomGotowosciNurkowej poziom) => poziom switch
    {
        PoziomGotowosciNurkowej.AB => "AB",
        PoziomGotowosciNurkowej.A => "A",
        _ => "-"
    };

    public static bool CzyEtykietaObslugiLodzi(string label) =>
        label.Contains("Stermotorzysta", StringComparison.OrdinalIgnoreCase)
        || label.Contains("obsługa łodzi", StringComparison.OrdinalIgnoreCase)
        || label.Contains("obsluga lodzi", StringComparison.OrdinalIgnoreCase)
        || label.Contains("łodzi", StringComparison.OrdinalIgnoreCase)
        || label.Contains("lodzi", StringComparison.OrdinalIgnoreCase);

    private static bool SpelniaAb(IReadOnlyList<OsobaDoOcenyPoziomu> osoby)
    {
        foreach (var kpp in osoby.Where(o => o.MaKpp))
        {
            var pozostali = osoby.Where(o => o.Id != kpp.Id).ToList();
            var nurkowie = pozostali.Where(o => o.MaKwalifikacjeNurka).Take(2).ToList();
            if (nurkowie.Count < 2)
                continue;

            var idNurkow = nurkowie.Select(n => n.Id).ToHashSet();
            var maLodz = kpp.MaObslugeLodzi
                || pozostali.Any(o => o.MaObslugeLodzi && !idNurkow.Contains(o.Id));
            if (maLodz)
                return true;
        }

        return false;
    }

    private static bool SpelniaA(IReadOnlyList<OsobaDoOcenyPoziomu> osoby)
    {
        var nurkowie = osoby.Where(o => o.MaKwalifikacjeNurka).Take(2).ToList();
        if (nurkowie.Count < 2)
            return false;

        var idNurkow = nurkowie.Select(n => n.Id).ToHashSet();
        return osoby.Any(o => o.MaObslugeLodzi && !idNurkow.Contains(o.Id));
    }
}
