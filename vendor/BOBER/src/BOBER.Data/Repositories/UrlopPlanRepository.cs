using System.Data.Common;
using System.Data.OleDb;
using BOBER.Core.Models;

namespace BOBER.Data.Repositories;

public sealed class UrlopPlanRepository(BoberConnectionFactory connectionFactory) : IUrlopPlanRepository
{
    public async Task<IReadOnlyList<UrlopPlanWpis>> GetByZmianaAndYearAsync(
        int zmianaId,
        int rok,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new OleDbCommand(
            "SELECT Id, FunkcjonariuszId, ZmianaId, Rok, Miesiac, Dzien, TypUrlopu FROM UrlopPlanWpisy WHERE ZmianaId = ? AND Rok = ?",
            connection);
        command.Parameters.AddWithValue("@p1", (short)zmianaId);
        command.Parameters.AddWithValue("@p2", (short)rok);
        return await ReadAllAsync(command, cancellationToken);
    }

    public async Task<IReadOnlyList<UrlopPlanWpis>> GetByZmianaAndMonthAsync(
        int zmianaId,
        int rok,
        int miesiac,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new OleDbCommand(
            "SELECT Id, FunkcjonariuszId, ZmianaId, Rok, Miesiac, Dzien, TypUrlopu FROM UrlopPlanWpisy WHERE ZmianaId = ? AND Rok = ? AND Miesiac = ?",
            connection);
        command.Parameters.AddWithValue("@p1", (short)zmianaId);
        command.Parameters.AddWithValue("@p2", (short)rok);
        command.Parameters.AddWithValue("@p3", (short)miesiac);
        return await ReadAllAsync(command, cancellationToken);
    }

    public async Task UpsertAsync(UrlopPlanWpis wpis, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();

        await using var checkCmd = new OleDbCommand(
            "SELECT COUNT(*) FROM UrlopPlanWpisy WHERE FunkcjonariuszId = ? AND ZmianaId = ? AND Rok = ? AND Miesiac = ? AND Dzien = ?",
            connection);
        checkCmd.Parameters.AddWithValue("@p1", wpis.FunkcjonariuszId);
        checkCmd.Parameters.AddWithValue("@p2", (short)wpis.ZmianaId);
        checkCmd.Parameters.AddWithValue("@p3", (short)wpis.Rok);
        checkCmd.Parameters.AddWithValue("@p4", (short)wpis.Miesiac);
        checkCmd.Parameters.AddWithValue("@p5", (short)wpis.Dzien);
        var exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync(cancellationToken)) > 0;

