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
        // Numer i rok z „Kraków, dn.” = dzień służby minus cykl zmian (3 dni)
        var dataWystawienia = data.AddDays(-3);
        var numer = dataWystawienia.DayOfYear;
        var samochody = await _samochodyRepo.GetAktywneAsync();

        var rozkaz = new RozkazDzienny
        {
            NumerRozkazu   = numer,
            Rok            = dataWystawienia.Year,
            Data           = data,
            ZmianaId       = nrZmiany,
            DataUtworzenia = dataWystawienia.ToDateTime(TimeOnly.FromDateTime(DateTime.Now)),
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
        RozkazDzienny? before = null;
        if (rozkaz.Id > 0)
            before = await _repo.GetByIdAsync(rozkaz.Id);

        await WalidujUnikalnoscAsync(rozkaz);
        var samochody = await _samochodyRepo.GetAktywneAsync();
        ValidateSluzba(rozkaz);
        ValidatePodzialBojowy(rozkaz, samochody, personel);
        var id = await _repo.SaveAsync(rozkaz);
        SkrybekLog.Info($"Zapisano rozkaz nr {rozkaz.NumerRozkazu}/{rozkaz.Rok}, Id={id}");

        await TryAuditRozkazSaveAsync(before, rozkaz, samochody);
        return id;
    }

    private static async Task TryAuditRozkazSaveAsync(
        RozkazDzienny? before,
        RozkazDzienny after,
        IReadOnlyList<Samochod> samochody)
    {
        var append = SKRYBEK.Core.Audit.GuestChangeAudit.TryAppendAsync;
        if (append is null)
            return;

        if (before is null)
        {
            await append("Rozkazy", $"Rozkazy dzienne dodano nr {after.NumerRozkazu}/{after.Rok}");
            return;
        }

        var samochodPoId = samochody.ToDictionary(s => s.Id);
        var beforeFilled = before.PodzialBojowy
            .Where(p => p.FunkcjonariuszId.HasValue)
            .Select(p => FormatPozycja(p, samochodPoId))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var afterFilled = after.PodzialBojowy
            .Where(p => p.FunkcjonariuszId.HasValue)
            .Select(p => FormatPozycja(p, samochodPoId))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var added in afterFilled.Except(beforeFilled, StringComparer.OrdinalIgnoreCase))
            await append("Rozkazy", $"dodano {added}");

        foreach (var removed in beforeFilled.Except(afterFilled, StringComparer.OrdinalIgnoreCase))
            await append("Rozkazy", $"usunieto {removed}");

        if (beforeFilled.SetEquals(afterFilled))
        {
            await append("Rozkazy", $"Rozkazy dzienne zapisano nr {after.NumerRozkazu}/{after.Rok}");
        }
    }

    private static string FormatPozycja(PozycjaSamochodu p, IReadOnlyDictionary<int, Samochod> samochody)
    {
        var nazwa = !string.IsNullOrWhiteSpace(p.NazwaSamochodu)
            ? p.NazwaSamochodu
            : samochody.TryGetValue(p.SamochodId, out var s) ? s.Nazwa : $"pojazd {p.SamochodId}";
        var max = samochody.TryGetValue(p.SamochodId, out var samochód)
            ? samochód.LiczbaPozycji
            : p.Pozycja;
        return $"{nazwa} {p.Pozycja}/{max}";
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

    /// <summary>
    /// Zatwierdza wszystkie robocze rozkazy w roku; gdy brak roboczych — odblokowuje wszystkie zatwierdzone.
    /// </summary>
    /// <returns>Liczba zmienionych rozkazów oraz czy wykonano zatwierdzenie (true) czy odblokowanie (false).</returns>
    public async Task<(int zmienionych, bool zatwierdzono)> ZatwierdzLubOdblokujWszystkieAsync(int rok)
    {
        var lista = await _repo.GetByRokAsync(rok);
        if (lista.Count == 0)
            return (0, true);

        if (RozkazZatwierdzanieRules.CzyZatwierdzicWszystkie(lista))
        {
            var doZatwierdzenia = RozkazZatwierdzanieRules.FiltrujDoZatwierdzenia(lista);
            foreach (var r in doZatwierdzenia)
                await UpdateStatusAsync(r.Id, StatusRozkazu.Zatwierdzony);

            SkrybekLog.Info($"Zatwierdzono zbiorczo {doZatwierdzenia.Count} rozkazów za rok {rok}");
            return (doZatwierdzenia.Count, true);
        }

        var doOdblokowania = RozkazZatwierdzanieRules.FiltrujDoOdblokowania(lista);
        foreach (var r in doOdblokowania)
            await UpdateStatusAsync(r.Id, StatusRozkazu.Roboczy);

        SkrybekLog.Info($"Odblokowano zbiorczo {doOdblokowania.Count} rozkazów za rok {rok}");
        return (doOdblokowania.Count, false);
    }

    public async Task UpdateSamochodySnapshotAsync(int id, string snapshotJson)
        => await _repo.UpdateSamochodySnapshotAsync(id, snapshotJson);

    public async Task UsunAsync(int id)
    {
        var before = await _repo.GetByIdAsync(id);
        await _repo.DeleteAsync(id);
        SkrybekLog.Info($"Usunięto rozkaz Id={id}");

        var append = SKRYBEK.Core.Audit.GuestChangeAudit.TryAppendAsync;
        if (append is not null && before is not null)
        {
            await append("Rozkazy", $"usunieto rozkaz nr {before.NumerRozkazu}/{before.Rok}");
        }
    }

    /// <summary>
    /// Sprawdza konflikt: czy dana osoba jest już na innym miejscu pojazdu podstawowego
    /// (ten sam pojazd albo inny podstawowy). Zwraca true, jeśli przypisanie jest dozwolone.
    /// </summary>
    public static bool MoznaAssignowacDoPodstawowego(
        RozkazDzienny rozkaz,
        int funkcjonariuszId,
        int docelowySamochodId,
        int docelowaPozycja,
        IEnumerable<Samochod> wszystkieSamochody)
        => !PodzialBojowyRules.CzyKonfliktPodstawowy(
            rozkaz.PodzialBojowy,
            wszystkieSamochody,
            funkcjonariuszId,
            docelowySamochodId,
            docelowaPozycja);

    public static void ValidateSluzba(RozkazDzienny rozkaz)
    {
        var konflikt = StanowiskoSluzbyRules.ZnajdzKonfliktWylacznosciWSluzbie(rozkaz.Sluzba);
        if (konflikt is not null)
            throw new InvalidOperationException(konflikt);
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

        var komunikatDuplikatu = PodzialBojowyRules.ZnajdzKomunikatDuplikatuNaPodstawowych(
            rozkaz.PodzialBojowy, samochody);
        if (komunikatDuplikatu is not null)
            throw new InvalidOperationException(komunikatDuplikatu);

        var podstawoweIds = samochody.Where(s => s.CzyPodstawowy).Select(s => s.Id).ToHashSet();
        var paIds = rozkaz.Sluzba
            .Where(s => s.Stanowisko == StanowiskoSluzby.DyzurnyPAJRG && s.FunkcjonariuszId.HasValue)
            .Select(s => s.FunkcjonariuszId!.Value)
            .ToHashSet();

        if (paIds.Count > 0)
        {
            var konfliktPa = rozkaz.PodzialBojowy.FirstOrDefault(p =>
                p.FunkcjonariuszId.HasValue
                && paIds.Contains(p.FunkcjonariuszId.Value)
                && podstawoweIds.Contains(p.SamochodId));

            if (konfliktPa is not null)
            {
                throw new InvalidOperationException(
                    $"Osoba {konfliktPa.Nazwisko} jest dyżurnym PA JRG i nie może być przypisana do pojazdu podstawowego.");
            }
        }
    }
}
