using BOBER.Core.Models;

namespace BOBER.Services.Settings;

public interface ISettingsService
{
    Task<string> GetChomikDbPathAsync(CancellationToken cancellationToken = default);
    Task SetChomikDbPathAsync(string path, CancellationToken cancellationToken = default);

    Task<int> GetStanZmianyAsync(int zmianaId, CancellationToken cancellationToken = default);
    Task SetStanZmianyAsync(int zmianaId, int stan, CancellationToken cancellationToken = default);

    Task<int> GetStanMinimalnyAsync(int zmianaId, CancellationToken cancellationToken = default);
    Task SetStanMinimalnyAsync(int zmianaId, int stan, CancellationToken cancellationToken = default);

    Task<int> GetMaxUrlopowNaSluzbieAsync(int zmianaId, CancellationToken cancellationToken = default);
    Task SetMaxUrlopowNaSluzbieAsync(int zmianaId, int max, CancellationToken cancellationToken = default);

    Task<string> GetExportPathRozkazyAsync(CancellationToken cancellationToken = default);
    Task SetExportPathRozkazyAsync(string path, CancellationToken cancellationToken = default);

    Task<string> GetExportPathGrafikSluzbAsync(CancellationToken cancellationToken = default);
    Task SetExportPathGrafikSluzbAsync(string path, CancellationToken cancellationToken = default);

    Task<string> GetExportPathGrafikNurkowyAsync(CancellationToken cancellationToken = default);
    Task SetExportPathGrafikNurkowyAsync(string path, CancellationToken cancellationToken = default);

    Task<KalendarzAutoDeleteMode> GetKalendarzAutoDeleteModeAsync(
        int? shiftNumber,
        CancellationToken cancellationToken = default);

    Task SetKalendarzAutoDeleteModeAsync(
        int? shiftNumber,
        KalendarzAutoDeleteMode mode,
        CancellationToken cancellationToken = default);
}
