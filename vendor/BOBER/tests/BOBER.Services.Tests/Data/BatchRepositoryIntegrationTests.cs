using System.Diagnostics;
using System.Data.OleDb;
using BOBER.Core.Models;
using BOBER.Data;
using BOBER.Data.Database;
using BOBER.Data.Repositories;
using Xunit.Abstractions;

namespace BOBER.Services.Tests.Data;

public sealed class BatchRepositoryIntegrationTests : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private string _root = null!;
    private BoberDatabaseOptions _options = null!;
    private GrafikRepository _repository = null!;
    private UrlopPlanRepository _urlopPlanRepository = null!;
    private KalendarzRepository _kalendarzRepository = null!;

    public BatchRepositoryIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public async Task InitializeAsync()
    {
        _root = Path.Combine(Path.GetTempPath(), $"TukanBatchTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_root);
        _options = new BoberDatabaseOptions
        {
            FilePath = Path.Combine(_root, "BatchTest.accdb"),
            DatabasePassword = "5359",
            UseDatabasePassword = true
        };
        await new DatabaseBootstrapper(_options).EnsureReadyAsync();
        var factory = new BoberConnectionFactory(_options);
        _repository = new GrafikRepository(factory);
        _urlopPlanRepository = new UrlopPlanRepository(factory);
        _kalendarzRepository = new KalendarzRepository(factory);
    }

    [Fact]
    public async Task Repozytoria_zbiorcze_zachowuja_dane_urlopow_i_odczytow_kalendarza()
    {
        var urlopy = Enumerable.Range(1, 5)
            .Select(day => new UrlopPlanWpis
            {
                FunkcjonariuszId = 1,
                ZmianaId = 1,
                Rok = 2028,
                Miesiac = 2,
                Dzien = day,
                TypUrlopu = "w"
            })
            .ToList();
        await _urlopPlanRepository.ApplyBatchAsync(urlopy, []);
        Assert.Equal(5, (await _urlopPlanRepository.GetByZmianaAndMonthAsync(1, 2028, 2)).Count);

        await _urlopPlanRepository.ApplyBatchAsync([], [urlopy[0], urlopy[1]]);
        Assert.Equal(3, (await _urlopPlanRepository.GetByZmianaAndMonthAsync(1, 2028, 2)).Count);

        var replacement = new UrlopPlanWpis
        {
            FunkcjonariuszId = 1,
            ZmianaId = 3,
            Rok = 1999,
            Miesiac = 3,
            Dzien = 1,
            TypUrlopu = "d"
        };
        await _urlopPlanRepository.ReplaceYearAsync(1, 2029, [replacement]);
        Assert.Single(await _urlopPlanRepository.GetByZmianaAndYearAsync(1, 2029));
        Assert.Empty(await _urlopPlanRepository.GetByZmianaAndYearAsync(3, 1999));

        var firstId = await _kalendarzRepository.AddAsync(CreateCalendarEntry(day: 1));
        await _kalendarzRepository.AddAsync(CreateCalendarEntry(day: 2));
        await _kalendarzRepository.MarkAsReadAsync(firstId, 1, "test");

        var calendarEntries = await _kalendarzRepository.GetByMonthAsync(2028, 2, viewerShiftId: 1);
        Assert.Equal(2, calendarEntries.Count);
        Assert.True(calendarEntries.Single(entry => entry.Id == firstId).Odczyt?.Przeczytane);
        Assert.Null(calendarEntries.Single(entry => entry.Id != firstId).Odczyt);
    }

    public Task DisposeAsync()
    {
        try
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, recursive: true);
        }
        catch
        {
            // ACE może zwalniać uchwyt do pliku z opóźnieniem.
        }

        return Task.CompletedTask;
    }

    [Fact]
    public async Task ApplyBatchAsync_zapisuje_i_usuwa_wiele_wpisow_atomowo()
    {
        var sequentialEntries = CreateEntries(year: 2027);
        var batchEntries = CreateEntries(year: 2028);
        var indexedBatchEntries = CreateEntries(year: 2029);

        var sequentialStopwatch = Stopwatch.StartNew();
        foreach (var entry in sequentialEntries)
            await _repository.UpsertAsync(entry);
        sequentialStopwatch.Stop();

        var batchStopwatch = Stopwatch.StartNew();
        await _repository.ApplyBatchAsync(batchEntries, []);
        batchStopwatch.Stop();

        await CreateGrafikIndexAsync();
        var indexedBatchStopwatch = Stopwatch.StartNew();
        await _repository.ApplyBatchAsync(indexedBatchEntries, []);
        indexedBatchStopwatch.Stop();

        var sequentialResult = await _repository.GetByZmianaAndYearAsync(1, 2027);
        var batchResult = await _repository.GetByZmianaAndYearAsync(1, 2028);
        var indexedBatchResult = await _repository.GetByZmianaAndYearAsync(1, 2029);
        Assert.Equal(sequentialEntries.Count, sequentialResult.Count);
        Assert.Equal(batchEntries.Count, batchResult.Count);
        Assert.Equal(indexedBatchEntries.Count, indexedBatchResult.Count);

        await _repository.ApplyBatchAsync([], batchEntries);
        Assert.Empty(await _repository.GetByZmianaAndYearAsync(1, 2028));

        var invalidBatch = CreateEntries(year: 2030).Take(2).ToList();
        invalidBatch[1].TypWpisu = new string('X', 1_000);
        await Assert.ThrowsAnyAsync<Exception>(() => _repository.ApplyBatchAsync(invalidBatch, []));
        Assert.Empty(await _repository.GetByZmianaAndYearAsync(1, 2030));

        _output.WriteLine(
            "31 wpisów: sekwencyjnie {0:F1} ms, batch bez indeksu {1:F1} ms, batch z indeksem {2:F1} ms.",
            sequentialStopwatch.Elapsed.TotalMilliseconds,
            batchStopwatch.Elapsed.TotalMilliseconds,
            indexedBatchStopwatch.Elapsed.TotalMilliseconds);
    }

    private async Task CreateGrafikIndexAsync()
    {
        await using var connection = new OleDbConnection(_options.BuildConnectionString());
        await connection.OpenAsync();
        await using var command = new OleDbCommand(
            "CREATE INDEX IX_Test_Grafik ON GrafikWpisy (ZmianaId, Rok, Miesiac, FunkcjonariuszId, Dzien)",
            connection);
        await command.ExecuteNonQueryAsync();
    }

    private static List<GrafikWpis> CreateEntries(int year) =>
        Enumerable.Range(1, 31)
            .Select(day => new GrafikWpis
            {
                FunkcjonariuszId = 1,
                ZmianaId = 1,
                Rok = year,
                Miesiac = 1,
                Dzien = day,
                TypWpisu = "U",
                IsAuto = false
            })
            .ToList();

    private static KalendarzWpis CreateCalendarEntry(int day) => new()
    {
        Data = new DateOnly(2028, 2, day),
        ZmianaId = 1,
        TypWpisu = KalendarzTypWpisu.Dca,
        Tresc = $"Wpis {day}",
        AutorLogin = "test"
    };
}
