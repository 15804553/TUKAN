using System.IO;
using BOBER.Core.Diagnostics;
using BOBER.Services.Logging;
using Serilog;
using SKRYBEK.App;
using SKRYBEK.Services.Logging;
using Tukan.App.Services.GuestAudit;

namespace Tukan.App.Services;

/// <summary>Inicjalizacja wspólnej bazy i modułów domenowych TUKAN.</summary>
public sealed class TukanAppServices : IDisposable
{
    public Chomik.Services.AppServices Chomik { get; }
    public BOBER.Services.AppServices Bober { get; }
    public SKRYBEK.Services.AppServices Skrybek { get; private set; } = null!;

    public SKRYBEK.Core.Models.SessionInfo? SkrybekSession { get; private set; }

    public GuestAuditFacade GuestAudit { get; }

    private TukanAppServices(
        Chomik.Services.AppServices chomik,
        BOBER.Services.AppServices bober,
        GuestAuditFacade guestAudit)
    {
        Chomik = chomik;
        Bober = bober;
        GuestAudit = guestAudit;
    }

    public static async Task<TukanAppServices> CreateAsync()
    {
        ConfigureLogging();

        var unifiedPath = TukanDatabaseOptions.GetFullPath();
        TukanDatabaseOptions.EnsurePathFileExists();

        await TukanUnifiedDatabaseBootstrapper.EnsureSchemaAsync(unifiedPath);
        var databasePassword = TukanDatabaseOptions.ResolvePassword();

        var chomik = new Chomik.Services.AppServices();
        chomik.DatabaseOptions.FilePath = unifiedPath;
        chomik.DatabaseOptions.DatabasePassword = databasePassword;
        chomik.DatabaseOptions.UseDatabasePassword = true;

        var bober = new BOBER.Services.AppServices();
        bober.BoberOptions.FilePath = unifiedPath;
        bober.BoberOptions.DatabasePassword = databasePassword;
        bober.BoberOptions.UseDatabasePassword = true;
        bober.ChomikOptions.FilePath = unifiedPath;
        bober.ChomikOptions.DatabasePassword = databasePassword;
        bober.ChomikOptions.UseDatabasePassword = true;

        var skrybek = await SKRYBEK.Services.AppServices.CreateAsync(
            unifiedPath,
            unifiedPath,
            ensureCreated: false,
            databasePassword: databasePassword);
        ServiceProvider.Services = skrybek;

        BoberLog.Information("TUKAN: wspólna baza: {Path}", unifiedPath);

        var guestAudit = new GuestAuditFacade(
            new GuestAuditLogService(),
            new GuestAuditSettingsService(bober.Ustawienia));

        await DatabasePerformanceDiagnostics.InspectAsync(unifiedPath, databasePassword);

        var services = new TukanAppServices(chomik, bober, guestAudit) { Skrybek = skrybek };
        WireOznaczeniaBridge();
        return services;
    }

