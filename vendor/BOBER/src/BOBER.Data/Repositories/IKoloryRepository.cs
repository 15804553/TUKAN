using BOBER.Core.Models;

namespace BOBER.Data.Repositories;

public interface IKoloryRepository
{
    Task<IReadOnlyList<KolorStanowiska>> GetAllAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(IReadOnlyList<KolorStanowiska> kolory, CancellationToken cancellationToken = default);
}
