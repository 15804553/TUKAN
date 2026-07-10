using System.Windows;
using System.Windows.Controls;
using Chomik.App.Controllers;
using Chomik.App.Views.Chrome;
using Chomik.Core.Constants;
using Chomik.Core.Enums;
using Chomik.Core.Models;

namespace Chomik.App.Views.Pages;

public partial class PasswordManagementView : UserControl
{
    private readonly PasswordManagementController _controller;
    private IReadOnlyList<UserAccount> _users = [];

    public PasswordManagementView(PasswordManagementController controller)
    {
        InitializeComponent();
        _controller = controller;
        SubtitleTextBlock.Text =
            $"Zakres: {controller.ScopeDescription} (zalogowany: {controller.ManagerDescription})";
        Loaded += async (_, _) => await LoadUsersAsync();
    }

    private Window? OwnerWindow => Window.GetWindow(this);

    private async Task LoadUsersAsync()
    {
        try
        {
            _users = await _controller.LoadUsersAsync();
            UsersListBox.ItemsSource = _users;
            if (_users.Count > 0)
            {
                UsersListBox.SelectedIndex = 0;
            }
        }
        catch (Exception ex)
        {
            ChomikMessageBox.Show(OwnerWindow, ex.Message, "Chomik");
        }
    }

    private void OnUserSelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateHint();

    private void UpdateHint()
    {
        if (UsersListBox.SelectedItem is not UserAccount user)
        {
            HintTextBlock.Text = string.Empty;
            return;
        }

        if (user.Role == UserRole.Pa)
        {
            HintTextBlock.Text = "Konto PA nie posiada hasła.";
            return;
        }

        if (user.Role == UserRole.Administrator)
        {
            HintTextBlock.Text = "Hasło konta Administrator nie podlega zmianie z tego poziomu.";
            return;
        }

        var defaultPassword = DefaultCredentials.DefaultPasswords[user.Role];
        HintTextBlock.Text = defaultPassword is null
            ? "Brak hasła domyślnego."
            : $"Hasło domyślne: {defaultPassword}";
    }

    private UserAccount? GetSelectedUser() => UsersListBox.SelectedItem as UserAccount;

    private static bool CanManageSelectedUser(UserAccount user) =>
        user.Role is not UserRole.Pa and not UserRole.Administrator;

    private async void OnSetPasswordClick(object sender, RoutedEventArgs e)
    {
        var user = GetSelectedUser();
        if (user is null || !CanManageSelectedUser(user))
        {
            return;
        }

        try
        {
            await _controller.ChangePasswordAsync(user.Id, NewPasswordBox.Password);
            NewPasswordBox.Clear();
            ChomikMessageBox.Show(OwnerWindow, "Hasło zostało zmienione.", "Informacja");
        }
        catch (Exception ex)
        {
            ChomikMessageBox.Show(OwnerWindow, ex.Message, "Chomik");
        }
    }

    private async void OnResetClick(object sender, RoutedEventArgs e)
    {
        var user = GetSelectedUser();
        if (user is null || !CanManageSelectedUser(user))
        {
            return;
        }

        if (ChomikMessageBox.Show(
                OwnerWindow,
                $"Przywrócić domyślne hasło dla {user.Login}?",
                "Potwierdzenie",
                ChomikMessageButtons.YesNo) != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            await _controller.ResetPasswordAsync(user.Id);
            NewPasswordBox.Clear();
            ChomikMessageBox.Show(OwnerWindow, "Hasło zostało zresetowane.", "Informacja");
        }
        catch (Exception ex)
        {
            ChomikMessageBox.Show(OwnerWindow, ex.Message, "Chomik");
        }
    }
}
