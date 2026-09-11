using BOBER.Core.Constants;
using BOBER.Core.Models;
using BOBER.Data.Repositories;
using BOBER.Services.Grafik;
using BOBER.Services.GrafikNurkowy;
using BOBER.Services.Kalendarz;
using BOBER.Services.Personnel;
using BOBER.Services.Settings;

namespace BOBER.Services.Tests.GrafikNurkowy;

public sealed class GrafikNurkowyBlokadaTests
{
    [Fact]
    public async Task CofnijZatwierdzenieAsync_SendsCalendarBroadcastToAllShifts()
    {
        var zatwierdzenia = new FakeZatwierdzeniaRepository();
        var kalendarz = new RecordingKalendarzService();
        var service = CreateService(zatwierdzenia, kalendarz);

        await zatwierdzenia.SetZatwierdzenieAsync(2026, 9, true, "dca.jrg");
        await service.CofnijZatwierdzenieAsync(2026, 9, "dca.jrg");

        Assert.False(await service.IsZatwierdzonyAsync(2026, 9));
        Assert.Single(kalendarz.Broadcasts);
        Assert.Equal(DateOnly.FromDateTime(DateTime.Today), kalendarz.Broadcasts[0].Data);
        Assert.Equal("dca.jrg", kalendarz.Broadcasts[0].AutorLogin);
        Assert.Contains("odblokował", kalendarz.Broadcasts[0].Tresc);
        Assert.Contains("Wrzesień 2026", kalendarz.Broadcasts[0].Tresc);
    }

