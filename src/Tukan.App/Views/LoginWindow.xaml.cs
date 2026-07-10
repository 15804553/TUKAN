using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Chomik.App.Controllers;
using Tukan.App.Services;

namespace Tukan.App.Views;

public partial class LoginWindow : Window
{
    private readonly TukanAppServices _tukanServices;
    private readonly LoginController _loginController;

    public LoginWindow(TukanAppServices tukanServices)
    {
        InitializeComponent();
        _tukanServices = tukanServices;
        _loginController = new LoginController(tukanServices.Chomik);
        Closing += (_, _) =>
        {
            if (DialogResult is null)
            {
                DialogResult = false;
            }
        };
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var logins = await _loginController.GetLoginsAsync();
            LoginComboBox.ItemsSource = logins;

            const string defaultLogin = "PA";
            var paIndex = logins
                .ToList()
                .FindIndex(login => string.Equals(login, defaultLogin, StringComparison.OrdinalIgnoreCase));

            LoginComboBox.SelectedIndex = paIndex >= 0 ? paIndex : logins.Count > 0 ? 0 : -1;
            FocusPasswordField();
        }
        catch (Exception ex)
        {
            ErrorTextBlock.Text = $"Błąd ładowania użytkowników: {ex.Message}";
            ErrorTextBlock.Visibility = Visibility.Visible;
        }
    }

    private void OnLoginSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded || LoginComboBox.SelectedItem is null)
        {
            return;
        }

        ErrorTextBlock.Visibility = Visibility.Collapsed;
        FocusPasswordField();
    }

    private void OnLoginComboPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Enter or Key.Tab)
        {
            FocusPasswordField();
        }
    }

    private void OnPasswordKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            OnLoginClick(sender, e);
        }
    }

    private void FocusPasswordField()
    {
        PasswordBox.Focus();
        Keyboard.Focus(PasswordBox);
    }

    private async void OnLoginClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var login = LoginComboBox.SelectedItem?.ToString() ?? string.Empty;
            var password = PasswordBox.Password;

            var (success, error) = await _tukanServices.TryLoginAsync(login, password);
            if (success)
            {
                DialogResult = true;
                return;
            }

            ErrorTextBlock.Text = error;
            ErrorTextBlock.Visibility = Visibility.Visible;
            FocusPasswordField();
        }
        catch (Exception ex)
        {
            ErrorTextBlock.Text = ex.Message;
            ErrorTextBlock.Visibility = Visibility.Visible;
            FocusPasswordField();
        }
    }
}
