using BOBER.Core.Enums;
using BOBER.Core.Models;

namespace BOBER.Data.Repositories;

public interface IGrafikZliczanieRepository
{
    Task<IReadOnlyList<GrafikZliczanieWiersz>> GetAllAsync(
        int zmianaId,
        CancellationToken cancellationToken = default);

    Task SaveAllAsync(
        int zmianaId,
        IReadOnlyList<GrafikZliczanieWiersz> items,
        CancellationToken cancellationToken = default);
}
