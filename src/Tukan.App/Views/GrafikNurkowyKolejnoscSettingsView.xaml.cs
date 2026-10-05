using System.Windows;
using System.Windows.Controls;
using BOBER.Core.Models;
using BOBER.Services.Settings;
using Tukan.App.Views.Chrome;

namespace Tukan.App.Views;

public partial class GrafikNurkowyKolejnoscSettingsView : UserControl
{
    private readonly ISettingsService _settings;
    private bool _loading;

    public event EventHandler? SettingsSaved;

    public GrafikNurkowyKolejnoscSettingsView(ISettingsService settings)
    {
        InitializeComponent();
        _settings = settings;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        _loading = true;
        try
        {
            var tryb = await _settings.GetGrafikNurkowyKolejnoscAsync();
            var zakres = await _settings.GetGrafikNurkowyKolejnoscZakresAsync();
            KppNaGorzeRadio.IsChecked = tryb == GrafikNurkowyKolejnoscTryb.KppNaGorze;
            KppNurekMlodszyRadio.IsChecked = tryb == GrafikNurkowyKolejnoscTryb.KppNurekMlodszy;
            WObrebieZmianRadio.IsChecked = zakres == GrafikNurkowyKolejnoscZakres.WObrebieZmian;
            WObrebieWszystkichRadio.IsChecked = zakres == GrafikNurkowyKolejnoscZakres.WObrebieWszystkichZmian;
        }
        catch (Exception ex)
        {
            TukanMessageBox.Show(Window.GetWindow(this), ex.Message, "Grafik nurkowy — błąd ustawień");
        }
        finally
        {
            _loading = false;
        }
    }

    private async void OnSaveClick(object sender, RoutedEventArgs e)
    {
        if (_loading)
            return;

        var tryb = KppNurekMlodszyRadio.IsChecked == true
            ? GrafikNurkowyKolejnoscTryb.KppNurekMlodszy
            : GrafikNurkowyKolejnoscTryb.KppNaGorze;
        var zakres = WObrebieWszystkichRadio.IsChecked == true
            ? GrafikNurkowyKolejnoscZakres.WObrebieWszystkichZmian
            : GrafikNurkowyKolejnoscZakres.WObrebieZmian;

        try
        {
            await _settings.SetGrafikNurkowyKolejnoscAsync(tryb);
            await _settings.SetGrafikNurkowyKolejnoscZakresAsync(zakres);
            TukanMessageBox.Show(
                Window.GetWindow(this),
                "Zapisano kolejność grafiku nurkowego. Obowiązuje przy kolejnej aktualizacji dokumentu.",
                "Ustawienia");
            SettingsSaved?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            TukanMessageBox.Show(Window.GetWindow(this), ex.Message, "Grafik nurkowy — błąd zapisu");
        }
    }
}
