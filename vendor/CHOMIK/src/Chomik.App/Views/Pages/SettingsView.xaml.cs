using System.Windows;
using System.Windows.Controls;
using Chomik.App.Controllers;
using Chomik.App.ViewModels;
using Chomik.App.Views.Chrome;
using Chomik.Core.GeneralView;
using Chomik.Core.Models;

namespace Chomik.App.Views.Pages;

public partial class SettingsView : UserControl
{
    private readonly SettingsController _controller;
    private readonly ChomikSettingsSection _section;
    private TypUprawnienia? _selectedTypUprawnienia;
    private List<GeneralViewColumnOptionViewModel> _columnOptions = [];
    private int? _selectedStopienId;
    private int? _selectedStanowiskoId;
    private int? _selectedOdznaczenieId;

    public event EventHandler? SettingsSaved;

    public SettingsView(
        SettingsController controller,
        ChomikSettingsSection section = ChomikSettingsSection.All)
    {
        InitializeComponent();
        _controller = controller;
        _section = section;
        GeneralViewColumnsPanel.Visibility = ShowsKolumny && _controller.CanCustomizeGeneralViewColumns
            ? Visibility.Visible
            : Visibility.Collapsed;
        var canManageSlowniki = ShowsAnySlownik && _controller.CanManageSettings;
        PersonelSlownikiPanel.Visibility = canManageSlowniki && (ShowsStopnie || ShowsStanowiska || ShowsOdznaczenia)
            ? Visibility.Visible
            : Visibility.Collapsed;
        StopniePanel.Visibility = ShowsStopnie && canManageSlowniki ? Visibility.Visible : Visibility.Collapsed;
        StanowiskaPanel.Visibility = ShowsStanowiska && canManageSlowniki ? Visibility.Visible : Visibility.Collapsed;
        OdznaczeniaPanel.Visibility = ShowsOdznaczenia && canManageSlowniki ? Visibility.Visible : Visibility.Collapsed;
        UprawnieniaPanel.Visibility = ShowsUprawnienia && (_controller.CanManagePermissionTypes || canManageSlowniki)
            ? Visibility.Visible
            : Visibility.Collapsed;

        if (_section == ChomikSettingsSection.Kolumny)
        {
            GeneralViewColumnsTitle.Visibility = Visibility.Collapsed;
            GeneralViewColumnsPanel.Background = System.Windows.Media.Brushes.Transparent;
            GeneralViewColumnsPanel.Padding = new Thickness(0);
            GeneralViewColumnsPanel.Margin = new Thickness(0);
        }

        if (canManageSlowniki)
        {
            if (ShowsStopnie)
                WireStopniePanel();
            if (ShowsStanowiska)
                WireStanowiskaPanel();
            if (ShowsOdznaczenia)
                WireOdznaczeniaPanel();
        }

        Loaded += OnLoaded;
    }

    private bool ShowsKolumny =>
        _section is ChomikSettingsSection.All or ChomikSettingsSection.Kolumny;

    private bool ShowsStopnie =>
        _section is ChomikSettingsSection.All or ChomikSettingsSection.Slowniki or ChomikSettingsSection.Stopnie;

    private bool ShowsStanowiska =>
        _section is ChomikSettingsSection.All or ChomikSettingsSection.Slowniki or ChomikSettingsSection.Stanowiska;

    private bool ShowsOdznaczenia =>
        _section is ChomikSettingsSection.All or ChomikSettingsSection.Slowniki or ChomikSettingsSection.Odznaczenia;

    private bool ShowsUprawnienia =>
        _section is ChomikSettingsSection.All or ChomikSettingsSection.Slowniki or ChomikSettingsSection.Uprawnienia;

    private bool ShowsAnySlownik =>
        ShowsStopnie || ShowsStanowiska || ShowsOdznaczenia || ShowsUprawnienia;

    private Window? OwnerWindow => Window.GetWindow(this);

