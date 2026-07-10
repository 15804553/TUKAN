using System.Globalization;
using System.Windows;
using System.Windows.Threading;
using Chomik.App.Controls;
using Chomik.App.Controllers;
using Chomik.App.Views;
using Chomik.App.Views.Chrome;
using Chomik.Services;

namespace Chomik.App;

public partial class App : Application
{
    private AppServices? _services;

    protected override void OnStartup(StartupEventArgs e)
    {
        var culture = CultureInfo.GetCultureInfo("pl-PL");
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        base.OnStartup(e);
        ToolTipPlacementFix.Register();
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            args.SetObserved();
            ShowFatalError(args.Exception);
        };
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception ex)
            {
                ShowFatalError(ex);
            }
        };

        _services = new AppServices();

        try
        {
            _services.Database.InitializeAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            ChomikMessageBox.Show(
                null,
                $"Nie udało się przygotować bazy danych.\n\n{ex.Message}\n\n" +
                "Upewnij się, że zainstalowany jest Microsoft Access Database Engine (ACE).",
                "Chomik");
            Shutdown();
            return;
        }

        RunLoginLoop();
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

            try
            {
                var dashboardController = new DashboardController(_services!);
                var dashboard = new DashboardWindow(dashboardController, _services!);
                if (dashboard.ShowDialog() != true)
                {
                    continue;
                }
            }
            catch (Exception ex)
            {
                ShowFatalError(ex);
                Shutdown();
                return;
            }
        }
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs args)
    {
        ShowFatalError(args.Exception);
        args.Handled = true;
    }

    private static void ShowFatalError(Exception ex)
    {
        ChomikMessageBox.Show(
            null,
            $"Wystąpił nieoczekiwany błąd:\n\n{ex.Message}\n\n{ex.GetType().Name}",
            "Chomik");
    }
}
