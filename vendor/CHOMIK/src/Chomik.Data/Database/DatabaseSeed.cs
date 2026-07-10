using System.Data.OleDb;
using Chomik.Core.Constants;
using Chomik.Core.Enums;
using Chomik.Core.Security;
using Chomik.Core.Slowniki;
using Chomik.Data;

namespace Chomik.Data.Database;

internal static class DatabaseSeed
{
    public static async Task ApplyAsync(
        OleDbConnection connection,
        string? databaseDirectory = null,
        CancellationToken cancellationToken = default)
    {
        await InsertStopnieAsync(connection, databaseDirectory, cancellationToken);
        await InsertStanowiskaAsync(connection, databaseDirectory, cancellationToken);
        await InsertTypyUprawnienAsync(connection, cancellationToken);
        await InsertUsersAsync(connection, cancellationToken);
        await InsertTypyOdznaczenAsync(connection, cancellationToken);
    }

    private static async Task InsertSettingAsync(
        OleDbConnection connection,
        string key,
        string value,
        CancellationToken cancellationToken)
    {
        await using var command = new OleDbCommand(
            "INSERT INTO UstawieniaAplikacji (Klucz, Wartosc) VALUES (?, ?)",
            connection);
        command.Parameters.AddWithValue("@p1", key);
        command.Parameters.AddWithValue("@p2", value);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<Dictionary<string, int>> InsertStopnieAsync(
        OleDbConnection connection,
        string? databaseDirectory,
        CancellationToken cancellationToken)
    {
        return await InsertSlownikAsync(
            connection,
            "StopnieSlownik",
            ResolveStopnieNames(databaseDirectory),
            cancellationToken);
    }

    private static async Task<Dictionary<string, int>> InsertStanowiskaAsync(
        OleDbConnection connection,
        string? databaseDirectory,
        CancellationToken cancellationToken)
    {
        return await InsertSlownikAsync(
            connection,
            "StanowiskaSlownik",
            ResolveStanowiskaNames(databaseDirectory),
            cancellationToken);
    }

    private static IReadOnlyList<string> ResolveStopnieNames(string? databaseDirectory)
    {
        var fromFile = SlownikTextFiles.ReadStopnie(databaseDirectory);
        return fromFile.Count > 0 ? fromFile : StopnieSlownikDefaults.NazwyPoKolei;
    }

    private static IReadOnlyList<string> ResolveStanowiskaNames(string? databaseDirectory)
    {
        var fromFile = SlownikTextFiles.ReadStanowiska(databaseDirectory);
        return fromFile.Count > 0 ? fromFile : StanowiskaSlownikDefaults.NazwyPoKolei;
    }

    private static async Task<Dictionary<string, int>> InsertTypyUprawnienAsync(
        OleDbConnection connection,
        CancellationToken cancellationToken)
    {
        var types = new (string Nazwa, string? Podtyp, bool WymagaDaty)[]
        {
            ("Prawo jazdy", "kat. B", true),
            ("Prawo jazdy", "kat. C", true),
            ("Prawo jazdy", "kat. C+E", true),
            ("Prawo jazdy", "kat. D", true),
            ("Wkładka KM", null, true),
            ("Obsługa drabin", null, true),
            ("Obsługa żurawia HDS", null, true),
            ("Sprężarki", null, true),
            ("Nurek", null, true),
            ("Kierownik prac podwodnych", null, false),
            ("Sonar", null, false),
            ("Stermotorzysta / obsługa łodzi", null, true),
            ("GPS", null, false),
            ("Medyk", null, false),
            ("Chemiczny kurs", null, true),
            ("IDZ", null, true)
        };

        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var (nazwa, podtyp, wymagaDaty) in types)
        {
            await using var command = new OleDbCommand(
                "INSERT INTO TypyUprawnien (Nazwa, Podtyp, WymagaDaty) VALUES (?, ?, ?)",
                connection);
            command.Parameters.AddWithValue("@p1", nazwa);
            command.Parameters.AddWithValue("@p2", podtyp ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@p3", wymagaDaty);
            await command.ExecuteNonQueryAsync(cancellationToken);

            await using var idCommand = new OleDbCommand("SELECT @@IDENTITY", connection);
            var id = Convert.ToInt32(await idCommand.ExecuteScalarAsync(cancellationToken));
            map[$"{nazwa}|{podtyp}"] = id;
        }

        return map;
    }

    private static async Task<Dictionary<string, int>> InsertTypyOdznaczenAsync(
        OleDbConnection connection,
        CancellationToken cancellationToken)
    {
        var medals = new[]
        {
            "Brązowa Odznaka \"Zasłużony dla ochrony przeciwpożarowej\"",
            "Srebrna Odznaka \"Zasłużony dla Ochrony Przeciwpożarowej\"",
            "Złota Odznaka \"Zasłużony dla Ochrony Przeciwpożarowej\"",
            "Brązowy Medal \"Za Zasługi dla Pożarnictwa\"",
            "Srebrny Medal \"Za Zasługi dla Pożarnictwa\"",
            "Złoty Medal \"Za Zasługi dla Pożarnictwa\"",
            "Brązowy medal za długoletnią służbę",
            "Srebrny medal za długoletnią służbę",
            "Złoty medal za długoletnią służbę"
        };

        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var nazwa in medals)
        {
            await using var command = new OleDbCommand(
                "INSERT INTO TypyOdznaczen (Nazwa) VALUES (?)",
                connection);
            command.Parameters.AddWithValue("@p1", nazwa);
            await command.ExecuteNonQueryAsync(cancellationToken);

            await using var idCommand = new OleDbCommand("SELECT @@IDENTITY", connection);
            map[nazwa] = Convert.ToInt32(await idCommand.ExecuteScalarAsync(cancellationToken));
        }

        return map;
    }

