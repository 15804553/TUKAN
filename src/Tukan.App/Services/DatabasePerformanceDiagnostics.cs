using System.Data;
using System.Data.OleDb;
using BOBER.Core.Diagnostics;
using BOBER.Services.Logging;

namespace Tukan.App.Services;

/// <summary>
/// Odczytuje metadane i liczebność tabel bez modyfikowania bazy.
/// </summary>
internal static class DatabasePerformanceDiagnostics
{
    public static async Task InspectAsync(
        string databasePath,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (!PerformanceDiagnostics.IsEnabled)
            return;

        await using var connection = new OleDbConnection(
            TukanDatabaseOptions.BuildConnectionString(databasePath, password));
        await connection.OpenAsync(cancellationToken);

        LogIndexes(connection);

        foreach (var tableName in GetUserTableNames(connection))
        {
            await using var command = new OleDbCommand(
                $"SELECT COUNT(*) FROM [{EscapeIdentifier(tableName)}]",
                connection);
            var count = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
            BoberLog.Information(
                "WYDAJNOSC Schemat Tabela={TableName} Wiersze={RowCount}",
                tableName,
                count);
        }
    }

    private static IReadOnlyList<string> GetUserTableNames(OleDbConnection connection)
    {
        var tables = connection.GetSchema("Tables");
        return tables.Rows
            .Cast<DataRow>()
            .Where(row => string.Equals(row["TABLE_TYPE"]?.ToString(), "TABLE", StringComparison.OrdinalIgnoreCase))
            .Select(row => row["TABLE_NAME"]?.ToString())
            .Where(name => !string.IsNullOrWhiteSpace(name) && !name.StartsWith("MSys", StringComparison.OrdinalIgnoreCase))
            .Cast<string>()
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void LogIndexes(OleDbConnection connection)
    {
        var indexes = connection.GetSchema("Indexes");
        var groupedIndexes = indexes.Rows
            .Cast<DataRow>()
            .Where(row => row["TABLE_NAME"]?.ToString() is { } tableName
                          && !tableName.StartsWith("MSys", StringComparison.OrdinalIgnoreCase))
            .GroupBy(row => new
            {
                Table = row["TABLE_NAME"]?.ToString() ?? string.Empty,
                Index = row["INDEX_NAME"]?.ToString() ?? string.Empty,
                PrimaryKey = Convert.ToBoolean(row["PRIMARY_KEY"]),
                Unique = Convert.ToBoolean(row["UNIQUE"])
            });

        foreach (var index in groupedIndexes)
        {
            var columns = index
                .OrderBy(row => Convert.ToInt32(row["ORDINAL_POSITION"]))
                .Select(row => row["COLUMN_NAME"]?.ToString())
                .Where(column => !string.IsNullOrWhiteSpace(column));
            BoberLog.Information(
                "WYDAJNOSC Schemat Tabela={TableName} Indeks={IndexName} Kolumny={Columns} PK={PrimaryKey} Unikalny={Unique}",
                index.Key.Table,
                index.Key.Index,
                string.Join(",", columns),
                index.Key.PrimaryKey,
                index.Key.Unique);
        }
    }

    private static string EscapeIdentifier(string identifier) =>
        identifier.Replace("]", "]]", StringComparison.Ordinal);
}