    private void WireStopniePanel()
    {
        StopniePanel.AddRequested += async nazwa =>
        {
            await _controller.AddStopienAsync(nazwa);
            await ReloadStopnieAsync();
        };
        StopniePanel.UpdateRequested += async (id, nazwa) =>
        {
            await _controller.UpdateStopienAsync(id, nazwa);
            _selectedStopienId = id;
            await ReloadStopnieAsync();
        };
        StopniePanel.DeleteRequested += async id =>
        {
            await _controller.DeleteStopienAsync(id);
            _selectedStopienId = null;
            await ReloadStopnieAsync();
        };
        StopniePanel.Changed += (_, _) => SettingsSaved?.Invoke(this, EventArgs.Empty);
    }

    private void WireStanowiskaPanel()
    {
        StanowiskaPanel.AddRequested += async nazwa =>
        {
            await _controller.AddStanowiskoAsync(nazwa);
            await ReloadStanowiskaAsync();
        };
        StanowiskaPanel.UpdateRequested += async (id, nazwa) =>
        {
            await _controller.UpdateStanowiskoAsync(id, nazwa);
            _selectedStanowiskoId = id;
            await ReloadStanowiskaAsync();
        };
        StanowiskaPanel.DeleteRequested += async id =>
        {
            await _controller.DeleteStanowiskoAsync(id);
            _selectedStanowiskoId = null;
            await ReloadStanowiskaAsync();
        };
        StanowiskaPanel.Changed += (_, _) => SettingsSaved?.Invoke(this, EventArgs.Empty);
    }

