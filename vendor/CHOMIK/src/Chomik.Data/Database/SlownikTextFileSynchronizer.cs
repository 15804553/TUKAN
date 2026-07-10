using System.Data.OleDb;
using Chomik.Core.Slowniki;
using Chomik.Data;

namespace Chomik.Data.Database;

internal static class SlownikTextFileSynchronizer
{
    public static async Task SyncAsync(
        OleDbConnection connection,
        DatabaseOptions options,
        CancellationToken cancellationToken = default)
    {
        var databaseDirectory = Path.GetDirectoryName(options.GetFullPath());
        SlownikTextFiles.EnsureFilesExist(databaseDirectory);

        await SyncTableAsync(
            connection,
            "StopnieSlownik",
            SlownikTextFiles.ReadStopnie(databaseDirectory),
            cancellationToken);

        await SyncTableAsync(
            connection,
            "StanowiskaSlownik",
            SlownikTextFiles.ReadStanowiska(databaseDirectory),
            cancellationToken);
    }

    private static async Task SyncTableAsync(
        OleDbConnection connection,
        string table,
        IReadOnlyList<string> namesFromFile,
        CancellationToken cancellationToken)
    {
        if (namesFromFile.Count == 0)
        {
            return;
        }

        try
        {
            var existingIds = await LoadIdsOrderedAsync(connection, table, cancellationToken);
            for (var i = 0; i < namesFromFile.Count; i++)
            {
                var nazwa = namesFromFile[i];
                if (i < existingIds.Count)
                {
                    await UpdateNazwaAsync(connection, table, existingIds[i], nazwa, cancellationToken);
                }
                else
                {
                    await InsertNazwaAsync(connection, table, nazwa, cancellationToken);
                }
            }
        }
        catch (OleDbException)
        {
            // Tabela może jeszcze nie istnieć (pierwsze uruchomienie przed seedem).
        }
    }

    private static async Task<List<int>> LoadIdsOrderedAsync(
        OleDbConnection connection,
        string table,
        CancellationToken cancellationToken)
    {
        var ids = new List<int>();
        await using var command = new OleDbCommand($"SELECT Id FROM {table} ORDER BY Id", connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            ids.Add(reader.GetFieldInt32(0));
        }

        return ids;
    }

    private static async Task UpdateNazwaAsync(
        OleDbConnection connection,
        string table,
        int id,
        string nazwa,
        CancellationToken cancellationToken)
    {
        await using var command = new OleDbCommand($"UPDATE {table} SET Nazwa = ? WHERE Id = ?", connection);
        command.Parameters.AddWithValue("@p1", nazwa);
        command.Parameters.AddWithValue("@p2", id);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InsertNazwaAsync(
        OleDbConnection connection,
        string table,
        string nazwa,
        CancellationToken cancellationToken)
    {
        await using var command = new OleDbCommand($"INSERT INTO {table} (Nazwa) VALUES (?)", connection);
        command.Parameters.AddWithValue("@p1", nazwa);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
