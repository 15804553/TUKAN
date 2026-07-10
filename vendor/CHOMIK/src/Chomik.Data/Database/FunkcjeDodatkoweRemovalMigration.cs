using System.Data.OleDb;

namespace Chomik.Data.Database;

/// <summary>Usuwa tabele funkcji dodatkowych z istniejących baz (feature wycofany z CHOMIK).</summary>
internal static class FunkcjeDodatkoweRemovalMigration
{
    public static async Task ApplyAsync(OleDbConnection connection, CancellationToken cancellationToken = default)
    {
        await DropTableIfExistsAsync(connection, "FunkcjonariuszFunkcjeDodatkowe", cancellationToken);
        await DropTableIfExistsAsync(connection, "FunkcjeDodatkoweSlownik", cancellationToken);
    }

    private static async Task DropTableIfExistsAsync(
        OleDbConnection connection,
        string tableName,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var command = new OleDbCommand($"DROP TABLE {tableName}", connection);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (OleDbException)
        {
            // Tabela nie istnieje lub została już usunięta.
        }
    }
}
