using BOBER.Core.Constants;
using BOBER.Core.Models;

namespace BOBER.Services.Urlop;

public interface IUrlopPlanService
{
    Task<IReadOnlyList<UrlopPlanWpis>> GetYearAsync(int zmianaId, int rok, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UrlopPlanWpis>> GetMonthAsync(int zmianaId, int rok, int miesiac, CancellationToken cancellationToken = default);
    Task SetWpisAsync(int funkcjonariuszId, int zmianaId, int rok, int miesiac, int dzien, string typUrlopu, CancellationToken cancellationToken = default);
    Task ClearWpisAsync(int funkcjonariuszId, int zmianaId, int rok, int miesiac, int dzien, CancellationToken cancellationToken = default);
    Task ClearHalfYearAsync(int zmianaId, int rok, int polrocze, CancellationToken cancellationToken = default);
    Task ClearYearAsync(int zmianaId, int rok, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UrlopPlanValidationIssue>> ValidateAsync(int zmianaId, int rok, CancellationToken cancellationToken = default);
    Task<UrlopPlanSyncResult> ApplyToGrafikAsync(int zmianaId, int rok, CancellationToken cancellationToken = default);
    Task ImportFromExcelAsync(int zmianaId, int rok, string filePath, CancellationToken cancellationToken = default);
    void ExportToExcel(int zmianaId, int rok, IReadOnlyList<Funkcjonariusz> funkcjonariusze, IReadOnlyList<UrlopPlanWpis> wpisy, string filePath);
}
