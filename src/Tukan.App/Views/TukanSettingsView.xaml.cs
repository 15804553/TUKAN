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
    private RatownikMedycznyUstawieniaView? _ratownikMedycznySettingsView;

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
            InitializeShiftCalendarSettings();
            if (CanEditPojazdy)
                InitializePojazdyTab();
            InitializeGuestAuditSettings();
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
        GuestAuditTab.Visibility = Visibility.Collapsed;
        ExportPathsTab.Visibility = Visibility.Visible;

        var pathsControl = new ExportPathsSettingsControl(_tukanServices.Bober.Settings);
        pathsControl.SettingsSaved += (_, _) => SettingsSaved?.Invoke(this, EventArgs.Empty);
        ExportPathsHost.Content = pathsControl;
        SettingsTabControl.SelectedItem = ExportPathsTab;
    }

    private void ConfigureDcaJrgTabs()
    {
        ChomikTab.Header = "Słowniki";
        GrafikTab.Visibility = Visibility.Collapsed;
        RozkazyTab.Visibility = Visibility.Visible;
        KalendarzTab.Visibility = Visibility.Visible;
        GuestAuditTab.Visibility = Visibility.Collapsed;

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

    private void InitializeShiftCalendarSettings()
    {
        var user = _tukanServices.Chomik.Auth.CurrentUser;
        if (user?.IsShiftAccount != true || user.ShiftNumber is not int shiftNumber || shiftNumber is < 1 or > 3)
        {
            KalendarzTab.Visibility = Visibility.Collapsed;
            return;
        }

        KalendarzTab.Visibility = Visibility.Visible;
        var kalendarzController = new KalendarzController(_tukanServices.Bober);
        var kalendarzSettings = new KalendarzSettingsView(
            kalendarzController,
            showColorSettings: false,
            settingsShiftNumber: shiftNumber);
        kalendarzSettings.SettingsSaved += (_, _) => SettingsSaved?.Invoke(this, EventArgs.Empty);
        KalendarzSettingsHost.Content = kalendarzSettings;
    }

    private void InitializeGuestAuditSettings()
    {
        var user = _tukanServices.Chomik.Auth.CurrentUser;
        if (user?.IsShiftAccount != true || user.ShiftNumber is not int shiftNumber || shiftNumber is < 1 or > 3)
        {
            GuestAuditTab.Visibility = Visibility.Collapsed;
            return;
        }

        GuestAuditTab.Visibility = Visibility.Visible;
        GuestAuditHost.Content = new GuestAuditSettingsView(
            _tukanServices.GuestAudit,
            shiftNumber,
            canConfigure: true);
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
        skrybekViewModel.SettingsSaved -= OnSkrybekSettingsSaved;
        skrybekViewModel.SettingsSaved += OnSkrybekSettingsSaved;
        SkrybekPojazdyHost.Content = new SkrybekSettingsView(
            session, SkrybekSettingsSection.Pojazdy, skrybekViewModel);
    }

    private async void OnSkrybekSettingsSaved(object? sender, EventArgs e)
    {
        try
        {
            if (_ratownikMedycznySettingsView is not null)
                await _ratownikMedycznySettingsView.ReloadAsync();

            await TryAuditSettingsAsync("Ustawienia pojazdów");
        }
        finally
        {
            SettingsSaved?.Invoke(this, EventArgs.Empty);
        }
    }

    private async Task TryAuditSettingsAsync(string message)
    {
        try
        {
            await _tukanServices.GuestAudit.TryAppendAsync(
                Services.GuestAudit.GuestAuditModule.Ustawienia,
                message);
        }
        catch
        {
            // Audyt nie może blokować zapisu ustawień.
        }
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
            _chomikSettingsView.SettingsSaved += async (_, _) =>
            {
                await TryAuditSettingsAsync("Ustawienia personelu");
                SettingsSaved?.Invoke(this, EventArgs.Empty);
            };
            ChomikSettingsHost.Content = _chomikSettingsView;
        }
        else if (_tukanServices.Chomik.Auth.CurrentUser?.IsGuest == true)
        {
            ChomikSettingsHost.Content = new TextBlock
            {
                Text = "Ustawienia personelu (kolumny widoku ogólnego) są niedostępne dla konta Gość.",
                Margin = new Thickness(16),
                TextWrapping = TextWrapping.Wrap
            };
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

        _ratownikMedycznySettingsView = new RatownikMedycznyUstawieniaView(zmianaId);
        _ratownikMedycznySettingsView.SettingsSaved += async (_, _) =>
        {
            await TryAuditSettingsAsync("Ustawienia ratowników medycznych");
            SettingsSaved?.Invoke(this, EventArgs.Empty);
        };
        RatownikMedycznySettingsHost.Content = _ratownikMedycznySettingsView;
    }

    private void InitializeBoberSettings()
    {
        var controller = new MainController(_tukanServices.Bober).CreateSettingsController();
        var view = new BoberSettingsView(controller)
        {
            ShowCancelButton = false
        };
        view.SettingsSaved += async (_, _) =>
        {
            await TryAuditSettingsAsync("Ustawienia grafiku");
            SettingsSaved?.Invoke(this, EventArgs.Empty);
        };
        BoberSettingsHost.Content = view;
    }
}
