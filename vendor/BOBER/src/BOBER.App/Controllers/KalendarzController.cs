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
        int? zmianaFilter = null,
        CancellationToken cancellationToken = default) =>
        services.Kalendarz.GetMonthAsync(rok, miesiac, zmianaFilter, cancellationToken);

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

    public Task MarkAsReadAsync(
        int wpisId,
        int zmianaId,
        string login,
        CancellationToken cancellationToken = default) =>
        services.Kalendarz.MarkAsReadAsync(wpisId, zmianaId, login, cancellationToken);

    public Task<IReadOnlyDictionary<int, string>> GetKoloryZmianAsync(
        CancellationToken cancellationToken = default) =>
        services.Kalendarz.GetKoloryZmianAsync(cancellationToken);

    public Task SaveKoloryZmianAsync(
        IReadOnlyDictionary<int, string> kolory,
        CancellationToken cancellationToken = default) =>
        services.Kalendarz.SaveKoloryZmianAsync(kolory, cancellationToken);

    public Task<IReadOnlyDictionary<int, int>> GetWorkingShiftsForMonthAsync(
        int rok,
        int miesiac,
        CancellationToken cancellationToken = default) =>
        services.Kalendarz.GetWorkingShiftsForMonthAsync(rok, miesiac, cancellationToken);

    public Task<int> GetWorkingShiftAsync(DateOnly data, CancellationToken cancellationToken = default) =>
        services.Kalendarz.GetWorkingShiftAsync(data, cancellationToken);
}
