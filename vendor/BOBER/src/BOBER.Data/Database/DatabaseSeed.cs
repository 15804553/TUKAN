using System.Data.OleDb;
using BOBER.Core.Constants;
using BOBER.Core.Security;

namespace BOBER.Data.Database;

internal static class DatabaseSeed
{
    public static async Task EnsureDefaultsAsync(
        OleDbConnection connection,
        BoberDatabaseOptions options,
        CancellationToken cancellationToken)
    {
        await EnsureUsersAsync(connection, cancellationToken);
        await EnsureKoloryAsync(connection, cancellationToken);
        await EnsureUstawieniaAsync(connection, options, cancellationToken);
    }

    private static async Task EnsureUsersAsync(OleDbConnection connection, CancellationToken cancellationToken)
    {
        await using var countCmd = new OleDbCommand("SELECT COUNT(*) FROM UzytkownicyBOBER", connection);
        var count = Convert.ToInt32(await countCmd.ExecuteScalarAsync(cancellationToken));
        if (count > 0)
            return;

        var users = new (string Login, int Zmiana)[]
        {
            ("Zmiana 1", 1),
            ("Zmiana 2", 2),
            ("Zmiana 3", 3)
        };

        foreach (var (login, zmiana) in users)
        {
            var defaultPwd = $"zmiana{zmiana}";
            var (hash, salt) = PasswordHasher.HashPassword(defaultPwd);

            await using var cmd = new OleDbCommand(
                "INSERT INTO UzytkownicyBOBER (Login, NumerZmiany, HasloHash, HasloSol) VALUES (?, ?, ?, ?)",
                connection);
            cmd.Parameters.AddWithValue("@p1", login);
            cmd.Parameters.AddWithValue("@p2", (short)zmiana);
            cmd.Parameters.AddWithValue("@p3", hash);
            cmd.Parameters.AddWithValue("@p4", salt);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static async Task EnsureKoloryAsync(OleDbConnection connection, CancellationToken cancellationToken)
    {
        await using var countCmd = new OleDbCommand("SELECT COUNT(*) FROM KoloryStanowisk", connection);
        var count = Convert.ToInt32(await countCmd.ExecuteScalarAsync(cancellationToken));
        if (count > 0)
            return;

        foreach (var (klucz, kolor) in RoleKeys.DomyslneKolory)
        {
            await using var cmd = new OleDbCommand(
                "INSERT INTO KoloryStanowisk (KluczRoli, KolorHex) VALUES (?, ?)",
                connection);
            cmd.Parameters.AddWithValue("@p1", klucz);
            cmd.Parameters.AddWithValue("@p2", kolor);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var (klucz, kolor) in RoleKeys.DomyslneKoloryWpisow)
        {
            await using var cmd = new OleDbCommand(
                "INSERT INTO KoloryStanowisk (KluczRoli, KolorHex) VALUES (?, ?)",
                connection);
            cmd.Parameters.AddWithValue("@p1", klucz);
            cmd.Parameters.AddWithValue("@p2", kolor);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var (klucz, kolor) in RoleKeys.DomyslneKoloryEksportu)
        {
            await using var cmd = new OleDbCommand(
                "INSERT INTO KoloryStanowisk (KluczRoli, KolorHex) VALUES (?, ?)",
                connection);
            cmd.Parameters.AddWithValue("@p1", klucz);
            cmd.Parameters.AddWithValue("@p2", kolor);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static async Task EnsureUstawieniaAsync(
        OleDbConnection connection,
        BoberDatabaseOptions options,
        CancellationToken cancellationToken)
    {
        var defaults = new Dictionary<string, string>
        {
            { "ChomikDbPath", Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CHOMIK", "ChomikDatabase.accdb") },
            { "StanZmiany", "10" },
            { "StanMinimalny", "6" },
            { "DataReferencyjna", "2026-01-01" },
            { "OffsetyZmian", "{\"1\":1,\"2\":2,\"3\":0}" },
            { "LiczbaZmian", "3" }
        };

        foreach (var (klucz, wartosc) in defaults)
        {
            await using var check = new OleDbCommand(
                "SELECT COUNT(*) FROM Ustawienia WHERE Klucz = ?", connection);
            check.Parameters.AddWithValue("@p1", klucz);
            var exists = Convert.ToInt32(await check.ExecuteScalarAsync(cancellationToken)) > 0;

            if (!exists)
            {
                await using var insert = new OleDbCommand(
                    "INSERT INTO Ustawienia (Klucz, Wartosc) VALUES (?, ?)", connection);
                insert.Parameters.AddWithValue("@p1", klucz);
                insert.Parameters.AddWithValue("@p2", wartosc);
                await insert.ExecuteNonQueryAsync(cancellationToken);
            }
        }
    }
}
