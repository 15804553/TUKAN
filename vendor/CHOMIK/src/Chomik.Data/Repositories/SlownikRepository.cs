using System.Data.OleDb;
using Chomik.Core.Models;
using Chomik.Data;

namespace Chomik.Data.Repositories;

public sealed class SlownikRepository(AccessConnectionFactory connectionFactory) : ISlownikRepository
{
    public Task<IReadOnlyList<SlownikItem>> GetStopnieAsync(CancellationToken cancellationToken = default) =>
        GetNazwaSlownikAsync("StopnieSlownik", orderById: true, cancellationToken);

    public Task<IReadOnlyList<SlownikItem>> GetStanowiskaAsync(CancellationToken cancellationToken = default) =>
        GetNazwaSlownikAsync("StanowiskaSlownik", orderById: true, cancellationToken);

    public Task<int> InsertStopienAsync(string nazwa, CancellationToken cancellationToken = default) =>
        InsertNazwaAsync("StopnieSlownik", nazwa, cancellationToken);

    public Task UpdateStopienAsync(int id, string nazwa, CancellationToken cancellationToken = default) =>
        UpdateNazwaAsync("StopnieSlownik", id, nazwa, "stopnia", cancellationToken);

    public Task DeleteStopienAsync(int id, CancellationToken cancellationToken = default) =>
        DeleteNazwaAsync("StopnieSlownik", id, "stopnia", cancellationToken);

    public Task<int> CountFunkcjonariuszeByStopienAsync(int id, CancellationToken cancellationToken = default) =>
        CountByColumnAsync("Funkcjonariusze", "StopienId", id, cancellationToken);

    public Task<int> InsertStanowiskoAsync(string nazwa, CancellationToken cancellationToken = default) =>
        InsertNazwaAsync("StanowiskaSlownik", nazwa, cancellationToken);

    public Task UpdateStanowiskoAsync(int id, string nazwa, CancellationToken cancellationToken = default) =>
        UpdateNazwaAsync("StanowiskaSlownik", id, nazwa, "stanowiska", cancellationToken);

    public Task DeleteStanowiskoAsync(int id, CancellationToken cancellationToken = default) =>
        DeleteNazwaAsync("StanowiskaSlownik", id, "stanowiska", cancellationToken);

    public Task<int> CountFunkcjonariuszeByStanowiskoAsync(int id, CancellationToken cancellationToken = default) =>
        CountByColumnAsync("Funkcjonariusze", "StanowiskoId", id, cancellationToken);

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

    public Task<int> InsertTypOdznaczeniaAsync(string nazwa, CancellationToken cancellationToken = default) =>
        InsertNazwaAsync("TypyOdznaczen", nazwa, cancellationToken);

    public Task UpdateTypOdznaczeniaAsync(int id, string nazwa, CancellationToken cancellationToken = default) =>
        UpdateNazwaAsync("TypyOdznaczen", id, nazwa, "odznaczenia", cancellationToken);

    public async Task DeleteTypOdznaczeniaAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        await using (var clear = new OleDbCommand(
            "DELETE FROM FunkcjonariuszOdznaczenia WHERE TypOdznaczeniaId = ?",
            connection))
        {
            clear.Parameters.AddWithValue("@p1", id);
            await clear.ExecuteNonQueryAsync(cancellationToken);
        }

        await using var delete = new OleDbCommand("DELETE FROM TypyOdznaczen WHERE Id = ?", connection);
        delete.Parameters.AddWithValue("@p1", id);
        var affected = await delete.ExecuteNonQueryAsync(cancellationToken);
        if (affected == 0)
        {
            throw new InvalidOperationException("Nie znaleziono odznaczenia do usunięcia.");
        }
    }

    public Task<int> CountOdznaczeniaAssignmentsAsync(int id, CancellationToken cancellationToken = default) =>
        CountByColumnAsync("FunkcjonariuszOdznaczenia", "TypOdznaczeniaId", id, cancellationToken);

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

    private async Task<IReadOnlyList<SlownikItem>> GetNazwaSlownikAsync(
        string table,
        bool orderById,
        CancellationToken cancellationToken)
    {
        table = RequireTable(table);
        var orderBy = orderById ? "Id" : "Nazwa";
        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new OleDbCommand($"SELECT Id, Nazwa FROM {table} ORDER BY {orderBy}", connection);

        var result = new List<SlownikItem>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new SlownikItem { Id = reader.GetFieldInt32(0), Nazwa = reader.GetString(1) });
        }

        return result;
    }

    private async Task<int> InsertNazwaAsync(
        string table,
        string nazwa,
        CancellationToken cancellationToken)
    {
        table = RequireTable(table);
        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new OleDbCommand($"INSERT INTO {table} (Nazwa) VALUES (?)", connection);
        command.Parameters.AddWithValue("@p1", nazwa.Trim());
        await command.ExecuteNonQueryAsync(cancellationToken);

        await using var idCommand = new OleDbCommand("SELECT @@IDENTITY", connection);
        return Convert.ToInt32(await idCommand.ExecuteScalarAsync(cancellationToken));
    }

    private async Task UpdateNazwaAsync(
        string table,
        int id,
        string nazwa,
        string entityGenitive,
        CancellationToken cancellationToken)
    {
        table = RequireTable(table);
        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new OleDbCommand($"UPDATE {table} SET Nazwa = ? WHERE Id = ?", connection);
        command.Parameters.AddWithValue("@p1", nazwa.Trim());
        command.Parameters.AddWithValue("@p2", id);
        var affected = await command.ExecuteNonQueryAsync(cancellationToken);
        if (affected == 0)
        {
            throw new InvalidOperationException($"Nie znaleziono {entityGenitive} do aktualizacji.");
        }
    }

    private async Task DeleteNazwaAsync(
        string table,
        int id,
        string entityGenitive,
        CancellationToken cancellationToken)
    {
        table = RequireTable(table);
        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new OleDbCommand($"DELETE FROM {table} WHERE Id = ?", connection);
        command.Parameters.AddWithValue("@p1", id);
        var affected = await command.ExecuteNonQueryAsync(cancellationToken);
        if (affected == 0)
        {
            throw new InvalidOperationException($"Nie znaleziono {entityGenitive} do usunięcia.");
        }
    }

    private async Task<int> CountByColumnAsync(
        string table,
        string column,
        int id,
        CancellationToken cancellationToken)
    {
        table = RequireUsageTable(table);
        column = RequireUsageColumn(column);
        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new OleDbCommand(
            $"SELECT COUNT(*) FROM {table} WHERE {column} = ?",
            connection);
        command.Parameters.AddWithValue("@p1", id);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    private static string RequireTable(string table) => table switch
    {
        "StopnieSlownik" or "StanowiskaSlownik" or "TypyOdznaczen" => table,
        _ => throw new ArgumentOutOfRangeException(nameof(table), table, "Niedozwolona tabela słownika.")
    };

    private static string RequireUsageTable(string table) => table switch
    {
        "Funkcjonariusze" or "FunkcjonariuszOdznaczenia" => table,
        _ => throw new ArgumentOutOfRangeException(nameof(table), table, "Niedozwolona tabela użycia.")
    };

    private static string RequireUsageColumn(string column) => column switch
    {
        "StopienId" or "StanowiskoId" or "TypOdznaczeniaId" => column,
        _ => throw new ArgumentOutOfRangeException(nameof(column), column, "Niedozwolona kolumna użycia.")
    };
}
