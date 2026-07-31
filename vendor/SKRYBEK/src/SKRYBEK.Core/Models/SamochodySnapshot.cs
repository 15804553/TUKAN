using System.Text.Json;
using SKRYBEK.Core.Enums;

namespace SKRYBEK.Core.Models;

/// <summary>
/// Serializacja konfiguracji pojazdów w chwili zapisu/zatwierdzenia rozkazu,
/// żeby zablokowany meldunek nie przejmował późniejszych zmian nazw z ustawień.
/// </summary>
public static class SamochodySnapshot
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static string Serializuj(IEnumerable<Samochod> samochody)
    {
        var dto = samochody
            .OrderBy(s => s.Kolejnosc)
            .Select(s => new SamochodSnapshotDto
            {
                Id = s.Id,
                Nazwa = s.Nazwa,
                LiczbaPozycji = s.LiczbaPozycji,
                Typ = (int)s.Typ,
                Kolejnosc = s.Kolejnosc,
                CzyAktywny = s.CzyAktywny,
                CzySprawdzajPoziomNurkowy = s.CzySprawdzajPoziomNurkowy,
                WymaganeUprawnieniaIds = s.WymaganeUprawnieniaIds.ToList()
            })
            .ToList();

        return JsonSerializer.Serialize(dto, JsonOptions);
    }

    public static List<Samochod>? Deserializuj(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            var dto = JsonSerializer.Deserialize<List<SamochodSnapshotDto>>(json, JsonOptions);
            if (dto is null || dto.Count == 0)
                return null;

            return dto
                .Select(s => new Samochod
                {
                    Id = s.Id,
                    Nazwa = s.Nazwa ?? string.Empty,
                    LiczbaPozycji = s.LiczbaPozycji,
                    Typ = (TypSamochodu)s.Typ,
                    Kolejnosc = s.Kolejnosc,
                    CzyAktywny = s.CzyAktywny,
                    CzySprawdzajPoziomNurkowy = s.CzySprawdzajPoziomNurkowy,
                    WymaganeUprawnieniaIds = s.WymaganeUprawnieniaIds ?? []
                })
                .OrderBy(s => s.Kolejnosc)
                .ToList();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Dla zatwierdzonego rozkazu zwraca pojazdy ze snapshota; w przeciwnym razie bieżącą listę.
    /// </summary>
    public static List<Samochod> DlaRozkazu(RozkazDzienny rozkaz, List<Samochod> aktualne)
    {
        if (rozkaz.Status != StatusRozkazu.Zatwierdzony)
            return aktualne;

        var snap = Deserializuj(rozkaz.SamochodySnapshotJson);
        return snap is { Count: > 0 } ? snap : aktualne;
    }

    /// <summary>Wybiera pojazdy występujące w podziale bojowym (kolejność z katalogu).</summary>
    public static List<Samochod> ZPodzialu(IEnumerable<Samochod> zrodlo, IEnumerable<PozycjaSamochodu> podzial)
    {
        var ids = podzial.Select(p => p.SamochodId).Distinct().ToHashSet();
        return zrodlo
            .Where(s => ids.Contains(s.Id))
            .OrderBy(s => s.Kolejnosc)
            .ToList();
    }

    /// <summary>
    /// Nadpisuje nazwy z katalogu wartościami zamrożonymi w wierszach podziału.
    /// Zwraca null, gdy w podziale nie ma żadnej zamrożonej nazwy.
    /// </summary>
    public static List<Samochod>? ZastosujZamrozoneNazwy(
        IReadOnlyList<Samochod> katalog,
        IEnumerable<PozycjaSamochodu> podzial)
    {
        var pozycje = podzial.ToList();
        var nazwy = pozycje
            .Where(p => !string.IsNullOrWhiteSpace(p.NazwaSamochodu))
            .GroupBy(p => p.SamochodId)
            .ToDictionary(g => g.Key, g => g.First().NazwaSamochodu.Trim());

        if (nazwy.Count == 0)
            return null;

        var wynik = new List<Samochod>();
        var uzyte = new HashSet<int>();

        foreach (var s in katalog)
        {
            uzyte.Add(s.Id);
            wynik.Add(nazwy.TryGetValue(s.Id, out var nazwa)
                ? KopiujZNazwa(s, nazwa)
                : s);
        }

        foreach (var (samochodId, nazwa) in nazwy)
        {
            if (uzyte.Contains(samochodId))
                continue;

            var maxPozycja = pozycje.Where(p => p.SamochodId == samochodId).Max(p => p.Pozycja);
            wynik.Add(new Samochod
            {
                Id = samochodId,
                Nazwa = nazwa,
                LiczbaPozycji = Math.Max(maxPozycja, 1),
                Typ = TypSamochodu.Podstawowy,
                Kolejnosc = wynik.Count + 1,
                CzyAktywny = true
            });
        }

        return wynik.OrderBy(s => s.Kolejnosc).ToList();
    }

    private static Samochod KopiujZNazwa(Samochod zrodlo, string nazwa) => new()
    {
        Id = zrodlo.Id,
        Nazwa = nazwa,
        LiczbaPozycji = zrodlo.LiczbaPozycji,
        Typ = zrodlo.Typ,
        Kolejnosc = zrodlo.Kolejnosc,
        CzyAktywny = zrodlo.CzyAktywny,
        CzySprawdzajPoziomNurkowy = zrodlo.CzySprawdzajPoziomNurkowy,
        WymaganeUprawnieniaIds = zrodlo.WymaganeUprawnieniaIds.ToList()
    };

    private sealed class SamochodSnapshotDto
    {
        public int Id { get; set; }
        public string? Nazwa { get; set; }
        public int LiczbaPozycji { get; set; }
        public int Typ { get; set; }
        public int Kolejnosc { get; set; }
        public bool CzyAktywny { get; set; } = true;
        public bool CzySprawdzajPoziomNurkowy { get; set; }
        public List<int>? WymaganeUprawnieniaIds { get; set; }
    }
}
