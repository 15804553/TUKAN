using BOBER.Core.Models;
using BOBER.Services;

namespace BOBER.App.Controllers;

public sealed class KalendarzController(AppServices services)
{
    public int DefaultYear => DateTime.Today.Year;
    public int DefaultMonth => DateTime.Today.Month;

    public Task<IReadOnlyList<KalendarzWpis>> GetMonthAsync(
        int rok,
        int miesiac,
        int? viewerShiftId = null,
        bool includePrivateEntries = false,
        CancellationToken cancellationToken = default) =>
        services.Kalendarz.GetMonthAsync(rok, miesiac, viewerShiftId, includePrivateEntries, cancellationToken);

    public Task UpsertAsync(
        DateOnly data,
        IReadOnlyList<int> zmianaIds,
        string tresc,
        string autorLogin,
        CancellationToken cancellationToken = default) =>
        services.Kalendarz.UpsertAsync(data, zmianaIds, tresc, autorLogin, cancellationToken);

    public Task DeleteAsync(
        DateOnly data,
        IReadOnlyList<int> zmianaIds,
        CancellationToken cancellationToken = default) =>
        services.Kalendarz.DeleteAsync(data, zmianaIds, cancellationToken);

    public Task DeleteManyAsync(
        IReadOnlyList<int> wpisIds,
        CancellationToken cancellationToken = default) =>
        services.Kalendarz.DeleteManyAsync(wpisIds, cancellationToken);

    public Task MarkAsReadAsync(
        int wpisId,
        int zmianaId,
        string login,
        CancellationToken cancellationToken = default) =>
        services.Kalendarz.MarkAsReadAsync(wpisId, zmianaId, login, cancellationToken);

    public Task AddShiftNoteAsync(
        DateOnly data,
        int authorShiftId,
        IReadOnlyList<int> recipientShiftIds,
        string tresc,
        string autorLogin,
        CancellationToken cancellationToken = default) =>
        services.Kalendarz.AddShiftNoteAsync(data, authorShiftId, recipientShiftIds, tresc, autorLogin, cancellationToken);

    public Task AddDcaReplyAsync(
        DateOnly data,
        int authorShiftId,
        string tresc,
        string autorLogin,
        CancellationToken cancellationToken = default) =>
        services.Kalendarz.AddDcaReplyAsync(data, authorShiftId, tresc, autorLogin, cancellationToken);

    public Task<IReadOnlyDictionary<int, string>> GetKoloryZmianAsync(
        CancellationToken cancellationToken = default) =>
        services.Kalendarz.GetKoloryZmianAsync(cancellationToken);

    public Task SaveKoloryZmianAsync(
        IReadOnlyDictionary<int, string> kolory,
        CancellationToken cancellationToken = default) =>
        services.Kalendarz.SaveKoloryZmianAsync(kolory, cancellationToken);

    public Task<KalendarzAutoDeleteMode> GetAutoDeleteModeAsync(
        int? shiftNumber,
        CancellationToken cancellationToken = default) =>
        services.Kalendarz.GetAutoDeleteModeAsync(shiftNumber, cancellationToken);

    public Task SaveAutoDeleteModeAsync(
        int? shiftNumber,
        KalendarzAutoDeleteMode mode,
        CancellationToken cancellationToken = default) =>
        services.Kalendarz.SaveAutoDeleteModeAsync(shiftNumber, mode, cancellationToken);

    public Task ApplyAutoDeleteAsync(
        int? shiftNumber,
        bool canEditDcaEntries,
        CancellationToken cancellationToken = default) =>
        services.Kalendarz.ApplyAutoDeleteAsync(shiftNumber, canEditDcaEntries, cancellationToken);

    public Task<bool> HasUnreadForRecipientAsync(
        int zmianaId,
        CancellationToken cancellationToken = default) =>
        services.Kalendarz.HasUnreadForRecipientAsync(zmianaId, cancellationToken);

    public Task<IReadOnlyDictionary<int, int>> GetWorkingShiftsForMonthAsync(
        int rok,
        int miesiac,
        CancellationToken cancellationToken = default) =>
        services.Kalendarz.GetWorkingShiftsForMonthAsync(rok, miesiac, cancellationToken);

    public Task<int> GetWorkingShiftAsync(DateOnly data, CancellationToken cancellationToken = default) =>
        services.Kalendarz.GetWorkingShiftAsync(data, cancellationToken);
}
