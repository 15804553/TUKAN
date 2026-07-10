using System.Windows;
using Chomik.App.Controllers;
using Chomik.App.ViewModels;
using Chomik.App.Views.Chrome;
using Chomik.Core.GeneralView;

namespace Chomik.App.Views;

public partial class GeneralViewColumnsSettingsWindow : Window
{
    private readonly SettingsController _controller;
    private readonly List<GeneralViewColumnOptionViewModel> _options;

    public GeneralViewColumnPreferences? SavedPreferences { get; private set; }

    public GeneralViewColumnsSettingsWindow(
        SettingsController controller,
        GeneralViewColumnPreferences currentPreferences)
    {
        InitializeComponent();
        _controller = controller;
        ChromeWindowConfigurator.Apply(this, canResize: false);

        var selectableColumns = controller.GetSelectableGeneralViewColumns();
        _options = GeneralViewColumnOptionsFactory.Create(selectableColumns, currentPreferences);

        ColumnsListBox.ItemsSource = _options;
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private async void OnSaveClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var preferences = GeneralViewColumnOptionsFactory.ToPreferences(_options);

            await _controller.SaveGeneralViewColumnPreferencesAsync(preferences);
            SavedPreferences = preferences;
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            ChomikMessageBox.Show(this, ex.Message, "Chomik");
        }
    }
}
