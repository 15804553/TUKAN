namespace Chomik.Data.Repositories;

public interface ISettingsRepository
{
    Task<string?> GetAsync(string key, CancellationToken cancellationToken = default);
    Task<bool> GetBoolAsync(string key, bool defaultValue = false, CancellationToken cancellationToken = default);
    Task SetAsync(string key, string value, CancellationToken cancellationToken = default);
}
