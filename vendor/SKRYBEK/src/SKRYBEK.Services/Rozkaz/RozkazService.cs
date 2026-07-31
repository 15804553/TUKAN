using SKRYBEK.Core.Enums;
using SKRYBEK.Core.Models;
using SKRYBEK.Core.Rules;
using SKRYBEK.Data.Repositories;
using SKRYBEK.Services.Logging;

namespace SKRYBEK.Services.Rozkaz;

public sealed class RozkazService
{
    private readonly RozkazRepository _repo;
    private readonly SamochodyRepository _samochodyRepo;

    public RozkazService(RozkazRepository repo, SamochodyRepository samochodyRepo)
    {
        _repo         = repo;
        _samochodyRepo = samochodyRepo;
    }

    public async Task<List<RozkazDzienny>> GetByRokAsync(int rok)
        => await _repo.GetByRokAsync(rok);

    public async Task<RozkazDzienny?> GetByIdAsync(int id)
        => await _repo.GetByIdAsync(id);

    public async Task<RozkazDzienny> NowyRozkazAsync(DateOnly data, int nrZmiany)
    {
        // Numer = dzień roku wybranej daty (umożliwia rozkaz z datą wsteczną)
        var numer = data.DayOfYear;
        var samochody = await _samochodyRepo.GetAktywneAsync();

        var rozkaz = new RozkazDzienny
        {
            NumerRozkazu   = numer,
            Rok            = data.Year,
            Data           = data,
            ZmianaId       = nrZmiany,
            DataUtworzenia = DateTime.Now,
            Status         = StatusRozkazu.Roboczy,
            Zajecia        = "Według planu doskonalenia zawodowego"
        };

        // Inicjalizuj 9 stałych pozycji SŁUŻBA
        foreach (StanowiskoSluzby stanowisko in Enum.GetValues<StanowiskoSluzby>())
        {
            rozkaz.Sluzba.Add(new PozycjaSluzby { Stanowisko = stanowisko });
        }

        // Inicjalizuj pozycje dla aktywnych samochodów
        foreach (var s in samochody)
        {
            for (int poz = 1; poz <= s.LiczbaPozycji; poz++)
            {
                rozkaz.PodzialBojowy.Add(new PozycjaSamochodu
                {
                    SamochodId = s.Id,
                    Pozycja    = poz
                });
            }
        }

        // 2 pozycje ratowników medycznych
        rozkaz.RatwnicyMedyczni.Add(new RatownikMedyczny { Pozycja = 1 });
        rozkaz.RatwnicyMedyczni.Add(new RatownikMedyczny { Pozycja = 2 });

        return rozkaz;
    }

    public async Task<int> ZapiszAsync(
        RozkazDzienny rozkaz,
        IReadOnlyList<Funkcjonariusz>? personel = null)
    {
        await WalidujUnikalnoscAsync(rozkaz);
        var samochody = await _samochodyRepo.GetAktywneAsync();
        ValidatePodzialBojowy(rozkaz, samochody, personel);
        var id = await _repo.SaveAsync(rozkaz);
        SkrybekLog.Info($"Zapisano rozkaz nr {rozkaz.NumerRozkazu}/{rozkaz.Rok}, Id={id}");
        return id;
    }

    /// <summary>
    /// Zwraca komunikat konfliktu, gdy istnieje już rozkaz o tej dacie lub numerze w roku; w przeciwnym razie null.
    /// </summary>
    public async Task<string?> SprawdzUnikalnoscAsync(
        DateOnly data,
        int numerRozkazu,
        int rok,
        int excludeId = 0)
    {
        var konflikt = await _repo.ZnajdzKonfliktAsync(data, numerRozkazu, rok, excludeId);
        if (konflikt is null)
            return null;

        if (konflikt.Data == data && konflikt.NumerRozkazu == numerRozkazu && konflikt.Rok == rok)
        {
            return $"Rozkaz nr {konflikt.NumerFormatowany} na dzień {konflikt.DataFormatowana} już istnieje.";
        }

        if (konflikt.Data == data)
        {
            return $"Na dzień {data:dd.MM.yyyy} istnieje już rozkaz nr {konflikt.NumerFormatowany}.";
        }

        return $"Rozkaz nr {numerRozkazu}/{rok} już istnieje (data: {konflikt.DataFormatowana}).";
    }

