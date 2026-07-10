using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using BOBER.App.Controllers;
using BOBER.App.Views;
using Chomik.App.Controllers;
using Chomik.App.Views;
using Tukan.App.Views.Chrome;
using Chomik.App.Views.Pages;
using Microsoft.Win32;
using SKRYBEK.App.Views;
using Tukan.App.Infrastructure;
using Tukan.App.Services;

namespace Tukan.App.Views;

public partial class MainWindow : Window
{
    private readonly TukanAppServices _tukanServices;
    private readonly DashboardController _dashboardController;

    private GeneralPersonnelView? _generalView;
    private PersonnelManagementView? _personnelView;
    private PasswordManagementView? _passwordView;
    private TukanSettingsView? _unifiedSettingsView;
    private BoberGrafikView? _boberView;
    private SkrybekMainView? _skrybekView;

    private MainController? _boberController;
    private bool _sidebarExpanded = true;

    public MainWindow(TukanAppServices tukanServices)
    {
        InitializeComponent();
        _tukanServices = tukanServices;
        _dashboardController = new DashboardController(tukanServices.Chomik);

        Closing += (_, _) =>
        {
            if (DialogResult is null)
            {
                DialogResult = false;
            }
        };

        ApplyRoleUi();
        UpdateTitleBarAccount();
        Loaded += OnLoaded;
        StateChanged += OnStateChanged;
        SourceInitialized += OnSourceInitialized;
    }

    private void OnSourceInitialized(object? sender, EventArgs e) => ApplyMaximizedLayout();

    private void OnStateChanged(object? sender, EventArgs e) => ApplyMaximizedLayout();

    private void ApplyMaximizedLayout()
    {
        if (WindowState == WindowState.Maximized)
        {
            CustomWindowChromeHelper.ApplyMaximizedWorkArea(this);
            return;
        }

        CustomWindowChromeHelper.ClearMaximizedWorkAreaConstraints(this);
    }

    private void ApplyRoleUi()
    {
        PersonnelEditButton.Visibility = _dashboardController.CanEditPersonnel
            ? Visibility.Visible : Visibility.Collapsed;
        PasswordAdminButton.Visibility = _dashboardController.CanManagePasswords
            ? Visibility.Visible : Visibility.Collapsed;
        SettingsButton.Visibility = _dashboardController.ShowSettingsNavButton
            || _tukanServices.SkrybekSession?.CanEditAll == true
            ? Visibility.Visible : Visibility.Collapsed;
        CreatePersonnelListButton.Visibility = _dashboardController.CanCreatePersonnelList
            ? Visibility.Visible : Visibility.Collapsed;
        BoberViewButton.Visibility = _tukanServices.Chomik.Auth.CurrentUser?.IsPaUser == true
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        try
        {
            ShowGeneralView();
        }
        catch (Exception ex)
        {
            TukanMessageBox.Show(this, $"Nie można otworzyć widoku głównego:\n\n{ex.Message}", "TUKAN");
            DialogResult = false;
            Close();
        }
    }

    private void OnToggleSidebarClick(object sender, RoutedEventArgs e)
    {
        _sidebarExpanded = !_sidebarExpanded;
        SidebarColumn.Width = _sidebarExpanded ? new GridLength(220) : new GridLength(56);
        BrandTextBlock.Visibility = _sidebarExpanded ? Visibility.Visible : Visibility.Collapsed;

        foreach (var button in new[]
        {
            GeneralViewButton, PersonnelEditButton, PasswordAdminButton, CreatePersonnelListButton,
            BoberViewButton, SkrybekViewButton, SettingsButton, LogoutButton
        })
        {
            button.Content = _sidebarExpanded ? GetButtonLabel(button) : GetButtonIcon(button);
        }
    }

    private static string GetButtonLabel(Button button) => button.Name switch
    {
        nameof(GeneralViewButton) => "Widok ogólny",
        nameof(PersonnelEditButton) => "Edycja personelu",
        nameof(PasswordAdminButton) => "Zarządzanie hasłami",
        nameof(CreatePersonnelListButton) => "Utwórz listę osób",
        nameof(BoberViewButton) => "Grafik służb",
        nameof(SkrybekViewButton) => "Rozkazy dzienne",
        nameof(SettingsButton) => "Ustawienia",
        nameof(LogoutButton) => "Wyloguj",
        _ => button.Content?.ToString() ?? string.Empty
    };

    private static string GetButtonIcon(Button button) => button.Name switch
    {
        nameof(GeneralViewButton) => "◎",
        nameof(PersonnelEditButton) => "✎",
        nameof(PasswordAdminButton) => "🔑",
        nameof(CreatePersonnelListButton) => "📋",
        nameof(BoberViewButton) => "📅",
        nameof(SkrybekViewButton) => "📄",
        nameof(SettingsButton) => "⚙",
        nameof(LogoutButton) => "⎋",
        _ => "•"
    };

