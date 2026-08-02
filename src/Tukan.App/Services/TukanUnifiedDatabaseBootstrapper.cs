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
    public const string SchemaVersion = "20260802-pa-no-password";

    private const string SchemaVersionKey = "TukanSchemaVersion";

    public static async Task EnsureSchemaAsync(string unifiedPath, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(unifiedPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var workingPassword = await ResolveWorkingPasswordAsync(unifiedPath, cancellationToken);

        if (File.Exists(unifiedPath) && await IsSchemaCurrentAsync(unifiedPath, workingPassword, cancellationToken))
        {
            return;
        }

        var chomikOptions = new DatabaseOptions
        {
            FilePath = unifiedPath,
            DatabasePassword = workingPassword,
            UseDatabasePassword = true
        };

        var chomikBootstrapper = new ChomikDatabaseBootstrapper(chomikOptions);
        await chomikBootstrapper.EnsureReadyAsync(cancellationToken);

        var boberOptions = new BoberDatabaseOptions
        {
            FilePath = unifiedPath,
            DatabasePassword = workingPassword,
            UseDatabasePassword = true
        };

        var boberBootstrapper = new BoberDatabaseBootstrapper(boberOptions);
        await boberBootstrapper.EnsureReadyAsync(cancellationToken);

        var skrybekFactory = new SkrybekConnectionFactory(unifiedPath, workingPassword);
        var skrybekBootstrapper = new SkrybekDatabaseBootstrapper(skrybekFactory);
        await skrybekBootstrapper.EnsureCreatedAsync();

        await MarkSchemaCurrentAsync(unifiedPath, workingPassword, cancellationToken);
    }

    private static async Task<string> ResolveWorkingPasswordAsync(
        string unifiedPath,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(unifiedPath))
        {
            var password = TukanDatabaseOptions.ResolvePassword();
            TukanDatabaseOptions.RememberWorkingPassword(password);
            return password;
        }

        Exception? lastError = null;
        foreach (var candidate in TukanDatabaseOptions.GetPasswordCandidates())
        {
            try
            {
                await using var connection = new OleDbConnection(
                    TukanDatabaseOptions.BuildConnectionString(unifiedPath, candidate));
                await connection.OpenAsync(cancellationToken);
                TukanDatabaseOptions.RememberWorkingPassword(candidate);
                return candidate;
            }
            catch (Exception ex)
            {
                lastError = ex;
            }
        }

        throw new InvalidOperationException(
            $"Nie można otworzyć bazy TUKAN:\n{unifiedPath}",
            lastError);
    }

    private static async Task<bool> IsSchemaCurrentAsync(
        string unifiedPath,
        string password,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = OpenConnection(unifiedPath, password);
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
            return false;
        }
    }

    private static async Task MarkSchemaCurrentAsync(
        string unifiedPath,
        string password,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = OpenConnection(unifiedPath, password);
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

    private static OleDbConnection OpenConnection(string databasePath, string password) =>
        new(TukanDatabaseOptions.BuildConnectionString(databasePath, password));
}
