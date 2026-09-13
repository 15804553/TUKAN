using System.Data.OleDb;
using BOBER.Core.Constants;
using BOBER.Core.Enums;
using BOBER.Core.Models;
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
        await EnsureOznaczeniaGrafikuAsync(connection, cancellationToken);
    }

    private static async Task EnsureUsersAsync(OleDbConnection connection, CancellationToken cancellationToken)
    {
        await using var countCmd = new OleDbCommand("SELECT COUNT(*) FROM UzytkownicyBOBER", connection);
        var count = Convert.ToInt32(await countCmd.ExecuteScalarAsync(cancellationToken));
        if (count > 0)
            return;

        var users = new (string Login, int Zmiana, UserRole Role)[]
        {
            ("Zmiana 1", 1, UserRole.Zmiana1),
            ("Zmiana 2", 2, UserRole.Zmiana2),
            ("Zmiana 3", 3, UserRole.Zmiana3)
        };

        foreach (var (login, zmiana, role) in users)
        {
            var defaultPwd = DefaultCredentials.DefaultPasswords[role];
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
            await InsertKolorAsync(connection, klucz, kolor, cancellationToken);

        foreach (var (klucz, kolor) in RoleKeys.DomyslneKoloryWpisow)
            await InsertKolorAsync(connection, klucz, kolor, cancellationToken);

        foreach (var (klucz, kolor) in RoleKeys.DomyslneKoloryEksportu)
            await InsertKolorAsync(connection, klucz, kolor, cancellationToken);

        foreach (var (klucz, kolor) in RoleKeys.DomyslneKoloryKalendarza)
            await InsertKolorAsync(connection, klucz, kolor, cancellationToken);
    }

    private static async Task InsertKolorAsync(
        OleDbConnection connection,
        string klucz,
        string kolor,
        CancellationToken cancellationToken)
    {
        await using var cmd = new OleDbCommand(
            "INSERT INTO KoloryStanowisk (KluczRoli, KolorHex, Aktywny) VALUES (?, ?, ?)",
            connection);
        cmd.Parameters.AddWithValue("@p1", klucz);
        cmd.Parameters.AddWithValue("@p2", kolor);
        cmd.Parameters.Add("@p3", OleDbType.Boolean).Value = true;
        await cmd.ExecuteNonQueryAsync(cancellationToken);
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
            { "MaxUrlopowNaSluzbie", "5" },
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

    private static async Task EnsureOznaczeniaGrafikuAsync(
        OleDbConnection connection,
        CancellationToken cancellationToken)
    {
        var kolorWs = await GetKolorHexAsync(connection, RoleKeys.WolnaSluzba, cancellationToken);
        var kolorDel = await GetKolorHexAsync(connection, RoleKeys.Delegacja, cancellationToken);
        var kolorS = await GetKolorHexAsync(connection, RoleKeys.Szkolenie, cancellationToken);
        var defaults = OznaczeniaGrafikuSeed.CreateDefaults(kolorWs, kolorDel, kolorS);

        for (short zmianaId = 1; zmianaId <= 3; zmianaId++)
        {
            await using var countCmd = new OleDbCommand(
                "SELECT COUNT(*) FROM OznaczeniaGrafiku WHERE ZmianaId = ?",
                connection);
            countCmd.Parameters.AddWithValue("@p1", zmianaId);
            var count = Convert.ToInt32(await countCmd.ExecuteScalarAsync(cancellationToken));
            if (count > 0)
                continue;

            foreach (var item in defaults)
                await InsertOznaczenieAsync(connection, zmianaId, item, cancellationToken);
        }
    }

    private static async Task InsertOznaczenieAsync(
        OleDbConnection connection,
        short zmianaId,
        OznaczenieGrafiku item,
        CancellationToken cancellationToken)
    {
        await using var cmd = new OleDbCommand(
            """
            INSERT INTO OznaczeniaGrafiku
                (ZmianaId, Kod, Nazwa, KolorHex, WPracy, SekcjaRozkazu, SkrotKlawiszowy,
                 TekstWyswietlany, MoznaOddac, MoznaKropke, ZachowajTloWsPrzyBraku,
                 DodatkowaSekcjaRozkazu, RolaNalozania, Kolejnosc,
                 EksportDoExcela, KolorExcelHex, AdnotacjaRozkazu,
                 StylWyswietlania, FlagaPozycja)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
            """,
            connection);

        cmd.Parameters.AddWithValue("@p0", zmianaId);
        cmd.Parameters.AddWithValue("@p1", item.Kod);
        cmd.Parameters.AddWithValue("@p2", item.Nazwa);
        cmd.Parameters.AddWithValue("@p3", item.KolorHex);
        cmd.Parameters.AddWithValue("@p4", item.WPracy);
        AddNullableShort(cmd, item.SekcjaRozkazu is null ? null : (short?)item.SekcjaRozkazu.Value);
        cmd.Parameters.AddWithValue("@p6", item.SkrotKlawiszowy ?? string.Empty);
        cmd.Parameters.AddWithValue("@p7", (object?)item.TekstWyswietlany ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@p8", false);
        cmd.Parameters.AddWithValue("@p9", false);
        cmd.Parameters.AddWithValue("@p10", false);
        AddNullableShort(cmd, null);
        cmd.Parameters.AddWithValue("@p12", (short)0);
        cmd.Parameters.AddWithValue("@p13", item.Kolejnosc);
        cmd.Parameters.AddWithValue("@p14", item.EksportDoExcela);
        cmd.Parameters.AddWithValue("@p15",
            string.IsNullOrWhiteSpace(item.KolorExcelHex) ? item.KolorHex : item.KolorExcelHex);
        cmd.Parameters.AddWithValue("@p16", item.AdnotacjaRozkazu ?? string.Empty);
        cmd.Parameters.AddWithValue("@p17", (short)item.StylWyswietlania);
        cmd.Parameters.AddWithValue("@p18", (short)item.FlagaPozycja);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<string?> GetKolorHexAsync(
        OleDbConnection connection,
        string klucz,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var cmd = new OleDbCommand(
                "SELECT KolorHex FROM KoloryStanowisk WHERE KluczRoli = ?",
                connection);
            cmd.Parameters.AddWithValue("@p1", klucz);
            var raw = await cmd.ExecuteScalarAsync(cancellationToken);
            return raw is null or DBNull ? null : raw.ToString();
        }
        catch
        {
            return null;
        }
    }

    private static void AddNullableShort(OleDbCommand command, short? value)
    {
        var p = command.Parameters.Add("@p", OleDbType.SmallInt);
        p.Value = value.HasValue ? value.Value : DBNull.Value;
    }
}
