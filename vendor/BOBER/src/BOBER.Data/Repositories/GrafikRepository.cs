using System.Data.Common;
using System.Data.OleDb;
using BOBER.Core.Diagnostics;
using BOBER.Core.Models;

namespace BOBER.Data.Repositories;

public sealed class GrafikRepository(BoberConnectionFactory connectionFactory) : IGrafikRepository
{
    public async Task<IReadOnlyList<GrafikWpis>> GetByZmianaAndYearAsync(
        int zmianaId,
        int rok,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new OleDbCommand(
            "SELECT Id, FunkcjonariuszId, ZmianaId, Rok, Miesiac, Dzien, TypWpisu, IsAuto FROM GrafikWpisy WHERE ZmianaId = ? AND Rok = ?",
            connection);
        command.Parameters.AddWithValue("@p1", (short)zmianaId);
        command.Parameters.AddWithValue("@p2", (short)rok);
        return await ReadAllAsync(command, cancellationToken);
    }

    public async Task<IReadOnlyList<GrafikWpis>> GetByZmianaAndMonthAsync(
        int zmianaId,
        int rok,
        int miesiac,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new OleDbCommand(
            "SELECT Id, FunkcjonariuszId, ZmianaId, Rok, Miesiac, Dzien, TypWpisu, IsAuto FROM GrafikWpisy WHERE ZmianaId = ? AND Rok = ? AND Miesiac = ?",
            connection);
        command.Parameters.AddWithValue("@p1", (short)zmianaId);
        command.Parameters.AddWithValue("@p2", (short)rok);
        command.Parameters.AddWithValue("@p3", (short)miesiac);
        return await ReadAllAsync(command, cancellationToken);
    }

    public Task UpsertAsync(GrafikWpis wpis, CancellationToken cancellationToken = default) =>
        ApplyBatchAsync([wpis], [], cancellationToken);

    public async Task ApplyBatchAsync(
        IReadOnlyList<GrafikWpis> upserts,
        IReadOnlyList<GrafikWpis> deletes,
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
                "UPDATE GrafikWpisy SET TypWpisu = ?, IsAuto = ? WHERE FunkcjonariuszId = ? AND ZmianaId = ? AND Rok = ? AND Miesiac = ? AND Dzien = ?",
                connection,
                transaction);
            await using var insertCmd = new OleDbCommand(
                "INSERT INTO GrafikWpisy (FunkcjonariuszId, ZmianaId, Rok, Miesiac, Dzien, TypWpisu, IsAuto) VALUES (?, ?, ?, ?, ?, ?, ?)",
                connection,
                transaction);
            await using var deleteCmd = new OleDbCommand(
                "DELETE FROM GrafikWpisy WHERE FunkcjonariuszId = ? AND ZmianaId = ? AND Rok = ? AND Miesiac = ? AND Dzien = ?",
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
            "Grafik.ApplyBatch",
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
            [new GrafikWpis
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
            "DELETE FROM GrafikWpisy WHERE ZmianaId = ? AND Rok = ? AND Miesiac >= ? AND Miesiac <= ?",
            connection);
        command.Parameters.AddWithValue("@p1", (short)zmianaId);
        command.Parameters.AddWithValue("@p2", (short)rok);
        command.Parameters.AddWithValue("@p3", (short)miesiacOd);
        command.Parameters.AddWithValue("@p4", (short)miesiacDo);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<List<GrafikWpis>> ReadAllAsync(OleDbCommand command, CancellationToken cancellationToken)
    {
        var result = new List<GrafikWpis>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            result.Add(Map(reader));
        return result;
    }

    private static GrafikWpis Map(DbDataReader reader) => new()
    {
        Id = reader.GetFieldInt32(0),
        FunkcjonariuszId = reader.GetFieldInt32(1),
        ZmianaId = reader.GetFieldInt32(2),
        Rok = reader.GetFieldInt32(3),
        Miesiac = reader.GetFieldInt32(4),
        Dzien = reader.GetFieldInt32(5),
        TypWpisu = reader.GetString(6),
        IsAuto = reader.GetFieldBoolean(7)
    };

    private static void AddUpdateParameters(OleDbCommand command, GrafikWpis wpis)
    {
        command.Parameters.AddWithValue("@p1", wpis.TypWpisu);
        command.Parameters.AddWithValue("@p2", wpis.IsAuto);
        AddKeyParameters(command, wpis, 3);
    }

    private static void AddInsertParameters(OleDbCommand command, GrafikWpis wpis)
    {
        AddKeyParameters(command, wpis);
        command.Parameters.AddWithValue("@p6", wpis.TypWpisu);
        command.Parameters.AddWithValue("@p7", wpis.IsAuto);
    }

    private static void AddKeyParameters(OleDbCommand command, GrafikWpis wpis, int firstParameter = 1)
    {
        command.Parameters.AddWithValue($"@p{firstParameter}", wpis.FunkcjonariuszId);
        command.Parameters.AddWithValue($"@p{firstParameter + 1}", (short)wpis.ZmianaId);
        command.Parameters.AddWithValue($"@p{firstParameter + 2}", (short)wpis.Rok);
        command.Parameters.AddWithValue($"@p{firstParameter + 3}", (short)wpis.Miesiac);
        command.Parameters.AddWithValue($"@p{firstParameter + 4}", (short)wpis.Dzien);
    }
}
