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
using Tukan.App.Controllers;
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
    private UrlopPlanView? _urlopPlanView;
    private UrlopPlanController? _urlopPlanController;
    private GrafikNurkowyView? _grafikNurkowyView;
    private KalendarzView? _kalendarzView;
    private DutyAssignmentsView? _dutyAssignmentsView;
    private DutyAssignmentsWindow? _dutyAssignmentsWindow;
    private SkrybekMainView? _skrybekView;

    private MainController? _boberController;
    private DutyAssignmentsController? _dutyAssignmentsController;
    private bool _sidebarExpanded = true;

    public MainWindow(TukanAppServices tukanServices)
    {
        InitializeComponent();
        _tukanServices = tukanServices;
        _dashboardController = new DashboardController(tukanServices.Chomik);

        Closing += (_, _) =>
        {
            _dutyAssignmentsWindow?.Close();
            if (DialogResult is null)
            {
                DialogResult = false;
            }
        };

        ApplyRoleUi();
        UpdateTitleBarAccount();
        WindowState = WindowState.Maximized;
        Loaded += OnLoaded;
        Activated += OnActivated;
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
        GeneralViewButton.Visibility = _dashboardController.ShowGeneralViewNavButton
            ? Visibility.Visible : Visibility.Collapsed;
        PersonnelEditButton.Visibility = _dashboardController.CanEditPersonnel
            ? Visibility.Visible : Visibility.Collapsed;
        PasswordAdminButton.Visibility = _dashboardController.CanManagePasswords
            ? Visibility.Visible : Visibility.Collapsed;
        SettingsButton.Visibility = _dashboardController.ShowSettingsNavButton
            || _tukanServices.SkrybekSession?.CanEditAll == true
            ? Visibility.Visible : Visibility.Collapsed;
        CreatePersonnelListButton.Visibility = _dashboardController.CanCreatePersonnelList
            ? Visibility.Visible : Visibility.Collapsed;
        var user = _tukanServices.Chomik.Auth.CurrentUser;
        // Grafik służb: tylko zmiany/goście — ukryty dla PA i DCA JRG.
        BoberViewButton.Visibility = user is { IsPaUser: false, IsDcaJrgUser: false }
            ? Visibility.Visible
            : Visibility.Collapsed;
        UrlopPlanButton.Visibility = _tukanServices.Chomik.Auth.CurrentUser?.CanManageUrlopPlan == true
            ? Visibility.Visible
            : Visibility.Collapsed;
        GrafikNurkowyButton.Visibility = _tukanServices.Chomik.Auth.CurrentUser?.CanViewGrafikNurkowy == true
            ? Visibility.Visible
            : Visibility.Collapsed;
        KalendarzNavItem.Visibility = _tukanServices.Chomik.Auth.CurrentUser?.CanViewKalendarz == true
            ? Visibility.Visible
            : Visibility.Collapsed;
        DutyAssignmentsButton.Visibility = CanOpenDutyAssignments()
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private bool CanOpenDutyAssignments()
    {
        var user = _tukanServices.Chomik.Auth.CurrentUser;
        return user is { IsShiftScoped: true, IsPaUser: false } && user.ShiftNumber is >= 1 and <= 3;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        WindowState = WindowState.Maximized;
        try
        {
            if (_dashboardController.IsAdministrator)
            {
                OnPasswordAdminClick(PasswordAdminButton, new RoutedEventArgs());
            }
            else if (_tukanServices.Chomik.Auth.CurrentUser?.IsGuest == true
                     && _dashboardController.CanEditPersonnel)
            {
                OnPersonnelEditClick(PersonnelEditButton, new RoutedEventArgs());
            }
            else if (_dashboardController.CanViewGeneralView)
            {
                ShowGeneralView();
            }
            else if (_dashboardController.CanEditPersonnel)
            {
                OnPersonnelEditClick(PersonnelEditButton, new RoutedEventArgs());
            }

            _ = RefreshKalendarzUnreadBadgeAsync();
        }
        catch (Exception ex)
        {
            TukanMessageBox.Show(this, $"Nie można otworzyć widoku głównego:\n\n{ex.Message}", "TUKAN");
            DialogResult = false;
            Close();
        }
    }

    private void OnActivated(object? sender, EventArgs e) =>
        _ = RefreshKalendarzUnreadBadgeAsync();

    private async Task RefreshKalendarzUnreadBadgeAsync()
    {
        try
        {
            var user = _tukanServices.Chomik.Auth.CurrentUser;
            if (user?.CanViewKalendarz != true
                || user.IsShiftScoped != true
                || user.ShiftNumber is not int shiftNumber
                || shiftNumber is < 1 or > 3)
            {
                KalendarzUnreadBadge.Visibility = Visibility.Collapsed;
                return;
            }

            var controller = new KalendarzController(_tukanServices.Bober);
            var hasUnread = await controller.HasUnreadForRecipientAsync(shiftNumber);
            KalendarzUnreadBadge.Visibility = hasUnread ? Visibility.Visible : Visibility.Collapsed;
        }
        catch
        {
            // Badge jest tylko wskazówką — błąd bazy nie powinien blokować UI.
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
            UrlopPlanButton, GrafikNurkowyButton, BoberViewButton, SkrybekViewButton,
            DutyAssignmentsButton, KalendarzButton, SettingsButton, LogoutButton
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
        nameof(UrlopPlanButton) => "Plan urlopów",
        nameof(GrafikNurkowyButton) => "Grafik nurkowy",
        nameof(KalendarzButton) => "Kalendarz",
        nameof(DutyAssignmentsButton) => "Obsada funkcji",
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
        nameof(UrlopPlanButton) => "🏖",
        nameof(GrafikNurkowyButton) => "🤿",
        nameof(KalendarzButton) => "🗓",
        nameof(DutyAssignmentsButton) => "👥",
        nameof(SkrybekViewButton) => "📄",
        nameof(SettingsButton) => "⚙",
        nameof(LogoutButton) => "⎋",
        _ => "•"
    };

    private void ShowGeneralView()
    {
        var isFirstCreate = _generalView is null;
        _generalView ??= CreateGeneralView();
        NavigateTo(_generalView, "Widok ogólny — personel", GeneralViewButton);

        // Pierwsze utworzenie ładuje dane w OnViewLoaded — unikamy podwójnego odczytu personelu.
        if (!isFirstCreate)
        {
            _ = RefreshGeneralViewAsync();
        }
    }

    private async Task RefreshGeneralViewAsync()
    {
        if (_generalView is null) return;

        _dashboardController.InvalidatePersonnelCache();
        await _generalView.LoadPersonnelAsync();
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
        var user = _tukanServices.Chomik.Auth.CurrentUser;
        if (user is null || user.IsPaUser || user.IsDcaJrgUser)
        {
            return;
        }

        _boberController ??= new MainController(_tukanServices.Bober);
        _boberView ??= new BoberGrafikView { IsEmbedded = true };
        _boberView.Initialize(_boberController);
        NavigateTo(_boberView, "Grafik służb", BoberViewButton);
    }

    private async void OnUrlopPlanClick(object sender, RoutedEventArgs e)
    {
        if (_tukanServices.Chomik.Auth.CurrentUser?.CanManageUrlopPlan != true)
            return;

        var user = _tukanServices.Chomik.Auth.CurrentUser;
        var zmianaId = user?.ShiftNumber
            ?? _tukanServices.Bober.Auth.CurrentSession?.ZmianaId
            ?? 1;
        var nazwaZmiany = user?.Login
            ?? _tukanServices.Bober.Auth.CurrentSession?.NazwaZmiany
            ?? $"Zmiana {zmianaId}";

        var urlopLocked = false;
        if (user?.IsGuest == true)
        {
            try
            {
                urlopLocked = await _tukanServices.GuestAudit.IsUrlopPlanLockedAsync(zmianaId);
            }
            catch
            {
                urlopLocked = false;
            }
        }

        _urlopPlanController = new UrlopPlanController(_tukanServices.Bober, zmianaId, nazwaZmiany);
        _urlopPlanView ??= new UrlopPlanView { IsEmbedded = true };
        _urlopPlanView.IsReadOnlyMode = urlopLocked;
        _urlopPlanView.Initialize(_urlopPlanController);
        NavigateTo(_urlopPlanView, "Plan urlopów", UrlopPlanButton);
    }

    private void OnGrafikNurkowyClick(object sender, RoutedEventArgs e)
    {
        var user = _tukanServices.Chomik.Auth.CurrentUser;
        if (user?.CanViewGrafikNurkowy != true)
            return;

        var controller = new GrafikNurkowyController(_tukanServices.Bober);
        _grafikNurkowyView ??= new GrafikNurkowyView { IsEmbedded = true };
        _grafikNurkowyView.Initialize(
            controller,
            canApprove: user.CanApproveGrafikNurkowy,
            approverLogin: user.Login);
        NavigateTo(_grafikNurkowyView, "Grafik nurkowy", GrafikNurkowyButton);
    }

    private void OnKalendarzClick(object sender, RoutedEventArgs e)
    {
        var user = _tukanServices.Chomik.Auth.CurrentUser;
        if (user?.CanViewKalendarz != true)
            return;

        var controller = new KalendarzController(_tukanServices.Bober);
        _kalendarzView ??= new KalendarzView { IsEmbedded = true };
        _kalendarzView.NotesChanged -= OnKalendarzNotesChanged;
        _kalendarzView.NotesChanged += OnKalendarzNotesChanged;
        _kalendarzView.Initialize(
            controller,
            canEdit: user.CanEditKalendarz,
            userLogin: user.Login,
            shiftNumber: user.ShiftNumber);
        NavigateTo(_kalendarzView, "Kalendarz", KalendarzButton);
    }

    private void OnKalendarzNotesChanged(object? sender, EventArgs e) =>
        _ = RefreshKalendarzUnreadBadgeAsync();

    private void OnDutyAssignmentsClick(object sender, RoutedEventArgs e)
    {
        var user = _tukanServices.Chomik.Auth.CurrentUser;
        if (user?.IsShiftScoped != true || user.IsPaUser || user.ShiftNumber is not int shiftNumber)
        {
            return;
        }

        if (_dutyAssignmentsWindow is { IsLoaded: true })
        {
            if (_dutyAssignmentsWindow.WindowState == WindowState.Minimized)
            {
                _dutyAssignmentsWindow.WindowState = WindowState.Normal;
            }

            _dutyAssignmentsWindow.Activate();
            return;
        }

        var shiftName = $"Zmiana {shiftNumber}";
        _dutyAssignmentsController ??= new DutyAssignmentsController(_tukanServices, shiftNumber, shiftName);
        _dutyAssignmentsView ??= new DutyAssignmentsView();
        DetachFromVisualParent(_dutyAssignmentsView);
        _dutyAssignmentsView.Initialize(_dutyAssignmentsController);

        var title = $"Obsada funkcji — {shiftName}";
        _dutyAssignmentsWindow = new DutyAssignmentsWindow(_dutyAssignmentsView, title);
        // Bez Owner — okno niezależne, można przenieść na drugi monitor obok Rozkazów.
        _dutyAssignmentsWindow.Closed += (_, _) => _dutyAssignmentsWindow = null;
        _dutyAssignmentsWindow.Show();
    }

    private static void DetachFromVisualParent(FrameworkElement element)
    {
        switch (element.Parent)
        {
            case ContentControl contentControl:
                contentControl.Content = null;
                break;
            case Decorator decorator:
                decorator.Child = null;
                break;
            case Panel panel:
                panel.Children.Remove(element);
                break;
        }
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

        if (_urlopPlanView is not null && _urlopPlanController is not null)
        {
            try
            {
                await _urlopPlanView.OdswiezPoAktywacjiAsync();
            }
            catch (Exception ex)
            {
                TukanMessageBox.Show(this, $"Nie udało się odświeżyć planu urlopów po zapisie ustawień:\n\n{ex.Message}", "TUKAN");
            }
        }

        if (_skrybekView is not null)
        {
            try
            {
                await _skrybekView.OdswiezPoUstawieniachAsync();
            }
            catch (Exception ex)
            {
                TukanMessageBox.Show(this, $"Nie udało się odświeżyć rozkazów po zapisie ustawień:\n\n{ex.Message}", "TUKAN");
            }
        }

        if (_kalendarzView is not null)
        {
            try
            {
                await _kalendarzView.RefreshAsync();
            }
            catch (Exception ex)
            {
                TukanMessageBox.Show(this, $"Nie udało się odświeżyć kalendarza po zapisie ustawień:\n\n{ex.Message}", "TUKAN");
            }
        }

        await RefreshKalendarzUnreadBadgeAsync();
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
        _dashboardController.InvalidatePersonnelCache();

        if (_generalView is not null)
        {
            await _generalView.LoadPersonnelAsync();
        }

        if (_skrybekView is not null && _skrybekView.DataContext is not null)
        {
            try
            {
                await _skrybekView.OdswiezPoAktywacjiAsync();
            }
            catch (Exception ex)
            {
                TukanMessageBox.Show(this, $"Nie udało się odświeżyć rozkazów po zmianie personelu:\n\n{ex.Message}", "TUKAN");
            }
        }

        if (_boberView is not null)
        {
            try
            {
                await _boberView.OdswiezPoAktywacjiAsync();
            }
            catch (Exception ex)
            {
                TukanMessageBox.Show(this, $"Nie udało się odświeżyć grafiku po zmianie personelu:\n\n{ex.Message}", "TUKAN");
            }
        }

        if (_dutyAssignmentsView is not null)
        {
            try
            {
                await _dutyAssignmentsView.RefreshAsync();
            }
            catch (Exception ex)
            {
                TukanMessageBox.Show(this, $"Nie udało się odświeżyć obsady funkcji po zmianie personelu:\n\n{ex.Message}", "TUKAN");
            }
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
            UrlopPlanButton, GrafikNurkowyButton, BoberViewButton, SkrybekViewButton,
            DutyAssignmentsButton, KalendarzButton, SettingsButton
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
