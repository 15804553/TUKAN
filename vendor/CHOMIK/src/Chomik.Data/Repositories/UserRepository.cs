using System.Data.Common;
using System.Data.OleDb;
using Chomik.Core.Enums;
using Chomik.Core.Models;
using Chomik.Data;

namespace Chomik.Data.Repositories;

public sealed class UserRepository(AccessConnectionFactory connectionFactory) : IUserRepository
{
    public async Task<UserAccount?> GetByLoginAsync(string login, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new OleDbCommand(
            "SELECT Id, Login, Rola, NumerZmiany, HasloHash, HasloSol FROM Uzytkownicy WHERE Login = ?",
            connection);
        command.Parameters.AddWithValue("@p1", login);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return Map(reader);
    }

    public async Task<IReadOnlyList<UserAccount>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new OleDbCommand(
            "SELECT Id, Login, Rola, NumerZmiany, HasloHash, HasloSol FROM Uzytkownicy ORDER BY Login",
            connection);

        var result = new List<UserAccount>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(Map(reader));
        }

        return result;
    }

    public async Task UpdatePasswordAsync(
        int userId,
        string hash,
        string salt,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new OleDbCommand(
            "UPDATE Uzytkownicy SET HasloHash = ?, HasloSol = ? WHERE Id = ?",
            connection);
        command.Parameters.AddWithValue("@p1", hash);
        command.Parameters.AddWithValue("@p2", salt);
        command.Parameters.AddWithValue("@p3", userId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static UserAccount Map(DbDataReader reader) => new()
    {
        Id = reader.GetFieldInt32(0),
        Login = reader.GetString(1),
        Role = (UserRole)reader.GetFieldInt32(2),
        NumerZmiany = reader.GetNullableFieldInt32(3),
        HasloHash = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
        HasloSol = reader.IsDBNull(5) ? string.Empty : reader.GetString(5)
    };
}
