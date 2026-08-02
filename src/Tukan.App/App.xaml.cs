using System.Globalization;
using System.Windows;
using System.Windows.Media.Imaging;
using BOBER.App.Views.Chrome;
using Chomik.App.Controls;
using Chomik.App.Views.Branding;
using Chomik.App.Views.Chrome;
using SKRYBEK.App.Helpers;
using Tukan.App.Infrastructure;
using Tukan.App.Services;
using Tukan.App.Views;

namespace Tukan.App;

public partial class App : Application
{
    private const string ApplicationTitle = "TUKAN";
    private static readonly Uri TukanIconUri = new("pack://application:,,,/Assets/tukan.ico");

    private TukanAppServices? _services;

    protected override void OnStartup(StartupEventArgs e)
    {
        var culture = CultureInfo.GetCultureInfo("pl-PL");
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        base.OnStartup(e);
        ToolTipPlacementFix.Register();
        ConfigureHostedModuleBranding();
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        TukanAppTheme.Apply();

        DispatcherUnhandledException += (_, args) =>
        {
            SKRYBEK.Services.Logging.SkrybekLog.Error(
                "Nieobsłużony wyjątek w wątku UI TUKAN",
                args.Exception);
            MessageBox.Show(
                $"Wystąpił nieoczekiwany błąd:\n\n{args.Exception.Message}",
                ApplicationTitle,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            args.Handled = true;
        };

        try
        {
            _services = TukanAppServices.CreateAsync().GetAwaiter().GetResult();
            _services.StartBackgroundBackup();
        }
        catch (Exception ex)
        {
            var aceArch = Environment.Is64BitProcess ? "x64" : "x86 (32-bit)";
            MessageBox.Show(
                $"Nie udało się przygotować bazy danych.\n\n{ex.Message}\n\n" +
                $"Aplikacja działa jako proces {aceArch}. " +
                $"Zainstaluj Microsoft Access Database Engine (ACE) w tej samej architekturze ({aceArch}).",
                ApplicationTitle,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
            return;
        }

        RunLoginLoop();
    }

    private static void ConfigureHostedModuleBranding()
    {
        BoberMessageBox.ApplicationTitleOverride = ApplicationTitle;
        ChomikMessageBox.ApplicationTitleOverride = ApplicationTitle;
        SkrybekMessageBox.ApplicationTitleOverride = ApplicationTitle;

        ChomikAppIcon.IconOverride = BitmapFrame.Create(TukanIconUri);
        ChomikTitleBar.ShowLogoOverride = false;
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
