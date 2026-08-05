using BOBER.Data.Repositories;
using BOBER.Core.Constants;
using BOBER.Core.Models;

namespace BOBER.Services.Settings;

public sealed class SettingsService(IUstawieniaRepository ustawieniaRepository) : ISettingsService
{
    public async Task<string> GetChomikDbPathAsync(CancellationToken cancellationToken = default)
    {
        var path = await ustawieniaRepository.GetAsync("ChomikDbPath", cancellationToken);
        return path ?? string.Empty;
    }

    public Task SetChomikDbPathAsync(string path, CancellationToken cancellationToken = default) =>
        ustawieniaRepository.SetAsync("ChomikDbPath", path, cancellationToken);

    public async Task<int> GetStanZmianyAsync(int zmianaId, CancellationToken cancellationToken = default)
    {
        var perShift = await ustawieniaRepository.GetAsync($"StanZmiany_{zmianaId}", cancellationToken);
        if (perShift is not null && int.TryParse(perShift, out var parsed))
            return parsed;

        // fallback do wartości globalnej (migracja ze starszej wersji)
        return await ustawieniaRepository.GetIntAsync("StanZmiany", 10, cancellationToken);
    }

    public Task SetStanZmianyAsync(int zmianaId, int stan, CancellationToken cancellationToken = default) =>
        ustawieniaRepository.SetAsync($"StanZmiany_{zmianaId}", stan.ToString(), cancellationToken);

    public async Task<int> GetStanMinimalnyAsync(int zmianaId, CancellationToken cancellationToken = default)
    {
        var perShift = await ustawieniaRepository.GetAsync($"StanMinimalny_{zmianaId}", cancellationToken);
        if (perShift is not null && int.TryParse(perShift, out var parsed))
            return parsed;

        // fallback do wartości globalnej (migracja ze starszej wersji)
        return await ustawieniaRepository.GetIntAsync("StanMinimalny", 6, cancellationToken);
    }

    public Task SetStanMinimalnyAsync(int zmianaId, int stan, CancellationToken cancellationToken = default) =>
        ustawieniaRepository.SetAsync($"StanMinimalny_{zmianaId}", stan.ToString(), cancellationToken);

    public async Task<int> GetMaxUrlopowNaSluzbieAsync(int zmianaId, CancellationToken cancellationToken = default)
    {
        var perShift = await ustawieniaRepository.GetAsync($"MaxUrlopowNaSluzbie_{zmianaId}", cancellationToken);
        if (perShift is not null && int.TryParse(perShift, out var parsed))
            return parsed;

        return await ustawieniaRepository.GetIntAsync(
            "MaxUrlopowNaSluzbie",
            UrlopPlanInstructions.DefaultMaxUrlopowNaSluzbie,
            cancellationToken);
    }

    public Task SetMaxUrlopowNaSluzbieAsync(int zmianaId, int max, CancellationToken cancellationToken = default) =>
        ustawieniaRepository.SetAsync($"MaxUrlopowNaSluzbie_{zmianaId}", max.ToString(), cancellationToken);

    public async Task<string> GetExportPathRozkazyAsync(CancellationToken cancellationToken = default) =>
        await ustawieniaRepository.GetAsync("ExportPathRozkazy", cancellationToken) ?? string.Empty;

    public Task SetExportPathRozkazyAsync(string path, CancellationToken cancellationToken = default) =>
        ustawieniaRepository.SetAsync("ExportPathRozkazy", path, cancellationToken);

    public async Task<string> GetExportPathGrafikSluzbAsync(CancellationToken cancellationToken = default) =>
        await ustawieniaRepository.GetAsync("ExportPathGrafikSluzb", cancellationToken) ?? string.Empty;

    public Task SetExportPathGrafikSluzbAsync(string path, CancellationToken cancellationToken = default) =>
        ustawieniaRepository.SetAsync("ExportPathGrafikSluzb", path, cancellationToken);

    public async Task<string> GetExportPathGrafikNurkowyAsync(CancellationToken cancellationToken = default) =>
        await ustawieniaRepository.GetAsync("ExportPathGrafikNurkowy", cancellationToken) ?? string.Empty;

    public Task SetExportPathGrafikNurkowyAsync(string path, CancellationToken cancellationToken = default) =>
        ustawieniaRepository.SetAsync("ExportPathGrafikNurkowy", path, cancellationToken);

    public Task<bool> GetLessColorAsync(CancellationToken cancellationToken = default) =>
        GetBoolSettingAsync("LessColor", defaultValue: true, cancellationToken);

