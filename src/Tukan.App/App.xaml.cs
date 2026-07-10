using System.Globalization;
using System.Windows;
using System.Windows.Threading;
using Chomik.App.Controls;
using Tukan.App.Infrastructure;
using Tukan.App.Services;
using Tukan.App.Views;

namespace Tukan.App;

public partial class App : Application
{
    private TukanAppServices? _services;
    private readonly TukanJsonSettingsService _settingsService = new();

    public static TukanJsonSettingsService SettingsService =>
        ((App)Current)._settingsService;

    protected override void OnStartup(StartupEventArgs e)
    {
        var culture = CultureInfo.GetCultureInfo("pl-PL");
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        base.OnStartup(e);
        ToolTipPlacementFix.Register();
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        var settings = _settingsService.Load();
        TukanAppTheme.Apply(TukanAppTheme.Parse(settings.UiColorPalette));

        DispatcherUnhandledException += (_, args) =>
        {
            SKRYBEK.Services.Logging.SkrybekLog.Error(
                "Nieobsłużony wyjątek w wątku UI TUKAN",
                args.Exception);
            MessageBox.Show(
                $"Wystąpił nieoczekiwany błąd:\n\n{args.Exception.Message}",
                "TUKAN",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            args.Handled = true;
        };

        try
        {
            var (services, migration) = TukanAppServices.CreateAsync().GetAwaiter().GetResult();
            _services = services;

            if (migration.Any)
            {
                MessageBox.Show(
                    TukanMigrationSummary.FormatForUser(migration),
                    "TUKAN — migracja danych",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Nie udało się przygotować baz danych.\n\n{ex.Message}\n\n" +
                "Upewnij się, że zainstalowany jest Microsoft Access Database Engine (ACE) x64.",
                "TUKAN",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
            return;
        }

        RunLoginLoop();
    }

    private void RunLoginLoop()
    {
        while (true)
        {
            var login = new LoginWindow(_services!);
            if (login.ShowDialog() != true)
            {
                _services?.Dispose();
                Shutdown();
                return;
            }

            var main = new MainWindow(_services!);
            main.WindowState = WindowState.Maximized;
            main.ShowDialog();
            _services!.Logout();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _services?.Dispose();
        Serilog.Log.CloseAndFlush();
        base.OnExit(e);
    }
}
