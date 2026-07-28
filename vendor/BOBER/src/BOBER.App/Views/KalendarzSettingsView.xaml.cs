using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BOBER.App.Controllers;
using BOBER.App.ViewModels;
using BOBER.App.Views.Chrome;
using BOBER.Core.Constants;
using BOBER.Core.Models;
using MediaColor = System.Windows.Media.Color;
using WpfColorConverter = System.Windows.Media.ColorConverter;

namespace BOBER.App.Views;

public partial class KalendarzSettingsView : UserControl
{
    private readonly KalendarzController _controller;
    private readonly bool _showColorSettings;
    private readonly int? _settingsShiftNumber;
    private readonly ObservableCollection<KolorRoliViewModel> _kolory = new();
    private int _loadGeneration;

    public event EventHandler? SettingsSaved;

    public KalendarzSettingsView(
        KalendarzController controller,
        bool showColorSettings = true,
        int? settingsShiftNumber = null)
    {
        InitializeComponent();
        _controller = controller;
        _showColorSettings = showColorSettings;
        _settingsShiftNumber = settingsShiftNumber;
        KoloryItemsControl.ItemsSource = _kolory;
        AutoDeleteComboBox.ItemsSource = BuildAutoDeleteOptions();
        ConfigureLayout();
        Loaded += OnLoaded;
    }

    private void ConfigureLayout()
    {
        if (_showColorSettings)
        {
            AutoDeleteDescriptionTextBlock.Text =
                "Ustaw po jakim czasie stare notatki DCA mają być usuwane automatycznie z kalendarza.";
            return;
        }

        ColorsHeaderTextBlock.Visibility = Visibility.Collapsed;
        ColorsDescriptionTextBlock.Visibility = Visibility.Collapsed;
        ColorsBorder.Visibility = Visibility.Collapsed;
        ResetDefaultsButton.Visibility = Visibility.Collapsed;
        AutoDeleteDescriptionTextBlock.Text =
            "Ustaw po jakim czasie stare notatki widoczne dla tej zmiany mają być usuwane automatycznie.";
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        var generation = ++_loadGeneration;
        try
        {
            if (generation != _loadGeneration)
                return;

            if (_showColorSettings)
            {
                var kolory = await _controller.GetKoloryZmianAsync();
                if (generation != _loadGeneration)
                    return;

                _kolory.Clear();
                for (var zmiana = 1; zmiana <= 3; zmiana++)
                {
                    var klucz = RoleKeys.KalendarzKluczForZmiana(zmiana);
                    _kolory.Add(new KolorRoliViewModel
                    {
                        KluczRoli = klucz,
                        Etykieta = RoleKeys.DomyslneEtykiety.TryGetValue(klucz, out var etykieta)
                            ? etykieta
                            : $"Zmiana {zmiana}",
                        KolorHex = kolory.TryGetValue(zmiana, out var hex)
                            ? hex
                            : RoleKeys.GetDefaultKolorHex(klucz)
                    });
                }
            }

            var autoDeleteMode = await _controller.GetAutoDeleteModeAsync(_settingsShiftNumber);
            if (generation == _loadGeneration)
                AutoDeleteComboBox.SelectedValue = autoDeleteMode;
        }
        catch (Exception ex)
        {
            if (generation == _loadGeneration)
                BoberMessageBox.Show(OwnerWindow, ex.Message, "Kalendarz — błąd ustawień");
        }
    }

    private void OnColorPreviewClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement element || element.DataContext is not KolorRoliViewModel vm)
            return;

        var dialog = new System.Windows.Forms.ColorDialog
        {
            FullOpen = true,
            SolidColorOnly = true
        };

        try
        {
            var color = (MediaColor)WpfColorConverter.ConvertFromString(vm.KolorHex)!;
            dialog.Color = System.Drawing.Color.FromArgb(color.R, color.G, color.B);
        }
        catch
        {
            dialog.Color = System.Drawing.Color.FromArgb(0xFF, 0xFF, 0x00);
        }

        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            return;

        var wybrany = dialog.Color;
        vm.KolorHex = $"#{wybrany.R:X2}{wybrany.G:X2}{wybrany.B:X2}";
    }

    private void OnResetDefaultsClick(object sender, RoutedEventArgs e)
    {
        foreach (var vm in _kolory)
            vm.KolorHex = RoleKeys.GetDefaultKolorHex(vm.KluczRoli);
    }

    private async void OnSaveClick(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_showColorSettings)
            {
                var map = new Dictionary<int, string>();
                foreach (var vm in _kolory)
                {
                    var zmiana = vm.KluczRoli switch
                    {
                        RoleKeys.KalendarzZmiana1 => 1,
                        RoleKeys.KalendarzZmiana2 => 2,
                        RoleKeys.KalendarzZmiana3 => 3,
                        _ => 0
                    };
                    if (zmiana > 0)
                        map[zmiana] = vm.KolorHex;
                }

                await _controller.SaveKoloryZmianAsync(map);
            }

            var mode = AutoDeleteComboBox.SelectedValue is KalendarzAutoDeleteMode selected
                ? selected
                : KalendarzAutoDeleteMode.Nigdy;
            await _controller.SaveAutoDeleteModeAsync(_settingsShiftNumber, mode);

            var message = _showColorSettings
                ? "Ustawienia kalendarza zostały zapisane."
                : "Automatyczne usuwanie notatek zostało zapisane.";
            BoberMessageBox.Show(OwnerWindow, message, "Kalendarz");
            SettingsSaved?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            BoberMessageBox.Show(OwnerWindow, ex.Message, "Kalendarz — błąd zapisu");
        }
    }

    private static IReadOnlyList<AutoDeleteOption> BuildAutoDeleteOptions() =>
    [
        new(KalendarzAutoDeleteMode.Nigdy, "Nigdy"),
        new(KalendarzAutoDeleteMode.RazNaMiesiac, "Raz na miesiąc"),
        new(KalendarzAutoDeleteMode.RazNaPolRoku, "Raz na pół roku")
    ];

    private sealed record AutoDeleteOption(KalendarzAutoDeleteMode Value, string Label);

    private Window? OwnerWindow => Window.GetWindow(this);
}
