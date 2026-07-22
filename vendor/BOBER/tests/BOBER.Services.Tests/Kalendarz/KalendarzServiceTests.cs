using BOBER.Core.Constants;
using BOBER.Core.Models;
using BOBER.Data.Repositories;
using BOBER.Services.Grafik;
using BOBER.Services.Kalendarz;

namespace BOBER.Services.Tests.Kalendarz;

public sealed class KalendarzServiceTests
{
    [Fact]
    public async Task UpsertAsync_SingleShift_CreatesOneWpis()
    {
        var repo = new FakeKalendarzRepository();
        var kolory = new FakeKoloryRepository();
        var service = CreateService(repo, kolory);

        await service.UpsertAsync(new DateOnly(2026, 7, 22), [2], "Briefing", "dca");

        Assert.Single(repo.Wpisy);
        Assert.Equal(2, repo.Wpisy[0].ZmianaId);
        Assert.Equal("Briefing", repo.Wpisy[0].Tresc);
        Assert.Equal("dca", repo.Wpisy[0].AutorLogin);
    }

    [Fact]
    public async Task UpsertAsync_AllShifts_CreatesThreeWpisy()
    {
        var repo = new FakeKalendarzRepository();
        var service = CreateService(repo, new FakeKoloryRepository());

        await service.UpsertAsync(new DateOnly(2026, 7, 22), [1, 2, 3], "Dla wszystkich", "dca");

        Assert.Equal(3, repo.Wpisy.Count);
        Assert.Equal(new[] { 1, 2, 3 }, repo.Wpisy.Select(w => w.ZmianaId).OrderBy(x => x));
    }

    [Fact]
    public async Task GetMonthAsync_FiltersByZmiana()
    {
        var repo = new FakeKalendarzRepository();
        var service = CreateService(repo, new FakeKoloryRepository());
        var data = new DateOnly(2026, 7, 22);

        await service.UpsertAsync(data, [1, 2, 3], "X", "dca");
        var filtered = await service.GetMonthAsync(2026, 7, zmianaFilter: 2);

        Assert.Single(filtered);
        Assert.Equal(2, filtered[0].ZmianaId);
    }

    [Fact]
    public async Task MarkAsReadAsync_SetsStatus_AndIsIdempotent()
    {
        var repo = new FakeKalendarzRepository();
        var service = CreateService(repo, new FakeKoloryRepository());
        var data = new DateOnly(2026, 7, 22);

        await service.UpsertAsync(data, [1], "Treść", "dca");
        var wpisId = repo.Wpisy[0].Id;

        await service.MarkAsReadAsync(wpisId, 1, "zmiana1");
        await service.MarkAsReadAsync(wpisId, 1, "zmiana1-ponownie");

        var odczyt = await repo.GetOdczytAsync(wpisId, 1);
        Assert.NotNull(odczyt);
        Assert.True(odczyt!.Przeczytane);
        Assert.Equal("zmiana1", odczyt.PrzeczytanePrzez);
    }

    [Fact]
    public async Task UpsertAsync_ContentChange_ResetsOdczyt()
    {
        var repo = new FakeKalendarzRepository();
        var service = CreateService(repo, new FakeKoloryRepository());
        var data = new DateOnly(2026, 7, 22);

        await service.UpsertAsync(data, [1], "Stara", "dca");
        var wpisId = repo.Wpisy[0].Id;
        await service.MarkAsReadAsync(wpisId, 1, "zmiana1");

        await service.UpsertAsync(data, [1], "Nowa", "dca");

        var odczyt = await repo.GetOdczytAsync(wpisId, 1);
        Assert.Null(odczyt);
    }

    [Fact]
    public async Task GetKoloryZmianAsync_ReturnsDefaultsWhenEmpty()
    {
        var service = CreateService(new FakeKalendarzRepository(), new FakeKoloryRepository());
        var kolory = await service.GetKoloryZmianAsync();

        Assert.Equal(GrafikNurkowyConstants.ColorZmiana1, kolory[1]);
        Assert.Equal(GrafikNurkowyConstants.ColorZmiana2, kolory[2]);
        Assert.Equal(GrafikNurkowyConstants.ColorZmiana3, kolory[3]);
    }

