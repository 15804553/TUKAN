using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Chomik.App.Controllers;
using Chomik.App.Views.Chrome;

namespace Chomik.App.Views;

public partial class LoginWindow : Window
{
    private readonly LoginController _controller;

    public LoginWindow(LoginController controller)
    {
        InitializeComponent();
        ChromeWindowConfigurator.Apply(this, canResize: false);
        _controller = controller;
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
            var logins = await _controller.GetLoginsAsync();
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

    private void OnLoginClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var login = LoginComboBox.SelectedItem?.ToString() ?? string.Empty;
            var password = PasswordBox.Password;

            if (_controller.TryAuthenticate(login, password, out var error))
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
