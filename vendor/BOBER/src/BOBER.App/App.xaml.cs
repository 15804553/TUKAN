using System.Globalization;
using System.IO;
using System.Windows;
using Serilog;
using BOBER.App.Controllers;
using BOBER.App.Logging;
using BOBER.App.Views;
using BOBER.App.Views.Chrome;
using BOBER.Services;
using BOBER.Services.Logging;
using BOBER.Services.Startup;
using DatabasePathFile = BOBER.Services.Startup.DatabasePathFile;

namespace BOBER.App;

/// <summary>
/// Punkt wejścia WPF: logowanie (Serilog → BOBER.log), inicjalizacja BoberDatabase, pętla logowanie → grafik.
/// </summary>
public partial class App : Application
{
    private AppServices? _services;

    protected override void OnStartup(StartupEventArgs e)
    {
        ConfigureLogging();

        var culture = CultureInfo.GetCultureInfo("pl-PL");
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        DispatcherUnhandledException += (_, args) =>
        {
            BoberLog.Error(args.Exception, "Nieobsłużony wyjątek UI");
            BoberMessageBox.Show(
                null,
                BoberLog.FormatUserMessage(args.Exception, "Wystąpił nieoczekiwany błąd"),
                "BOBER");
            args.Handled = true;
        };

        _services = new AppServices();

        try
        {
            _services.Database.InitializeAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            BoberLog.Error(ex, "Błąd inicjalizacji bazy danych BOBER");
            BoberMessageBox.Show(
                null,
                BoberLog.FormatUserMessage(ex, "Nie udało się przygotować bazy danych") +
                "\n\nUpewnij się, że zainstalowany jest Microsoft Access Database Engine (ACE).",
                "BOBER");
            Shutdown();
            return;
        }

        LoadChomikDbPathFromSettings();
        StartupDiagnostics.Run(_services.BoberOptions, _services.ChomikOptions);
        RunLoginLoop();
    }

    private void LoadChomikDbPathFromSettings()
    {
        try
        {
            var pathFromFile = DatabasePathFile.TryRead();
            if (!string.IsNullOrWhiteSpace(pathFromFile))
            {
                _services!.ChomikOptions.FilePath = pathFromFile;
                BoberLog.Information("Ścieżka ChomikDB z databasepath.txt: {Path}", pathFromFile);
                return;
            }

            // Brak pliku databasepath.txt — utwórz z domyślną ścieżką (AppPath\CHOMIK\ChomikDatabase.accdb)
            DatabasePathFile.EnsureExists();
            BoberLog.Information("Utworzono databasepath.txt z domyślną ścieżką: {Path}", DatabasePathFile.DefaultPath);
        }
        catch (Exception ex)
        {
            BoberLog.Warning(ex, "Błąd ładowania ścieżki ChomikDB");
        }
    }

    private void RunLoginLoop()
    {
        while (true)
        {
            var loginController = new LoginController(_services!);
            var loginWindow = new LoginWindow(loginController);
            if (loginWindow.ShowDialog() != true)
            {
                Shutdown();
                return;
            }

            var mainController = new MainController(_services!);
            var mainWindow = new MainWindow(mainController);
            mainWindow.ShowDialog();

            _services!.Auth.Logout();
        }
    }

    private static void ConfigureLogging()
    {
        var logPath = Path.Combine(AppContext.BaseDirectory, "BOBER.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        BoberLog.Information("Dziennik: {LogPath}", logPath);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
