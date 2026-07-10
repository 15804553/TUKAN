using Chomik.Core.Models;
using Chomik.Services;

namespace Chomik.App.Controllers;

public sealed class PasswordManagementController(AppServices services)
{
    public string ManagerDescription =>
        services.Auth.CurrentUser?.Login ?? string.Empty;

    public bool CanManageAllUsers =>
        services.Auth.CurrentUser?.CanResetAllPasswords ?? false;

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
}
