using System.IO;

namespace Tukan.App.Services;

/// <summary>Wspólna baza Access TUKAN (personel, grafik, rozkazy).</summary>
public static class TukanDatabaseOptions
{
    public const string FileName = "TukanDatabase.accdb";
    public const string Password = "5359";

    public static string GetFullPath() =>
        Path.Combine(AppContext.BaseDirectory, FileName);

    public static string BuildConnectionString(string databasePath) =>
        $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={databasePath};Jet OLEDB:Database Password={Password};";
}
