using System.Data.OleDb;
using BOBER.Core.Models;

namespace BOBER.Data.Repositories;

public sealed class KalendarzRepository(BoberConnectionFactory connectionFactory) : IKalendarzRepository
{
    public async Task<IReadOnlyList<KalendarzWpis>> GetByMonthAsync(
        int rok,
        int miesiac,
        int? zmianaFilter = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();

        // Zakres dat zamiast YEAR()/MONTH() — Access lepiej porównuje OleDbType.Date.
        var from = new DateTime(rok, miesiac, 1);
        var to = from.AddMonths(1);

        var sql =
            """
            SELECT Id, Data, ZmianaId, Tresc, AutorLogin, DataUtworzenia, DataModyfikacji
            FROM KalendarzWpisy
            WHERE Data >= ? AND Data < ?
            """;
        if (zmianaFilter is not null)
            sql += " AND ZmianaId = ?";

        await using var command = new OleDbCommand(sql, connection);
        AddDate(command, from);
        AddDate(command, to);
        if (zmianaFilter is not null)
            AddShort(command, zmianaFilter.Value);

        var result = new List<KalendarzWpis>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(ReadWpis(reader));
        }

        foreach (var wpis in result)
        {
            wpis.Odczyt = await GetOdczytInternalAsync(connection, wpis.Id, wpis.ZmianaId, cancellationToken);
        }

