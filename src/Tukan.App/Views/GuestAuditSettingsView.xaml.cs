using System.Windows;
using System.Windows.Controls;
using Tukan.App.Services.GuestAudit;

namespace Tukan.App.Views;

public partial class GuestAuditSettingsView : UserControl
{
    private readonly GuestAuditFacade _guestAudit;
    private readonly int _shiftNumber;
    private readonly bool _canConfigure;
    private bool _loading;

    public GuestAuditSettingsView(GuestAuditFacade guestAudit, int shiftNumber, bool canConfigure)
    {
        InitializeComponent();
        _guestAudit = guestAudit;
        _shiftNumber = shiftNumber;
        _canConfigure = canConfigure;

        ConfigurePanel.Visibility = canConfigure ? Visibility.Visible : Visibility.Collapsed;
        LogHintText.Text = canConfigure
            ? $"Log zmian Gość {_shiftNumber} (tylko odczyt):"
            : "Podgląd logu audytu jest dostępny tylko dla użytkownika Zmiana.";

        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _loading = true;
        try
        {
            if (_canConfigure)
            {
                var scope = await _guestAudit.Settings.GetScopeAsync(_shiftNumber);
                AuditGrafikCheckBox.IsChecked = scope.Grafik;
                AuditPersonelCheckBox.IsChecked = scope.Personel;
                AuditRozkazyCheckBox.IsChecked = scope.Rozkazy;
                AuditUrlopyCheckBox.IsChecked = scope.Urlopy;
                AuditUstawieniaCheckBox.IsChecked = scope.Ustawienia;

                UrlopLockCheckBox.IsChecked =
                    await _guestAudit.Settings.GetUrlopPlanLockedAsync(_shiftNumber);
            }

            LogTextBox.Text = _canConfigure
                ? await _guestAudit.Log.ReadAsync(_shiftNumber)
                : string.Empty;
            if (!_canConfigure)
                LogTextBox.Visibility = Visibility.Collapsed;
        }
        finally
        {
            _loading = false;
        }
    }

    private async void OnConfigChanged(object sender, RoutedEventArgs e)
    {
        if (_loading || !_canConfigure)
            return;

        try
        {
            var scope = new GuestAuditScope
            {
                Grafik = AuditGrafikCheckBox.IsChecked == true,
                Personel = AuditPersonelCheckBox.IsChecked == true,
                Rozkazy = AuditRozkazyCheckBox.IsChecked == true,
                Urlopy = AuditUrlopyCheckBox.IsChecked == true,
                Ustawienia = AuditUstawieniaCheckBox.IsChecked == true
            };
            await _guestAudit.Settings.SaveScopeAsync(_shiftNumber, scope);
            await _guestAudit.Settings.SetUrlopPlanLockedAsync(
                _shiftNumber,
                UrlopLockCheckBox.IsChecked == true);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Nie udało się zapisać ustawień audytu:\n{ex.Message}",
                "TUKAN",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private async void OnRefreshLogClick(object sender, RoutedEventArgs e)
    {
        if (!_canConfigure)
            return;

        try
        {
            LogTextBox.Text = await _guestAudit.Log.ReadAsync(_shiftNumber);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Nie udało się odczytać logu:\n{ex.Message}",
                "TUKAN",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }
}
