using System.Data.OleDb;
using Chomik.Core.Models;
using Chomik.Data;

namespace Chomik.Data.Repositories;

public sealed class SlownikRepository(AccessConnectionFactory connectionFactory) : ISlownikRepository
{
    public Task<IReadOnlyList<SlownikItem>> GetStopnieAsync(CancellationToken cancellationToken = default) =>
        GetSlownikAsync("StopnieSlownik", cancellationToken);

    public Task<IReadOnlyList<SlownikItem>> GetStanowiskaAsync(CancellationToken cancellationToken = default) =>
        GetSlownikAsync("StanowiskaSlownik", cancellationToken);

    public async Task<IReadOnlyList<TypOdznaczenia>> GetTypyOdznaczenAsync(
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new OleDbCommand(
            "SELECT Id, Nazwa FROM TypyOdznaczen ORDER BY Nazwa",
            connection);

        var result = new List<TypOdznaczenia>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new TypOdznaczenia
            {
                Id = reader.GetFieldInt32(0),
                Nazwa = reader.GetString(1)
            });
        }

        return result;
    }

    public async Task<IReadOnlyList<TypUprawnienia>> GetTypyUprawnienAsync(
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new OleDbCommand(
            "SELECT Id, Nazwa, Podtyp, WymagaDaty FROM TypyUprawnien ORDER BY Nazwa, Podtyp",
            connection);

        var result = new List<TypUprawnienia>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new TypUprawnienia
            {
                Id = reader.GetFieldInt32(0),
                Nazwa = reader.GetString(1),
                Podtyp = reader.IsDBNull(2) ? null : reader.GetString(2),
                WymagaDaty = reader.GetFieldBoolean(3)
            });
        }

        return result;
    }

    public async Task<int> InsertTypUprawnieniaAsync(
        string nazwa,
        string? podtyp,
        bool wymagaDaty,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new OleDbCommand(
            "INSERT INTO TypyUprawnien (Nazwa, Podtyp, WymagaDaty) VALUES (?, ?, ?)",
            connection);
        command.Parameters.AddWithValue("@p1", nazwa.Trim());
        command.Parameters.AddWithValue("@p2", string.IsNullOrWhiteSpace(podtyp) ? DBNull.Value : podtyp.Trim());
        command.Parameters.AddWithValue("@p3", wymagaDaty);
        await command.ExecuteNonQueryAsync(cancellationToken);

        await using var idCommand = new OleDbCommand("SELECT @@IDENTITY", connection);
        return Convert.ToInt32(await idCommand.ExecuteScalarAsync(cancellationToken));
    }

    public async Task UpdateTypUprawnieniaAsync(
        int id,
        string nazwa,
        string? podtyp,
        bool wymagaDaty,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new OleDbCommand(
            "UPDATE TypyUprawnien SET Nazwa = ?, Podtyp = ?, WymagaDaty = ? WHERE Id = ?",
            connection);
        command.Parameters.AddWithValue("@p1", nazwa.Trim());
        command.Parameters.AddWithValue("@p2", string.IsNullOrWhiteSpace(podtyp) ? DBNull.Value : podtyp.Trim());
        command.Parameters.AddWithValue("@p3", wymagaDaty);
        command.Parameters.AddWithValue("@p4", id);
        var affected = await command.ExecuteNonQueryAsync(cancellationToken);
        if (affected == 0)
        {
            throw new InvalidOperationException("Nie znaleziono uprawnienia / kursu do aktualizacji.");
        }
    }

    private async Task<IReadOnlyList<SlownikItem>> GetSlownikAsync(
        string table,
        CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        var orderBy = table is "StopnieSlownik" or "StanowiskaSlownik" ? "Id" : "Nazwa";
        await using var command = new OleDbCommand($"SELECT Id, Nazwa FROM {table} ORDER BY {orderBy}", connection);

        var result = new List<SlownikItem>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new SlownikItem { Id = reader.GetFieldInt32(0), Nazwa = reader.GetString(1) });
        }

        return result;
    }
}
