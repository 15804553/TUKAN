using BOBER.Core.Models;

namespace BOBER.Data.Repositories;

public interface IUrlopPlanRepository
{
    Task<IReadOnlyList<UrlopPlanWpis>> GetByZmianaAndYearAsync(
        int zmianaId,
        int rok,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UrlopPlanWpis>> GetByZmianaAndMonthAsync(
        int zmianaId,
        int rok,
        int miesiac,
        CancellationToken cancellationToken = default);

    Task UpsertAsync(UrlopPlanWpis wpis, CancellationToken cancellationToken = default);

    Task DeleteAsync(
        int funkcjonariuszId,
        int zmianaId,
        int rok,
        int miesiac,
        int dzien,
        CancellationToken cancellationToken = default);

    Task DeleteByHalfYearAsync(
        int zmianaId,
        int rok,
        int polrocze,
        CancellationToken cancellationToken = default);

    Task DeleteByYearAsync(
        int zmianaId,
        int rok,
        CancellationToken cancellationToken = default);

    Task ReplaceYearAsync(
        int zmianaId,
        int rok,
        IReadOnlyList<UrlopPlanWpis> wpisy,
        CancellationToken cancellationToken = default);
}
