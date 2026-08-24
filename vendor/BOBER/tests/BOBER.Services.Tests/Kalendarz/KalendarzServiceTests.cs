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
        var filtered = await service.GetMonthAsync(2026, 7, viewerShiftId: 2);

        Assert.Single(filtered);
        Assert.Equal(2, filtered[0].ZmianaId);
    }

    [Fact]
    public async Task AddShiftNoteAsync_ShiftViewSeesPrivateEntries_ButDcaDoesNot()
    {
        var repo = new FakeKalendarzRepository();
        var service = CreateService(repo, new FakeKoloryRepository());
        var data = new DateOnly(2026, 7, 22);

        await service.UpsertAsync(data, [2], "DCA", "dca");
        await service.AddShiftNoteAsync(data, 1, [2], "Prywatna", "zmiana1");

        var recipientView = await service.GetMonthAsync(2026, 7, viewerShiftId: 2, includePrivateEntries: true);
        var authorView = await service.GetMonthAsync(2026, 7, viewerShiftId: 1, includePrivateEntries: true);
        var dcaView = await service.GetMonthAsync(2026, 7);

        Assert.Equal(2, recipientView.Count);
        Assert.Contains(authorView, wpis => wpis.TypWpisu == KalendarzTypWpisu.MiedzyZmianami && wpis.ZmianaId == 2);
        Assert.Single(dcaView.Where(w => w.TypWpisu == KalendarzTypWpisu.Dca));
        Assert.DoesNotContain(dcaView, wpis => wpis.TypWpisu == KalendarzTypWpisu.MiedzyZmianami);
    }

    [Fact]
    public async Task AddDcaReplyAsync_IsVisibleToDca_AndAuthor()
    {
        var repo = new FakeKalendarzRepository();
        var service = CreateService(repo, new FakeKoloryRepository());
        var data = new DateOnly(2026, 7, 22);

        await service.AddDcaReplyAsync(data, 1, "Odpowiedź", "zmiana1");

        var dcaView = await service.GetMonthAsync(2026, 7);
        var authorView = await service.GetMonthAsync(2026, 7, viewerShiftId: 1, includePrivateEntries: true);

        Assert.Contains(dcaView, w => w.TypWpisu == KalendarzTypWpisu.OdpowiedzDca);
        Assert.Contains(authorView, w => w.TypWpisu == KalendarzTypWpisu.OdpowiedzDca);
    }

    [Fact]
    public async Task ApplyAutoDeleteAsync_ForShift_RemovesOldRecipientEntries()
    {
        var repo = new FakeKalendarzRepository();
        var settings = new FakeSettingsService
        {
            AutoDeleteModes = { ["2"] = KalendarzAutoDeleteMode.RazNaMiesiac }
        };
        var service = CreateService(repo, new FakeKoloryRepository(), settings);
        var oldDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(-2));

        await service.UpsertAsync(oldDate, [2], "Stare DCA", "dca");
        await service.AddShiftNoteAsync(oldDate, 1, [2], "Stara prywatna", "zmiana1");

        await service.ApplyAutoDeleteAsync(2, canEditDcaEntries: false);

        var remaining = await service.GetMonthAsync(oldDate.Year, oldDate.Month, viewerShiftId: 2, includePrivateEntries: true);
        Assert.Empty(remaining);
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
    public async Task HasUnreadForRecipientAsync_TrueUntilMarkedAsRead()
    {
        var repo = new FakeKalendarzRepository();
        var service = CreateService(repo, new FakeKoloryRepository());
        var data = new DateOnly(2026, 7, 22);

        await service.UpsertAsync(data, [2], "Briefing", "dca");
        Assert.True(await service.HasUnreadForRecipientAsync(2));
        Assert.False(await service.HasUnreadForRecipientAsync(1));

        await service.MarkAsReadAsync(repo.Wpisy[0].Id, 2, "zmiana2");
        Assert.False(await service.HasUnreadForRecipientAsync(2));
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
        IKoloryRepository koloryRepository,
        FakeSettingsService? settings = null)
    {
        var ustawienia = new FakeUstawieniaRepository();
        var calendar = new ShiftCalendarEngine(ustawienia);
        return new KalendarzService(repository, koloryRepository, calendar, settings ?? new FakeSettingsService());
    }

    private sealed class FakeKalendarzRepository : IKalendarzRepository
    {
        private int _nextId = 1;
        public List<KalendarzWpis> Wpisy { get; } = [];
        public List<KalendarzOdczyt> Odczyty { get; } = [];

        public Task<IReadOnlyList<KalendarzWpis>> GetByMonthAsync(
            int rok,
            int miesiac,
            int? viewerShiftId = null,
            bool includePrivateEntries = false,
            CancellationToken cancellationToken = default)
        {
            var result = Wpisy
                .Where(w => w.Data.Year == rok && w.Data.Month == miesiac)
                .Where(w =>
                    includePrivateEntries && viewerShiftId is not null
                        ? w.ZmianaId == viewerShiftId
                          || (w.TypWpisu == KalendarzTypWpisu.MiedzyZmianami
                              && w.AutorZmianaId == viewerShiftId)
                          || (w.TypWpisu == KalendarzTypWpisu.OdpowiedzDca
                              && w.AutorZmianaId == viewerShiftId)
                        : (w.TypWpisu == KalendarzTypWpisu.Dca
                           || w.TypWpisu == KalendarzTypWpisu.OdpowiedzDca)
                          && (viewerShiftId is null || w.ZmianaId == viewerShiftId))
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
            var existing = Wpisy.FirstOrDefault(w =>
                w.Data == wpis.Data && w.ZmianaId == wpis.ZmianaId && w.TypWpisu == wpis.TypWpisu);
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

        public Task<int> AddAsync(KalendarzWpis wpis, CancellationToken cancellationToken = default)
        {
            wpis.Id = _nextId++;
            wpis.DataUtworzenia = DateTime.Now;
            wpis.DataModyfikacji = DateTime.Now;
            Wpisy.Add(wpis);
            return Task.FromResult(wpis.Id);
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

        public Task DeleteOlderThanAsync(
            DateOnly thresholdDate,
            KalendarzTypWpisu typWpisu,
            int? recipientShiftId = null,
            CancellationToken cancellationToken = default)
        {
            var ids = Wpisy
                .Where(w => w.Data < thresholdDate && w.TypWpisu == typWpisu)
                .Where(w => recipientShiftId is null || w.ZmianaId == recipientShiftId)
                .Select(w => w.Id)
                .ToList();
            Wpisy.RemoveAll(w => ids.Contains(w.Id));
            Odczyty.RemoveAll(o => ids.Contains(o.WpisId));
            return Task.CompletedTask;
        }

        public Task<bool> HasUnreadForRecipientAsync(
            int zmianaId,
            CancellationToken cancellationToken = default)
        {
            var hasUnread = Wpisy
                .Where(w => w.ZmianaId == zmianaId)
                .Any(w =>
                {
                    var odczyt = Odczyty.FirstOrDefault(o => o.WpisId == w.Id && o.ZmianaId == zmianaId);
                    return odczyt?.Przeczytane != true;
                });
            return Task.FromResult(hasUnread);
        }

        private KalendarzWpis CloneWithOdczyt(KalendarzWpis source) =>
            new()
            {
                Id = source.Id,
                Data = source.Data,
                ZmianaId = source.ZmianaId,
                TypWpisu = source.TypWpisu,
                AutorZmianaId = source.AutorZmianaId,
                Tresc = source.Tresc,
                AutorLogin = source.AutorLogin,
                DataUtworzenia = source.DataUtworzenia,
                DataModyfikacji = source.DataModyfikacji,
                Odczyt = Odczyty.FirstOrDefault(o => o.WpisId == source.Id && o.ZmianaId == source.ZmianaId)
            };
    }

    private sealed class FakeSettingsService : BOBER.Services.Settings.ISettingsService
    {
        public Dictionary<string, KalendarzAutoDeleteMode> AutoDeleteModes { get; } = [];

        public Task<string> GetChomikDbPathAsync(CancellationToken cancellationToken = default) => Task.FromResult(string.Empty);
        public Task SetChomikDbPathAsync(string path, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<int> GetStanZmianyAsync(int zmianaId, CancellationToken cancellationToken = default) => Task.FromResult(10);
        public Task SetStanZmianyAsync(int zmianaId, int stan, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<int> GetStanMinimalnyAsync(int zmianaId, CancellationToken cancellationToken = default) => Task.FromResult(6);
        public Task SetStanMinimalnyAsync(int zmianaId, int stan, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<int> GetMaxUrlopowNaSluzbieAsync(int zmianaId, CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task SetMaxUrlopowNaSluzbieAsync(int zmianaId, int max, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<string> GetExportPathRozkazyAsync(CancellationToken cancellationToken = default) => Task.FromResult(string.Empty);
        public Task SetExportPathRozkazyAsync(string path, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<string> GetExportPathGrafikSluzbAsync(CancellationToken cancellationToken = default) => Task.FromResult(string.Empty);
        public Task SetExportPathGrafikSluzbAsync(string path, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<string> GetExportPathGrafikNurkowyAsync(CancellationToken cancellationToken = default) => Task.FromResult(string.Empty);
        public Task SetExportPathGrafikNurkowyAsync(string path, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<bool> GetLessColorAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task SetLessColorAsync(bool enabled, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<GrafikRowColorSettings> GetGrafikRowColorSettingsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new GrafikRowColorSettings());
        public Task SetGrafikRowColorSettingsAsync(GrafikRowColorSettings settings, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
        public Task<KalendarzAutoDeleteMode> GetKalendarzAutoDeleteModeAsync(int? shiftNumber, CancellationToken cancellationToken = default) =>
            Task.FromResult(AutoDeleteModes.TryGetValue(BuildKey(shiftNumber), out var mode) ? mode : KalendarzAutoDeleteMode.Nigdy);
        public Task SetKalendarzAutoDeleteModeAsync(int? shiftNumber, KalendarzAutoDeleteMode mode, CancellationToken cancellationToken = default)
        {
            AutoDeleteModes[BuildKey(shiftNumber)] = mode;
            return Task.CompletedTask;
        }

        private static string BuildKey(int? shiftNumber) => shiftNumber?.ToString() ?? "DCA";
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
