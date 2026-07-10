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
            .Select(u => u.Login)
            .OrderBy(login => login.Equals("PA", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(login => login, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

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
            return (false, "Nieprawidłowy login.");
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
            return (false, "Nieprawidłowe hasło.");
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
