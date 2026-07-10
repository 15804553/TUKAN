namespace BOBER.Services.Settings;

public interface ISettingsService
{
    Task<string> GetChomikDbPathAsync(CancellationToken cancellationToken = default);
    Task SetChomikDbPathAsync(string path, CancellationToken cancellationToken = default);

    Task<int> GetStanZmianyAsync(int zmianaId, CancellationToken cancellationToken = default);
    Task SetStanZmianyAsync(int zmianaId, int stan, CancellationToken cancellationToken = default);

    Task<int> GetStanMinimalnyAsync(int zmianaId, CancellationToken cancellationToken = default);
    Task SetStanMinimalnyAsync(int zmianaId, int stan, CancellationToken cancellationToken = default);
}