    private void ShowGeneralView()
    {
        _generalView ??= CreateGeneralView();
        NavigateTo(_generalView, "Widok ogólny — personel", GeneralViewButton);
    }

    private GeneralPersonnelView CreateGeneralView()
    {
        var view = new GeneralPersonnelView(_dashboardController);
        view.PersonnelEditRequested += OnGeneralViewPersonnelEditRequested;
        view.PersonnelProfileRequested += OnGeneralViewPersonnelProfileRequested;
        view.LoadFailed += (_, message) => TukanMessageBox.Show(this, message, "TUKAN");
        return view;
    }

    private async void OnGeneralViewPersonnelProfileRequested(object? sender, int funkcjonariuszId)
    {
        if (!_dashboardController.CanOpenPersonnelProfile)
        {
            return;
        }

        try
        {
            var profile = await _dashboardController.GetPersonnelProfileAsync(funkcjonariuszId);
            if (profile is null)
            {
                TukanMessageBox.Show(this, "Nie można wyświetlić profilu tego funkcjonariusza.", "Informacja");
                return;
            }

            var window = new PersonnelProfileWindow(profile) { Owner = this };
            window.ShowDialog();
        }
        catch (Exception ex)
        {
            TukanMessageBox.Show(this, ex.Message, "TUKAN");
        }
    }

    private async void OnGeneralViewPersonnelEditRequested(object? sender, int funkcjonariuszId)
    {
        if (!_dashboardController.CanEditPersonnel)
        {
            return;
        }

        var personnelController = new PersonnelManagementController(_tukanServices.Chomik);
        var entity = await personnelController.GetForEditAsync(funkcjonariuszId);
        if (entity is null)
        {
            TukanMessageBox.Show(this, "Brak uprawnień do edycji tego funkcjonariusza.", "Informacja");
            return;
        }

        var dictionaries = await personnelController.GetDictionariesAsync();
        var window = new PersonnelEditWindow(personnelController, dictionaries, entity) { Owner = this };
        if (window.ShowDialog() == true && _generalView is not null)
        {
            await _generalView.LoadPersonnelAsync();
        }
    }

    private void OnGeneralViewClick(object sender, RoutedEventArgs e) => ShowGeneralView();

    private void OnPersonnelEditClick(object sender, RoutedEventArgs e)
    {
        var personnelController = new PersonnelManagementController(_tukanServices.Chomik);
        _personnelView ??= new PersonnelManagementView(personnelController);
        _personnelView.PersonnelChanged -= OnPersonnelChanged;
        _personnelView.PersonnelChanged += OnPersonnelChanged;
        NavigateTo(_personnelView, $"Edycja personelu — zmiana {personnelController.ShiftNumber}", PersonnelEditButton);
    }

    private void OnPasswordAdminClick(object sender, RoutedEventArgs e)
    {
        _passwordView ??= new PasswordManagementView(new PasswordManagementController(_tukanServices.Chomik));
        NavigateTo(_passwordView, "Zarządzanie hasłami", PasswordAdminButton);
    }

    private void OnBoberViewClick(object sender, RoutedEventArgs e)
    {
        if (_tukanServices.Chomik.Auth.CurrentUser?.IsPaUser == true)
        {
            return;
        }

        _boberController ??= new MainController(_tukanServices.Bober);
        _boberView ??= new BoberGrafikView { IsEmbedded = true };
        _boberView.Initialize(_boberController);
        NavigateTo(_boberView, "Grafik służb", BoberViewButton);
    }

    private async void OnSkrybekViewClick(object sender, RoutedEventArgs e)
    {
        if (_tukanServices.SkrybekSession is null)
        {
            return;
        }

        _skrybekView ??= new SkrybekMainView { IsEmbedded = true };
        var session = _tukanServices.SkrybekSession;
        var login = _tukanServices.Chomik.Auth.CurrentUser?.Login ?? session.Login;

        if (_skrybekView.DataContext is null)
        {
            await _skrybekView.InitializeAsync(session);
        }
        else
        {
            _skrybekView.SetLoggedInUser(login, session.CanEditAll);
        }

        NavigateTo(_skrybekView, "Rozkazy dzienne", SkrybekViewButton);
    }

