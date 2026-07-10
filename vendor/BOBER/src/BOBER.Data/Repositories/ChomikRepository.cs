using System.Data;
using System.Data.Common;
using System.Data.OleDb;
using BOBER.Core.Enums;
using BOBER.Core.Models;

namespace BOBER.Data.Repositories;

public sealed class ChomikRepository(ChomikConnectionFactory connectionFactory) : IChomikRepository
{
    public async Task<IReadOnlyList<UserAccount>> GetUserAccountsAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new OleDbCommand(
            "SELECT Id, Login, Rola, NumerZmiany, HasloHash, HasloSol FROM Uzytkownicy ORDER BY Rola, Login",
            connection);

        var result = new List<UserAccount>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var numerZmiany = reader.IsDBNull(3) ? 0 : reader.GetFieldInt32(3);
            result.Add(new UserAccount
            {
                Id = reader.GetFieldInt32(0),
                Login = reader.GetString(1),
                NumerZmiany = numerZmiany,
                Role = MapShiftRole(numerZmiany),
                HasloHash = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                HasloSol = reader.IsDBNull(5) ? string.Empty : reader.GetString(5)
            });
        }

        return result;
    }

    private static UserRole MapShiftRole(int numerZmiany) => numerZmiany switch
    {
        1 => UserRole.Zmiana1,
        2 => UserRole.Zmiana2,
        3 => UserRole.Zmiana3,
        _ => UserRole.Zmiana1
    };

    private const string SelectSql = """
        SELECT f.Id, f.NumerZmiany, f.StopienId, f.StanowiskoId, s.Nazwa AS Stopien, f.Imie, f.Nazwisko,
               st.Nazwa AS Stanowisko
        FROM ((Funkcjonariusze AS f
        INNER JOIN StopnieSlownik AS s ON s.Id = f.StopienId)
        INNER JOIN StanowiskaSlownik AS st ON st.Id = f.StanowiskoId)
        """;

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = connectionFactory.CreateOpenConnection();
            return connection.State == ConnectionState.Open;
        }
        catch
        {
            return false;
        }
    }

    public async Task<IReadOnlyList<Funkcjonariusz>> GetByZmianaAsync(
        int zmianaId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new OleDbCommand(
            SelectSql + " WHERE f.NumerZmiany = ? ORDER BY f.Nazwisko, f.Imie",
            connection);
        command.Parameters.AddWithValue("@p1", (short)zmianaId);

        var result = new List<Funkcjonariusz>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            result.Add(MapBase(reader));

        await AttachUprawnieniaAsync(connection, result, cancellationToken);
        return result;
    }

    private static async Task AttachUprawnieniaAsync(
        OleDbConnection connection,
        IReadOnlyList<Funkcjonariusz> list,
        CancellationToken cancellationToken)
    {
        if (list.Count == 0) return;

        await using var command = new OleDbCommand(
            """
            SELECT fu.FunkcjonariuszId, tu.Nazwa, tu.Podtyp
            FROM FunkcjonariuszUprawnienia fu
            INNER JOIN TypyUprawnien tu ON tu.Id = fu.TypUprawnieniaId
            ORDER BY fu.FunkcjonariuszId
            """,
            connection);

        var lookup = list.ToDictionary(f => f.Id);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var fId = reader.GetFieldInt32(0);
            if (!lookup.TryGetValue(fId, out var f)) continue;

            var nazwa = reader.GetString(1);
            var podtyp = reader.IsDBNull(2) ? null : reader.GetString(2);
            var label = podtyp != null ? $"{nazwa} {podtyp}" : nazwa;
            f.NazwyUprawnien.Add(label);
        }
    }

    public async Task UpdateNrAsync(
        IReadOnlyDictionary<int, int> idToNr,
        CancellationToken cancellationToken = default)
    {
        if (idToNr.Count == 0) return;

        await using var connection = connectionFactory.CreateOpenConnection();
        foreach (var (id, nr) in idToNr)
        {
            await using var cmd = new OleDbCommand(
                "UPDATE Funkcjonariusze SET NumerPorzadkowy = ? WHERE Id = ?",
                connection);
            cmd.Parameters.AddWithValue("@p1", (short)nr);
            cmd.Parameters.AddWithValue("@p2", id);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static Funkcjonariusz MapBase(DbDataReader reader) => new()
    {
        Id = reader.GetFieldInt32(0),
        NumerZmiany = reader.GetFieldInt32(1),
        StopienId = reader.GetFieldInt32(2),
        StanowiskoId = reader.GetFieldInt32(3),
        Stopien = reader.GetString(4),
        Imie = reader.GetString(5),
        Nazwisko = reader.GetString(6),
        Stanowisko = reader.GetString(7)
    };
}
