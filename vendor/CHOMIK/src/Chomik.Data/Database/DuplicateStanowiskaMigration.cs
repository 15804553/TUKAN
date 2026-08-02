using System.Data.OleDb;
using Chomik.Data;

namespace Chomik.Data.Database;

/// <summary>
/// Usuwa zduplikowane nazwy w StanowiskaSlownik (np. dwa „Dowódca zmiany”).
/// Zachowuje najniższe Id, przenosi funkcjonariuszy i kasuje zbędne rekordy.
/// </summary>
internal static class DuplicateStanowiskaMigration
{
    public static async Task ApplyAsync(OleDbConnection connection, CancellationToken cancellationToken = default)
    {
        var byName = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);

        await using (var command = new OleDbCommand(
            "SELECT Id, Nazwa FROM StanowiskaSlownik ORDER BY Id",
            connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var id = reader.GetFieldInt32(0);
                var nazwa = reader.GetString(1);
                if (!byName.TryGetValue(nazwa, out var ids))
                {
                    ids = [];
                    byName[nazwa] = ids;
                }

                ids.Add(id);
            }
        }

        foreach (var ids in byName.Values)
        {
            if (ids.Count < 2)
            {
                continue;
            }

            var keepId = ids[0];
            foreach (var duplicateId in ids.Skip(1))
            {
                await using (var reassign = new OleDbCommand(
                    "UPDATE Funkcjonariusze SET StanowiskoId = ? WHERE StanowiskoId = ?",
                    connection))
                {
                    reassign.Parameters.AddWithValue("@p1", keepId);
                    reassign.Parameters.AddWithValue("@p2", duplicateId);
                    await reassign.ExecuteNonQueryAsync(cancellationToken);
                }

                await using var delete = new OleDbCommand(
                    "DELETE FROM StanowiskaSlownik WHERE Id = ?",
                    connection);
                delete.Parameters.AddWithValue("@p1", duplicateId);
                await delete.ExecuteNonQueryAsync(cancellationToken);
            }
        }
    }
}
