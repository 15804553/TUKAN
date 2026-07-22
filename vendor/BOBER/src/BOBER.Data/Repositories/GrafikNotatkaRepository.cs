using System.Data.OleDb;
using BOBER.Core.Models;

namespace BOBER.Data.Repositories;

public sealed class GrafikNotatkaRepository(BoberConnectionFactory connectionFactory) : IGrafikNotatkaRepository
{
    public async Task<IReadOnlyList<GrafikNotatka>> GetByZmianaAndMonthAsync(
        int zmianaId,
        int rok,
        int miesiac,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new OleDbCommand(
            "SELECT Id, ZmianaId, Rok, Miesiac, Dzien, Tresc FROM GrafikNotatki WHERE ZmianaId = ? AND Rok = ? AND Miesiac = ?",
            connection);
        command.Parameters.AddWithValue("@p1", (short)zmianaId);
        command.Parameters.AddWithValue("@p2", (short)rok);
        command.Parameters.AddWithValue("@p3", (short)miesiac);

        var result = new List<GrafikNotatka>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new GrafikNotatka
            {
                Id = Convert.ToInt32(reader["Id"]),
                ZmianaId = Convert.ToInt32(reader["ZmianaId"]),
                Rok = Convert.ToInt32(reader["Rok"]),
                Miesiac = Convert.ToInt32(reader["Miesiac"]),
                Dzien = Convert.ToInt32(reader["Dzien"]),
                Tresc = reader["Tresc"]?.ToString() ?? string.Empty
            });
        }

        return result;
    }

    public async Task UpsertAsync(GrafikNotatka notatka, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();

        await using var checkCmd = new OleDbCommand(
            "SELECT COUNT(*) FROM GrafikNotatki WHERE ZmianaId = ? AND Rok = ? AND Miesiac = ? AND Dzien = ?",
            connection);
        checkCmd.Parameters.AddWithValue("@p1", (short)notatka.ZmianaId);
        checkCmd.Parameters.AddWithValue("@p2", (short)notatka.Rok);
        checkCmd.Parameters.AddWithValue("@p3", (short)notatka.Miesiac);
        checkCmd.Parameters.AddWithValue("@p4", (short)notatka.Dzien);
        var exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync(cancellationToken)) > 0;

        if (exists)
        {
            await using var updateCmd = new OleDbCommand(
                "UPDATE GrafikNotatki SET Tresc = ? WHERE ZmianaId = ? AND Rok = ? AND Miesiac = ? AND Dzien = ?",
                connection);
            updateCmd.Parameters.Add(new OleDbParameter("@p1", OleDbType.LongVarWChar) { Value = notatka.Tresc });
            updateCmd.Parameters.AddWithValue("@p2", (short)notatka.ZmianaId);
            updateCmd.Parameters.AddWithValue("@p3", (short)notatka.Rok);
            updateCmd.Parameters.AddWithValue("@p4", (short)notatka.Miesiac);
            updateCmd.Parameters.AddWithValue("@p5", (short)notatka.Dzien);
            await updateCmd.ExecuteNonQueryAsync(cancellationToken);
        }
        else
        {
            await using var insertCmd = new OleDbCommand(
                "INSERT INTO GrafikNotatki (ZmianaId, Rok, Miesiac, Dzien, Tresc) VALUES (?, ?, ?, ?, ?)",
                connection);
            insertCmd.Parameters.AddWithValue("@p1", (short)notatka.ZmianaId);
            insertCmd.Parameters.AddWithValue("@p2", (short)notatka.Rok);
            insertCmd.Parameters.AddWithValue("@p3", (short)notatka.Miesiac);
            insertCmd.Parameters.AddWithValue("@p4", (short)notatka.Dzien);
            insertCmd.Parameters.Add(new OleDbParameter("@p5", OleDbType.LongVarWChar) { Value = notatka.Tresc });
            await insertCmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public async Task DeleteAsync(
        int zmianaId,
        int rok,
        int miesiac,
        int dzien,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new OleDbCommand(
            "DELETE FROM GrafikNotatki WHERE ZmianaId = ? AND Rok = ? AND Miesiac = ? AND Dzien = ?",
            connection);
        command.Parameters.AddWithValue("@p1", (short)zmianaId);
        command.Parameters.AddWithValue("@p2", (short)rok);
        command.Parameters.AddWithValue("@p3", (short)miesiac);
        command.Parameters.AddWithValue("@p4", (short)dzien);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
