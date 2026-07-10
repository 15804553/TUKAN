using BOBER.Core.Models;

namespace BOBER.Data.Repositories;

public interface IKolejnoscRepository
{
    Task<IReadOnlyList<KolejnoscFunkcjonariusza>> GetByZmianaAsync(int zmianaId, CancellationToken cancellationToken = default);
    Task SaveAsync(int zmianaId, IReadOnlyList<int> kolejnoscIds, CancellationToken cancellationToken = default);
}
