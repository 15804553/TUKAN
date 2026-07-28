using BOBER.Core.Models;

namespace BOBER.Data.Repositories;

public interface IKalendarzRepository
{
    Task<IReadOnlyList<KalendarzWpis>> GetByMonthAsync(
        int rok,
        int miesiac,
        int? viewerShiftId = null,
        bool includePrivateEntries = false,
        CancellationToken cancellationToken = default);

    Task<KalendarzWpis?> GetByDateAndZmianaAsync(
        DateOnly data,
        int zmianaId,
        CancellationToken cancellationToken = default);

    Task<int> UpsertAsync(KalendarzWpis wpis, CancellationToken cancellationToken = default);

    Task<int> AddAsync(KalendarzWpis wpis, CancellationToken cancellationToken = default);

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

    Task DeleteOlderThanAsync(
        DateOnly thresholdDate,
        KalendarzTypWpisu typWpisu,
        int? recipientShiftId = null,
        CancellationToken cancellationToken = default);

    /// <summary>Czy zmiana ma co najmniej jedną nieprzeczytaną notatkę (DCA lub prywatną).</summary>
    Task<bool> HasUnreadForRecipientAsync(
        int zmianaId,
        CancellationToken cancellationToken = default);
}
