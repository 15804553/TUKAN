using System.Data.OleDb;
using System.Runtime.InteropServices;
using Chomik.Core.Slowniki;

namespace Chomik.Data.Database;

public sealed class DatabaseBootstrapper(DatabaseOptions options)
{
    public async Task EnsureReadyAsync(CancellationToken cancellationToken = default)
    {
        options.MigrateLegacyDatabaseIfNeeded();

        var fullPath = options.GetFullPath();
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (!File.Exists(fullPath))
        {
            CreateDatabaseFile(options);
        }

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        foreach (var ddl in SchemaScripts.CreateTables)
        {
            await ExecuteDdlAsync(connection, ddl, cancellationToken);
        }

        await EnsureSchemaUpgradesAsync(connection, cancellationToken);
        await NumerPorzadkowyMigration.ApplyAsync(connection, cancellationToken);
        await FunkcjeDodatkoweRemovalMigration.ApplyAsync(connection, cancellationToken);
        await Zmiana4RemovalMigration.ApplyAsync(connection, cancellationToken);

        var databaseDirectory = Path.GetDirectoryName(fullPath);
        SlownikTextFiles.EnsureFilesExist(databaseDirectory);

        if (!await HasUsersAsync(connection, cancellationToken))
        {
            await DatabaseSeed.ApplyAsync(connection, databaseDirectory, cancellationToken);
        }
        else
        {
            await SlownikTextFileSynchronizer.SyncAsync(connection, options, cancellationToken);
            await MlodszyNurekUprawnienieMigration.ApplyAsync(connection, cancellationToken);
        }
    }

    private OleDbConnection CreateConnection() => new(options.BuildConnectionString());

    private static void CreateDatabaseFile(DatabaseOptions databaseOptions)
    {
        var type = Type.GetTypeFromProgID("ADOX.Catalog")
            ?? throw new InvalidOperationException(
                "Nie znaleziono ADOX.Catalog. Zainstaluj Microsoft Access Database Engine (ACE).");

        dynamic catalog = Activator.CreateInstance(type)!;
        try
        {
            catalog.Create(databaseOptions.BuildCreateConnectionString());
        }
        finally
        {
            Marshal.ReleaseComObject(catalog);
        }
    }

    private static async Task ExecuteDdlAsync(
        OleDbConnection connection,
        string ddl,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var command = new OleDbCommand(ddl, connection);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (OleDbException ex) when (ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase)
                                         || ex.Message.Contains("już istnieje", StringComparison.OrdinalIgnoreCase))
        {
            // Tabela już utworzona.
        }
    }

    private static async Task EnsureSchemaUpgradesAsync(
        OleDbConnection connection,
        CancellationToken cancellationToken)
    {
        await EnsureColumnAsync(
            connection,
            "Funkcjonariusze",
            "DataWstepieniaDoSluzby",
            "DATETIME",
            cancellationToken);
    }

    private static async Task EnsureColumnAsync(
        OleDbConnection connection,
        string tableName,
        string columnName,
        string columnType,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var command = new OleDbCommand(
                $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnType}",
                connection);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (OleDbException ex) when (ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase)
                                         || ex.Message.Contains("już istnieje", StringComparison.OrdinalIgnoreCase)
                                         || ex.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase)
                                         || ex.Message.Contains("istnieje", StringComparison.OrdinalIgnoreCase))
        {
            // Kolumna już dodana.
        }
    }

    private static async Task<bool> HasUsersAsync(OleDbConnection connection, CancellationToken cancellationToken)
    {
        try
        {
            await using var command = new OleDbCommand("SELECT COUNT(*) FROM Uzytkownicy", connection);
            var count = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
            return count > 0;
        }
        catch (OleDbException)
        {
            return false;
        }
    }
}
