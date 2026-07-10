using BOBER.Core.Constants;

namespace BOBER.Data;

public sealed class BoberDatabaseOptions
{
    private static readonly string DefaultFilePath =
        Path.Combine(AppContext.BaseDirectory, "BoberDatabase.accdb");

    public string FilePath { get; set; } = DefaultFilePath;
    public string DatabasePassword { get; set; } = DefaultCredentials.DatabasePassword;
    public bool UseDatabasePassword { get; set; } = true;

    public string GetFullPath() =>
        Path.IsPathRooted(FilePath)
            ? FilePath
            : Path.Combine(AppContext.BaseDirectory, FilePath);

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

    public void EnsureDirectoryExists()
    {
        var dir = Path.GetDirectoryName(GetFullPath());
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
    }
}
