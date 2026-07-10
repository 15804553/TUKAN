using System.IO;

namespace Tukan.App.Services;

/// <summary>Wspólna baza Access dla modułów CHOMIK, BOBER i SKRYBEK w TUKAN.</summary>
public static class TukanDatabaseOptions
{
    public const string FileName = "TukanDatabase.accdb";
    public const string Password = "5359";

    /// <summary>Poprzednie nazwy plików (standalone / wcześniejszy TUKAN).</summary>
    public static readonly string[] LegacyFileNames =
    [
        "ChomikDatabase.accdb",
        "BoberDatabase.accdb",
        "SkrybekDatabase.accdb"
    ];

    public static string GetFullPath() =>
        Path.Combine(AppContext.BaseDirectory, FileName);

    public static string BuildConnectionString(string databasePath) =>
        $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={databasePath};Jet OLEDB:Database Password={Password};";
}