        if (exists)
        {
            await using var updateCmd = new OleDbCommand(
                "UPDATE UrlopPlanWpisy SET TypUrlopu = ? WHERE FunkcjonariuszId = ? AND ZmianaId = ? AND Rok = ? AND Miesiac = ? AND Dzien = ?",
                connection);
            updateCmd.Parameters.AddWithValue("@p1", wpis.TypUrlopu);
            updateCmd.Parameters.AddWithValue("@p2", wpis.FunkcjonariuszId);
            updateCmd.Parameters.AddWithValue("@p3", (short)wpis.ZmianaId);
            updateCmd.Parameters.AddWithValue("@p4", (short)wpis.Rok);
            updateCmd.Parameters.AddWithValue("@p5", (short)wpis.Miesiac);
            updateCmd.Parameters.AddWithValue("@p6", (short)wpis.Dzien);
            await updateCmd.ExecuteNonQueryAsync(cancellationToken);
        }
        else
        {
            await using var insertCmd = new OleDbCommand(
                "INSERT INTO UrlopPlanWpisy (FunkcjonariuszId, ZmianaId, Rok, Miesiac, Dzien, TypUrlopu) VALUES (?, ?, ?, ?, ?, ?)",
                connection);
            insertCmd.Parameters.AddWithValue("@p1", wpis.FunkcjonariuszId);
            insertCmd.Parameters.AddWithValue("@p2", (short)wpis.ZmianaId);
            insertCmd.Parameters.AddWithValue("@p3", (short)wpis.Rok);
            insertCmd.Parameters.AddWithValue("@p4", (short)wpis.Miesiac);
            insertCmd.Parameters.AddWithValue("@p5", (short)wpis.Dzien);
            insertCmd.Parameters.AddWithValue("@p6", wpis.TypUrlopu);
            await insertCmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public async Task DeleteAsync(
        int funkcjonariuszId,
        int zmianaId,
        int rok,
        int miesiac,
        int dzien,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new OleDbCommand(
            "DELETE FROM UrlopPlanWpisy WHERE FunkcjonariuszId = ? AND ZmianaId = ? AND Rok = ? AND Miesiac = ? AND Dzien = ?",
            connection);
        command.Parameters.AddWithValue("@p1", funkcjonariuszId);
        command.Parameters.AddWithValue("@p2", (short)zmianaId);
        command.Parameters.AddWithValue("@p3", (short)rok);
        command.Parameters.AddWithValue("@p4", (short)miesiac);
        command.Parameters.AddWithValue("@p5", (short)dzien);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteByHalfYearAsync(
        int zmianaId,
        int rok,
        int polrocze,
        CancellationToken cancellationToken = default)
    {
        var miesiacOd = polrocze == 1 ? 1 : 7;
        var miesiacDo = polrocze == 1 ? 6 : 12;

        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new OleDbCommand(
            "DELETE FROM UrlopPlanWpisy WHERE ZmianaId = ? AND Rok = ? AND Miesiac >= ? AND Miesiac <= ?",
            connection);
        command.Parameters.AddWithValue("@p1", (short)zmianaId);
        command.Parameters.AddWithValue("@p2", (short)rok);
        command.Parameters.AddWithValue("@p3", (short)miesiacOd);
        command.Parameters.AddWithValue("@p4", (short)miesiacDo);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteByYearAsync(
        int zmianaId,
        int rok,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new OleDbCommand(
            "DELETE FROM UrlopPlanWpisy WHERE ZmianaId = ? AND Rok = ?",
            connection);
        command.Parameters.AddWithValue("@p1", (short)zmianaId);
        command.Parameters.AddWithValue("@p2", (short)rok);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task ReplaceYearAsync(
        int zmianaId,
        int rok,
        IReadOnlyList<UrlopPlanWpis> wpisy,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();

        await using var deleteCmd = new OleDbCommand(
            "DELETE FROM UrlopPlanWpisy WHERE ZmianaId = ? AND Rok = ?",
            connection);
        deleteCmd.Parameters.AddWithValue("@p1", (short)zmianaId);
        deleteCmd.Parameters.AddWithValue("@p2", (short)rok);
        await deleteCmd.ExecuteNonQueryAsync(cancellationToken);

        foreach (var wpis in wpisy)
        {
            await using var insertCmd = new OleDbCommand(
                "INSERT INTO UrlopPlanWpisy (FunkcjonariuszId, ZmianaId, Rok, Miesiac, Dzien, TypUrlopu) VALUES (?, ?, ?, ?, ?, ?)",
                connection);
            insertCmd.Parameters.AddWithValue("@p1", wpis.FunkcjonariuszId);
            insertCmd.Parameters.AddWithValue("@p2", (short)zmianaId);
            insertCmd.Parameters.AddWithValue("@p3", (short)rok);
            insertCmd.Parameters.AddWithValue("@p4", (short)wpis.Miesiac);
            insertCmd.Parameters.AddWithValue("@p5", (short)wpis.Dzien);
            insertCmd.Parameters.AddWithValue("@p6", wpis.TypUrlopu);
            await insertCmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static async Task<List<UrlopPlanWpis>> ReadAllAsync(OleDbCommand command, CancellationToken cancellationToken)
    {
        var result = new List<UrlopPlanWpis>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            result.Add(Map(reader));
        return result;
    }

    private static UrlopPlanWpis Map(DbDataReader reader) => new()
    {
        Id = reader.GetFieldInt32(0),
        FunkcjonariuszId = reader.GetFieldInt32(1),
        ZmianaId = reader.GetFieldInt32(2),
        Rok = reader.GetFieldInt32(3),
        Miesiac = reader.GetFieldInt32(4),
        Dzien = reader.GetFieldInt32(5),
        TypUrlopu = reader.GetString(6)
    };
}
