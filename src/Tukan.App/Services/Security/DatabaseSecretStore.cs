using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Tukan.App.Services.Security;

/// <summary>
/// Przechowuje hasło bazy Access w pliku chronionym DPAPI (LocalMachine).
/// Legacy hasła z kodu służą tylko jako kandydaci pierwszego otwarcia / migracji.
/// </summary>
public static class DatabaseSecretStore
{
    private const string SecretFileName = "TukanDatabase.secret";
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("TUKAN.DatabaseSecret.v1");

    /// <summary>Historyczne hasła Jet — tylko fallback migracji, nie źródło prawdy.</summary>
    public static IReadOnlyList<string> LegacyMigrationCandidates { get; } =
        ["5359", "5393"];

    public static string GetSecretFilePath(string? baseDirectory = null) =>
        Path.Combine(baseDirectory ?? AppContext.BaseDirectory, SecretFileName);

    public static bool TryLoad(out string password, string? baseDirectory = null)
    {
        password = string.Empty;
        var path = GetSecretFilePath(baseDirectory);
        if (!File.Exists(path))
            return false;

        try
        {
            var protectedBytes = File.ReadAllBytes(path);
            var plain = ProtectedData.Unprotect(protectedBytes, Entropy, DataProtectionScope.LocalMachine);
            password = Encoding.UTF8.GetString(plain);
            return !string.IsNullOrEmpty(password);
        }
        catch
        {
            return false;
        }
    }

    public static void Save(string password, string? baseDirectory = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);
        var plain = Encoding.UTF8.GetBytes(password);
        var protectedBytes = ProtectedData.Protect(plain, Entropy, DataProtectionScope.LocalMachine);
        File.WriteAllBytes(GetSecretFilePath(baseDirectory), protectedBytes);
        CryptographicOperations.ZeroMemory(plain);
    }

    /// <summary>Kolejność: sekret DPAPI (jeśli jest), potem kandydaci migracji bez duplikatów.</summary>
    public static IReadOnlyList<string> GetPasswordCandidates(string? baseDirectory = null)
    {
        var result = new List<string>();
        if (TryLoad(out var stored, baseDirectory))
            result.Add(stored);

        foreach (var candidate in LegacyMigrationCandidates)
        {
            if (!result.Contains(candidate, StringComparer.Ordinal))
                result.Add(candidate);
        }

        return result;
    }
}