    public Task SetLessColorAsync(bool enabled, CancellationToken cancellationToken = default) =>
        ustawieniaRepository.SetAsync("LessColor", enabled ? "True" : "False", cancellationToken);

    public Task<bool> GetGrafikDelYellowAsync(CancellationToken cancellationToken = default) =>
        GetBoolSettingAsync("GrafikDelYellow", defaultValue: true, cancellationToken);

    public Task SetGrafikDelYellowAsync(bool enabled, CancellationToken cancellationToken = default) =>
        ustawieniaRepository.SetAsync("GrafikDelYellow", enabled ? "True" : "False", cancellationToken);

    public Task<bool> GetGrafikMultiSelectAsync(CancellationToken cancellationToken = default) =>
        GetBoolSettingAsync("GrafikMultiSelect", defaultValue: true, cancellationToken);

    public Task SetGrafikMultiSelectAsync(bool enabled, CancellationToken cancellationToken = default) =>
        ustawieniaRepository.SetAsync("GrafikMultiSelect", enabled ? "True" : "False", cancellationToken);

    public async Task<GrafikRowColorSettings> GetGrafikRowColorSettingsAsync(
        CancellationToken cancellationToken = default)
    {
        var modeRaw = await ustawieniaRepository.GetAsync("GrafikRowColorMode", cancellationToken);
        var mode = Enum.TryParse<GrafikRowColorMode>(modeRaw, ignoreCase: true, out var parsed)
            ? parsed
            : GrafikRowColorMode.Role;

        var colorA = await ustawieniaRepository.GetAsync("GrafikAltColorA", cancellationToken);
        var colorB = await ustawieniaRepository.GetAsync("GrafikAltColorB", cancellationToken);

        return new GrafikRowColorSettings
        {
            Mode = mode,
            ColorA = NormalizeHex(colorA, GrafikRowColorSettings.DefaultColorA),
            ColorB = NormalizeHex(colorB, GrafikRowColorSettings.DefaultColorB)
        };
    }

    public async Task SetGrafikRowColorSettingsAsync(
        GrafikRowColorSettings settings,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        await ustawieniaRepository.SetAsync(
            "GrafikRowColorMode",
            settings.Mode.ToString(),
            cancellationToken);
        await ustawieniaRepository.SetAsync(
            "GrafikAltColorA",
            NormalizeHex(settings.ColorA, GrafikRowColorSettings.DefaultColorA),
            cancellationToken);
        await ustawieniaRepository.SetAsync(
            "GrafikAltColorB",
            NormalizeHex(settings.ColorB, GrafikRowColorSettings.DefaultColorB),
            cancellationToken);
    }

    public async Task<KalendarzAutoDeleteMode> GetKalendarzAutoDeleteModeAsync(
        int? shiftNumber,
        CancellationToken cancellationToken = default)
    {
        var key = BuildKalendarzAutoDeleteKey(shiftNumber);
        var raw = await ustawieniaRepository.GetAsync(key, cancellationToken);
        return Enum.TryParse<KalendarzAutoDeleteMode>(raw, ignoreCase: true, out var mode)
            ? mode
            : KalendarzAutoDeleteMode.Nigdy;
    }

    public Task SetKalendarzAutoDeleteModeAsync(
        int? shiftNumber,
        KalendarzAutoDeleteMode mode,
        CancellationToken cancellationToken = default) =>
        ustawieniaRepository.SetAsync(
            BuildKalendarzAutoDeleteKey(shiftNumber),
            mode.ToString(),
            cancellationToken);

    private static string BuildKalendarzAutoDeleteKey(int? shiftNumber) =>
        shiftNumber is >= 1 and <= 3
            ? $"KalendarzAutoDelete_Zmiana_{shiftNumber.Value}"
            : "KalendarzAutoDelete_DCA";

    private async Task<bool> GetBoolSettingAsync(
        string key,
        bool defaultValue,
        CancellationToken cancellationToken)
    {
        var raw = await ustawieniaRepository.GetAsync(key, cancellationToken);
        if (raw is null)
            return defaultValue;

        if (bool.TryParse(raw, out var parsed))
            return parsed;

        return raw.Equals("1", StringComparison.OrdinalIgnoreCase)
            || raw.Equals("yes", StringComparison.OrdinalIgnoreCase)
            || raw.Equals("tak", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeHex(string? hex, string fallback)
    {
        if (string.IsNullOrWhiteSpace(hex))
            return fallback;

        var trimmed = hex.Trim();
        if (!trimmed.StartsWith('#'))
            trimmed = "#" + trimmed;

        return trimmed.Length is 7 or 9 ? trimmed.ToUpperInvariant() : fallback;
    }
}
