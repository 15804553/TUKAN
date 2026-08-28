using BOBER.Core.Models;

namespace BOBER.Services.ObsadaFunkcji;

public interface IObsadaFunkcjiService
{
    Task<IReadOnlyList<ObsadaFunkcjiUwagaMiesieczna>> GetUwagiMonthAsync(
        int zmianaId,
        int rok,
        int miesiac,
        CancellationToken cancellationToken = default);

    Task SetUwagaMiesiecznaAsync(
        int funkcjonariuszId,
        int zmianaId,
        int rok,
        int miesiac,
        string tresc,
        CancellationToken cancellationToken = default);

    Task ClearUwagaMiesiecznaAsync(
        int funkcjonariuszId,
        int zmianaId,
        int rok,
        int miesiac,
        CancellationToken cancellationToken = default);
}
