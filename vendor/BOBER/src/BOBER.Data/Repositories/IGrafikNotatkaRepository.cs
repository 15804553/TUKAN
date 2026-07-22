using BOBER.Core.Models;

namespace BOBER.Data.Repositories;

public interface IGrafikNotatkaRepository
{
    Task<IReadOnlyList<GrafikNotatka>> GetByZmianaAndMonthAsync(
        int zmianaId,
        int rok,
        int miesiac,
        CancellationToken cancellationToken = default);

    Task UpsertAsync(GrafikNotatka notatka, CancellationToken cancellationToken = default);

    Task DeleteAsync(
        int zmianaId,
        int rok,
        int miesiac,
        int dzien,
        CancellationToken cancellationToken = default);
}
