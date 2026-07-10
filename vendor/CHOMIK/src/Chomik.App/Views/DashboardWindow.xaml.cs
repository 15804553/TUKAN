using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Chomik.App.Controllers;
using Chomik.App.ViewModels;
using Microsoft.Win32;
using Chomik.App.Views.Chrome;
using Chomik.App.Views.Pages;
using Chomik.Services;

namespace Chomik.App.Views;

public partial class DashboardWindow : Window
{
    private const string MainWindowTitle = "Chomik — system ewidencji personelu PSP";

    private readonly DashboardController _controller;
    private readonly AppServices _services;

    private GeneralPersonnelView? _generalView;
    private PersonnelManagementView? _personnelView;
    private PasswordManagementView? _passwordView;
    private SettingsView? _settingsView;

    public DashboardWindow(DashboardController controller, AppServices services)
    {
        InitializeComponent();
        Title = MainWindowTitle;
        TitleBar.Title = MainWindowTitle;
        ChromeWindowConfigurator.Apply(this);
        _controller = controller;
        _services = services;
        AppWindowLayout.ApplyMain(this);
        ApplyRoleUi();
        Loaded += OnLoaded;
    }

    private void ApplyRoleUi()
    {
        var user = _services.Auth.CurrentUser!;
        UserLoginTextBlock.Text = user.Login;
        GeneralViewButton.Visibility = _controller.ShowGeneralViewNavButton
            ? Visibility.Visible
            : Visibility.Collapsed;
        PersonnelEditButton.Visibility = _controller.CanEditPersonnel ? Visibility.Visible : Visibility.Collapsed;
        PasswordAdminButton.Visibility = _controller.CanManagePasswords ? Visibility.Visible : Visibility.Collapsed;
        SettingsButton.Visibility = _controller.ShowSettingsNavButton ? Visibility.Visible : Visibility.Collapsed;
        CreatePersonnelListButton.Visibility = _controller.CanCreatePersonnelList
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        try
        {
            AppWindowLayout.ApplyMain(this);
            if (_controller.IsAdministrator)
            {
                ShowPasswordAdmin();
            }
            else
            {
                ShowGeneralView();
            }
        }
        catch (Exception ex)
        {
            ChomikMessageBox.Show(this, $"Nie można otworzyć widoku głównego:\n\n{ex.Message}", "Chomik");
            DialogResult = false;
            Close();
        }
    }

    private void ShowGeneralView()
    {
        if (_generalView is null)
        {
            _generalView = new GeneralPersonnelView(_controller);
            _generalView.PersonnelEditRequested -= OnGeneralViewPersonnelEditRequested;
            _generalView.PersonnelEditRequested += OnGeneralViewPersonnelEditRequested;
            _generalView.PersonnelProfileRequested -= OnGeneralViewPersonnelProfileRequested;
            _generalView.PersonnelProfileRequested += OnGeneralViewPersonnelProfileRequested;
            _generalView.LoadFailed -= OnGeneralViewLoadFailed;
            _generalView.LoadFailed += OnGeneralViewLoadFailed;
        }

        var activeButton = _controller.ShowGeneralViewNavButton ? GeneralViewButton : null;
        NavigateTo(_generalView, "Widok ogólny", activeButton);
    }

    private void OnGeneralViewLoadFailed(object? sender, string message)
    {
        ChomikMessageBox.Show(this, message, "Chomik");
    }

    private async void OnGeneralViewPersonnelProfileRequested(object? sender, int funkcjonariuszId)
    {
        if (!_controller.CanOpenPersonnelProfile)
        {
            return;
        }

        try
        {
            var profile = await _controller.GetPersonnelProfileAsync(funkcjonariuszId);
            if (profile is null)
            {
                ChomikMessageBox.Show(this, "Nie można wyświetlić profilu tego funkcjonariusza.", "Informacja");
                return;
            }

            var window = new PersonnelProfileWindow(profile);
            AppWindowLayout.ApplyProfileDialog(window, this);
            window.ShowDialog();
        }
        catch (Exception ex)
        {
            ChomikMessageBox.Show(this, ex.Message, "Chomik");
        }
    }

    private async void OnGeneralViewPersonnelEditRequested(object? sender, int funkcjonariuszId)
    {
        if (!_controller.CanEditPersonnel)
        {
            return;
        }

        var personnelController = new PersonnelManagementController(_services);
        var entity = await personnelController.GetForEditAsync(funkcjonariuszId);
        if (entity is null)
        {
            ChomikMessageBox.Show(this, "Brak uprawnień do edycji tego funkcjonariusza.", "Informacja");
            return;
        }

        var dictionaries = await personnelController.GetDictionariesAsync();
        var window = new PersonnelEditWindow(personnelController, dictionaries, entity);
        AppWindowLayout.ApplyDialog(window, this);
        if (window.ShowDialog() == true && _generalView is not null)
        {
            await _generalView.LoadPersonnelAsync();
        }
    }

    private void OnGeneralViewClick(object sender, RoutedEventArgs e) => ShowGeneralView();

    private void OnPersonnelEditClick(object sender, RoutedEventArgs e)
    {
        var personnelController = new PersonnelManagementController(_services);
        _personnelView ??= new PersonnelManagementView(personnelController);
        _personnelView.PersonnelChanged -= OnPersonnelChanged;
        _personnelView.PersonnelChanged += OnPersonnelChanged;
        NavigateTo(_personnelView, $"Edycja personelu — zmiana {personnelController.ShiftNumber}", PersonnelEditButton);
    }

