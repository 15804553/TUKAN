using System.Security.Cryptography;
using System.Text;

namespace BOBER.Core.Security;

public static class PasswordHasher
{
    public static (string Hash, string Salt) HashPassword(string password)
    {
        var saltBytes = RandomNumberGenerator.GetBytes(16);
        var salt = Convert.ToBase64String(saltBytes);
        var hash = ComputeHash(password, salt);
        return (hash, salt);
    }

    public static bool Verify(string password, string hash, string salt) =>
        ComputeHash(password, salt) == hash;

    private static string ComputeHash(string password, string salt)
    {
        var bytes = Encoding.UTF8.GetBytes(password + salt);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToBase64String(hashBytes);
    }
}
