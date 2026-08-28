using BOBER.Core.Models;

namespace BOBER.Data.Repositories;

public interface IObsadaFunkcjiUwagaMiesiecznaRepository
{
    Task<IReadOnlyList<ObsadaFunkcjiUwagaMiesieczna>> GetByZmianaAndMonthAsync(
        int zmianaId,
        int rok,
        int miesiac,
        CancellationToken cancellationToken = default);

    Task UpsertAsync(ObsadaFunkcjiUwagaMiesieczna uwaga, CancellationToken cancellationToken = default);

    Task DeleteAsync(
        int funkcjonariuszId,
        int zmianaId,
        int rok,
        int miesiac,
        CancellationToken cancellationToken = default);
}
