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

    /// <summary>
    /// LessColor: eksport grafiku służb bez kolorów ról/nurek — białe wiersze, czarna czcionka,
    /// żółte tło dla WS i D. Domyślnie włączone.
    /// </summary>
    Task<bool> GetLessColorAsync(CancellationToken cancellationToken = default);
    Task SetLessColorAsync(bool enabled, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kolorowanie wierszy grafiku służb w UI (role vs dwa kolory naprzemiennie).
    /// Nie wpływa na eksport Excel. Dotyczy wszystkich zmian.
    /// </summary>
    Task<GrafikRowColorSettings> GetGrafikRowColorSettingsAsync(
        CancellationToken cancellationToken = default);

    Task SetGrafikRowColorSettingsAsync(
        GrafikRowColorSettings settings,
        CancellationToken cancellationToken = default);

    Task<KalendarzAutoDeleteMode> GetKalendarzAutoDeleteModeAsync(
        int? shiftNumber,
        CancellationToken cancellationToken = default);

    Task SetKalendarzAutoDeleteModeAsync(
        int? shiftNumber,
        KalendarzAutoDeleteMode mode,
        CancellationToken cancellationToken = default);
}
