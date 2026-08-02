namespace SKRYBEK.Core.Configuration;

/// <summary>
/// Ścieżki baz używane przez moduł rozkazów w TUKAN.
/// Host ustawia jedną wspólną bazę przez <see cref="FromUnifiedDatabase"/>.
/// </summary>
public sealed class DatabasePatch
{
    public string ChomikDatabasePath { get; }
    public string BoberDatabasePath { get; }

    private DatabasePatch(string chomikPath, string boberPath)
    {
        ChomikDatabasePath = chomikPath;
        BoberDatabasePath = boberPath;
    }

    /// <summary>TUKAN: obie ścieżki wskazują na jedną wspólną bazę.</summary>
    public static DatabasePatch FromUnifiedDatabase(string path)
    {
        var resolved = ResolveDatabasePath(path);
        return new DatabasePatch(resolved, resolved);
    }

    public static string ResolveDatabasePath(string configured)
    {
        var raw = configured.Trim();
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        var expanded = Environment.ExpandEnvironmentVariables(raw);
        var full = Path.IsPathRooted(expanded)
            ? expanded
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, expanded));

        return NormalizeAccessExtension(full);
    }

    private static string NormalizeAccessExtension(string path)
    {
        if (File.Exists(path))
            return path;

        if (path.EndsWith(".acc", StringComparison.OrdinalIgnoreCase))
        {
            var accdb = path[..^4] + "accdb";
            if (File.Exists(accdb))
                return accdb;
        }

        return path;
    }
}
