using System.Data.OleDb;
using BOBER.Core.Models;

namespace BOBER.Data.Repositories;

public sealed class ObsadaFunkcjiUwagaMiesiecznaRepository(BoberConnectionFactory connectionFactory)
    : IObsadaFunkcjiUwagaMiesiecznaRepository
{
    public async Task<IReadOnlyList<ObsadaFunkcjiUwagaMiesieczna>> GetByZmianaAndMonthAsync(
        int zmianaId,
        int rok,
        int miesiac,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new OleDbCommand(
            """
            SELECT Id, FunkcjonariuszId, ZmianaId, Rok, Miesiac, Tresc
            FROM ObsadaFunkcjiUwagiMiesieczne
            WHERE ZmianaId = ? AND Rok = ? AND Miesiac = ?
            """,
            connection);
        command.Parameters.AddWithValue("@p1", (short)zmianaId);
        command.Parameters.AddWithValue("@p2", (short)rok);
        command.Parameters.AddWithValue("@p3", (short)miesiac);

        var result = new List<ObsadaFunkcjiUwagaMiesieczna>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new ObsadaFunkcjiUwagaMiesieczna
            {
                Id = Convert.ToInt32(reader["Id"]),
                FunkcjonariuszId = Convert.ToInt32(reader["FunkcjonariuszId"]),
                ZmianaId = Convert.ToInt32(reader["ZmianaId"]),
                Rok = Convert.ToInt32(reader["Rok"]),
                Miesiac = Convert.ToInt32(reader["Miesiac"]),
                Tresc = reader["Tresc"]?.ToString() ?? string.Empty
            });
        }

        return result;
    }

    public async Task UpsertAsync(ObsadaFunkcjiUwagaMiesieczna uwaga, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();

        await using var checkCmd = new OleDbCommand(
            """
            SELECT COUNT(*) FROM ObsadaFunkcjiUwagiMiesieczne
            WHERE FunkcjonariuszId = ? AND ZmianaId = ? AND Rok = ? AND Miesiac = ?
            """,
            connection);
        checkCmd.Parameters.AddWithValue("@p1", uwaga.FunkcjonariuszId);
        checkCmd.Parameters.AddWithValue("@p2", (short)uwaga.ZmianaId);
        checkCmd.Parameters.AddWithValue("@p3", (short)uwaga.Rok);
        checkCmd.Parameters.AddWithValue("@p4", (short)uwaga.Miesiac);
        var exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync(cancellationToken)) > 0;

        if (exists)
        {
            await using var updateCmd = new OleDbCommand(
                """
                UPDATE ObsadaFunkcjiUwagiMiesieczne SET Tresc = ?
                WHERE FunkcjonariuszId = ? AND ZmianaId = ? AND Rok = ? AND Miesiac = ?
                """,
                connection);
            updateCmd.Parameters.Add(new OleDbParameter("@p1", OleDbType.LongVarWChar) { Value = uwaga.Tresc });
            updateCmd.Parameters.AddWithValue("@p2", uwaga.FunkcjonariuszId);
            updateCmd.Parameters.AddWithValue("@p3", (short)uwaga.ZmianaId);
            updateCmd.Parameters.AddWithValue("@p4", (short)uwaga.Rok);
            updateCmd.Parameters.AddWithValue("@p5", (short)uwaga.Miesiac);
            await updateCmd.ExecuteNonQueryAsync(cancellationToken);
        }
        else
        {
            await using var insertCmd = new OleDbCommand(
                """
                INSERT INTO ObsadaFunkcjiUwagiMiesieczne (FunkcjonariuszId, ZmianaId, Rok, Miesiac, Tresc)
                VALUES (?, ?, ?, ?, ?)
                """,
                connection);
            insertCmd.Parameters.AddWithValue("@p1", uwaga.FunkcjonariuszId);
            insertCmd.Parameters.AddWithValue("@p2", (short)uwaga.ZmianaId);
            insertCmd.Parameters.AddWithValue("@p3", (short)uwaga.Rok);
            insertCmd.Parameters.AddWithValue("@p4", (short)uwaga.Miesiac);
            insertCmd.Parameters.Add(new OleDbParameter("@p5", OleDbType.LongVarWChar) { Value = uwaga.Tresc });
            await insertCmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public async Task DeleteAsync(
        int funkcjonariuszId,
        int zmianaId,
        int rok,
        int miesiac,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new OleDbCommand(
            """
            DELETE FROM ObsadaFunkcjiUwagiMiesieczne
            WHERE FunkcjonariuszId = ? AND ZmianaId = ? AND Rok = ? AND Miesiac = ?
            """,
            connection);
        command.Parameters.AddWithValue("@p1", funkcjonariuszId);
        command.Parameters.AddWithValue("@p2", (short)zmianaId);
        command.Parameters.AddWithValue("@p3", (short)rok);
        command.Parameters.AddWithValue("@p4", (short)miesiac);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
