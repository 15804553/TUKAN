using System.Security.Cryptography;
using System.Text;

namespace Chomik.Core.Security;

/// <summary>
/// Hashowanie haseł użytkowników: PBKDF2-SHA256 (nowe) z weryfikacją legacy SHA256(password+salt).
/// </summary>
public static class PasswordHasher
{
    private const string Pbkdf2Prefix = "pbkdf2$";
    private const int Iterations = 210_000;
    private const int SaltSize = 16;
    private const int KeySize = 32;

    public static (string Hash, string Salt) HashPassword(string password)
    {
        var saltBytes = RandomNumberGenerator.GetBytes(SaltSize);
        var salt = Convert.ToBase64String(saltBytes);
        var derived = Pbkdf2(password, saltBytes);
        return (Pbkdf2Prefix + Convert.ToBase64String(derived), salt);
    }

    public static bool Verify(string password, string hash, string salt)
    {
        if (string.IsNullOrEmpty(hash) || string.IsNullOrEmpty(salt))
            return false;

        try
        {
            if (hash.StartsWith(Pbkdf2Prefix, StringComparison.Ordinal))
            {
                var expected = Convert.FromBase64String(hash[Pbkdf2Prefix.Length..]);
                var saltBytes = Convert.FromBase64String(salt);
                var actual = Pbkdf2(password, saltBytes);
                return CryptographicOperations.FixedTimeEquals(actual, expected);
            }

            // Legacy: Base64(SHA256(UTF8(password + salt)))
            var legacyExpected = Convert.FromBase64String(hash);
            var legacyActual = SHA256.HashData(Encoding.UTF8.GetBytes(password + salt));
            return CryptographicOperations.FixedTimeEquals(legacyActual, legacyExpected);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    /// <summary>True, gdy hash jest w formacie legacy i warto przepisać na PBKDF2 po udanym logowaniu.</summary>
    public static bool NeedsRehash(string hash) =>
        !string.IsNullOrEmpty(hash) && !hash.StartsWith(Pbkdf2Prefix, StringComparison.Ordinal);

    private static byte[] Pbkdf2(string password, byte[] salt) =>
        Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            KeySize);
}
