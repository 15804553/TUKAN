using SKRYBEK.Core.Enums;
using SKRYBEK.Core.Models;
using SKRYBEK.Data.Grafik;
using SKRYBEK.Data.Repositories;
using SKRYBEK.Services.Logging;

namespace SKRYBEK.Services.Personnel;

public sealed class PersonnelService
{
    private readonly PersonnelRepository _repo;
    private readonly ShiftCalendarEngine _calendar;

    public PersonnelService(PersonnelRepository repo, ShiftCalendarEngine calendar)
    {
        _repo     = repo;
        _calendar = calendar;
    }

    public async Task<List<Funkcjonariusz>> GetDostepniAsync(DateOnly data, int nrZmiany)
    {
        var wszyscy = await _repo.GetByZmianaAsync(nrZmiany);
        SkrybekLog.Info($"CHOMIK — zmiana {nrZmiany}: {wszyscy.Count} funkcjonariuszy");

        var dzienSluzby = await _calendar.IsWorkDayAsync(nrZmiany, data);
        SkrybekLog.Info($"Kalendarz — {data:yyyy-MM-dd}, zmiana {nrZmiany}, dzień służby: {dzienSluzby}");

        var lista = await _repo.GetDostepniWDniuAsync(data, nrZmiany, wszyscy);
        SkrybekLog.Info($"Personel dostępny na {data:yyyy-MM-dd}: {lista.Count} osób");

        return lista;
    }

    /// <summary>Następny dzień służby zmiany po podanej dacie (bez dnia „po”).</summary>
    public Task<DateOnly> GetNastepnyDzienSluzbyPoAsync(int nrZmiany, DateOnly poDniu) =>
        _calendar.GetNextWorkDayAfterAsync(nrZmiany, poDniu);

    public Task<bool> CzyDzienSluzbyAsync(int nrZmiany, DateOnly data) =>
        _calendar.IsWorkDayAsync(nrZmiany, data);

    public async Task<List<Funkcjonariusz>> GetWszyscyZmianaAsync(int nrZmiany)
    {
        return await _repo.GetByZmianaAsync(nrZmiany);
    }

    public List<Funkcjonariusz> FiltrujWgUprawnieniami(
        IEnumerable<Funkcjonariusz> lista,
        IReadOnlyCollection<int> wymaganeTypyIds)
    {
        if (wymaganeTypyIds.Count == 0)
            return lista.ToList();

        return lista
            .Where(f => wymaganeTypyIds.All(id => f.IdUprawnien.Contains(id)))
            .ToList();
    }

    /// <summary>
    /// Pobiera nieobecnych w danym dniu z BOBER i zwraca jako listę NieobecnyWSluzbie
    /// wstępnie wypełnionych danymi z CHOMIK. Zwraca pustą listę gdy BOBER niedostępny.
    /// </summary>
    public async Task<List<NieobecnyWSluzbie>> GetNieobecniWDniuAsync(
        DateOnly data, int nrZmiany, IReadOnlyList<Funkcjonariusz> wszyscy)
    {
        var nieobecniZBober = await _repo.PobierzNieobecnychZTypemAsync(data, nrZmiany);
        if (nieobecniZBober.Count == 0) return [];

        var personelPoId = wszyscy.ToDictionary(f => f.Id);
        var wynik = new List<NieobecnyWSluzbie>();

        foreach (var (fid, typ) in nieobecniZBober)
        {
            personelPoId.TryGetValue(fid, out var osoba);
            wynik.Add(new NieobecnyWSluzbie
            {
                FunkcjonariuszId = fid,
                Nazwisko = osoba is not null
                    ? $"{osoba.Stopien} {osoba.Nazwisko}".Trim()
                    : $"ID:{fid}",
                TypNieobecnosci = typ
            });
        }

        var uzupelniony = UzupelnijDyzuryOWolnaSluzbe(wynik);
        SkrybekLog.Info($"BOBER — nieobecni na {data:yyyy-MM-dd}: {uzupelniony.Count} osób");
        return uzupelniony;
    }

    /// <summary>
    /// Osoby z dyżuru domowego muszą być też wpisane jako wolna służba (w dwóch sekcjach).
    /// </summary>
    public static List<NieobecnyWSluzbie> UzupelnijDyzuryOWolnaSluzbe(IEnumerable<NieobecnyWSluzbie> nieobecni)
    {
        var wynik = nieobecni.ToList();
        var wolnaPoId = wynik
            .Where(n => n.TypNieobecnosci == TypNieobecnosci.CzasWolny && n.FunkcjonariuszId.HasValue)
            .Select(n => n.FunkcjonariuszId!.Value)
            .ToHashSet();
        var wolnaPoNazwisku = wynik
            .Where(n => n.TypNieobecnosci == TypNieobecnosci.CzasWolny && !string.IsNullOrWhiteSpace(n.Nazwisko))
            .Select(n => n.Nazwisko.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var dyzur in wynik
                     .Where(n => n.TypNieobecnosci == TypNieobecnosci.DyzurDomowy)
                     .ToList())
        {
            if (dyzur.FunkcjonariuszId is int fid && wolnaPoId.Contains(fid))
                continue;

            var nazwisko = dyzur.Nazwisko?.Trim() ?? string.Empty;
            if (dyzur.FunkcjonariuszId is null &&
                !string.IsNullOrWhiteSpace(nazwisko) &&
                wolnaPoNazwisku.Contains(nazwisko))
                continue;

            if (string.IsNullOrWhiteSpace(nazwisko) && dyzur.FunkcjonariuszId is null)
                continue;

            wynik.Add(new NieobecnyWSluzbie
            {
                FunkcjonariuszId = dyzur.FunkcjonariuszId,
                Nazwisko = dyzur.Nazwisko,
                TypNieobecnosci = TypNieobecnosci.CzasWolny
            });

            if (dyzur.FunkcjonariuszId is int noweId)
                wolnaPoId.Add(noweId);
            if (!string.IsNullOrWhiteSpace(nazwisko))
                wolnaPoNazwisku.Add(nazwisko);
        }

        return wynik;
    }

    public Task<List<(int Id, string Nazwa)>> GetTypyUprawnienAsync()
        => _repo.GetTypyUprawnienAsync();
}