    [Fact]
    public async Task ZatwierdzAsync_SendsCalendarBroadcast_WhenFileExists()
    {
        var dir = Path.Combine(Path.GetTempPath(), "tukan-grafik-nurkowy-blokada");
        Directory.CreateDirectory(dir);
        var filePath = Path.Combine(dir, GrafikNurkowyConstants.BuildFileName(9, 2026));
        await File.WriteAllTextAsync(filePath, "placeholder");

        try
        {
            var zatwierdzenia = new FakeZatwierdzeniaRepository();
            var kalendarz = new RecordingKalendarzService();
            var settings = new PathSettingsService(dir);
            var service = CreateService(zatwierdzenia, kalendarz, settings);

            await service.ZatwierdzAsync(2026, 9, "dca.jrg");

            Assert.True(await service.IsZatwierdzonyAsync(2026, 9));
            Assert.Single(kalendarz.Broadcasts);
            Assert.Contains("zablokował", kalendarz.Broadcasts[0].Tresc);
            Assert.Contains("Wrzesień 2026", kalendarz.Broadcasts[0].Tresc);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    private static GrafikNurkowyService CreateService(
        IGrafikNurkowyRepository zatwierdzenia,
        IKalendarzService kalendarz,
        ISettingsService? settings = null)
    {
        var ustawienia = new FakeUstawieniaRepository();
        return new GrafikNurkowyService(
            new FakeGrafikRepository(),
            zatwierdzenia,
            new ShiftCalendarEngine(ustawienia),
            new FakeFunkcjonariuszService(),
            settings ?? new PathSettingsService(string.Empty),
            new GrafikNurkowyExcelService(),
            kalendarz);
    }

    private sealed class FakeZatwierdzeniaRepository : IGrafikNurkowyRepository
    {
        private readonly Dictionary<(int Rok, int Miesiac), GrafikNurkowyZatwierdzenie> _items = [];

        public Task<GrafikNurkowyZatwierdzenie?> GetAsync(
            int rok, int miesiac, CancellationToken cancellationToken = default)
        {
            _items.TryGetValue((rok, miesiac), out var item);
            return Task.FromResult(item);
        }

        public Task SetZatwierdzenieAsync(
            int rok, int miesiac, bool zatwierdzony, string? zatwierdzonyPrzez,
            CancellationToken cancellationToken = default)
        {
            _items[(rok, miesiac)] = new GrafikNurkowyZatwierdzenie
            {
                Rok = rok,
                Miesiac = miesiac,
                Zatwierdzony = zatwierdzony,
                ZatwierdzonyPrzez = zatwierdzonyPrzez,
                DataZatwierdzenia = zatwierdzony ? DateTime.Now : null
            };
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingKalendarzService : IKalendarzService
    {
        public List<(DateOnly Data, string Tresc, string AutorLogin)> Broadcasts { get; } = [];

        public Task AddDcaBroadcastAsync(
            DateOnly data, string tresc, string autorLogin,
            CancellationToken cancellationToken = default)
        {
            Broadcasts.Add((data, tresc, autorLogin));
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<KalendarzWpis>> GetMonthAsync(
            int rok, int miesiac, int? viewerShiftId = null, bool includePrivateEntries = false,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<KalendarzWpis>>([]);

        public Task UpsertAsync(
            DateOnly data, IReadOnlyList<int> zmianaIds, string tresc, string autorLogin,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task DeleteAsync(
            DateOnly data, IReadOnlyList<int> zmianaIds,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task MarkAsReadAsync(
            int wpisId, int zmianaId, string login,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task AddShiftNoteAsync(
            DateOnly data, int authorShiftId, IReadOnlyList<int> recipientShiftIds,
            string tresc, string autorLogin, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task AddDcaReplyAsync(
            DateOnly data, int authorShiftId, string tresc, string autorLogin,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task DeleteManyAsync(
            IReadOnlyList<int> wpisIds, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyDictionary<int, string>> GetKoloryZmianAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<int, string>>(new Dictionary<int, string>());

        public Task SaveKoloryZmianAsync(
            IReadOnlyDictionary<int, string> kolory, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<KalendarzAutoDeleteMode> GetAutoDeleteModeAsync(
            int? shiftNumber, CancellationToken cancellationToken = default) =>
            Task.FromResult(KalendarzAutoDeleteMode.Nigdy);

        public Task SaveAutoDeleteModeAsync(
            int? shiftNumber, KalendarzAutoDeleteMode mode,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task ApplyAutoDeleteAsync(
            int? shiftNumber, bool canEditDcaEntries,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<bool> HasUnreadForRecipientAsync(
            int zmianaId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<IReadOnlyDictionary<int, int>> GetWorkingShiftsForMonthAsync(
            int rok, int miesiac, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<int, int>>(new Dictionary<int, int>());

        public Task<int> GetWorkingShiftAsync(
            DateOnly data, CancellationToken cancellationToken = default) =>
            Task.FromResult(1);
    }

    private sealed class FakeGrafikRepository : IGrafikRepository
    {
        public Task<IReadOnlyList<GrafikWpis>> GetByZmianaAndYearAsync(
            int zmianaId, int rok, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<GrafikWpis>>([]);

        public Task<IReadOnlyList<GrafikWpis>> GetByZmianaAndMonthAsync(
            int zmianaId, int rok, int miesiac, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<GrafikWpis>>([]);

        public Task UpsertAsync(GrafikWpis wpis, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task ApplyBatchAsync(
            IReadOnlyList<GrafikWpis> upserts, IReadOnlyList<GrafikWpis> deletes,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task DeleteAsync(
            int funkcjonariuszId, int zmianaId, int rok, int miesiac, int dzien,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task DeleteByHalfYearAsync(
            int zmianaId, int rok, int polrocze, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class FakeFunkcjonariuszService : IFunkcjonariuszService
    {
        public Task<IReadOnlyList<Funkcjonariusz>> GetByZmianaAsync(
            int zmianaId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Funkcjonariusz>>([]);
    }

    private sealed class FakeUstawieniaRepository : IUstawieniaRepository
    {
        public Task<string?> GetAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(null);

        public Task<int> GetIntAsync(string key, int defaultValue = 0, CancellationToken cancellationToken = default) =>
            Task.FromResult(defaultValue);

        public Task SetAsync(string key, string value, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class PathSettingsService(string exportPath) : ISettingsService
    {
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
        public Task<string> GetExportPathGrafikNurkowyAsync(CancellationToken cancellationToken = default) => Task.FromResult(exportPath);
        public Task SetExportPathGrafikNurkowyAsync(string path, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<string> GetBackupPathAsync(CancellationToken cancellationToken = default) => Task.FromResult(string.Empty);
        public Task SetBackupPathAsync(string path, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<int> GetBackupRetentionMonthsAsync(CancellationToken cancellationToken = default) => Task.FromResult(6);
        public Task SetBackupRetentionMonthsAsync(int months, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<bool> GetLessColorAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task SetLessColorAsync(bool enabled, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<GrafikRowColorSettings> GetGrafikRowColorSettingsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new GrafikRowColorSettings());
        public Task SetGrafikRowColorSettingsAsync(GrafikRowColorSettings settings, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
        public Task<GrafikExportAlternatingSettings> GetGrafikExportAlternatingSettingsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new GrafikExportAlternatingSettings());
        public Task SetGrafikExportAlternatingSettingsAsync(GrafikExportAlternatingSettings settings, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
        public Task<KalendarzAutoDeleteMode> GetKalendarzAutoDeleteModeAsync(int? shiftNumber, CancellationToken cancellationToken = default) =>
            Task.FromResult(KalendarzAutoDeleteMode.Nigdy);
        public Task SetKalendarzAutoDeleteModeAsync(int? shiftNumber, KalendarzAutoDeleteMode mode, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
