using System.Data.OleDb;
using System.IO;
using SKRYBEK.Services.Logging;

namespace Tukan.App.Services;

/// <summary>
/// Scala dane z osobnych baz CHOMIK / BOBER / SKRYBEK do jednego pliku TukanDatabase.accdb.
/// </summary>
public static class TukanDatabaseConsolidator
{
    private static readonly string[] BoberTables =
    [
        "UzytkownicyBOBER",
        "GrafikWpisy",
        "UrlopPlanWpisy",
        "GrafikNurkowyZatwierdzenia",
        "KolejnoscFunkcjonariuszy",
        "KoloryStanowisk"
    ];

    private static readonly string[] SkrybekTables =
    [
        "Samochody",
        "Rozkazy",
        "RozkazSluzba",
        "RozkazPodzialBojowy",
        "RozkazNieobecni",
        "RozkazRatwnicyMedyczni",
        "SamochodWymagania"
    ];

    public sealed record ConsolidationEntry(string Description, string SourcePath);

    public sealed record ConsolidationResult(
        string UnifiedPath,
        IReadOnlyList<ConsolidationEntry> Actions)
    {
        public bool Any => Actions.Count > 0;
    }

    public static async Task<ConsolidationResult> ConsolidateAsync(
        string targetDirectory,
        string? chomikSource,
        string? boberSource,
        string? skrybekSource,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(targetDirectory);
        var unifiedPath = Path.Combine(targetDirectory, TukanDatabaseOptions.FileName);
        var actions = new List<ConsolidationEntry>();

        chomikSource = ResolveExisting(chomikSource, targetDirectory, "ChomikDatabase.accdb");
        boberSource = ResolveExisting(boberSource, targetDirectory, "BoberDatabase.accdb");
        skrybekSource = ResolveExisting(skrybekSource, targetDirectory, "SkrybekDatabase.accdb");

        if (!File.Exists(unifiedPath))
        {
            if (chomikSource is not null && !PathsEqual(chomikSource, unifiedPath))
            {
                File.Copy(chomikSource, unifiedPath);
                actions.Add(new ConsolidationEntry("Skopiowano bazę personelu (CHOMIK)", chomikSource));
            }
            else if (boberSource is not null && !PathsEqual(boberSource, unifiedPath))
            {
                File.Copy(boberSource, unifiedPath);
                actions.Add(new ConsolidationEntry("Skopiowano bazę grafiku (BOBER) jako bazę roboczą", boberSource));
            }
            else if (skrybekSource is not null && !PathsEqual(skrybekSource, unifiedPath))
            {
                File.Copy(skrybekSource, unifiedPath);
                actions.Add(new ConsolidationEntry("Skopiowano bazę rozkazów (SKRYBEK) jako bazę roboczą", skrybekSource));
            }
        }

        await TukanUnifiedDatabaseBootstrapper.EnsureSchemaAsync(unifiedPath, cancellationToken);

        if (chomikSource is not null
            && !PathsEqual(chomikSource, unifiedPath)
            && await NeedsChomikImportAsync(unifiedPath, chomikSource, cancellationToken))
        {
            await ImportChomikCoreAsync(unifiedPath, chomikSource, actions, cancellationToken);
        }

        if (boberSource is not null && !PathsEqual(boberSource, unifiedPath))
        {
            await ImportModuleTablesAsync(
                unifiedPath,
                boberSource,
                BoberTables,
                "BOBER",
                actions,
                cancellationToken);
            await MergeUstawieniaAsync(unifiedPath, boberSource, actions, cancellationToken);
        }

        if (skrybekSource is not null && !PathsEqual(skrybekSource, unifiedPath))
        {
            await ImportModuleTablesAsync(
                unifiedPath,
                skrybekSource,
                SkrybekTables,
                "SKRYBEK",
                actions,
                cancellationToken);
            await MergeUstawieniaAsync(unifiedPath, skrybekSource, actions, cancellationToken);
        }

        ArchiveLegacyFiles(targetDirectory, unifiedPath, actions);

        return new ConsolidationResult(unifiedPath, actions);
    }

    private static async Task<bool> NeedsChomikImportAsync(
        string unifiedPath,
        string chomikSource,
        CancellationToken cancellationToken)
    {
        var unifiedCount = await CountRowsAsync(unifiedPath, "Funkcjonariusze", cancellationToken);
        if (unifiedCount == 0)
        {
            return true;
        }

        var sourceCount = await CountRowsAsync(chomikSource, "Funkcjonariusze", cancellationToken);
        return sourceCount > unifiedCount;
    }

    private static async Task ImportChomikCoreAsync(
        string unifiedPath,
        string chomikSource,
        List<ConsolidationEntry> actions,
        CancellationToken cancellationToken)
    {
        var chomikOnlyTables = new[]
        {
            "StopnieSlownik", "StanowiskaSlownik", "TypyUprawnien", "TypyOdznaczen",
            "Funkcjonariusze", "FunkcjonariuszUprawnienia", "FunkcjonariuszOdznaczenia",
            "Uzytkownicy", "UstawieniaAplikacji"
        };

        await ImportModuleTablesAsync(
            unifiedPath,
            chomikSource,
            chomikOnlyTables,
            "CHOMIK",
            actions,
            cancellationToken);
    }

    private static async Task ImportModuleTablesAsync(
        string unifiedPath,
        string sourcePath,
        IReadOnlyList<string> tables,
        string moduleLabel,
        List<ConsolidationEntry> actions,
        CancellationToken cancellationToken)
    {
        await using var dest = OpenConnection(unifiedPath);
        await dest.OpenAsync(cancellationToken);

        foreach (var table in tables)
        {
            if (!await TableExistsInDatabaseAsync(sourcePath, table, cancellationToken))
            {
                continue;
            }

            if (await CountRowsAsync(dest, table, cancellationToken) > 0)
            {
                continue;
            }

            var imported = await TryImportTableAsync(dest, table, sourcePath, cancellationToken);
            if (imported)
            {
                actions.Add(new ConsolidationEntry($"Zaimportowano tabelę {table} ({moduleLabel})", sourcePath));
            }
        }
    }

