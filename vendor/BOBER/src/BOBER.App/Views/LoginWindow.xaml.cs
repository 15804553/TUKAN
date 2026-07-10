using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BOBER.App.Controllers;
using BOBER.App.Views.Chrome;
using BOBER.Services.Logging;

namespace BOBER.App.Views;

public partial class LoginWindow : Window
{
    private readonly LoginController _controller;
    private bool _initializingSelection;

    public LoginWindow(LoginController controller)
    {
        InitializeComponent();
        ChromeWindowConfigurator.Apply(this, canResize: false);
        _controller = controller;
        Closing += (_, _) => { if (DialogResult is null) DialogResult = false; };
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var logins = await _controller.GetLoginsAsync();
            PopulateLoginCombo(logins);
        }
        catch (Exception ex)
        {
            BoberLog.Error(ex, "Błąd ładowania listy loginów");
            ShowError($"Błąd ładowania użytkowników: {ex.Message}");
        }
    }

    private void PopulateLoginCombo(IReadOnlyList<string> logins)
    {
        _initializingSelection = true;
        try
        {
            LoginComboBox.Items.Clear();
            foreach (var login in logins)
                LoginComboBox.Items.Add(login);

            if (LoginComboBox.Items.Count == 0)
            {
                LoginComboBox.SelectedIndex = -1;
                return;
            }

            LoginComboBox.SelectedIndex = FindDefaultIndex(logins);
        }
        finally
        {
            _initializingSelection = false;
        }

        FocusPassword();
    }

    private static int FindDefaultIndex(IReadOnlyList<string> logins)
    {
        for (var i = 0; i < logins.Count; i++)
        {
            if (logins[i].Contains("Zmiana 1", StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return 0;
    }

    private void OnLoginSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded || _initializingSelection) return;
        ErrorTextBlock.Visibility = Visibility.Collapsed;
        FocusPassword();
    }

    private void OnLoginComboPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Enter or Key.Tab) FocusPassword();
    }

    private void OnPasswordKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) OnLoginClick(sender, e);
    }

    private void FocusPassword()
    {
        PasswordBox.Focus();
        Keyboard.Focus(PasswordBox);
    }

    private void OnLoginClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var login = LoginComboBox.SelectedItem as string ?? string.Empty;
            if (string.IsNullOrWhiteSpace(login) && LoginComboBox.Items.Count > 0)
                LoginComboBox.SelectedIndex = 0;

            login = LoginComboBox.SelectedItem as string ?? string.Empty;
            var password = PasswordBox.Password;

            if (_controller.TryAuthenticate(login, password, out var error))
            {
                DialogResult = true;
                return;
            }

            ShowError(error);
            FocusPassword();
        }
        catch (Exception ex)
        {
            BoberLog.Error(ex, "Błąd podczas logowania");
            ShowError(ex.Message);
        }
    }

    private void ShowError(string message)
    {
        ErrorTextBlock.Text = message;
        ErrorTextBlock.Visibility = Visibility.Visible;
    }
}
