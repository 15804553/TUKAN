using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BOBER.App.Controllers;
using BOBER.App.Views;
using Chomik.App.Controllers;
using Chomik.App.Views.Pages;
using SKRYBEK.App.ViewModels;
using SKRYBEK.App.Views;
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

        PlaceThemeSettingsPanel();
        InitializeChomikSettings(dashboardController);
        InitializeRatownikMedycznySettings();

        if (IsDcaJrgAccount)
        {
            ConfigureDcaJrgTabs();
        }
        else
        {
            InitializeBoberSettings();
            InitializeSkrybekSettings();
        }
    }

    private bool IsDcaJrgAccount => _tukanServices.SkrybekSession?.CanEditAll == true;

    private void PlaceThemeSettingsPanel()
    {
        var themePanel = new TukanThemeSettingsControl();

        if (IsDcaJrgAccount)
        {
            DcaThemeHost.Content = themePanel;
            return;
        }

        WygladThemeHost.Content = themePanel;
    }

    private void ConfigureDcaJrgTabs()
    {
        ChomikTab.Header = "Uprawnienia/Kursy";
        WygladTab.Visibility = Visibility.Collapsed;
        GrafikTab.Visibility = Visibility.Collapsed;
        RozkazyTab.Visibility = Visibility.Collapsed;
        OgolneTab.Visibility = Visibility.Visible;
        PojazdyTab.Visibility = Visibility.Visible;

        var session = _tukanServices.SkrybekSession!;
        var skrybekViewModel = new SettingsViewModel(session);
        _ = skrybekViewModel.LoadAsync();

        SkrybekOgolneBackupHost.Content = new SkrybekSettingsView(
            session, SkrybekSettingsSection.OgolneZBackupem, skrybekViewModel);
        SkrybekPojazdyHost.Content = new SkrybekSettingsView(session, SkrybekSettingsSection.Pojazdy, skrybekViewModel);

        SelectDefaultTab();
    }

    public void SelectDefaultTab()
    {
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

    private void InitializeSkrybekSettings()
    {
        if (_tukanServices.SkrybekSession?.CanEditAll == true)
        {
            SkrybekSettingsHost.Content = new SkrybekSettingsView(_tukanServices.SkrybekSession);
            return;
        }

        SkrybekSettingsHost.Content = new TextBlock
        {
            Text = "Ustawienia rozkazów są dostępne wyłącznie dla konta DCA JRG.",
            Margin = new Thickness(16),
            TextWrapping = TextWrapping.Wrap,
            Foreground = (Brush)FindResource("MutedTextBrush")
        };
    }
}