    private static async Task MergeUstawieniaAsync(
        string unifiedPath,
        string sourcePath,
        List<ConsolidationEntry> actions,
        CancellationToken cancellationToken)
    {
        if (!await TableExistsInDatabaseAsync(sourcePath, "Ustawienia", cancellationToken))
        {
            return;
        }

        await using var dest = OpenConnection(unifiedPath);
        await dest.OpenAsync(cancellationToken);

        var merged = 0;
        await using (var source = OpenConnection(sourcePath))
        {
            await source.OpenAsync(cancellationToken);
            await using var read = new OleDbCommand("SELECT Klucz, Wartosc FROM Ustawienia", source);
            await using var reader = await read.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var klucz = reader.GetString(0);
                var wartosc = reader.GetString(1);

                await using var check = new OleDbCommand(
                    "SELECT COUNT(*) FROM Ustawienia WHERE Klucz=?",
                    dest);
                check.Parameters.AddWithValue("@p1", klucz);
                var exists = Convert.ToInt32(await check.ExecuteScalarAsync(cancellationToken)) > 0;
                if (exists)
                {
                    continue;
                }

                await using var insert = new OleDbCommand(
                    "INSERT INTO Ustawienia (Klucz, Wartosc) VALUES (?, ?)",
                    dest);
                insert.Parameters.AddWithValue("@p1", klucz);
                insert.Parameters.AddWithValue("@p2", wartosc);
                await insert.ExecuteNonQueryAsync(cancellationToken);
                merged++;
            }
        }

        if (merged > 0)
        {
            actions.Add(new ConsolidationEntry(
                $"Scalono {merged} wpisów Ustawienia",
                sourcePath));
        }
    }

    private static async Task<bool> TryImportTableAsync(
        OleDbConnection dest,
        string tableName,
        string sourcePath,
        CancellationToken cancellationToken)
    {
        var inClause = FormatInClause(sourcePath);
        var sql = $"INSERT INTO [{tableName}] SELECT * FROM [{tableName}] IN {inClause}";

        try
        {
            await using var cmd = new OleDbCommand(sql, dest);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            SkrybekLog.Warning($"Import tabeli {tableName} z {sourcePath}: {ex.Message}");
            return false;
        }
    }

    private static async Task<int> CountRowsAsync(
        string databasePath,
        string tableName,
        CancellationToken cancellationToken)
    {
        await using var conn = OpenConnection(databasePath);
        await conn.OpenAsync(cancellationToken);
        return await CountRowsAsync(conn, tableName, cancellationToken);
    }

    private static async Task<int> CountRowsAsync(
        OleDbConnection connection,
        string tableName,
        CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(connection, tableName, cancellationToken))
        {
            return 0;
        }

        try
        {
            await using var cmd = new OleDbCommand($"SELECT COUNT(*) FROM [{tableName}]", connection);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync(cancellationToken));
        }
        catch
        {
            return 0;
        }
    }

    private static async Task<bool> TableExistsInDatabaseAsync(
        string databasePath,
        string tableName,
        CancellationToken cancellationToken)
    {
        await using var conn = OpenConnection(databasePath);
        await conn.OpenAsync(cancellationToken);
        return await TableExistsAsync(conn, tableName, cancellationToken);
    }

    private static async Task<bool> TableExistsAsync(
        OleDbConnection connection,
        string tableName,
        CancellationToken cancellationToken)
    {
        var schema = await connection.GetSchemaAsync("Tables", cancellationToken: cancellationToken);
        foreach (System.Data.DataRow row in schema.Rows)
        {
            if (row["TABLE_TYPE"]?.ToString() == "TABLE"
                && string.Equals(row["TABLE_NAME"]?.ToString(), tableName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static void ArchiveLegacyFiles(
        string targetDirectory,
        string unifiedPath,
        List<ConsolidationEntry> actions)
    {
        foreach (var legacyName in TukanDatabaseOptions.LegacyFileNames)
        {
            var legacyPath = Path.Combine(targetDirectory, legacyName);
            if (!File.Exists(legacyPath) || PathsEqual(legacyPath, unifiedPath))
            {
                continue;
            }

            var archivePath = legacyPath + ".legacy.bak";
            if (File.Exists(archivePath))
            {
                continue;
            }

            try
            {
                File.Move(legacyPath, archivePath);
                actions.Add(new ConsolidationEntry(
                    $"Zarchiwizowano stary plik {legacyName}",
                    archivePath));
            }
            catch (Exception ex)
            {
                SkrybekLog.Warning($"Nie udało się zarchiwizować {legacyPath}: {ex.Message}");
            }
        }
    }

    private static string? ResolveExisting(string? preferred, string targetDirectory, string fileName)
    {
        if (!string.IsNullOrWhiteSpace(preferred) && File.Exists(preferred))
        {
            return Path.GetFullPath(preferred);
        }

        var local = Path.Combine(targetDirectory, fileName);
        return File.Exists(local) ? local : null;
    }

    private static OleDbConnection OpenConnection(string databasePath) =>
        new(TukanDatabaseOptions.BuildConnectionString(databasePath));

    private static string FormatInClause(string sourcePath)
    {
        var escaped = sourcePath.Replace("'", "''");
        return $"'{escaped}'; '{TukanDatabaseOptions.BuildConnectionString(sourcePath).Replace("'", "''")}'";
    }

    private static bool PathsEqual(string a, string b) =>
        string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase);
}
