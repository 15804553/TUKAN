using BOBER.Core.Models;

namespace BOBER.Data.Repositories;

public interface IGrafikUwagaMiesiecznaRepository
{
    Task<IReadOnlyList<GrafikUwagaMiesieczna>> GetByZmianaAndMonthAsync(
        int zmianaId,
        int rok,
        int miesiac,
        CancellationToken cancellationToken = default);

    Task UpsertAsync(GrafikUwagaMiesieczna uwaga, CancellationToken cancellationToken = default);

    Task DeleteAsync(
        int funkcjonariuszId,
        int zmianaId,
        int rok,
        int miesiac,
        CancellationToken cancellationToken = default);
}
