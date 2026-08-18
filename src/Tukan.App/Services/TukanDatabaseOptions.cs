using System.IO;
using BOBER.Services.Startup;
using Tukan.App.Services.Security;

namespace Tukan.App.Services;

/// <summary>Wspólna baza Access TUKAN (personel, grafik, rozkazy).</summary>
public static class TukanDatabaseOptions
{
    public const string FileName = "TukanDatabase.accdb";

    /// <summary>
    /// Ścieżka z <c>databasepath.txt</c> obok EXE (dysk lokalny, mapa sieciowa lub UNC).
    /// Brak pliku lub pusta treść → baza obok programu.
    /// </summary>
    public static string GetFullPath()
    {
        var configured = DatabasePathFile.TryRead();
        return string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(AppContext.BaseDirectory, FileName)
            : ResolveConfiguredPath(configured, AppContext.BaseDirectory);
    }

    /// <summary>
    /// Zapisuje domyślną ścieżkę tylko gdy <c>databasepath.txt</c> jeszcze nie istnieje.
    /// Nie nadpisuje ręcznie ustawionej lokalizacji sieciowej.
    /// </summary>
    public static void EnsurePathFileExists()
    {
        if (DatabasePathFile.TryRead() is not null)
            return;

        DatabasePathFile.Write(Path.Combine(AppContext.BaseDirectory, FileName));
    }

    public static string ResolveConfiguredPath(string configured, string baseDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseDirectory);

        var raw = StripSurroundingQuotes(configured.Trim());
        if (string.IsNullOrWhiteSpace(raw))
            return Path.Combine(baseDirectory, FileName);

        var expanded = Environment.ExpandEnvironmentVariables(raw);

        string full;
        try
        {
            full = Path.IsPathRooted(expanded)
                ? Path.GetFullPath(expanded)
                : Path.GetFullPath(Path.Combine(baseDirectory, expanded));
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            throw new InvalidOperationException(
                $"Nieprawidłowa ścieżka bazy w databasepath.txt:\n{configured}",
                ex);
        }

        return LooksLikeAccessDatabaseFile(full)
            ? full
            : Path.Combine(full, FileName);
    }

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

    private static string StripSurroundingQuotes(string value)
    {
        if (value.Length >= 2
            && ((value[0] == '"' && value[^1] == '"')
                || (value[0] == '\'' && value[^1] == '\'')))
        {
            return value[1..^1].Trim();
        }

        return value;
    }

    private static bool LooksLikeAccessDatabaseFile(string path) =>
        path.EndsWith(".accdb", StringComparison.OrdinalIgnoreCase)
        || path.EndsWith(".mdb", StringComparison.OrdinalIgnoreCase);
}
