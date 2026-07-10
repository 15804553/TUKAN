using BOBER.Core.Models;

namespace BOBER.Services.Grafik;

public interface IGrafikService
{
    Task<IReadOnlyList<GrafikWpis>> GetMonthAsync(int zmianaId, int rok, int miesiac, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GrafikWpis>> GetYearAsync(int zmianaId, int rok, CancellationToken cancellationToken = default);
    Task SetWpisAsync(int funkcjonariuszId, int zmianaId, int rok, int miesiac, int dzien, string typWpisu, CancellationToken cancellationToken = default);
    Task ClearWpisAsync(int funkcjonariuszId, int rok, int miesiac, int dzien, CancellationToken cancellationToken = default);
    Task ClearHalfYearAsync(int zmianaId, int rok, int polrocze, CancellationToken cancellationToken = default);
    Task GenerateBaseScheduleAsync(int zmianaId, int rok, IReadOnlyList<int> funkcjonariuszIds, CancellationToken cancellationToken = default);
}
