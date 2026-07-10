using Chomik.Core.Constants;

namespace Chomik.Data;

public sealed class DatabaseOptions
{
    public const string DatabaseFileName = "ChomikDatabase.accdb";

    public string FilePath { get; set; } = DatabaseFileName;
    public string DatabasePassword { get; set; } = DefaultCredentials.DatabasePassword;
    public bool UseDatabasePassword { get; set; } = true;

    public string GetApplicationDirectory() => AppContext.BaseDirectory;

    public string GetFullPath() =>
        Path.IsPathRooted(FilePath)
            ? FilePath
            : Path.Combine(GetApplicationDirectory(), FilePath);

    public void MigrateLegacyDatabaseIfNeeded()
    {
        var targetPath = GetFullPath();
        if (File.Exists(targetPath))
        {
            return;
        }

        foreach (var legacyPath in EnumerateLegacyDatabasePaths())
        {
            if (!File.Exists(legacyPath))
            {
                continue;
            }

            if (string.Equals(legacyPath, targetPath, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            File.Copy(legacyPath, targetPath);
            return;
        }
    }

    private IEnumerable<string> EnumerateLegacyDatabasePaths()
    {
        var appDirectory = GetApplicationDirectory();

        yield return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CHOMIK",
            DatabaseFileName);

        yield return Path.Combine(appDirectory, "Data", DatabaseFileName);
        yield return Path.Combine(Environment.CurrentDirectory, "Data", DatabaseFileName);

        var repositoryDatabase = FindRepositoryDatabasePath();
        if (repositoryDatabase is not null)
        {
            yield return repositoryDatabase;
        }
    }

    private static string? FindRepositoryDatabasePath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "Data", DatabaseFileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return null;
    }

    public string BuildConnectionString()
    {
        var fullPath = GetFullPath();
        var passwordPart = UseDatabasePassword
            ? $";Jet OLEDB:Database Password={DatabasePassword}"
            : string.Empty;

        return $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={fullPath}{passwordPart};";
    }

    public string BuildCreateConnectionString()
    {
        var fullPath = GetFullPath();
        return UseDatabasePassword
            ? $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={fullPath};Jet OLEDB:Database Password={DatabasePassword};"
            : $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={fullPath};";
    }
}
