namespace BOBER.Data.Repositories;

public interface IUstawieniaRepository
{
    Task<string?> GetAsync(string key, CancellationToken cancellationToken = default);
    Task<int> GetIntAsync(string key, int defaultValue = 0, CancellationToken cancellationToken = default);
    Task SetAsync(string key, string value, CancellationToken cancellationToken = default);
}