    private async Task WalidujUnikalnoscAsync(RozkazDzienny rozkaz)
    {
        var komunikat = await SprawdzUnikalnoscAsync(
            rozkaz.Data, rozkaz.NumerRozkazu, rozkaz.Rok, rozkaz.Id);
        if (komunikat is not null)
            throw new InvalidOperationException(komunikat);
    }

    public async Task UpdateStatusAsync(int id, StatusRozkazu status)
    {
        await _repo.UpdateStatusAsync(id, status);
        SkrybekLog.Info($"Status rozkazu Id={id} zmieniony na {status}");
    }

    public async Task UpdateSamochodySnapshotAsync(int id, string snapshotJson)
        => await _repo.UpdateSamochodySnapshotAsync(id, snapshotJson);

    public async Task UsunAsync(int id)
    {
        await _repo.DeleteAsync(id);
        SkrybekLog.Info($"Usunięto rozkaz Id={id}");
    }

    /// <summary>
    /// Sprawdza konflikt: czy dana osoba jest już przypisana do innego pojazdu podstawowego.
    /// Zwraca true jeśli przypisanie jest dozwolone.
    /// </summary>
    public static bool MoznaAssignowacDoPodstawowego(
        RozkazDzienny rozkaz,
        int funkcjonariuszId,
        int docelowySamochodId,
        IEnumerable<Samochod> wszystkieSamochody)
    {
        var podstawoweIds = wszystkieSamochody
            .Where(s => s.CzyPodstawowy && s.Id != docelowySamochodId)
            .Select(s => s.Id)
            .ToHashSet();

        return !rozkaz.PodzialBojowy.Any(p =>
            p.FunkcjonariuszId == funkcjonariuszId &&
            podstawoweIds.Contains(p.SamochodId));
    }

    public static void ValidatePodzialBojowy(
        RozkazDzienny rozkaz,
        IReadOnlyList<Samochod> samochody,
        IReadOnlyList<Funkcjonariusz>? personel = null)
    {
        var samochodPoId = samochody.ToDictionary(s => s.Id);
        var personelPoId = personel?.ToDictionary(p => p.Id);

        if (personelPoId is not null)
        {
            foreach (var pozycja in rozkaz.PodzialBojowy.Where(p => p.FunkcjonariuszId.HasValue))
            {
                if (!samochodPoId.TryGetValue(pozycja.SamochodId, out var samochod))
                    continue;

                if (!personelPoId.TryGetValue(pozycja.FunkcjonariuszId!.Value, out var osoba))
                    continue;

                if (!PozycjaSamochoduRules.CzyOsobaDozwolonaNaPozycji(osoba, pozycja.Pozycja))
                {
                    throw new InvalidOperationException(
                        $"{osoba.StopienINazwisko} — pozycja {PozycjaSamochoduRules.EtykietaPozycji(pozycja.Pozycja)} " +
                        $"w pojeździe „{samochod.Nazwa}”: {PozycjaSamochoduRules.OpisWymagania(pozycja.Pozycja)}");
                }
            }
        }

        var podstawoweIds = samochody.Where(s => s.CzyPodstawowy).Select(s => s.Id).ToHashSet();
        var duplikaty = rozkaz.PodzialBojowy
            .Where(p => p.FunkcjonariuszId.HasValue && podstawoweIds.Contains(p.SamochodId))
            .GroupBy(p => p.FunkcjonariuszId!.Value)
            .Where(g => g.Count() > 1)
            .ToList();

        if (duplikaty.Count > 0)
        {
            var nazwisko = duplikaty[0].First().Nazwisko;
            throw new InvalidOperationException(
                $"Osoba {nazwisko} jest przypisana do więcej niż jednego pojazdu podstawowego. " +
                "Ta sama osoba nie może siedzieć na dwóch pojazdach podstawowych.");
        }
    }
}
