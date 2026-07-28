using BOBER.Core.Models;

namespace BOBER.Services.Kalendarz;

public interface IKalendarzService
{
    Task<IReadOnlyList<KalendarzWpis>> GetMonthAsync(
        int rok,
        int miesiac,
        int? viewerShiftId = null,
        bool includePrivateEntries = false,
        CancellationToken cancellationToken = default);

    Task UpsertAsync(
        DateOnly data,
        IReadOnlyList<int> zmianaIds,
        string tresc,
        string autorLogin,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        DateOnly data,
        IReadOnlyList<int> zmianaIds,
        CancellationToken cancellationToken = default);

    Task MarkAsReadAsync(
        int wpisId,
        int zmianaId,
        string login,
        CancellationToken cancellationToken = default);

    Task AddShiftNoteAsync(
        DateOnly data,
        int authorShiftId,
        IReadOnlyList<int> recipientShiftIds,
        string tresc,
        string autorLogin,
        CancellationToken cancellationToken = default);

    Task AddDcaReplyAsync(
        DateOnly data,
        int authorShiftId,
        string tresc,
        string autorLogin,
        CancellationToken cancellationToken = default);

    Task DeleteManyAsync(
        IReadOnlyList<int> wpisIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<int, string>> GetKoloryZmianAsync(
        CancellationToken cancellationToken = default);

    Task SaveKoloryZmianAsync(
        IReadOnlyDictionary<int, string> kolory,
        CancellationToken cancellationToken = default);

    Task<KalendarzAutoDeleteMode> GetAutoDeleteModeAsync(
        int? shiftNumber,
        CancellationToken cancellationToken = default);

    Task SaveAutoDeleteModeAsync(
        int? shiftNumber,
        KalendarzAutoDeleteMode mode,
        CancellationToken cancellationToken = default);

    Task ApplyAutoDeleteAsync(
        int? shiftNumber,
        bool canEditDcaEntries,
        CancellationToken cancellationToken = default);

    Task<bool> HasUnreadForRecipientAsync(
        int zmianaId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<int, int>> GetWorkingShiftsForMonthAsync(
        int rok,
        int miesiac,
        CancellationToken cancellationToken = default);

    Task<int> GetWorkingShiftAsync(DateOnly data, CancellationToken cancellationToken = default);
}
