using System.IO;
using BOBER.Services;
using BOBER.Services.Logging;
using BOBER.Services.Startup;
using Serilog;
using Chomik.Data;
using Chomik.Services;
using SKRYBEK.App;
using SKRYBEK.Services;
using SKRYBEK.Services.Logging;

namespace Tukan.App.Services;

/// <summary>Inicjalizacja i synchronizacja trzech modułów w jednym katalogu programu.</summary>
public sealed class TukanAppServices : IDisposable
{
    public Chomik.Services.AppServices Chomik { get; }
    public BOBER.Services.AppServices Bober { get; }
    public SKRYBEK.Services.AppServices Skrybek { get; private set; } = null!;

    public SKRYBEK.Core.Models.SessionInfo? SkrybekSession { get; private set; }

    private TukanAppServices(Chomik.Services.AppServices chomik, BOBER.Services.AppServices bober)
    {
        Chomik = chomik;
        Bober = bober;
    }

    public static async Task<(TukanAppServices Services, TukanDatabaseMigrator.MigrationResult Migration)> CreateAsync()
    {
        ConfigureLogging();

        var targetDirectory = AppContext.BaseDirectory;
        var migration = await TukanDatabaseMigrator.MigrateAllAsync(targetDirectory);

        var unifiedPath = TukanDatabaseOptions.GetFullPath();

        var chomik = new Chomik.Services.AppServices();
        chomik.DatabaseOptions.FilePath = unifiedPath;
        chomik.DatabaseOptions.DatabasePassword = TukanDatabaseOptions.Password;
        chomik.DatabaseOptions.UseDatabasePassword = true;
        chomik.DatabaseOptions.MigrateLegacyDatabaseIfNeeded();

        var bober = new BOBER.Services.AppServices();
        bober.BoberOptions.FilePath = unifiedPath;
        bober.ChomikOptions.FilePath = unifiedPath;
        DatabasePathFile.Write(unifiedPath);

        // Warm: szybki SELECT wersji schematu; cold: pełny bootstrap raz (konsolidator też woła —
        // wtedy flaga TukanSchemaVersion pomija drugi przebieg).
        await TukanUnifiedDatabaseBootstrapper.EnsureSchemaAsync(unifiedPath);

        // CreateAsync bez ponownego EnsureCreated — schemat jest już gotowy.
        var skrybek = await SKRYBEK.Services.AppServices.CreateAsync(
            unifiedPath,
            unifiedPath,
            ensureCreated: false);
        ServiceProvider.Services = skrybek;

        BoberLog.Information("TUKAN: wspólna baza={UnifiedPath}", unifiedPath);

        return (new TukanAppServices(chomik, bober) { Skrybek = skrybek }, migration);
    }

    /// <summary>Backup w tle — nie blokuje okna logowania.</summary>
    public void StartBackgroundBackup()
    {
        _ = RunBackupSafelyAsync();
    }

    private async Task RunBackupSafelyAsync()
    {
        try
        {
            await Skrybek.Backup.SprawdzIWykonajBackupAsync();
        }
        catch (Exception ex)
        {
            SkrybekLog.Warning($"TUKAN backup w tle: {ex.Message}");
        }
    }

    public async Task<(bool Success, string ErrorMessage)> TryLoginAsync(string login, string password)
    {
        if (!Chomik.Auth.TryLogin(login, password, out var chomikError))
        {
            return (false, chomikError ?? "Logowanie nie powiodło się.");
        }

        await Bober.Auth.GetLoginsAsync();
        if (!Bober.Auth.TryAuthenticate(login, password, out var boberError))
        {
            Chomik.Auth.Logout();
            return (false, boberError ?? "Nie udało się zsynchronizować sesji BOBER.");
        }

        var skrybekSession = await Skrybek.Auth.LoginAsync(login, password);
        if (skrybekSession is null)
        {
            Chomik.Auth.Logout();
            Bober.Auth.Logout();
            return (false, "Nie udało się zsynchronizować sesji SKRYBEK.");
        }

        SkrybekSession = skrybekSession;
        SkrybekSession.NormalizePaFlags();
        return (true, string.Empty);
    }

    public void Logout()
    {
        SkrybekSession = null;
        Chomik.Auth.Logout();
        Bober.Auth.Logout();
    }

    public void Dispose()
    {
        Logout();
    }

    private static void ConfigureLogging()
    {
        // katalog\LOG\TUKANyyyy-MM-dd.txt — tylko Warning / Error / wyjątki.
        var logDir = Path.Combine(AppContext.BaseDirectory, "LOG");
        Directory.CreateDirectory(logDir);
        UsunStarePlikiLogow(logDir, DateTime.Now.AddMonths(-6));

        var logPath = Path.Combine(logDir, $"TUKAN{DateTime.Now:yyyy-MM-dd}.txt");
        SkrybekLog.Initialize(
            logPath,
            rollingInterval: RollingInterval.Infinite,
            minimumLevel: Serilog.Events.LogEventLevel.Warning);
    }

    /// <summary>Usuwa pliki logów starsze niż podana data (wg daty ostatniej modyfikacji).</summary>
    private static void UsunStarePlikiLogow(string logDir, DateTime starszeNiz)
    {
        foreach (var path in Directory.EnumerateFiles(logDir, "TUKAN*.txt"))
        {
            try
            {
                if (File.GetLastWriteTime(path) < starszeNiz)
                    File.Delete(path);
            }
            catch
            {
                // Plik zajęty / brak uprawnień — pomijamy.
            }
        }
    }
}
