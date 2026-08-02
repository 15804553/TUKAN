using Chomik.Core.Enums;
using Chomik.Core.Security;
using Chomik.Data.Repositories;

namespace Chomik.Services.Security;

public sealed class AuthService(IUserRepository userRepository) : IAuthService
{
    private const int LegacyZmiana4Role = 4;

    public SessionUser? CurrentUser { get; private set; }

    public async Task<IReadOnlyList<string>> GetAvailableLoginsAsync(CancellationToken cancellationToken = default)
    {
        var users = await userRepository.GetAllAsync(cancellationToken);

        return users
            .Where(u => (int)u.Role != LegacyZmiana4Role)
            .OrderBy(u => LoginSortOrder(u.Role))
            .ThenBy(u => u.Login, StringComparer.OrdinalIgnoreCase)
            .Select(u => u.Login)
            .ToList();
    }

    /// <summary>PA, DCA, Zmiana 1, Gość 1, Zmiana 2, Gość 2, Zmiana 3, Gość 3, …</summary>
    private static int LoginSortOrder(UserRole role) => role switch
    {
        UserRole.Pa => 0,
        UserRole.DcaJrg => 1,
        UserRole.Zmiana1 => 2,
        UserRole.Gosc1 => 3,
        UserRole.Zmiana2 => 4,
        UserRole.Gosc2 => 5,
        UserRole.Zmiana3 => 6,
        UserRole.Gosc3 => 7,
        UserRole.Administrator => 8,
        _ => 99
    };

    public bool TryLogin(string login, string? password, out string? errorMessage)
    {
        var (success, error) = TryLoginCoreAsync(login, password, CancellationToken.None)
            .GetAwaiter().GetResult();
        errorMessage = error;
        return success;
    }

    private async Task<(bool Success, string? Error)> TryLoginCoreAsync(
        string login,
        string? password,
        CancellationToken cancellationToken)
    {
        var account = await userRepository.GetByLoginAsync(login.Trim(), cancellationToken);
        if (account is null)
        {
            return (false, "Nieprawidłowy login lub hasło.");
        }

        if ((int)account.Role == LegacyZmiana4Role)
        {
            return (false, "Konto Zmiana 4 nie jest obsługiwane.");
        }

        if (string.IsNullOrEmpty(account.HasloHash))
        {
            if (!string.IsNullOrEmpty(password))
            {
                return (false, "Konto PA nie wymaga hasła — pozostaw pole puste.");
            }
        }
        else if (!PasswordHasher.Verify(password ?? string.Empty, account.HasloHash, account.HasloSol))
        {
            return (false, "Nieprawidłowy login lub hasło.");
        }

        CurrentUser = new SessionUser
        {
            Login = account.Login,
            Role = account.Role,
            ShiftNumber = account.NumerZmiany
        };

        return (true, null);
    }

    public void Logout() => CurrentUser = null;
}
