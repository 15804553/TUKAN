using System.Data.Common;
using System.Data.OleDb;
using BOBER.Core.Diagnostics;
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

    public Task UpsertAsync(UrlopPlanWpis wpis, CancellationToken cancellationToken = default) =>
        ApplyBatchAsync([wpis], [], cancellationToken);

    public async Task ApplyBatchAsync(
        IReadOnlyList<UrlopPlanWpis> upserts,
        IReadOnlyList<UrlopPlanWpis> deletes,
        CancellationToken cancellationToken = default)
    {
        if (upserts.Count == 0 && deletes.Count == 0)
            return;

        var stopwatch = PerformanceDiagnostics.Start();
        await using var connection = connectionFactory.CreateOpenConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            await using var updateCmd = new OleDbCommand(
                "UPDATE UrlopPlanWpisy SET TypUrlopu = ? WHERE FunkcjonariuszId = ? AND ZmianaId = ? AND Rok = ? AND Miesiac = ? AND Dzien = ?",
                connection,
                transaction);
            await using var insertCmd = new OleDbCommand(
                "INSERT INTO UrlopPlanWpisy (FunkcjonariuszId, ZmianaId, Rok, Miesiac, Dzien, TypUrlopu) VALUES (?, ?, ?, ?, ?, ?)",
                connection,
                transaction);
            await using var deleteCmd = new OleDbCommand(
                "DELETE FROM UrlopPlanWpisy WHERE FunkcjonariuszId = ? AND ZmianaId = ? AND Rok = ? AND Miesiac = ? AND Dzien = ?",
                connection,
                transaction);

            foreach (var wpis in upserts)
            {
                AddUpdateParameters(updateCmd, wpis);
                var affected = await updateCmd.ExecuteNonQueryAsync(cancellationToken);
                updateCmd.Parameters.Clear();
                if (affected == 0)
                {
                    AddInsertParameters(insertCmd, wpis);
                    await insertCmd.ExecuteNonQueryAsync(cancellationToken);
                    insertCmd.Parameters.Clear();
                }
            }

            foreach (var wpis in deletes)
            {
                AddKeyParameters(deleteCmd, wpis);
                await deleteCmd.ExecuteNonQueryAsync(cancellationToken);
                deleteCmd.Parameters.Clear();
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }

        PerformanceDiagnostics.Log(
            "UrlopPlan.ApplyBatch",
            "SQL",
            stopwatch,
            upserts.Count + deletes.Count);
    }

    public async Task DeleteAsync(
        int funkcjonariuszId,
        int zmianaId,
        int rok,
        int miesiac,
        int dzien,
        CancellationToken cancellationToken = default)
    {
        await ApplyBatchAsync(
            [],
            [new UrlopPlanWpis
            {
                FunkcjonariuszId = funkcjonariuszId,
                ZmianaId = zmianaId,
                Rok = rok,
                Miesiac = miesiac,
                Dzien = dzien
            }],
            cancellationToken);
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
        using var transaction = connection.BeginTransaction();

        try
        {
            await using var deleteCmd = new OleDbCommand(
                "DELETE FROM UrlopPlanWpisy WHERE ZmianaId = ? AND Rok = ?",
                connection,
                transaction);
            deleteCmd.Parameters.AddWithValue("@p1", (short)zmianaId);
            deleteCmd.Parameters.AddWithValue("@p2", (short)rok);
            await deleteCmd.ExecuteNonQueryAsync(cancellationToken);

            await using var insertCmd = new OleDbCommand(
                "INSERT INTO UrlopPlanWpisy (FunkcjonariuszId, ZmianaId, Rok, Miesiac, Dzien, TypUrlopu) VALUES (?, ?, ?, ?, ?, ?)",
                connection,
                transaction);
            foreach (var wpis in wpisy)
            {
                AddInsertParameters(insertCmd, wpis, zmianaId, rok);
                await insertCmd.ExecuteNonQueryAsync(cancellationToken);
                insertCmd.Parameters.Clear();
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
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

    private static void AddUpdateParameters(OleDbCommand command, UrlopPlanWpis wpis)
    {
        command.Parameters.AddWithValue("@p1", wpis.TypUrlopu);
        AddKeyParameters(command, wpis, 2);
    }

    private static void AddInsertParameters(
        OleDbCommand command,
        UrlopPlanWpis wpis,
        int? zmianaId = null,
        int? rok = null)
    {
        command.Parameters.AddWithValue("@p1", wpis.FunkcjonariuszId);
        command.Parameters.AddWithValue("@p2", (short)(zmianaId ?? wpis.ZmianaId));
        command.Parameters.AddWithValue("@p3", (short)(rok ?? wpis.Rok));
        command.Parameters.AddWithValue("@p4", (short)wpis.Miesiac);
        command.Parameters.AddWithValue("@p5", (short)wpis.Dzien);
        command.Parameters.AddWithValue("@p6", wpis.TypUrlopu);
    }

    private static void AddKeyParameters(OleDbCommand command, UrlopPlanWpis wpis, int firstParameter = 1)
    {
        command.Parameters.AddWithValue($"@p{firstParameter}", wpis.FunkcjonariuszId);
        command.Parameters.AddWithValue($"@p{firstParameter + 1}", (short)wpis.ZmianaId);
        command.Parameters.AddWithValue($"@p{firstParameter + 2}", (short)wpis.Rok);
        command.Parameters.AddWithValue($"@p{firstParameter + 3}", (short)wpis.Miesiac);
        command.Parameters.AddWithValue($"@p{firstParameter + 4}", (short)wpis.Dzien);
    }
}
