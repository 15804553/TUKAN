using System.Data.OleDb;

namespace BOBER.Data.Repositories;

public sealed class UstawieniaRepository(BoberConnectionFactory connectionFactory) : IUstawieniaRepository
{
    public async Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new OleDbCommand(
            "SELECT Wartosc FROM Ustawienia WHERE Klucz = ?", connection);
        command.Parameters.AddWithValue("@p1", key);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null or DBNull ? null : Convert.ToString(value);
    }

    public async Task<int> GetIntAsync(
        string key,
        int defaultValue = 0,
        CancellationToken cancellationToken = default)
    {
        var value = await GetAsync(key, cancellationToken);
        return value is null ? defaultValue : int.TryParse(value, out var parsed) ? parsed : defaultValue;
    }

    public async Task SetAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();

        await using var update = new OleDbCommand(
            "UPDATE Ustawienia SET Wartosc = ? WHERE Klucz = ?", connection);
        update.Parameters.AddWithValue("@p1", value);
        update.Parameters.AddWithValue("@p2", key);
        var affected = await update.ExecuteNonQueryAsync(cancellationToken);

        if (affected == 0)
        {
            await using var insert = new OleDbCommand(
                "INSERT INTO Ustawienia (Klucz, Wartosc) VALUES (?, ?)", connection);
            insert.Parameters.AddWithValue("@p1", key);
            insert.Parameters.AddWithValue("@p2", value);
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
