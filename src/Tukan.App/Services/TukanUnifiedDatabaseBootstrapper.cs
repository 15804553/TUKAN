using System.Data.OleDb;
using System.IO;
using BOBER.Data;
using BOBER.Data.Database;
using Chomik.Data;
using Chomik.Data.Database;
using SKRYBEK.Data.Connections;
using BoberDatabaseBootstrapper = BOBER.Data.Database.DatabaseBootstrapper;
using ChomikDatabaseBootstrapper = Chomik.Data.Database.DatabaseBootstrapper;
using SkrybekDatabaseBootstrapper = SKRYBEK.Data.Database.DatabaseBootstrapper;

namespace Tukan.App.Services;

/// <summary>Tworzy pełny schemat trzech modułów w jednym pliku .accdb.</summary>
public static class TukanUnifiedDatabaseBootstrapper
{
    /// <summary>
    /// Podbij przy każdej zmianie schematu CHOMIK/BOBER/SKRYBEK, która wymaga EnsureReady.
    /// </summary>
    public const string SchemaVersion = "20260731-nazwa-samochodu";

    private const string SchemaVersionKey = "TukanSchemaVersion";

    public static async Task EnsureSchemaAsync(string unifiedPath, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(unifiedPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (File.Exists(unifiedPath) && await IsSchemaCurrentAsync(unifiedPath, cancellationToken))
        {
            return;
        }

        var chomikOptions = new DatabaseOptions
        {
            FilePath = unifiedPath,
            DatabasePassword = TukanDatabaseOptions.Password,
            UseDatabasePassword = true
        };

        var chomikBootstrapper = new ChomikDatabaseBootstrapper(chomikOptions);
        await chomikBootstrapper.EnsureReadyAsync(cancellationToken);

        var boberOptions = new BoberDatabaseOptions
        {
            FilePath = unifiedPath,
            DatabasePassword = TukanDatabaseOptions.Password,
            UseDatabasePassword = true
        };

        var boberBootstrapper = new BoberDatabaseBootstrapper(boberOptions);
        await boberBootstrapper.EnsureReadyAsync(cancellationToken);

        var skrybekFactory = new SkrybekConnectionFactory(unifiedPath);
        var skrybekBootstrapper = new SkrybekDatabaseBootstrapper(skrybekFactory);
        await skrybekBootstrapper.EnsureCreatedAsync();

        await MarkSchemaCurrentAsync(unifiedPath, cancellationToken);
    }

    private static async Task<bool> IsSchemaCurrentAsync(
        string unifiedPath,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = OpenConnection(unifiedPath);
            await connection.OpenAsync(cancellationToken);

            await using var command = new OleDbCommand(
                "SELECT Wartosc FROM Ustawienia WHERE Klucz = ?",
                connection);
            command.Parameters.AddWithValue("@p1", SchemaVersionKey);
            var value = await command.ExecuteScalarAsync(cancellationToken);
            return value is string version
                && string.Equals(version, SchemaVersion, StringComparison.Ordinal);
        }
        catch
        {
            // Brak tabeli / pierwsze uruchomienie — wymagany pełny bootstrap.
            return false;
        }
    }

    private static async Task MarkSchemaCurrentAsync(
        string unifiedPath,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = OpenConnection(unifiedPath);
            await connection.OpenAsync(cancellationToken);

            await using (var delete = new OleDbCommand(
                "DELETE FROM Ustawienia WHERE Klucz = ?",
                connection))
            {
                delete.Parameters.AddWithValue("@p1", SchemaVersionKey);
                await delete.ExecuteNonQueryAsync(cancellationToken);
            }

            await using var insert = new OleDbCommand(
                "INSERT INTO Ustawienia (Klucz, Wartosc) VALUES (?, ?)",
                connection);
            insert.Parameters.AddWithValue("@p1", SchemaVersionKey);
            insert.Parameters.AddWithValue("@p2", SchemaVersion);
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }
        catch
        {
            // Flaga przyspiesza kolejne starty — błąd zapisu nie powinien blokować uruchomienia.
        }
    }

    private static OleDbConnection OpenConnection(string databasePath) =>
        new(TukanDatabaseOptions.BuildConnectionString(databasePath));
}
