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
    public const string SchemaVersion = "20260911-flaga-kolor-czcionki";

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

        await EnsurePerformanceIndexesAsync(unifiedPath, workingPassword, cancellationToken);
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

    private static async Task EnsurePerformanceIndexesAsync(
        string unifiedPath,
        string password,
        CancellationToken cancellationToken)
    {
        await using var connection = OpenConnection(unifiedPath, password);
        await connection.OpenAsync(cancellationToken);

        var existingIndexes = connection
            .GetSchema("Indexes")
            .Rows
            .Cast<System.Data.DataRow>()
            .Select(row => row["INDEX_NAME"]?.ToString())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Cast<string>()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var index in PerformanceIndexes)
        {
            if (existingIndexes.Contains(index.Name))
                continue;

            await using var command = new OleDbCommand(index.Sql, connection);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static IReadOnlyList<PerformanceIndex> PerformanceIndexes { get; } =
    [
        new("IX_Funkcjonariusze_ZmianaKolejnosc",
            "CREATE INDEX IX_Funkcjonariusze_ZmianaKolejnosc ON Funkcjonariusze (NumerZmiany, NumerPorzadkowy)"),
        new("IX_FunkcjonariuszUprawnienia_OsobaTyp",
            "CREATE INDEX IX_FunkcjonariuszUprawnienia_OsobaTyp ON FunkcjonariuszUprawnienia (FunkcjonariuszId, TypUprawnieniaId)"),
        new("IX_FunkcjonariuszOdznaczenia_Osoba",
            "CREATE INDEX IX_FunkcjonariuszOdznaczenia_Osoba ON FunkcjonariuszOdznaczenia (FunkcjonariuszId)"),
        new("IX_GrafikWpisy_ZmianaOkresOsobaDzien",
            "CREATE INDEX IX_GrafikWpisy_ZmianaOkresOsobaDzien ON GrafikWpisy (ZmianaId, Rok, Miesiac, FunkcjonariuszId, Dzien)"),
        new("IX_UrlopPlanWpisy_ZmianaOkresOsobaDzien",
            "CREATE INDEX IX_UrlopPlanWpisy_ZmianaOkresOsobaDzien ON UrlopPlanWpisy (ZmianaId, Rok, Miesiac, FunkcjonariuszId, Dzien)"),
        new("IX_KalendarzWpisy_DataZmianaTyp",
            "CREATE INDEX IX_KalendarzWpisy_DataZmianaTyp ON KalendarzWpisy (Data, ZmianaId, TypWpisu)"),
        new("IX_KalendarzOdczyty_WpisZmiana",
            "CREATE INDEX IX_KalendarzOdczyty_WpisZmiana ON KalendarzOdczyty (WpisId, ZmianaId)"),
        new("IX_Rozkazy_RokZmianaData",
            "CREATE INDEX IX_Rozkazy_RokZmianaData ON Rozkazy (Rok, ZmianaId, Data)"),
        new("IX_RozkazSluzba_Rozkaz",
            "CREATE INDEX IX_RozkazSluzba_Rozkaz ON RozkazSluzba (RozkazId)"),
        new("IX_RozkazPodzialBojowy_Rozkaz",
            "CREATE INDEX IX_RozkazPodzialBojowy_Rozkaz ON RozkazPodzialBojowy (RozkazId)"),
        new("IX_RozkazRatownicy_Rozkaz",
            "CREATE INDEX IX_RozkazRatownicy_Rozkaz ON RozkazRatwnicyMedyczni (RozkazId)"),
        new("IX_RozkazNieobecni_Rozkaz",
            "CREATE INDEX IX_RozkazNieobecni_Rozkaz ON RozkazNieobecni (RozkazId)")
    ];

    private static OleDbConnection OpenConnection(string databasePath, string password) =>
        new(TukanDatabaseOptions.BuildConnectionString(databasePath, password));

    private sealed record PerformanceIndex(string Name, string Sql);
}
