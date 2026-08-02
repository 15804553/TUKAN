using System.Security.Cryptography;
using System.Text;
using SKRYBEK.Core.Enums;
using SKRYBEK.Core.Models;
using SKRYBEK.Data.Repositories;
using SKRYBEK.Services.Logging;

namespace SKRYBEK.Services.Auth;

public sealed class AuthService
{
    private const string Pbkdf2Prefix = "pbkdf2$";
    private const int Pbkdf2Iterations = 210_000;
    private const int Pbkdf2KeySize = 32;

    private readonly ChomikAuthRepository _chomikAuth;

    public AuthService(ChomikAuthRepository chomikAuth)
    {
        _chomikAuth = chomikAuth;
    }

    /// <summary>Zwraca listę kont do wyświetlenia w oknie logowania: Zmiana 1–3, Gość 1–3, PA, DCA JRG.</summary>
    public async Task<List<UserAccount>> GetAvailableUsersAsync()
    {
        var dozwolone = new HashSet<UserRole>
        {
            UserRole.Zmiana1, UserRole.Zmiana2, UserRole.Zmiana3,
            UserRole.Gosc1, UserRole.Gosc2, UserRole.Gosc3,
            UserRole.PA, UserRole.DCAJRG
        };

        var kolejnosc = new Dictionary<UserRole, int>
        {
            [UserRole.PA]      = 0,
            [UserRole.DCAJRG]  = 1,
            [UserRole.Zmiana1] = 2,
            [UserRole.Gosc1]   = 3,
            [UserRole.Zmiana2] = 4,
            [UserRole.Gosc2]   = 5,
            [UserRole.Zmiana3] = 6,
            [UserRole.Gosc3]   = 7
        };

        var wszyscy = await _chomikAuth.GetAllAsync();
        return wszyscy
            .Where(u => dozwolone.Contains(u.Role))
            .GroupBy(u => u.Role)
            .Select(g => g.First())
            .OrderBy(u => kolejnosc[u.Role])
            .ToList();
    }

    /// <summary>Loguje użytkownika po loginie (ponowny odczyt z CHOMIK).</summary>
    public async Task<SessionInfo?> LoginAsync(string login, string haslo)
    {
        var user = await _chomikAuth.GetByLoginAsync(login.Trim());
        if (user is null)
        {
            SkrybekLog.Warning($"Nieudana próba logowania — brak użytkownika: {login}");
            return null;
        }

        return LoginCore(user, haslo);
    }

    /// <summary>Loguje wybranego użytkownika (hash z listy logowania — bez drugiego odczytu).</summary>
    public Task<SessionInfo?> LoginAsync(UserAccount user, string haslo) =>
        Task.FromResult(LoginCore(user, haslo));

    private SessionInfo? LoginCore(UserAccount user, string? haslo)
    {
        var password = (haslo ?? string.Empty).Trim();
        var hash = user.HasloHash.Trim();
        var salt = user.HasloSol.Trim();

        if (string.IsNullOrEmpty(hash))
        {
            if (!string.IsNullOrEmpty(password))
            {
                SkrybekLog.Warning($"Konto PA nie wymaga hasła: {user.Login}");
                return null;
            }
        }
        else if (!VerifyChomikPassword(password, hash, salt))
        {
            SkrybekLog.Warning($"Błędne hasło dla użytkownika: {user.Login}");
            return null;
        }

        SkrybekLog.Info($"Zalogowano: {user.Login} ({user.NazwaZmiany})");

        var session = new SessionInfo
        {
            UserId      = user.Id,
            Login       = user.Login,
            NazwaZmiany = user.NazwaZmiany,
            NumerZmiany = user.NumerZmiany,
            IsReadOnly  = user.IsReadOnly,
            CanEditAll  = user.Role == UserRole.DCAJRG,
            CanEditPojazdy = user.Role is UserRole.DCAJRG
                or UserRole.Zmiana1 or UserRole.Zmiana2 or UserRole.Zmiana3,
            IsPaAccount = user.Role == UserRole.PA
        };
        session.NormalizePaFlags();
        return session;
    }

    /// <summary>
    /// CHOMIK: PBKDF2 (prefix pbkdf2$), legacy Base64(SHA256), legacy HEX(SHA256) ze starego seeda SKRYBEK.
    /// </summary>
    public static bool VerifyChomikPassword(string password, string hash, string salt)
    {
        if (string.IsNullOrEmpty(hash) || string.IsNullOrEmpty(salt))
            return false;

        var p = password.Trim();
        var h = hash.Trim();
        var s = salt.Trim();

        try
        {
            if (h.StartsWith(Pbkdf2Prefix, StringComparison.Ordinal))
            {
                var expected = Convert.FromBase64String(h[Pbkdf2Prefix.Length..]);
                var saltBytes = Convert.FromBase64String(s);
                var actual = Rfc2898DeriveBytes.Pbkdf2(
                    p, saltBytes, Pbkdf2Iterations, HashAlgorithmName.SHA256, Pbkdf2KeySize);
                return CryptographicOperations.FixedTimeEquals(actual, expected);
            }

            var legacyExpected = Convert.FromBase64String(h);
            var legacyActual = SHA256.HashData(Encoding.UTF8.GetBytes(p + s));
            if (CryptographicOperations.FixedTimeEquals(legacyActual, legacyExpected))
                return true;
        }
        catch (FormatException)
        {
            // Może być legacy HEX
        }

        return ComputeLegacyHexHash(p, s).Equals(h, StringComparison.OrdinalIgnoreCase);
    }

    private static string ComputeLegacyHexHash(string password, string salt)
    {
        var bytes = Encoding.UTF8.GetBytes(password.Trim() + salt.Trim());
        return Convert.ToHexString(SHA256.HashData(bytes));
    }
}