    [Fact]
    public async Task SaveKoloryZmianAsync_PersistsAndPreservesOtherKeys()
    {
        var koloryRepo = new FakeKoloryRepository();
        koloryRepo.Items.Add(new KolorStanowiska { KluczRoli = RoleKeys.DowodcaZmiany, KolorHex = "#AAAAAA" });
        var service = CreateService(new FakeKalendarzRepository(), koloryRepo);

        await service.SaveKoloryZmianAsync(new Dictionary<int, string>
        {
            [1] = "#112233",
            [2] = "#445566",
            [3] = "#778899"
        });

        Assert.Contains(koloryRepo.Items, k => k.KluczRoli == RoleKeys.DowodcaZmiany && k.KolorHex == "#AAAAAA");
        Assert.Contains(koloryRepo.Items, k => k.KluczRoli == RoleKeys.KalendarzZmiana1 && k.KolorHex == "#112233");
        Assert.Contains(koloryRepo.Items, k => k.KluczRoli == RoleKeys.KalendarzZmiana2 && k.KolorHex == "#445566");
        Assert.Contains(koloryRepo.Items, k => k.KluczRoli == RoleKeys.KalendarzZmiana3 && k.KolorHex == "#778899");
    }

    private static KalendarzService CreateService(
        IKalendarzRepository repository,
        IKoloryRepository koloryRepository)
    {
        // Calendar engine nie jest używany w tych testach — stub przez null-object niepotrzebny,
        // ale konstruktor wymaga instancji. Używamy fake ustawień z domyślną datą.
        var ustawienia = new FakeUstawieniaRepository();
        var calendar = new ShiftCalendarEngine(ustawienia);
        return new KalendarzService(repository, koloryRepository, calendar);
    }

    private sealed class FakeKalendarzRepository : IKalendarzRepository
    {
        private int _nextId = 1;
        public List<KalendarzWpis> Wpisy { get; } = [];
        public List<KalendarzOdczyt> Odczyty { get; } = [];

        public Task<IReadOnlyList<KalendarzWpis>> GetByMonthAsync(
            int rok,
            int miesiac,
            int? zmianaFilter = null,
            CancellationToken cancellationToken = default)
        {
            var result = Wpisy
                .Where(w => w.Data.Year == rok && w.Data.Month == miesiac)
                .Where(w => zmianaFilter is null || w.ZmianaId == zmianaFilter)
                .Select(CloneWithOdczyt)
                .ToList();
            return Task.FromResult<IReadOnlyList<KalendarzWpis>>(result);
        }

        public Task<KalendarzWpis?> GetByDateAndZmianaAsync(
            DateOnly data,
            int zmianaId,
            CancellationToken cancellationToken = default)
        {
            var wpis = Wpisy.FirstOrDefault(w => w.Data == data && w.ZmianaId == zmianaId);
            return Task.FromResult(wpis is null ? null : CloneWithOdczyt(wpis));
        }

        public Task<int> UpsertAsync(KalendarzWpis wpis, CancellationToken cancellationToken = default)
        {
            var existing = Wpisy.FirstOrDefault(w => w.Data == wpis.Data && w.ZmianaId == wpis.ZmianaId);
            if (existing is null)
            {
                wpis.Id = _nextId++;
                wpis.DataUtworzenia = DateTime.Now;
                wpis.DataModyfikacji = DateTime.Now;
                Wpisy.Add(wpis);
                return Task.FromResult(wpis.Id);
            }

            var contentChanged = !string.Equals(existing.Tresc, wpis.Tresc, StringComparison.Ordinal);
            existing.Tresc = wpis.Tresc;
            existing.AutorLogin = wpis.AutorLogin;
            existing.DataModyfikacji = DateTime.Now;
            if (contentChanged)
                Odczyty.RemoveAll(o => o.WpisId == existing.Id);

            return Task.FromResult(existing.Id);
        }

