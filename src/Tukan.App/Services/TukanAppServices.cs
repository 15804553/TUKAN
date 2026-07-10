using System.IO;
using BOBER.Services;
using BOBER.Services.Logging;
using Serilog;
using BOBER.Services.Startup;
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
        chomik.DatabaseOptions.MigrateLegacyDatabaseIfNeeded();

        var bober = new BOBER.Services.AppServices();
        bober.BoberOptions.FilePath = unifiedPath;
        bober.ChomikOptions.FilePath = unifiedPath;
        DatabasePathFile.Write(unifiedPath);

        await TukanUnifiedDatabaseBootstrapper.EnsureSchemaAsync(unifiedPath);
        await chomik.Database.InitializeAsync();
        await bober.Database.InitializeAsync();

        var skrybek = await SKRYBEK.Services.AppServices.CreateAsync(unifiedPath, unifiedPath);
        ServiceProvider.Services = skrybek;
        await skrybek.Backup.SprawdzIWykonajBackupAsync();

        BoberLog.Information("TUKAN: wspólna baza={UnifiedPath}", unifiedPath);

        return (new TukanAppServices(chomik, bober) { Skrybek = skrybek }, migration);
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
        var logDir = AppContext.BaseDirectory;
        var boberLog = Path.Combine(logDir, "TUKAN-bober.log");
        var skrybekLog = Path.Combine(logDir, "TUKAN-skrybek.log");

        Serilog.Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(boberLog,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        SkrybekLog.Initialize(skrybekLog);
        SkrybekLog.Info("=== TUKAN START ===");
    }
}
