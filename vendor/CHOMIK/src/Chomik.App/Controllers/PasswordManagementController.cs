using Chomik.Core.Constants;
using Chomik.Core.Enums;
using Chomik.Core.Models;
using Chomik.Services;

namespace Chomik.App.Controllers;

public sealed class PasswordManagementController(AppServices services)
{
    private static readonly (UserRole Role, string Label)[] DefaultPasswordCatalog =
    [
        (UserRole.Zmiana1, "Zmiana 1"),
        (UserRole.Gosc1, "Gość 1"),
        (UserRole.Zmiana2, "Zmiana 2"),
        (UserRole.Gosc2, "Gość 2"),
        (UserRole.Zmiana3, "Zmiana 3"),
        (UserRole.Gosc3, "Gość 3"),
        (UserRole.DcaJrg, "DCA JRG")
    ];

    public string ManagerDescription =>
        services.Auth.CurrentUser?.Login ?? string.Empty;

    public bool CanManageAllUsers =>
        services.Auth.CurrentUser?.CanResetAllPasswords ?? false;

    /// <summary>Domyślne hasła w UI — wyłącznie dla zalogowanego DCA JRG.</summary>
    public bool CanRevealDefaultPasswords =>
        services.Auth.CurrentUser?.IsDcaJrgUser == true;

    public string ScopeDescription => CanManageAllUsers
        ? "konta zmian 1–3 i DCA JRG (bez PA i Administrator)"
        : services.Auth.CurrentUser?.IsDcaJrgUser == true
            ? "konta zmian 1–3 (bez własnego konta DCA)"
            : "konta zmian 1–3";

    public Task<IReadOnlyList<UserAccount>> LoadUsersAsync(CancellationToken cancellationToken = default)
    {
        var user = services.Auth.CurrentUser!;
        return services.UserAccounts.GetManageableUsersAsync(user, cancellationToken);
    }

    public Task ChangePasswordAsync(int userId, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = services.Auth.CurrentUser!;
        return services.UserAccounts.ChangePasswordAsync(user, userId, newPassword, cancellationToken);
    }

    public Task ResetPasswordAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = services.Auth.CurrentUser!;
        return services.UserAccounts.ResetToDefaultAsync(user, userId, cancellationToken);
    }

    /// <summary>Pełna lista haseł startowych widoczna tylko dla DCA JRG.</summary>
    public string BuildDefaultPasswordsSummary()
    {
        if (!CanRevealDefaultPasswords)
            return "Reset ustawi hasło startowe roli. Hasła domyślne są widoczne tylko dla DCA JRG.";

        var lines = new List<string> { "Hasła domyślne:" };
        foreach (var (role, label) in DefaultPasswordCatalog)
        {
            if (!DefaultCredentials.DefaultPasswords.TryGetValue(role, out var password) || password is null)
                continue;
            lines.Add($"• {label}: {password}");
        }

        lines.Add("• PA: bez hasła");
        return string.Join(Environment.NewLine, lines);
    }
}
