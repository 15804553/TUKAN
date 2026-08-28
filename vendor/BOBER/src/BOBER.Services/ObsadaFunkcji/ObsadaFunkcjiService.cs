using BOBER.Core.Models;
using BOBER.Data.Repositories;

namespace BOBER.Services.ObsadaFunkcji;

public sealed class ObsadaFunkcjiService(IObsadaFunkcjiUwagaMiesiecznaRepository uwagaRepository)
    : IObsadaFunkcjiService
{
    public Task<IReadOnlyList<ObsadaFunkcjiUwagaMiesieczna>> GetUwagiMonthAsync(
        int zmianaId,
        int rok,
        int miesiac,
        CancellationToken cancellationToken = default) =>
        uwagaRepository.GetByZmianaAndMonthAsync(zmianaId, rok, miesiac, cancellationToken);

    public Task SetUwagaMiesiecznaAsync(
        int funkcjonariuszId,
        int zmianaId,
        int rok,
        int miesiac,
        string tresc,
        CancellationToken cancellationToken = default) =>
        uwagaRepository.UpsertAsync(new ObsadaFunkcjiUwagaMiesieczna
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
        uwagaRepository.DeleteAsync(funkcjonariuszId, zmianaId, rok, miesiac, cancellationToken);
}