    private void WireOdznaczeniaPanel()
    {
        OdznaczeniaPanel.AddRequested += async nazwa =>
        {
            await _controller.AddTypOdznaczeniaAsync(nazwa);
            await ReloadOdznaczeniaAsync();
        };
        OdznaczeniaPanel.UpdateRequested += async (id, nazwa) =>
        {
            await _controller.UpdateTypOdznaczeniaAsync(id, nazwa);
            _selectedOdznaczenieId = id;
            await ReloadOdznaczeniaAsync();
        };
        OdznaczeniaPanel.DeleteRequested += async id =>
        {
            await _controller.DeleteTypOdznaczeniaAsync(id);
            _selectedOdznaczenieId = null;
            await ReloadOdznaczeniaAsync();
        };
        OdznaczeniaPanel.Changed += (_, _) => SettingsSaved?.Invoke(this, EventArgs.Empty);
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            if (ShowsStopnie && _controller.CanManageSettings)
                await ReloadStopnieAsync();

            if (ShowsStanowiska && _controller.CanManageSettings)
                await ReloadStanowiskaAsync();

            if (ShowsOdznaczenia && _controller.CanManageSettings)
                await ReloadOdznaczeniaAsync();

            if (ShowsUprawnienia && (_controller.CanManagePermissionTypes || _controller.CanManageSettings))
                await LoadTypyUprawnienAsync();

            if (ShowsKolumny && _controller.CanCustomizeGeneralViewColumns)
            {
                await LoadColumnOptionsAsync();
            }
        }
        catch (Exception ex)
        {
            ChomikMessageBox.Show(OwnerWindow, ex.Message, "Chomik");
        }
    }

    private async Task ReloadStopnieAsync()
    {
        var items = await _controller.LoadStopnieAsync();
        StopniePanel.BindItems(items, i => i.Id, i => i.Nazwa, _selectedStopienId);
    }

    private async Task ReloadStanowiskaAsync()
    {
        var items = await _controller.LoadStanowiskaAsync();
        StanowiskaPanel.BindItems(items, i => i.Id, i => i.Nazwa, _selectedStanowiskoId);
    }

    private async Task ReloadOdznaczeniaAsync()
    {
        var items = await _controller.LoadTypyOdznaczenAsync();
        OdznaczeniaPanel.BindItems(items, i => i.Id, i => i.Nazwa, _selectedOdznaczenieId);
    }

    private async Task LoadColumnOptionsAsync()
    {
        var preferences = await _controller.GetGeneralViewColumnPreferencesAsync();
        _columnOptions = GeneralViewColumnOptionsFactory.Create(
            _controller.GetSelectableGeneralViewColumns(),
            preferences);
        ColumnsListBox.ItemsSource = _columnOptions;
    }

    private async Task LoadTypyUprawnienAsync()
    {
        var selectedId = _selectedTypUprawnienia?.Id;
        var items = await _controller.LoadTypyUprawnienAsync();
        TypyUprawnienListBox.ItemsSource = items;

        if (selectedId is int id)
        {
            TypyUprawnienListBox.SelectedItem = items.FirstOrDefault(t => t.Id == id);
        }
    }

    private void OnTypUprawnieniaSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_controller.CanManagePermissionTypes)
        {
            EdycjaUprawnieniaPanel.Visibility = Visibility.Collapsed;
            _selectedTypUprawnienia = null;
            return;
        }

        _selectedTypUprawnienia = TypyUprawnienListBox.SelectedItem as TypUprawnienia;
        if (_selectedTypUprawnienia is null)
        {
            EdycjaUprawnieniaPanel.Visibility = Visibility.Collapsed;
            return;
        }

        EdytujUprawnienieNazwaTextBox.Text = _selectedTypUprawnienia.Nazwa;
        EdytujUprawnieniePodtypTextBox.Text = _selectedTypUprawnienia.Podtyp ?? string.Empty;
        EdytujUprawnienieWymagaDatyCheckBox.IsChecked = _selectedTypUprawnienia.WymagaDaty;
        EdycjaUprawnieniaPanel.Visibility = Visibility.Visible;
    }

    private async void OnSaveColumnsClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var preferences = GeneralViewColumnOptionsFactory.ToPreferences(_columnOptions);
            await _controller.SaveGeneralViewColumnPreferencesAsync(preferences);
            ChomikMessageBox.Show(OwnerWindow, "Widoczność kolumn została zapisana.", "Informacja");
            SettingsSaved?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            ChomikMessageBox.Show(OwnerWindow, ex.Message, "Chomik");
        }
    }

    private async void OnAddUprawnienieClick(object sender, RoutedEventArgs e)
    {
        try
        {
            await _controller.AddTypUprawnieniaAsync(
                NoweUprawnienieNazwaTextBox.Text,
                NoweUprawnieniePodtypTextBox.Text,
                NoweUprawnienieWymagaDatyCheckBox.IsChecked == true);
            NoweUprawnienieNazwaTextBox.Clear();
            NoweUprawnieniePodtypTextBox.Clear();
            NoweUprawnienieWymagaDatyCheckBox.IsChecked = true;
            await LoadTypyUprawnienAsync();
            ChomikMessageBox.Show(OwnerWindow, "Uprawnienie / kurs zostało dodane.", "Informacja");
            SettingsSaved?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            ChomikMessageBox.Show(OwnerWindow, ex.Message, "Chomik");
        }
    }

    private async void OnUpdateUprawnienieClick(object sender, RoutedEventArgs e)
    {
        if (_selectedTypUprawnienia is null)
        {
            ChomikMessageBox.Show(OwnerWindow, "Wybierz pozycję z listy.", "Informacja");
            return;
        }

        try
        {
            await _controller.UpdateTypUprawnieniaAsync(
                _selectedTypUprawnienia.Id,
                EdytujUprawnienieNazwaTextBox.Text,
                EdytujUprawnieniePodtypTextBox.Text,
                EdytujUprawnienieWymagaDatyCheckBox.IsChecked == true);
            await LoadTypyUprawnienAsync();
            ChomikMessageBox.Show(OwnerWindow, "Zmiany zostały zapisane.", "Informacja");
            SettingsSaved?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            ChomikMessageBox.Show(OwnerWindow, ex.Message, "Chomik");
        }
    }
}
