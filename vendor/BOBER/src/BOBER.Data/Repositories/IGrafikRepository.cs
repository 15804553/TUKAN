using BOBER.Core.Models;

namespace BOBER.Data.Repositories;

public interface IGrafikRepository
{
    Task<IReadOnlyList<GrafikWpis>> GetByZmianaAndYearAsync(int zmianaId, int rok, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GrafikWpis>> GetByZmianaAndMonthAsync(int zmianaId, int rok, int miesiac, CancellationToken cancellationToken = default);
    Task UpsertAsync(GrafikWpis wpis, CancellationToken cancellationToken = default);
    Task DeleteAsync(int funkcjonariuszId, int rok, int miesiac, int dzien, CancellationToken cancellationToken = default);
    Task DeleteByHalfYearAsync(int zmianaId, int rok, int polrocze, CancellationToken cancellationToken = default);
}