    private void OnPasswordAdminClick(object sender, RoutedEventArgs e) => ShowPasswordAdmin();

    private void ShowPasswordAdmin()
    {
        _passwordView ??= new PasswordManagementView(new PasswordManagementController(_services));
        NavigateTo(_passwordView, "Zarządzanie hasłami", PasswordAdminButton);
    }

    private async void OnSettingsClick(object sender, RoutedEventArgs e)
    {
        if (_controller.CanCustomizeGeneralViewColumns && !_controller.CanManageSettings)
        {
            await OpenShiftColumnSettingsAsync();
            return;
        }

        _settingsView ??= new SettingsView(new SettingsController(_services));
        _settingsView.SettingsSaved -= OnSettingsSaved;
        _settingsView.SettingsSaved += OnSettingsSaved;
        NavigateTo(_settingsView, "Ustawienia", SettingsButton);
    }

    private async Task OpenShiftColumnSettingsAsync()
    {
        try
        {
            var settingsController = new SettingsController(_services);
            var preferences = await settingsController.GetGeneralViewColumnPreferencesAsync();
            var window = new GeneralViewColumnsSettingsWindow(settingsController, preferences);
            AppWindowLayout.ApplyProfileDialog(window, this);
            if (window.ShowDialog() == true && window.SavedPreferences is not null)
            {
                if (_generalView is null)
                {
                    ShowGeneralView();
                }

                _generalView!.ApplyColumnPreferences(window.SavedPreferences);
            }
        }
        catch (Exception ex)
        {
            ChomikMessageBox.Show(this, ex.Message, "Chomik");
        }
    }

    private async void OnCreatePersonnelListClick(object sender, RoutedEventArgs e)
    {
        if (!_controller.CanCreatePersonnelList)
        {
            return;
        }

        int? shiftValue;
        string shiftLabel;
        if (_controller.ExportPersonnelListUsesOwnShiftOnly)
        {
            shiftValue = _controller.PersonnelListExportShiftNumber;
            shiftLabel = shiftValue is int shift ? $"Zmiana {shift}" : "Zmiana";
        }
        else
        {
            if (_generalView is null)
            {
                ShowGeneralView();
            }

            var shiftFilter = _generalView!.GetCurrentShiftFilter();
            shiftValue = shiftFilter.Value;
            shiftLabel = shiftFilter.Label;
        }

        var defaultFileName = $"Lista_osob_{SanitizeFileName(shiftLabel)}_{DateTime.Now:yyyy-MM-dd}.xlsx";

        var dialog = new SaveFileDialog
        {
            Title = "Zapisz listę osób",
            Filter = "Plik Excel (*.xlsx)|*.xlsx",
            DefaultExt = "xlsx",
            FileName = defaultFileName,
            AddExtension = true
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            CreatePersonnelListButton.IsEnabled = false;
            var names = await _controller.GetPersonnelNamesForExportAsync(shiftValue);
            DashboardController.ExportPersonnelListToExcel(names, dialog.FileName);
            OpenFileLocationInExplorer(dialog.FileName);

            ChomikMessageBox.Show(
                this,
                $"Zapisano {names.Count} osób (filtr: {shiftLabel}).\n\nLokalizacja pliku:\n{dialog.FileName}",
                "Lista osób");
        }
        catch (Exception ex)
        {
            ChomikMessageBox.Show(this, $"Nie udało się utworzyć listy:\n\n{ex.Message}", "Chomik");
        }
        finally
        {
            CreatePersonnelListButton.IsEnabled = true;
        }
    }

    private static void OpenFileLocationInExplorer(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{filePath}\"",
                UseShellExecute = true
            });
        }
        catch
        {
            // Jeśli Eksplorator się nie otworzy, użytkownik ma ścieżkę w oknie komunikatu.
        }
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

    private async void OnSettingsSaved(object? sender, EventArgs e)
    {
        if (_generalView is null)
        {
            return;
        }

        if (_controller.CanCustomizeGeneralViewColumns)
        {
            var preferences = await _controller.GetGeneralViewColumnPreferencesAsync();
            _generalView.ApplyColumnPreferences(preferences);
        }

        await _generalView.InitializeFiltersAsync();
        await _generalView.LoadPersonnelAsync();
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

    private void HighlightSidebarButton(Button? activeButton)
    {
        foreach (var button in new[] { GeneralViewButton, PersonnelEditButton, PasswordAdminButton, SettingsButton, CreatePersonnelListButton })
        {
            if (button.Visibility != Visibility.Visible)
            {
                continue;
            }

            var isActive = activeButton is not null && button == activeButton;
            button.FontWeight = isActive ? FontWeights.SemiBold : FontWeights.Normal;
            button.Opacity = isActive ? 1.0 : 0.85;
        }
    }

    private void OnAboutClick(object sender, RoutedEventArgs e)
    {
        var aboutWindow = new ChomikAboutWindow { Owner = this };
        aboutWindow.ShowDialog();
    }

    private void OnLogoutClick(object sender, RoutedEventArgs e)
    {
        _controller.Logout();
        DialogResult = false;
        Close();
    }
}
