using System.IO;
using Tukan.App.Services.Security;

namespace Tukan.App.Services;

/// <summary>Wspólna baza Access TUKAN (personel, grafik, rozkazy).</summary>
public static class TukanDatabaseOptions
{
    public const string FileName = "TukanDatabase.accdb";

    public static string GetFullPath() =>
        Path.Combine(AppContext.BaseDirectory, FileName);

    /// <summary>
    /// Preferowane hasło: sekret DPAPI, w przeciwnym razie pierwszy kandydat migracji.
    /// Po udanym otwarciu wywołaj <see cref="RememberWorkingPassword"/>.
    /// </summary>
    public static string ResolvePassword() =>
        DatabaseSecretStore.GetPasswordCandidates().First();

    public static IReadOnlyList<string> GetPasswordCandidates() =>
        DatabaseSecretStore.GetPasswordCandidates();

    public static void RememberWorkingPassword(string password) =>
        DatabaseSecretStore.Save(password);

    public static string BuildConnectionString(string databasePath, string? password = null)
    {
        var full = Path.GetFullPath(databasePath);
        if (full.Contains(';', StringComparison.Ordinal))
            throw new ArgumentException("Niedozwolony znak w ścieżce bazy.", nameof(databasePath));

        var pwd = password ?? ResolvePassword();
        return $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={full};Jet OLEDB:Database Password={pwd};";
    }
}
