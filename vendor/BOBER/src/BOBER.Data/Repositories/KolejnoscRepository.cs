using System.Data.Common;
using System.Data.OleDb;
using BOBER.Core.Models;

namespace BOBER.Data.Repositories;

public sealed class KolejnoscRepository(BoberConnectionFactory connectionFactory) : IKolejnoscRepository
{
    public async Task<IReadOnlyList<KolejnoscFunkcjonariusza>> GetByZmianaAsync(
        int zmianaId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new OleDbCommand(
            "SELECT FunkcjonariuszId, ZmianaId, Pozycja FROM KolejnoscFunkcjonariuszy WHERE ZmianaId = ? ORDER BY Pozycja",
            connection);
        command.Parameters.AddWithValue("@p1", (short)zmianaId);

        var result = new List<KolejnoscFunkcjonariusza>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new KolejnoscFunkcjonariusza
            {
                FunkcjonariuszId = reader.GetFieldInt32(0),
                ZmianaId = reader.GetFieldInt32(1),
                Pozycja = reader.GetFieldInt32(2)
            });
        }

        return result;
    }

    public async Task SaveAsync(
        int zmianaId,
        IReadOnlyList<int> kolejnoscIds,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();

        await using var deleteCmd = new OleDbCommand(
            "DELETE FROM KolejnoscFunkcjonariuszy WHERE ZmianaId = ?", connection);
        deleteCmd.Parameters.AddWithValue("@p1", (short)zmianaId);
        await deleteCmd.ExecuteNonQueryAsync(cancellationToken);

        for (int i = 0; i < kolejnoscIds.Count; i++)
        {
            await using var insertCmd = new OleDbCommand(
                "INSERT INTO KolejnoscFunkcjonariuszy (FunkcjonariuszId, ZmianaId, Pozycja) VALUES (?, ?, ?)",
                connection);
            insertCmd.Parameters.AddWithValue("@p1", kolejnoscIds[i]);
            insertCmd.Parameters.AddWithValue("@p2", (short)zmianaId);
            insertCmd.Parameters.AddWithValue("@p3", (short)i);
            await insertCmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
