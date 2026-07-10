using System.Data.OleDb;
using Chomik.Data;

namespace Chomik.Data.Database;

internal static class NumerPorzadkowyMigration
{
    public static async Task ApplyAsync(OleDbConnection connection, CancellationToken cancellationToken = default)
    {
        await EnsureColumnAsync(connection, cancellationToken);
        await BackfillMissingAsync(connection, cancellationToken);
    }

    private static async Task EnsureColumnAsync(OleDbConnection connection, CancellationToken cancellationToken)
    {
        try
        {
            await using var command = new OleDbCommand(
                "ALTER TABLE Funkcjonariusze ADD COLUMN NumerPorzadkowy SHORT",
                connection);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (OleDbException ex) when (ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase)
                                         || ex.Message.Contains("już istnieje", StringComparison.OrdinalIgnoreCase)
                                         || ex.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase)
                                         || ex.Message.Contains("istnieje", StringComparison.OrdinalIgnoreCase))
        {
            // Kolumna już dodana.
        }
    }

    private static async Task BackfillMissingAsync(OleDbConnection connection, CancellationToken cancellationToken)
    {
        var rows = new List<(int Id, int NumerZmiany)>();
        try
        {
            await using var selectCommand = new OleDbCommand(
                """
                SELECT Id, NumerZmiany
                FROM Funkcjonariusze
                WHERE NumerPorzadkowy IS NULL OR NumerPorzadkowy = 0
                ORDER BY NumerZmiany, Nazwisko, Imie, Id
                """,
                connection);
            await using var reader = await selectCommand.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                rows.Add((reader.GetFieldInt32(0), reader.GetFieldInt32(1)));
            }
        }
        catch (OleDbException)
        {
            return;
        }

        if (rows.Count == 0)
        {
            return;
        }

        var nextByShift = await LoadNextNumberByShiftAsync(connection, cancellationToken);

        foreach (var (id, numerZmiany) in rows)
        {
            if (!nextByShift.TryGetValue(numerZmiany, out var next))
            {
                next = 1;
            }

            await using var updateCommand = new OleDbCommand(
                "UPDATE Funkcjonariusze SET NumerPorzadkowy = ? WHERE Id = ?",
                connection);
            updateCommand.Parameters.AddWithValue("@p1", (short)next);
            updateCommand.Parameters.AddWithValue("@p2", id);
            await updateCommand.ExecuteNonQueryAsync(cancellationToken);
            nextByShift[numerZmiany] = next + 1;
        }
    }

    private static async Task<Dictionary<int, int>> LoadNextNumberByShiftAsync(
        OleDbConnection connection,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<int, int>();
        try
        {
            await using var command = new OleDbCommand(
                """
                SELECT NumerZmiany, MAX(NumerPorzadkowy)
                FROM Funkcjonariusze
                WHERE NumerPorzadkowy IS NOT NULL AND NumerPorzadkowy > 0
                GROUP BY NumerZmiany
                """,
                connection);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var shift = reader.GetFieldInt32(0);
                var max = reader.IsDBNull(1) ? 0 : reader.GetFieldInt32(1);
                result[shift] = max + 1;
            }
        }
        catch (OleDbException)
        {
            // Brak kolumny lub pustej tabeli.
        }

        return result;
    }
}
