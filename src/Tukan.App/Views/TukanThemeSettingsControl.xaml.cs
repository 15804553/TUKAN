using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Tukan.App.Infrastructure;
using Tukan.App.Models;
using Tukan.App.Services;
using Tukan.App.Views.Chrome;

namespace Tukan.App.Views;

public partial class TukanThemeSettingsControl : UserControl
{
    private readonly TukanJsonSettingsService _settingsService;
    private bool _themeUiReady;

    public TukanThemeSettingsControl()
    {
        InitializeComponent();
        _settingsService = App.SettingsService;
        InitializeThemePicker();
    }

    private void InitializeThemePicker()
    {
        ThemeComboBox.ItemsSource = TukanAppTheme.PaletteOptions;
        var settings = _settingsService.Load();
        ThemeComboBox.SelectedValue = settings.UiColorPalette;
        UpdateThemePreview(TukanAppTheme.Parse(settings.UiColorPalette));
        _themeUiReady = true;
    }

    private void OnThemeSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_themeUiReady || ThemeComboBox.SelectedItem is not TukanUiPaletteOption option)
        {
            return;
        }

        ThemeDescriptionText.Text = option.Description;
        UpdateThemePreview(option.Kind);
    }

    private void OnApplyThemeClick(object sender, RoutedEventArgs e)
    {
        if (ThemeComboBox.SelectedItem is not TukanUiPaletteOption option)
        {
            return;
        }

        var settings = _settingsService.Load();
        settings.UiColorPalette = option.Key;
        _settingsService.Save(settings);

        TukanAppTheme.Apply(option.Kind);
        UpdateThemePreview(option.Kind);

        TukanMessageBox.Show(
            Window.GetWindow(this),
            $"Zastosowano motyw „{option.DisplayName}”.",
            "TUKAN — wygląd");
    }

    private void UpdateThemePreview(TukanAppThemeKind kind)
    {
        var option = TukanAppTheme.GetOption(kind);
        ThemeDescriptionText.Text = option.Description;

        ThemePreviewPanel.Children.Clear();
        foreach (var color in TukanAppTheme.GetPreviewColors(kind))
        {
            ThemePreviewPanel.Children.Add(new Border
            {
                Background = new SolidColorBrush(color),
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(2),
                CornerRadius = new CornerRadius(4)
            });
        }
    }
}