        public Task DeleteAsync(int wpisId, CancellationToken cancellationToken = default)
        {
            Wpisy.RemoveAll(w => w.Id == wpisId);
            Odczyty.RemoveAll(o => o.WpisId == wpisId);
            return Task.CompletedTask;
        }

        public Task DeleteByDateAndZmianaAsync(
            DateOnly data,
            int zmianaId,
            CancellationToken cancellationToken = default)
        {
            var existing = Wpisy.FirstOrDefault(w => w.Data == data && w.ZmianaId == zmianaId);
            if (existing is null)
                return Task.CompletedTask;
            return DeleteAsync(existing.Id, cancellationToken);
        }

        public Task ResetOdczytAsync(int wpisId, CancellationToken cancellationToken = default)
        {
            Odczyty.RemoveAll(o => o.WpisId == wpisId);
            return Task.CompletedTask;
        }

        public Task MarkAsReadAsync(
            int wpisId,
            int zmianaId,
            string login,
            CancellationToken cancellationToken = default)
        {
            var existing = Odczyty.FirstOrDefault(o => o.WpisId == wpisId && o.ZmianaId == zmianaId);
            if (existing is not null)
            {
                if (existing.Przeczytane)
                    return Task.CompletedTask;

                existing.Przeczytane = true;
                existing.PrzeczytanePrzez = login;
                existing.DataOdczytu = DateTime.Now;
                return Task.CompletedTask;
            }

            Odczyty.Add(new KalendarzOdczyt
            {
                WpisId = wpisId,
                ZmianaId = zmianaId,
                Przeczytane = true,
                PrzeczytanePrzez = login,
                DataOdczytu = DateTime.Now
            });
            return Task.CompletedTask;
        }

        public Task<KalendarzOdczyt?> GetOdczytAsync(
            int wpisId,
            int zmianaId,
            CancellationToken cancellationToken = default)
        {
            var odczyt = Odczyty.FirstOrDefault(o => o.WpisId == wpisId && o.ZmianaId == zmianaId);
            return Task.FromResult(odczyt);
        }

        private KalendarzWpis CloneWithOdczyt(KalendarzWpis source) =>
            new()
            {
                Id = source.Id,
                Data = source.Data,
                ZmianaId = source.ZmianaId,
                Tresc = source.Tresc,
                AutorLogin = source.AutorLogin,
                DataUtworzenia = source.DataUtworzenia,
                DataModyfikacji = source.DataModyfikacji,
                Odczyt = Odczyty.FirstOrDefault(o => o.WpisId == source.Id && o.ZmianaId == source.ZmianaId)
            };
    }

    private sealed class FakeKoloryRepository : IKoloryRepository
    {
        public List<KolorStanowiska> Items { get; } = [];

        public Task<IReadOnlyList<KolorStanowiska>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<KolorStanowiska>>(Items.ToList());

        public Task SaveAsync(IReadOnlyList<KolorStanowiska> kolory, CancellationToken cancellationToken = default)
        {
            Items.Clear();
            Items.AddRange(kolory);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUstawieniaRepository : IUstawieniaRepository
    {
        private readonly Dictionary<string, string> _values = new()
        {
            ["DataReferencyjna"] = "2026-01-01",
            ["OffsetyZmian"] = "{\"1\":1,\"2\":2,\"3\":0}"
        };

        public Task<string?> GetAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromResult(_values.TryGetValue(key, out var v) ? v : null);

        public Task<int> GetIntAsync(string key, int defaultValue = 0, CancellationToken cancellationToken = default) =>
            Task.FromResult(_values.TryGetValue(key, out var v) && int.TryParse(v, out var n) ? n : defaultValue);

        public Task SetAsync(string key, string value, CancellationToken cancellationToken = default)
        {
            _values[key] = value;
            return Task.CompletedTask;
        }
    }
}
