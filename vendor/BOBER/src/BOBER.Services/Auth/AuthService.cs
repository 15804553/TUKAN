using BOBER.Core.Models;
using BOBER.Core.Security;
using BOBER.Data.Repositories;
using BOBER.Services.Logging;

namespace BOBER.Services.Auth;

/// <summary>Logowanie: konta z ChomikDB, przy braku połączenia — tabela UzytkownicyBOBER.</summary>
public sealed class AuthService(IAuthRepository authRepository, IChomikRepository chomikRepository) : IAuthService
{
    private IReadOnlyList<UserAccount>? _cachedAccounts;

    public SessionInfo? CurrentSession { get; private set; }

    public async Task<IReadOnlyList<string>> GetLoginsAsync(CancellationToken cancellationToken = default)
    {
        // Ładuj konta z ChomikDB; jeśli niedostępna — fallback na własną tabelę BOBER
        try
        {
            _cachedAccounts = await chomikRepository.GetUserAccountsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            BoberLog.Warning(ex, "ChomikDB niedostępna przy pobieraniu loginów — używam UzytkownicyBOBER");
            _cachedAccounts = await authRepository.GetAllAsync(cancellationToken);
        }

        return _cachedAccounts
            .Where(a => a.NumerZmiany != 4)
            .Select(a => a.Login)
            .ToList();
    }

    public bool TryAuthenticate(string login, string password, out string errorMessage)
    {
        if (_cachedAccounts is null)
        {
            errorMessage = "Lista użytkowników nie została załadowana.";
            return false;
        }

        var account = _cachedAccounts.FirstOrDefault(a =>
            a.NumerZmiany != 4 &&
            string.Equals(a.Login, login, StringComparison.OrdinalIgnoreCase));

        if (account is null)
        {
            errorMessage = "Nieprawidłowy login.";
            return false;
        }

        if (string.IsNullOrEmpty(account.HasloHash))
        {
            if (!string.IsNullOrEmpty(password))
            {
                errorMessage = "Konto PA nie wymaga hasła — pozostaw pole puste.";
                return false;
            }
        }
        else if (!PasswordHasher.Verify(password, account.HasloHash, account.HasloSol))
        {
            errorMessage = "Nieprawidłowe hasło.";
            return false;
        }

        CurrentSession = new SessionInfo
        {
            ZmianaId = ResolveZmianaId(account),
            NazwaZmiany = account.Login
        };

        errorMessage = string.Empty;
        return true;
    }

    private static int ResolveZmianaId(UserAccount account)
    {
        if (account.NumerZmiany is > 0 and not 4)
        {
            return account.NumerZmiany;
        }

        if (account.Login.StartsWith("Zmiana ", StringComparison.OrdinalIgnoreCase))
        {
            var suffix = account.Login["Zmiana ".Length..].Trim();
            if (int.TryParse(suffix, out var numer) && numer is > 0 and not 4)
            {
                return numer;
            }
        }

        return account.NumerZmiany > 0 ? account.NumerZmiany : 1;
    }

    public void Logout() => CurrentSession = null;
}