    /// <summary>Backup w tle przy starcie — nie blokuje okna logowania.</summary>
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
            return (false, boberError ?? "Nie udało się zsynchronizować sesji grafiku.");
        }

        var skrybekSession = await Skrybek.Auth.LoginAsync(login, password);
        if (skrybekSession is null)
        {
            Chomik.Auth.Logout();
            Bober.Auth.Logout();
            return (false, "Nie udało się zsynchronizować sesji rozkazów.");
        }

        SkrybekSession = skrybekSession;
        SkrybekSession.NormalizePaFlags();
        WireGuestAuditBridges();

        var zmianaId = Bober.Auth.CurrentSession?.ZmianaId ?? 1;
        await Bober.Oznaczenia.ReloadAsync(zmianaId);

        return (true, string.Empty);
    }

    public void Logout()
    {
        ClearGuestAuditBridges();
        SkrybekSession = null;
        Chomik.Auth.Logout();
        Bober.Auth.Logout();
    }

    public void Dispose()
    {
        Logout();
        PerformanceDiagnostics.Sink = null;
    }

    private void WireGuestAuditBridges()
    {
        ClearGuestAuditBridges();

        var user = Chomik.Auth.CurrentUser;
        if (user?.IsGuest == true && user.ShiftNumber is int shift)
        {
            GuestAudit.ActivateGuestSession(shift);
            BOBER.Core.Audit.GuestChangeAudit.IsGuestSession = true;
        }

        Task Append(string moduleKey, string message)
        {
            if (!Enum.TryParse<GuestAuditModule>(moduleKey, ignoreCase: true, out var module))
                return Task.CompletedTask;
            return GuestAudit.TryAppendAsync(module, message);
        }

        global::Chomik.Core.Audit.GuestChangeAudit.TryAppendAsync = Append;
        BOBER.Core.Audit.GuestChangeAudit.TryAppendAsync = Append;
        SKRYBEK.Core.Audit.GuestChangeAudit.TryAppendAsync = Append;
        BOBER.Core.Audit.GuestChangeAudit.IsUrlopPlanLockedAsync =
            shiftNumber => GuestAudit.IsUrlopPlanLockedAsync(shiftNumber);
        BOBER.Core.Audit.GuestChangeAudit.CanManageGrafikAsync =
            shiftNumber => GuestAudit.CanGuestManageGrafikAsync(shiftNumber);
    }

    private void ClearGuestAuditBridges()
    {
        GuestAudit.ClearSession();
        global::Chomik.Core.Audit.GuestChangeAudit.Clear();
        BOBER.Core.Audit.GuestChangeAudit.Clear();
        SKRYBEK.Core.Audit.GuestChangeAudit.Clear();
    }

    private static void WireOznaczeniaBridge()
    {
        SKRYBEK.Core.Rules.BoberOznaczeniaBridge.TryMap = (string? typWpisu, out SKRYBEK.Core.Enums.TypNieobecnosci? sekcja) =>
        {
            sekcja = null;
            if (string.IsNullOrWhiteSpace(typWpisu))
                return true;

            var bazowy = BOBER.Core.Constants.GrafikWpisTypy.BazowyKod(typWpisu);
            var ozn = BOBER.Core.Oznaczenia.OznaczeniaLookup.FindByKod(bazowy);
            if (ozn is null)
                return false;

            if (BOBER.Core.Constants.GrafikWpisTypy.MaOddal(typWpisu) && ozn.MoznaOddac)
            {
                sekcja = null;
                return true;
            }

            if (ozn.WPracy)
            {
                sekcja = null;
                return true;
            }

            if (ozn.SekcjaRozkazu is null)
            {
                sekcja = null;
                return true;
            }

            sekcja = (SKRYBEK.Core.Enums.TypNieobecnosci)(int)ozn.SekcjaRozkazu.Value;
            return true;
        };

        SKRYBEK.Core.Rules.BoberOznaczeniaBridge.MapDodatkowaSekcja = typWpisu =>
        {
            var bazowy = BOBER.Core.Constants.GrafikWpisTypy.BazowyKod(typWpisu);
            var ozn = BOBER.Core.Oznaczenia.OznaczeniaLookup.FindByKod(bazowy);
            if (ozn?.DodatkowaSekcjaRozkazu is null)
                return null;
            return (SKRYBEK.Core.Enums.TypNieobecnosci)(int)ozn.DodatkowaSekcjaRozkazu.Value;
        };

        SKRYBEK.Core.Rules.BoberOznaczeniaBridge.MapAdnotacja = (typWpisu, typSekcji) =>
        {
            var bazowy = BOBER.Core.Constants.GrafikWpisTypy.BazowyKod(typWpisu);
            var ozn = BOBER.Core.Oznaczenia.OznaczeniaLookup.FindByKod(bazowy);
            if (ozn is null || string.IsNullOrWhiteSpace(ozn.AdnotacjaRozkazu) || ozn.SekcjaRozkazu is null)
                return null;

            var oczekiwana = (SKRYBEK.Core.Enums.TypNieobecnosci)(int)ozn.SekcjaRozkazu.Value;
            if (oczekiwana != typSekcji)
                return null;

            return ozn.AdnotacjaRozkazu;
        };
    }

    private static void ConfigureLogging()
    {
        var logDir = Path.Combine(AppContext.BaseDirectory, "LOG");
        Directory.CreateDirectory(logDir);
        UsunStarePlikiLogow(logDir, DateTime.Now.AddMonths(-6));

        var logPath = Path.Combine(logDir, $"TUKAN{DateTime.Now:yyyy-MM-dd}.txt");
        SkrybekLog.Initialize(
            logPath,
            rollingInterval: RollingInterval.Infinite,
            minimumLevel: PerformanceDiagnostics.IsEnabled
                ? Serilog.Events.LogEventLevel.Information
                : Serilog.Events.LogEventLevel.Warning);
        PerformanceDiagnostics.Sink = measurement =>
            BoberLog.Information(
                "WYDAJNOSC Operacja={Operation} Etap={Stage} CzasMs={ElapsedMilliseconds:F1} Elementy={ItemCount}",
                measurement.Operation,
                measurement.Stage,
                measurement.ElapsedMilliseconds,
                measurement.ItemCount);
    }

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
