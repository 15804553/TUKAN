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
    private TypUprawnienia? _selectedTypUprawnienia;
    private List<GeneralViewColumnOptionViewModel> _columnOptions = [];

    public event EventHandler? SettingsSaved;

    public SettingsView(SettingsController controller)
    {
        InitializeComponent();
        _controller = controller;
        GeneralViewColumnsPanel.Visibility = _controller.CanCustomizeGeneralViewColumns
            ? Visibility.Visible
            : Visibility.Collapsed;
        UprawnieniaPanel.Visibility = _controller.CanManagePermissionTypes || _controller.CanManageSettings
            ? Visibility.Visible
            : Visibility.Collapsed;
        Loaded += OnLoaded;
    }

    private Window? OwnerWindow => Window.GetWindow(this);

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_controller.CanManagePermissionTypes || _controller.CanManageSettings)
            {
                await LoadTypyUprawnienAsync();
            }

            if (_controller.CanCustomizeGeneralViewColumns)
            {
                await LoadColumnOptionsAsync();
            }
        }
        catch (Exception ex)
        {
            ChomikMessageBox.Show(OwnerWindow, ex.Message, "Chomik");
        }
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
