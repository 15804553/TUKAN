using BOBER.Core.Models;

namespace BOBER.Data.Repositories;

public interface IOznaczeniaGrafikuRepository
{
    Task<IReadOnlyList<OznaczenieGrafiku>> GetAllAsync(
        int zmianaId,
        CancellationToken cancellationToken = default);

    Task SaveAllAsync(
        int zmianaId,
        IReadOnlyList<OznaczenieGrafiku> items,
        CancellationToken cancellationToken = default);

    Task<int> CountWpisowZKodemAsync(
        string kod,
        int zmianaId,
        CancellationToken cancellationToken = default);
}
