using BOBER.Core.Models;
using BOBER.Data.Repositories;

namespace BOBER.Services.Grafik;

/// <summary>Odczyt i zapis wpisów grafiku w BoberDatabase; podsumowania liczy UI (MainController).</summary>
public sealed class GrafikService(
    IGrafikRepository grafikRepository,
    IGrafikNotatkaRepository notatkaRepository,
    IGrafikUwagaMiesiecznaRepository uwagaMiesiecznaRepository) : IGrafikService
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

    public Task<IReadOnlyList<GrafikNotatka>> GetNotatkiMonthAsync(
        int zmianaId,
        int rok,
        int miesiac,
        CancellationToken cancellationToken = default) =>
        notatkaRepository.GetByZmianaAndMonthAsync(zmianaId, rok, miesiac, cancellationToken);

    public Task SetNotatkaAsync(
        int zmianaId,
        int rok,
        int miesiac,
        int dzien,
        string tresc,
        CancellationToken cancellationToken = default) =>
        notatkaRepository.UpsertAsync(new GrafikNotatka
        {
            ZmianaId = zmianaId,
            Rok = rok,
            Miesiac = miesiac,
            Dzien = dzien,
            Tresc = tresc
        }, cancellationToken);

    public Task ClearNotatkaAsync(
        int zmianaId,
        int rok,
        int miesiac,
        int dzien,
        CancellationToken cancellationToken = default) =>
        notatkaRepository.DeleteAsync(zmianaId, rok, miesiac, dzien, cancellationToken);

    public Task<IReadOnlyList<GrafikUwagaMiesieczna>> GetUwagiMonthAsync(
        int zmianaId,
        int rok,
        int miesiac,
        CancellationToken cancellationToken = default) =>
        uwagaMiesiecznaRepository.GetByZmianaAndMonthAsync(zmianaId, rok, miesiac, cancellationToken);

    public Task SetUwagaMiesiecznaAsync(
        int funkcjonariuszId,
        int zmianaId,
        int rok,
        int miesiac,
        string tresc,
        CancellationToken cancellationToken = default) =>
        uwagaMiesiecznaRepository.UpsertAsync(new GrafikUwagaMiesieczna
        {
            FunkcjonariuszId = funkcjonariuszId,
            ZmianaId = zmianaId,
            Rok = rok,
            Miesiac = miesiac,
            Tresc = tresc
        }, cancellationToken);

    public Task ClearUwagaMiesiecznaAsync(
        int funkcjonariuszId,
        int zmianaId,
        int rok,
        int miesiac,
        CancellationToken cancellationToken = default) =>
        uwagaMiesiecznaRepository.DeleteAsync(funkcjonariuszId, zmianaId, rok, miesiac, cancellationToken);
}
