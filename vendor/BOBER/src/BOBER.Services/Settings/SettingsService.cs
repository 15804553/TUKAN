using BOBER.Data.Repositories;

namespace BOBER.Services.Settings;

public sealed class SettingsService(IUstawieniaRepository ustawieniaRepository) : ISettingsService
{
    public async Task<string> GetChomikDbPathAsync(CancellationToken cancellationToken = default)
    {
        var path = await ustawieniaRepository.GetAsync("ChomikDbPath", cancellationToken);
        return path ?? string.Empty;
    }

    public Task SetChomikDbPathAsync(string path, CancellationToken cancellationToken = default) =>
        ustawieniaRepository.SetAsync("ChomikDbPath", path, cancellationToken);

    public async Task<int> GetStanZmianyAsync(int zmianaId, CancellationToken cancellationToken = default)
    {
        var perShift = await ustawieniaRepository.GetAsync($"StanZmiany_{zmianaId}", cancellationToken);
        if (perShift is not null && int.TryParse(perShift, out var parsed))
            return parsed;

        // fallback do wartości globalnej (migracja ze starszej wersji)
        return await ustawieniaRepository.GetIntAsync("StanZmiany", 10, cancellationToken);
    }

    public Task SetStanZmianyAsync(int zmianaId, int stan, CancellationToken cancellationToken = default) =>
        ustawieniaRepository.SetAsync($"StanZmiany_{zmianaId}", stan.ToString(), cancellationToken);

    public async Task<int> GetStanMinimalnyAsync(int zmianaId, CancellationToken cancellationToken = default)
    {
        var perShift = await ustawieniaRepository.GetAsync($"StanMinimalny_{zmianaId}", cancellationToken);
        if (perShift is not null && int.TryParse(perShift, out var parsed))
            return parsed;

        // fallback do wartości globalnej (migracja ze starszej wersji)
        return await ustawieniaRepository.GetIntAsync("StanMinimalny", 6, cancellationToken);
    }

    public Task SetStanMinimalnyAsync(int zmianaId, int stan, CancellationToken cancellationToken = default) =>
        ustawieniaRepository.SetAsync($"StanMinimalny_{zmianaId}", stan.ToString(), cancellationToken);
}
