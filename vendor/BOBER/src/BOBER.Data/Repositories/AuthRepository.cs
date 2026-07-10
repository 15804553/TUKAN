using System.Data.Common;
using System.Data.OleDb;
using BOBER.Core.Enums;
using BOBER.Core.Models;

namespace BOBER.Data.Repositories;

public sealed class AuthRepository(BoberConnectionFactory connectionFactory) : IAuthRepository
{
    public async Task<IReadOnlyList<UserAccount>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new OleDbCommand(
            "SELECT Id, Login, NumerZmiany, HasloHash, HasloSol FROM UzytkownicyBOBER ORDER BY NumerZmiany",
            connection);

        var result = new List<UserAccount>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            result.Add(Map(reader));

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
            "UPDATE UzytkownicyBOBER SET HasloHash = ?, HasloSol = ? WHERE Id = ?",
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
        NumerZmiany = reader.GetFieldInt32(2),
        Role = (UserRole)reader.GetFieldInt32(2),
        HasloHash = reader.GetString(3),
        HasloSol = reader.GetString(4)
    };
}