    private void OnSettingsClick(object sender, RoutedEventArgs e)
    {
        try
        {
            _unifiedSettingsView ??= new TukanSettingsView(_tukanServices, _dashboardController);
            _unifiedSettingsView.SettingsSaved -= OnChomikSettingsSaved;
            _unifiedSettingsView.SettingsSaved += OnChomikSettingsSaved;
            _unifiedSettingsView.SelectDefaultTab();
            NavigateTo(_unifiedSettingsView, "Ustawienia", SettingsButton);
        }
        catch (Exception ex)
        {
            TukanMessageBox.Show(this, $"Nie można otworzyć ustawień:\n\n{ex.Message}", "TUKAN");
        }
    }

    private async void OnChomikSettingsSaved(object? sender, EventArgs e)
    {
        if (_generalView is not null)
        {
            if (_dashboardController.CanCustomizeGeneralViewColumns)
            {
                var preferences = await _dashboardController.GetGeneralViewColumnPreferencesAsync();
                _generalView.ApplyColumnPreferences(preferences);
            }

            await _generalView.InitializeFiltersAsync();
            await _generalView.LoadPersonnelAsync();
        }

        if (_boberController is not null && _boberView is not null)
        {
            try
            {
                await _boberView.ReloadAfterSettingsAsync();
            }
            catch (Exception ex)
            {
                TukanMessageBox.Show(this, $"Nie udało się odświeżyć grafiku po zapisie ustawień:\n\n{ex.Message}", "TUKAN");
            }
        }
    }

    private async void OnCreatePersonnelListClick(object sender, RoutedEventArgs e)
    {
        if (!_dashboardController.CanCreatePersonnelList)
        {
            return;
        }

        int? shiftValue;
        string shiftLabel;
        if (_dashboardController.ExportPersonnelListUsesOwnShiftOnly)
        {
            shiftValue = _dashboardController.PersonnelListExportShiftNumber;
            shiftLabel = shiftValue is int shift ? $"Zmiana {shift}" : "Zmiana";
        }
        else
        {
            _generalView ??= CreateGeneralView();
            if (MainContentHost.Content != _generalView)
            {
                ShowGeneralView();
            }

            var shiftFilter = _generalView!.GetCurrentShiftFilter();
            shiftValue = shiftFilter.Value;
            shiftLabel = shiftFilter.Label;
        }

        var dialog = new SaveFileDialog
        {
            Title = "Zapisz listę osób",
            Filter = "Plik Excel (*.xlsx)|*.xlsx",
            DefaultExt = "xlsx",
            FileName = $"Lista_osob_{SanitizeFileName(shiftLabel)}_{DateTime.Now:yyyy-MM-dd}.xlsx",
            AddExtension = true
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            CreatePersonnelListButton.IsEnabled = false;
            var names = await _dashboardController.GetPersonnelNamesForExportAsync(shiftValue);
            DashboardController.ExportPersonnelListToExcel(names, dialog.FileName);
            TukanMessageBox.Show(
                this,
                $"Zapisano {names.Count} osób (filtr: {shiftLabel}).\n\nLokalizacja pliku:\n{dialog.FileName}",
                "Lista osób");
        }
        catch (Exception ex)
        {
            TukanMessageBox.Show(this, $"Nie udało się utworzyć listy:\n\n{ex.Message}", "TUKAN");
        }
        finally
        {
            CreatePersonnelListButton.IsEnabled = true;
        }
    }

    private async void OnPersonnelChanged(object? sender, EventArgs e)
    {
        if (_generalView is not null)
        {
            await _generalView.LoadPersonnelAsync();
        }
    }

    private void NavigateTo(UserControl view, string title, Button? activeButton)
    {
        ViewTitleTextBlock.Text = title;
        MainContentHost.Content = view;
        HighlightSidebarButton(activeButton);
    }

    private void UpdateTitleBarAccount()
    {
        TitleBar.Title = GetLoggedInAccountTitle();
    }

    private string GetLoggedInAccountTitle()
    {
        return _tukanServices.Chomik.Auth.CurrentUser?.Login ?? string.Empty;
    }

    private void HighlightSidebarButton(Button? activeButton)
    {
        foreach (var button in new[]
        {
            GeneralViewButton, PersonnelEditButton, PasswordAdminButton, CreatePersonnelListButton,
            BoberViewButton, SkrybekViewButton, SettingsButton
        })
        {
            if (button.Visibility != Visibility.Visible)
            {
                continue;
            }

            var isActive = activeButton is not null && button == activeButton;
            button.FontWeight = isActive ? FontWeights.SemiBold : FontWeights.Normal;
            button.Opacity = isActive ? 1.0 : 0.75;
        }
    }

    private void OnLogoutClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(name.Length);
        foreach (var ch in name)
        {
            builder.Append(invalid.Contains(ch) ? '_' : ch);
        }

        return builder.ToString().Replace(' ', '_');
    }
}
