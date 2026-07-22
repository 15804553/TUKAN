using BOBER.Core.Models;

namespace BOBER.Data.Repositories;

public interface IKalendarzRepository
{
    Task<IReadOnlyList<KalendarzWpis>> GetByMonthAsync(
        int rok,
        int miesiac,
        int? zmianaFilter = null,
        CancellationToken cancellationToken = default);

    Task<KalendarzWpis?> GetByDateAndZmianaAsync(
        DateOnly data,
        int zmianaId,
        CancellationToken cancellationToken = default);

    Task<int> UpsertAsync(KalendarzWpis wpis, CancellationToken cancellationToken = default);

    Task DeleteAsync(int wpisId, CancellationToken cancellationToken = default);

    Task DeleteByDateAndZmianaAsync(
        DateOnly data,
        int zmianaId,
        CancellationToken cancellationToken = default);

    Task ResetOdczytAsync(int wpisId, CancellationToken cancellationToken = default);

    Task MarkAsReadAsync(
        int wpisId,
        int zmianaId,
        string login,
        CancellationToken cancellationToken = default);

    Task<KalendarzOdczyt?> GetOdczytAsync(
        int wpisId,
        int zmianaId,
        CancellationToken cancellationToken = default);
}
