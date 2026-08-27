using System.IO;
using System.Windows;
using System.Windows.Controls;
using BOBER.Services.Settings;
using Microsoft.Win32;
using Tukan.App.Services;
using Tukan.App.Views.Chrome;

namespace Tukan.App.Views;

public partial class ExportPathsSettingsControl : UserControl
{
    private readonly ISettingsService _settings;

    public event EventHandler? SettingsSaved;
    public event EventHandler? InstallationNameChanged;

    public ExportPathsSettingsControl(ISettingsService settings)
    {
        InitializeComponent();
        _settings = settings;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        InstallationNameTextBox.Text = InstallationNameStore.Read();
        RozkazyPathTextBox.Text = await _settings.GetExportPathRozkazyAsync();
        GrafikSluzbPathTextBox.Text = await _settings.GetExportPathGrafikSluzbAsync();
        GrafikNurkowyPathTextBox.Text = await _settings.GetExportPathGrafikNurkowyAsync();
    }

    private void OnBrowseRozkazyClick(object sender, RoutedEventArgs e) =>
        BrowseInto(RozkazyPathTextBox);

    private void OnBrowseGrafikSluzbClick(object sender, RoutedEventArgs e) =>
        BrowseInto(GrafikSluzbPathTextBox);

    private void OnBrowseGrafikNurkowyClick(object sender, RoutedEventArgs e) =>
        BrowseInto(GrafikNurkowyPathTextBox);

    private static void BrowseInto(TextBox target)
    {
        var dlg = new OpenFolderDialog
        {
            Title = "Wybierz katalog eksportu"
        };

        if (!string.IsNullOrWhiteSpace(target.Text) && Directory.Exists(target.Text))
            dlg.InitialDirectory = target.Text;

        if (dlg.ShowDialog() == true)
            target.Text = dlg.FolderName;
    }

    private void OnSaveInstallationNameClick(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!InstallationNameStore.TryNormalize(
                    InstallationNameTextBox.Text, out var normalized, out var error))
            {
                TukanMessageBox.Show(Window.GetWindow(this), error!, "Błąd");
                return;
            }

            InstallationNameStore.Write(normalized);
            InstallationNameTextBox.Text = normalized;
            InstallationNameChanged?.Invoke(this, EventArgs.Empty);
            SettingsSaved?.Invoke(this, EventArgs.Empty);

            var message = string.IsNullOrEmpty(normalized)
                ? "Nazwa instalacji została wyczyszczona."
                : $"Nazwa instalacji zapisana: {normalized}";
            TukanMessageBox.Show(Window.GetWindow(this), message, "Ustawienia");
        }
        catch (Exception ex)
        {
            TukanMessageBox.Show(Window.GetWindow(this), ex.Message, "Błąd");
        }
    }

    private async void OnSaveClick(object sender, RoutedEventArgs e)
    {
        try
        {
            await _settings.SetExportPathRozkazyAsync(RozkazyPathTextBox.Text.Trim());
            await _settings.SetExportPathGrafikSluzbAsync(GrafikSluzbPathTextBox.Text.Trim());
            await _settings.SetExportPathGrafikNurkowyAsync(GrafikNurkowyPathTextBox.Text.Trim());
            SettingsSaved?.Invoke(this, EventArgs.Empty);
            TukanMessageBox.Show(Window.GetWindow(this), "Ścieżki eksportu zostały zapisane.", "Ustawienia");
        }
        catch (Exception ex)
        {
            TukanMessageBox.Show(Window.GetWindow(this), ex.Message, "Błąd");
        }
    }
}