    private static async Task InsertUsersAsync(OleDbConnection connection, CancellationToken cancellationToken)
    {
        var users = new (string Login, UserRole Role, int? Shift)[]
        {
            ("PA", UserRole.Pa, null),
            ("Zmiana 1", UserRole.Zmiana1, 1),
            ("Zmiana 2", UserRole.Zmiana2, 2),
            ("Zmiana 3", UserRole.Zmiana3, 3),
            ("DCA JRG", UserRole.DcaJrg, null),
            ("Administrator", UserRole.Administrator, null)
        };

        foreach (var (login, role, shift) in users)
        {
            var defaultPassword = DefaultCredentials.DefaultPasswords[role];
            string hash;
            string salt;
            if (defaultPassword is null)
            {
                hash = string.Empty;
                salt = string.Empty;
            }
            else
            {
                (hash, salt) = PasswordHasher.HashPassword(defaultPassword);
            }

            await using var command = new OleDbCommand(
                "INSERT INTO Uzytkownicy (Login, Rola, NumerZmiany, HasloHash, HasloSol) VALUES (?, ?, ?, ?, ?)",
                connection);
            command.Parameters.AddWithValue("@p1", login);
            command.Parameters.AddWithValue("@p2", (short)role);
            command.Parameters.AddWithValue("@p3", shift ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@p4", hash);
            command.Parameters.AddWithValue("@p5", salt);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static async Task<Dictionary<string, int>> InsertSlownikAsync(
        OleDbConnection connection,
        string table,
        IEnumerable<string> names,
        CancellationToken cancellationToken)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in names)
        {
            await using var command = new OleDbCommand($"INSERT INTO {table} (Nazwa) VALUES (?)", connection);
            command.Parameters.AddWithValue("@p1", name);
            await command.ExecuteNonQueryAsync(cancellationToken);

            await using var idCommand = new OleDbCommand("SELECT @@IDENTITY", connection);
            map[name] = Convert.ToInt32(await idCommand.ExecuteScalarAsync(cancellationToken));
        }

        return map;
    }
}
