using System.IO;
using System.Text.Json;
using Tukan.App.Models;

namespace Tukan.App.Services;

/// <summary>
/// Ustawienia użytkownika TUKAN (motyw UI itp.) w %AppData%\TUKAN\settings.json.
/// </summary>
public sealed class TukanJsonSettingsService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _settingsFilePath;

    public TukanJsonSettingsService()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TUKAN");
        Directory.CreateDirectory(folder);
        _settingsFilePath = Path.Combine(folder, "settings.json");
    }

    public TukanAppSettings Load()
    {
        if (!File.Exists(_settingsFilePath))
        {
            return TukanAppSettings.CreateDefault();
        }

        try
        {
            var json = File.ReadAllText(_settingsFilePath);
            return JsonSerializer.Deserialize<TukanAppSettings>(json, SerializerOptions)
                   ?? TukanAppSettings.CreateDefault();
        }
        catch
        {
            return TukanAppSettings.CreateDefault();
        }
    }

    public void Save(TukanAppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, SerializerOptions);
        File.WriteAllText(_settingsFilePath, json);
    }
}