        return result;
    }

    public async Task<KalendarzWpis?> GetByDateAndZmianaAsync(
        DateOnly data,
        int zmianaId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new OleDbCommand(
            """
            SELECT Id, Data, ZmianaId, Tresc, AutorLogin, DataUtworzenia, DataModyfikacji
            FROM KalendarzWpisy
            WHERE Data = ? AND ZmianaId = ?
            """,
            connection);
        AddDate(command, data.ToDateTime(TimeOnly.MinValue));
        AddShort(command, zmianaId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;

        var wpis = ReadWpis(reader);
        await reader.CloseAsync();
        wpis.Odczyt = await GetOdczytInternalAsync(connection, wpis.Id, wpis.ZmianaId, cancellationToken);
        return wpis;
    }

    public async Task<int> UpsertAsync(KalendarzWpis wpis, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();

        await using var checkCmd = new OleDbCommand(
            "SELECT Id, Tresc FROM KalendarzWpisy WHERE Data = ? AND ZmianaId = ?",
            connection);
        AddDate(checkCmd, wpis.Data.ToDateTime(TimeOnly.MinValue));
        AddShort(checkCmd, wpis.ZmianaId);

        int? existingId = null;
        string? existingTresc = null;
        await using (var reader = await checkCmd.ExecuteReaderAsync(cancellationToken))
        {
            if (await reader.ReadAsync(cancellationToken))
            {
                existingId = Convert.ToInt32(reader["Id"]);
                existingTresc = reader["Tresc"]?.ToString();
            }
        }

        var now = DateTime.Now;
        if (existingId is int id)
        {
            await using var updateCmd = new OleDbCommand(
                """
                UPDATE KalendarzWpisy
                SET Tresc = ?, AutorLogin = ?, DataModyfikacji = ?
                WHERE Id = ?
                """,
                connection);
            AddMemo(updateCmd, wpis.Tresc);
            AddVarWChar(updateCmd, wpis.AutorLogin);
            AddDate(updateCmd, now);
            AddLong(updateCmd, id);
            await updateCmd.ExecuteNonQueryAsync(cancellationToken);

            if (!string.Equals(existingTresc, wpis.Tresc, StringComparison.Ordinal))
                await ResetOdczytInternalAsync(connection, id, cancellationToken);

            return id;
        }

        await using var insertCmd = new OleDbCommand(
            """
            INSERT INTO KalendarzWpisy (Data, ZmianaId, Tresc, AutorLogin, DataUtworzenia, DataModyfikacji)
            VALUES (?, ?, ?, ?, ?, ?)
            """,
            connection);
        AddDate(insertCmd, wpis.Data.ToDateTime(TimeOnly.MinValue));
        AddShort(insertCmd, wpis.ZmianaId);
        AddMemo(insertCmd, wpis.Tresc);
        AddVarWChar(insertCmd, wpis.AutorLogin);
        AddDate(insertCmd, now);
        AddDate(insertCmd, now);
        await insertCmd.ExecuteNonQueryAsync(cancellationToken);

        await using var identityCmd = new OleDbCommand("SELECT @@IDENTITY", connection);
        return Convert.ToInt32(await identityCmd.ExecuteScalarAsync(cancellationToken));
    }

    public async Task DeleteAsync(int wpisId, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        await ResetOdczytInternalAsync(connection, wpisId, cancellationToken);

        await using var command = new OleDbCommand(
            "DELETE FROM KalendarzWpisy WHERE Id = ?",
            connection);
        AddLong(command, wpisId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteByDateAndZmianaAsync(
        DateOnly data,
        int zmianaId,
        CancellationToken cancellationToken = default)
    {
        var existing = await GetByDateAndZmianaAsync(data, zmianaId, cancellationToken);
        if (existing is null)
            return;

        await DeleteAsync(existing.Id, cancellationToken);
    }

    public async Task ResetOdczytAsync(int wpisId, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        await ResetOdczytInternalAsync(connection, wpisId, cancellationToken);
    }

    public async Task MarkAsReadAsync(
        int wpisId,
        int zmianaId,
        string login,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        var existing = await GetOdczytInternalAsync(connection, wpisId, zmianaId, cancellationToken);
        var now = DateTime.Now;

        if (existing is not null)
        {
            if (existing.Przeczytane)
                return;

            await using var updateCmd = new OleDbCommand(
                """
                UPDATE KalendarzOdczyty
                SET Przeczytane = ?, PrzeczytanePrzez = ?, DataOdczytu = ?
                WHERE WpisId = ? AND ZmianaId = ?
                """,
                connection);
            AddYesNo(updateCmd, true);
            AddVarWChar(updateCmd, login);
            AddDate(updateCmd, now);
            AddLong(updateCmd, wpisId);
            AddShort(updateCmd, zmianaId);
            await updateCmd.ExecuteNonQueryAsync(cancellationToken);
            return;
        }

        await using var insertCmd = new OleDbCommand(
            """
            INSERT INTO KalendarzOdczyty (WpisId, ZmianaId, Przeczytane, PrzeczytanePrzez, DataOdczytu)
            VALUES (?, ?, ?, ?, ?)
            """,
            connection);
        AddLong(insertCmd, wpisId);
        AddShort(insertCmd, zmianaId);
        AddYesNo(insertCmd, true);
        AddVarWChar(insertCmd, login);
        AddDate(insertCmd, now);
        await insertCmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<KalendarzOdczyt?> GetOdczytAsync(
        int wpisId,
        int zmianaId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        return await GetOdczytInternalAsync(connection, wpisId, zmianaId, cancellationToken);
    }

    private static async Task<KalendarzOdczyt?> GetOdczytInternalAsync(
        OleDbConnection connection,
        int wpisId,
        int zmianaId,
        CancellationToken cancellationToken)
    {
        await using var command = new OleDbCommand(
            """
            SELECT WpisId, ZmianaId, Przeczytane, PrzeczytanePrzez, DataOdczytu
            FROM KalendarzOdczyty
            WHERE WpisId = ? AND ZmianaId = ?
            """,
            connection);
        AddLong(command, wpisId);
        AddShort(command, zmianaId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return new KalendarzOdczyt
        {
            WpisId = Convert.ToInt32(reader["WpisId"]),
            ZmianaId = Convert.ToInt32(reader["ZmianaId"]),
            Przeczytane = Convert.ToBoolean(reader["Przeczytane"]),
            PrzeczytanePrzez = reader["PrzeczytanePrzez"] is DBNull
                ? null
                : reader["PrzeczytanePrzez"]?.ToString(),
            DataOdczytu = reader["DataOdczytu"] is DBNull or null
                ? null
                : Convert.ToDateTime(reader["DataOdczytu"])
        };
    }

    private static async Task ResetOdczytInternalAsync(
        OleDbConnection connection,
        int wpisId,
        CancellationToken cancellationToken)
    {
        await using var command = new OleDbCommand(
            "DELETE FROM KalendarzOdczyty WHERE WpisId = ?",
            connection);
        AddLong(command, wpisId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static KalendarzWpis ReadWpis(System.Data.Common.DbDataReader reader)
    {
        var data = Convert.ToDateTime(reader["Data"]);
        return new KalendarzWpis
        {
            Id = Convert.ToInt32(reader["Id"]),
            Data = DateOnly.FromDateTime(data),
            ZmianaId = Convert.ToInt32(reader["ZmianaId"]),
            Tresc = reader["Tresc"]?.ToString() ?? string.Empty,
            AutorLogin = reader["AutorLogin"]?.ToString() ?? string.Empty,
            DataUtworzenia = Convert.ToDateTime(reader["DataUtworzenia"]),
            DataModyfikacji = Convert.ToDateTime(reader["DataModyfikacji"])
        };
    }

    private static void AddDate(OleDbCommand command, DateTime value) =>
        command.Parameters.Add(new OleDbParameter { OleDbType = OleDbType.Date, Value = value });

    private static void AddShort(OleDbCommand command, int value) =>
        command.Parameters.Add(new OleDbParameter { OleDbType = OleDbType.SmallInt, Value = (short)value });

    private static void AddLong(OleDbCommand command, int value) =>
        command.Parameters.Add(new OleDbParameter { OleDbType = OleDbType.Integer, Value = value });

    private static void AddYesNo(OleDbCommand command, bool value) =>
        command.Parameters.Add(new OleDbParameter { OleDbType = OleDbType.Boolean, Value = value });

    private static void AddMemo(OleDbCommand command, string value) =>
        command.Parameters.Add(new OleDbParameter { OleDbType = OleDbType.LongVarWChar, Value = value });

    private static void AddVarWChar(OleDbCommand command, string value) =>
        command.Parameters.Add(new OleDbParameter { OleDbType = OleDbType.VarWChar, Value = value });
}
