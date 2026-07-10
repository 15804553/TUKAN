namespace Chomik.Core.Slowniki;

/// <summary>
/// Słowniki stopni i stanowisk w plikach tekstowych (jedna pozycja na wiersz).
/// Pliki leżą w tym samym katalogu co baza danych (obok pliku exe).
/// </summary>
public static class SlownikTextFiles
{
    public const string StopnieFileName = "Stopnie.txt";
    public const string StanowiskaFileName = "Stanowiska.txt";

    public static IReadOnlyList<string> ReadStopnie(string? databaseDirectory = null) =>
        ReadLines(StopnieFileName, databaseDirectory);

    public static IReadOnlyList<string> ReadStanowiska(string? databaseDirectory = null) =>
        ReadLines(StanowiskaFileName, databaseDirectory);

    public static void EnsureFilesExist(string? databaseDirectory = null)
    {
        MigrateLegacyDictionaryFilesIfNeeded(databaseDirectory);

        EnsureFile(StopnieFileName, Constants.StopnieSlownikDefaults.NazwyPoKolei, databaseDirectory);
        EnsureFile(StanowiskaFileName, Constants.StanowiskaSlownikDefaults.NazwyPoKolei, databaseDirectory);
    }

    public static IReadOnlyList<string> ReadLines(string fileName, string? databaseDirectory = null)
    {
        var path = ResolveExistingPath(fileName, databaseDirectory);
        if (path is null)
        {
            return [];
        }

        return File.ReadAllLines(path)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0)
            .ToList();
    }

    public static string? ResolveExistingPath(string fileName, string? databaseDirectory = null)
    {
        foreach (var candidate in EnumerateCandidatePaths(fileName, databaseDirectory))
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static void MigrateLegacyDictionaryFilesIfNeeded(string? databaseDirectory)
    {
        var targetDirectory = ResolveWritableDirectory(databaseDirectory);
        if (targetDirectory is null)
        {
            return;
        }

        Directory.CreateDirectory(targetDirectory);

        foreach (var fileName in new[] { StopnieFileName, StanowiskaFileName })
        {
            var targetPath = Path.Combine(targetDirectory, fileName);
            if (File.Exists(targetPath))
            {
                continue;
            }

            foreach (var legacyDirectory in EnumerateLegacyDictionaryDirectories(targetDirectory))
            {
                var legacyPath = Path.Combine(legacyDirectory, fileName);
                if (!File.Exists(legacyPath))
                {
                    continue;
                }

                File.Copy(legacyPath, targetPath);
                break;
            }
        }
    }

    private static void EnsureFile(
        string fileName,
        IReadOnlyList<string> defaultLines,
        string? databaseDirectory)
    {
        var existing = ResolveExistingPath(fileName, databaseDirectory);
        if (existing is not null)
        {
            return;
        }

        var targetDirectory = ResolveWritableDirectory(databaseDirectory)
            ?? throw new InvalidOperationException(
                $"Nie można utworzyć pliku słownika {fileName} — brak katalogu docelowego.");

        Directory.CreateDirectory(targetDirectory);
        var targetPath = Path.Combine(targetDirectory, fileName);
        File.WriteAllLines(targetPath, defaultLines);
    }

    private static string? ResolveWritableDirectory(string? databaseDirectory)
    {
        if (!string.IsNullOrWhiteSpace(databaseDirectory))
        {
            return databaseDirectory;
        }

        return AppContext.BaseDirectory;
    }

    private static IEnumerable<string> EnumerateCandidatePaths(string fileName, string? databaseDirectory)
    {
        foreach (var directory in EnumerateCandidateDirectories(databaseDirectory))
        {
            yield return Path.Combine(directory, fileName);
        }
    }

    private static IEnumerable<string> EnumerateCandidateDirectories(string? databaseDirectory)
    {
        if (!string.IsNullOrWhiteSpace(databaseDirectory))
        {
            yield return databaseDirectory;
        }

        yield return AppContext.BaseDirectory;
    }

    private static IEnumerable<string> EnumerateLegacyDictionaryDirectories(string targetDirectory)
    {
        yield return Path.Combine(AppContext.BaseDirectory, "Data");
        yield return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CHOMIK");

        if (!string.Equals(targetDirectory, AppContext.BaseDirectory, StringComparison.OrdinalIgnoreCase))
        {
            yield return AppContext.BaseDirectory;
        }
    }
}
