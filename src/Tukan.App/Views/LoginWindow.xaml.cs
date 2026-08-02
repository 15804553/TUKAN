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
            var items = BuildLoginList(logins);
            LoginComboBox.ItemsSource = items;

            var paItem = items.FirstOrDefault(i =>
                !i.IsSeparator &&
                string.Equals(i.Login, "PA", StringComparison.OrdinalIgnoreCase));
            LoginComboBox.SelectedItem = paItem ?? items.FirstOrDefault(i => !i.IsSeparator);
            FocusPasswordField();
        }
        catch (Exception ex)
        {
            ErrorTextBlock.Text = $"Błąd ładowania użytkowników: {ex.Message}";
            ErrorTextBlock.Visibility = Visibility.Visible;
        }
    }

    /// <summary>
    /// Grupy: PA | DCA | Zmiana N + Gość N | Administrator — z liniami poziomymi między grupami.
    /// </summary>
    internal static IReadOnlyList<LoginListItem> BuildLoginList(IReadOnlyList<string> logins)
    {
        static int GroupKey(string login)
        {
            if (login.Equals("PA", StringComparison.OrdinalIgnoreCase)) return 0;
            if (login.Contains("DCA", StringComparison.OrdinalIgnoreCase)) return 1;
            if (login.StartsWith("Zmiana 1", StringComparison.OrdinalIgnoreCase)
                || login.StartsWith("Gość 1", StringComparison.OrdinalIgnoreCase)
                || login.StartsWith("Gosc 1", StringComparison.OrdinalIgnoreCase)) return 2;
            if (login.StartsWith("Zmiana 2", StringComparison.OrdinalIgnoreCase)
                || login.StartsWith("Gość 2", StringComparison.OrdinalIgnoreCase)
                || login.StartsWith("Gosc 2", StringComparison.OrdinalIgnoreCase)) return 3;
            if (login.StartsWith("Zmiana 3", StringComparison.OrdinalIgnoreCase)
                || login.StartsWith("Gość 3", StringComparison.OrdinalIgnoreCase)
                || login.StartsWith("Gosc 3", StringComparison.OrdinalIgnoreCase)) return 4;
            if (login.Contains("Administrator", StringComparison.OrdinalIgnoreCase)
                || login.Equals("Admin", StringComparison.OrdinalIgnoreCase)) return 5;
            return 6;
        }

        var ordered = logins
            .OrderBy(GroupKey)
            .ThenBy(l => l, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var result = new List<LoginListItem>();
        int? previousGroup = null;
        foreach (var login in ordered)
        {
            var group = GroupKey(login);
            if (previousGroup is not null && previousGroup != group)
            {
                result.Add(LoginListItem.Separator());
            }

            result.Add(LoginListItem.Account(login));
            previousGroup = group;
        }

        return result;
    }

    private void OnLoginSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded)
        {
            return;
        }

        if (LoginComboBox.SelectedItem is LoginListItem { IsSeparator: true })
        {
            // Separator nie jest wybieralny — wróć do poprzedniego konta.
            if (e.RemovedItems.Count > 0 && e.RemovedItems[0] is LoginListItem previous && !previous.IsSeparator)
            {
                LoginComboBox.SelectedItem = previous;
            }
            else
            {
                LoginComboBox.SelectedItem = (LoginComboBox.ItemsSource as IEnumerable<LoginListItem>)
                    ?.FirstOrDefault(i => !i.IsSeparator);
            }

            return;
        }

        if (LoginComboBox.SelectedItem is null)
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
            var login = LoginComboBox.SelectedItem is LoginListItem item && !item.IsSeparator
                ? item.Login ?? string.Empty
                : LoginComboBox.SelectedItem?.ToString() ?? string.Empty;
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
