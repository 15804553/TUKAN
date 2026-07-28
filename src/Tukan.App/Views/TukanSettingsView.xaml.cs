using System.Windows;
using System.Windows.Controls;
using BOBER.App.Controllers;
using BOBER.App.Views;
using Chomik.App.Controllers;
using Chomik.App.Views.Pages;
using SKRYBEK.App.ViewModels;
using SKRYBEK.App.Views;
using SKRYBEK.Core.Models;
using Tukan.App.Services;

namespace Tukan.App.Views;

public partial class TukanSettingsView : UserControl
{
    private readonly TukanAppServices _tukanServices;
    private readonly Chomik.App.Controllers.SettingsController _chomikSettingsController;
    private SettingsView? _chomikSettingsView;

    public event EventHandler? SettingsSaved;

    public TukanSettingsView(TukanAppServices tukanServices, DashboardController dashboardController)
    {
        InitializeComponent();
        _tukanServices = tukanServices;
        _chomikSettingsController = new Chomik.App.Controllers.SettingsController(tukanServices.Chomik);

        if (IsAdministratorAccount)
        {
            ConfigureAdministratorTabs();
            return;
        }

        InitializeChomikSettings(dashboardController);
        InitializeRatownikMedycznySettings();

        if (IsDcaJrgAccount)
        {
            ConfigureDcaJrgTabs();
        }
        else
        {
            InitializeBoberSettings();
            RozkazyTab.Visibility = Visibility.Collapsed;
            KalendarzTab.Visibility = Visibility.Collapsed;
            if (CanEditPojazdy)
                InitializePojazdyTab();
        }
    }

    private bool IsDcaJrgAccount => _tukanServices.SkrybekSession?.CanEditAll == true;

    private bool CanEditPojazdy => _tukanServices.SkrybekSession?.CanEditPojazdy == true;

    private bool IsAdministratorAccount =>
        _tukanServices.Chomik.Auth.CurrentUser?.CanManageExportPaths == true;

    private void ConfigureAdministratorTabs()
    {
        ChomikTab.Visibility = Visibility.Collapsed;
        GrafikTab.Visibility = Visibility.Collapsed;
        RozkazyTab.Visibility = Visibility.Collapsed;
        PojazdyTab.Visibility = Visibility.Collapsed;
        KalendarzTab.Visibility = Visibility.Collapsed;
        ExportPathsTab.Visibility = Visibility.Visible;

        var pathsControl = new ExportPathsSettingsControl(_tukanServices.Bober.Settings);
        pathsControl.SettingsSaved += (_, _) => SettingsSaved?.Invoke(this, EventArgs.Empty);
        ExportPathsHost.Content = pathsControl;
        SettingsTabControl.SelectedItem = ExportPathsTab;
    }

    private void ConfigureDcaJrgTabs()
    {
        ChomikTab.Header = "Uprawnienia/Kursy";
        GrafikTab.Visibility = Visibility.Collapsed;
        RozkazyTab.Visibility = Visibility.Visible;
        KalendarzTab.Visibility = Visibility.Visible;

        var session = _tukanServices.SkrybekSession!;
        var skrybekViewModel = new SettingsViewModel(session);
        _ = skrybekViewModel.LoadAsync();

        SkrybekSettingsHost.Content = new SkrybekSettingsView(
            session, SkrybekSettingsSection.OgolneZBackupem, skrybekViewModel);
        InitializePojazdyTab(session, skrybekViewModel);

        var kalendarzController = new KalendarzController(_tukanServices.Bober);
        var kalendarzSettings = new KalendarzSettingsView(kalendarzController);
        kalendarzSettings.SettingsSaved += (_, _) => SettingsSaved?.Invoke(this, EventArgs.Empty);
        KalendarzSettingsHost.Content = kalendarzSettings;

        SelectDefaultTab();
    }

    private void InitializePojazdyTab()
    {
        var session = _tukanServices.SkrybekSession!;
        var skrybekViewModel = new SettingsViewModel(session);
        _ = skrybekViewModel.LoadAsync();
        InitializePojazdyTab(session, skrybekViewModel);
    }

    private void InitializePojazdyTab(SessionInfo session, SettingsViewModel skrybekViewModel)
    {
        PojazdyTab.Visibility = Visibility.Visible;
        SkrybekPojazdyHost.Content = new SkrybekSettingsView(
            session, SkrybekSettingsSection.Pojazdy, skrybekViewModel);
    }

    public void SelectDefaultTab()
    {
        if (IsAdministratorAccount)
        {
            SettingsTabControl.SelectedItem = ExportPathsTab;
            return;
        }

        if (IsDcaJrgAccount)
        {
            SettingsTabControl.SelectedItem = ChomikTab;
        }
    }

    private void InitializeChomikSettings(DashboardController dashboardController)
    {
        if (dashboardController.CanManageSettings || dashboardController.CanCustomizeGeneralViewColumns)
        {
            _chomikSettingsView = new SettingsView(_chomikSettingsController);
            _chomikSettingsView.SettingsSaved += (_, _) => SettingsSaved?.Invoke(this, EventArgs.Empty);
            ChomikSettingsHost.Content = _chomikSettingsView;
        }
        else
        {
            ChomikSettingsHost.Content = new TextBlock
            {
                Text = "Brak uprawnień do ustawień modułu personelu.",
                Margin = new Thickness(16),
                TextWrapping = TextWrapping.Wrap
            };
        }
    }

    private void InitializeRatownikMedycznySettings()
    {
        var user = _tukanServices.Chomik.Auth.CurrentUser;
        if (user?.IsShiftScoped != true || user.ShiftNumber is not int zmianaId)
        {
            RatownikMedycznySettingsHost.Visibility = Visibility.Collapsed;
            return;
        }

        var view = new RatownikMedycznyUstawieniaView(zmianaId);
        view.SettingsSaved += (_, _) => SettingsSaved?.Invoke(this, EventArgs.Empty);
        RatownikMedycznySettingsHost.Content = view;
    }

    private void InitializeBoberSettings()
    {
        var controller = new MainController(_tukanServices.Bober).CreateSettingsController();
        var view = new BoberSettingsView(controller)
        {
            ShowCancelButton = false
        };
        view.SettingsSaved += (_, _) => SettingsSaved?.Invoke(this, EventArgs.Empty);
        BoberSettingsHost.Content = view;
    }
}
