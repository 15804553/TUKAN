using BOBER.Core.Models;
using BOBER.Data.Repositories;

namespace BOBER.Services.Grafik;

/// <summary>Odczyt i zapis wpisów grafiku w BoberDatabase; podsumowania liczy UI (MainController).</summary>
public sealed class GrafikService(IGrafikRepository grafikRepository) : IGrafikService
{
    public Task<IReadOnlyList<GrafikWpis>> GetMonthAsync(
        int zmianaId,
        int rok,
        int miesiac,
        CancellationToken cancellationToken = default) =>
        grafikRepository.GetByZmianaAndMonthAsync(zmianaId, rok, miesiac, cancellationToken);

    public Task<IReadOnlyList<GrafikWpis>> GetYearAsync(
        int zmianaId,
        int rok,
        CancellationToken cancellationToken = default) =>
        grafikRepository.GetByZmianaAndYearAsync(zmianaId, rok, cancellationToken);

    public Task SetWpisAsync(
        int funkcjonariuszId,
        int zmianaId,
        int rok,
        int miesiac,
        int dzien,
        string typWpisu,
        CancellationToken cancellationToken = default) =>
        grafikRepository.UpsertAsync(new GrafikWpis
        {
            FunkcjonariuszId = funkcjonariuszId,
            ZmianaId = zmianaId,
            Rok = rok,
            Miesiac = miesiac,
            Dzien = dzien,
            TypWpisu = typWpisu,
            IsAuto = false
        }, cancellationToken);

    public Task ClearWpisAsync(
        int funkcjonariuszId,
        int rok,
        int miesiac,
        int dzien,
        CancellationToken cancellationToken = default) =>
        grafikRepository.DeleteAsync(funkcjonariuszId, rok, miesiac, dzien, cancellationToken);

    public Task ClearHalfYearAsync(
        int zmianaId,
        int rok,
        int polrocze,
        CancellationToken cancellationToken = default) =>
        grafikRepository.DeleteByHalfYearAsync(zmianaId, rok, polrocze, cancellationToken);

    /// <summary>
    /// Przygotowuje grafik bazowy: kolumny dni służby z rotacji; wpisy w bazie pozostają bez zmian.
    /// </summary>
    public Task GenerateBaseScheduleAsync(
        int zmianaId,
        int rok,
        IReadOnlyList<int> funkcjonariuszIds,
        CancellationToken cancellationToken = default)
    {
        _ = zmianaId;
        _ = rok;
        _ = funkcjonariuszIds;
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }
}
