using BOBER.Core.Constants;

namespace BOBER.Data;

public sealed class ChomikDatabaseOptions
{
    private static readonly string FallbackPath = Path.Combine(
        AppContext.BaseDirectory,
        "CHOMIK",
        "ChomikDatabase.accdb");

    public string FilePath { get; set; } = FallbackPath;
    public string DatabasePassword { get; set; } = DefaultCredentials.DatabasePassword;
    public bool UseDatabasePassword { get; set; } = true;

    public string GetFullPath() => FilePath;

    public string BuildConnectionString()
    {
        var passwordPart = UseDatabasePassword
            ? $";Jet OLEDB:Database Password={DatabasePassword}"
            : string.Empty;
        return $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={FilePath}{passwordPart};";
    }

    public bool FileExists() => File.Exists(FilePath);
}
