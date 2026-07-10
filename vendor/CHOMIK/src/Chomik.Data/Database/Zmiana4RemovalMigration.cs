using System.Data.OleDb;

namespace Chomik.Data.Database;

/// <summary>Usuwa konto, ustawienie i personel Zmiany 4 (feature wycofany z CHOMIK).</summary>
internal static class Zmiana4RemovalMigration
{
    private const short Zmiana4Role = 4;

    public static async Task ApplyAsync(OleDbConnection connection, CancellationToken cancellationToken = default)
    {
        await DeleteSettingAsync(connection, "WlaczZmiane4", cancellationToken);
        await DeleteShift4PersonnelAsync(connection, cancellationToken);
        await DeleteShift4UsersAsync(connection, cancellationToken);
    }

    private static async Task DeleteSettingAsync(
        OleDbConnection connection,
        string key,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var command = new OleDbCommand(
                "DELETE FROM UstawieniaAplikacji WHERE Klucz = ?",
                connection);
            command.Parameters.AddWithValue("@p1", key);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (OleDbException)
        {
            // Ustawienie lub tabela może nie istnieć.
        }
    }

    private static async Task DeleteShift4UsersAsync(
        OleDbConnection connection,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var command = new OleDbCommand(
                "DELETE FROM Uzytkownicy WHERE Rola = ? OR NumerZmiany = 4",
                connection);
            command.Parameters.AddWithValue("@p1", Zmiana4Role);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (OleDbException)
        {
            // Tabela może nie istnieć.
        }
    }

    private static async Task DeleteShift4PersonnelAsync(
        OleDbConnection connection,
        CancellationToken cancellationToken)
    {
        var personnelIds = await LoadShift4PersonnelIdsAsync(connection, cancellationToken);
        foreach (var id in personnelIds)
        {
            await DeletePersonnelChildRowsAsync(connection, id, cancellationToken);
        }

        if (personnelIds.Count == 0)
        {
            return;
        }

        try
        {
            await using var command = new OleDbCommand(
                "DELETE FROM Funkcjonariusze WHERE NumerZmiany = 4",
                connection);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (OleDbException)
        {
            // Tabela może nie istnieć.
        }
    }

    private static async Task<List<int>> LoadShift4PersonnelIdsAsync(
        OleDbConnection connection,
        CancellationToken cancellationToken)
    {
        var ids = new List<int>();
        try
        {
            await using var command = new OleDbCommand(
                "SELECT Id FROM Funkcjonariusze WHERE NumerZmiany = 4",
                connection);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                ids.Add(reader.GetFieldInt32(0));
            }
        }
        catch (OleDbException)
        {
            // Tabela może nie istnieć.
        }

        return ids;
    }

    private static async Task DeletePersonnelChildRowsAsync(
        OleDbConnection connection,
        int id,
        CancellationToken cancellationToken)
    {
        foreach (var sql in new[]
        {
            "DELETE FROM FunkcjonariuszUprawnienia WHERE FunkcjonariuszId = ?",
            "DELETE FROM FunkcjonariuszOdznaczenia WHERE FunkcjonariuszId = ?"
        })
        {
            try
            {
                await using var command = new OleDbCommand(sql, connection);
                command.Parameters.AddWithValue("@p1", id);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (OleDbException)
            {
                // Tabela może nie istnieć.
            }
        }
    }
}
