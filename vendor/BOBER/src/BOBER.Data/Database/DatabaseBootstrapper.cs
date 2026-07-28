using System.Data.OleDb;
using System.Runtime.InteropServices;
using BOBER.Core.Constants;

namespace BOBER.Data.Database;

/// <summary>Tworzy lub migruje BoberDatabase.accdb (schemat, seed, rename ze Skrybek).</summary>
public sealed class DatabaseBootstrapper(BoberDatabaseOptions options)
{
    public async Task EnsureReadyAsync(CancellationToken cancellationToken = default)
    {
        options.EnsureDirectoryExists();
        var fullPath = options.GetFullPath();

        MigrateLegacyDatabaseFile(fullPath);

        if (!File.Exists(fullPath))
            CreateDatabaseFile();

        await using var connection = new OleDbConnection(options.BuildConnectionString());
        await connection.OpenAsync(cancellationToken);

        await EnsureSchemaAsync(connection, cancellationToken);

        await DatabaseSeed.EnsureDefaultsAsync(connection, options, cancellationToken);
    }

    private void CreateDatabaseFile()
    {
        var type = Type.GetTypeFromProgID("ADOX.Catalog")
            ?? throw new InvalidOperationException(
                "Nie znaleziono ADOX.Catalog. Zainstaluj Microsoft Access Database Engine (ACE).");

        dynamic catalog = Activator.CreateInstance(type)!;
        try
        {
            catalog.Create(options.BuildCreateConnectionString());
        }
        finally
        {
            Marshal.ReleaseComObject(catalog);
        }
    }

    private static async Task EnsureSchemaAsync(OleDbConnection connection, CancellationToken cancellationToken)
    {
        foreach (var ddl in SchemaScripts.CreateTables)
            await ExecuteDdlAsync(connection, ddl, cancellationToken);

        await MigrateUstawieniaTabelaAsync(connection, cancellationToken);
        await MigrateUsersTableFromSkrybekAsync(connection, cancellationToken);
        await MigrateReferenceDate2026Async(connection, cancellationToken);
        await MigrateDefaultRoleColorsAsync(connection, cancellationToken);
        await MigrateKierowcaRoleMergeAsync(connection, cancellationToken);
        await MigrateWolnaSluzbaColorAsync(connection, cancellationToken);
        await MigrateNurekRoleMergeAsync(connection, cancellationToken);
        await MigrateRemoveDyzurColorAsync(connection, cancellationToken);
        await MigrateExportBandColorsAsync(connection, cancellationToken);
        await MigrateUrlopPlanWpisyTableAsync(connection, cancellationToken);
        await MigrateDzienSluzbyColorAsync(connection, cancellationToken);
        await MigrateGrafikNurkowyZatwierdzeniaTableAsync(connection, cancellationToken);
        await MigrateGrafikNotatkiTableAsync(connection, cancellationToken);
        await MigrateGrafikUwagiMiesieczneTableAsync(connection, cancellationToken);
        await MigrateKalendarzTablesAsync(connection, cancellationToken);
        await MigrateKalendarzKoloryAsync(connection, cancellationToken);
    }

    private static async Task MigrateKalendarzTablesAsync(
        OleDbConnection connection,
        CancellationToken cancellationToken)
    {
        await ExecuteDdlAsync(connection,
            """
            CREATE TABLE KalendarzWpisy (
                Id AUTOINCREMENT PRIMARY KEY,
                Data DATETIME NOT NULL,
                ZmianaId SHORT NOT NULL,
                Tresc MEMO NOT NULL,
                AutorLogin TEXT(100) NOT NULL,
                DataUtworzenia DATETIME NOT NULL,
                DataModyfikacji DATETIME NOT NULL
            )
            """,
            cancellationToken);

        await ExecuteDdlAsync(connection,
            """
            CREATE TABLE KalendarzOdczyty (
                WpisId LONG NOT NULL,
                ZmianaId SHORT NOT NULL,
                Przeczytane YESNO NOT NULL,
                PrzeczytanePrzez TEXT(100),
                DataOdczytu DATETIME
            )
            """,
            cancellationToken);
    }

    private static async Task MigrateKalendarzKoloryAsync(
        OleDbConnection connection,
        CancellationToken cancellationToken)
    {
        const string migrationKey = "MigratedKalendarzKolory20260722";

        try
        {
            await using var checkCmd = new OleDbCommand(
                "SELECT COUNT(*) FROM Ustawienia WHERE Klucz = ? AND Wartosc = '1'",
                connection);
            checkCmd.Parameters.AddWithValue("@p1", migrationKey);
            if (Convert.ToInt32(await checkCmd.ExecuteScalarAsync(cancellationToken)) > 0)
                return;

            foreach (var (klucz, kolor) in RoleKeys.DomyslneKoloryKalendarza)
            {
                await using var existsCmd = new OleDbCommand(
                    "SELECT COUNT(*) FROM KoloryStanowisk WHERE KluczRoli = ?",
                    connection);
                existsCmd.Parameters.AddWithValue("@p1", klucz);
                if (Convert.ToInt32(await existsCmd.ExecuteScalarAsync(cancellationToken)) > 0)
                    continue;

                await using var insertCmd = new OleDbCommand(
                    "INSERT INTO KoloryStanowisk (KluczRoli, KolorHex) VALUES (?, ?)",
                    connection);
                insertCmd.Parameters.AddWithValue("@p1", klucz);
                insertCmd.Parameters.AddWithValue("@p2", kolor);
                await insertCmd.ExecuteNonQueryAsync(cancellationToken);
            }

            await using var flagCmd = new OleDbCommand(
                "INSERT INTO Ustawienia (Klucz, Wartosc) VALUES (?, ?)",
                connection);
            flagCmd.Parameters.AddWithValue("@p1", migrationKey);
            flagCmd.Parameters.AddWithValue("@p2", "1");
            await flagCmd.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (OleDbException)
        {
            // Tabela może jeszcze nie istnieć przy pierwszym starcie — pomijamy.
        }
    }

    private static async Task MigrateGrafikNotatkiTableAsync(
        OleDbConnection connection,
        CancellationToken cancellationToken)
    {
        await ExecuteDdlAsync(connection,
            """
            CREATE TABLE GrafikNotatki (
                Id AUTOINCREMENT PRIMARY KEY,
                ZmianaId SHORT NOT NULL,
                Rok SHORT NOT NULL,
                Miesiac SHORT NOT NULL,
                Dzien SHORT NOT NULL,
                Tresc MEMO NOT NULL
            )
            """,
            cancellationToken);
    }

    private static async Task MigrateGrafikUwagiMiesieczneTableAsync(
        OleDbConnection connection,
        CancellationToken cancellationToken)
    {
        await ExecuteDdlAsync(connection,
            """
            CREATE TABLE GrafikUwagiMiesieczne (
                Id AUTOINCREMENT PRIMARY KEY,
                FunkcjonariuszId LONG NOT NULL,
                ZmianaId SHORT NOT NULL,
                Rok SHORT NOT NULL,
                Miesiac SHORT NOT NULL,
                Tresc MEMO NOT NULL
            )
            """,
            cancellationToken);
    }

    private static async Task MigrateGrafikNurkowyZatwierdzeniaTableAsync(
        OleDbConnection connection,
        CancellationToken cancellationToken)
    {
        await ExecuteDdlAsync(connection,
            """
            CREATE TABLE GrafikNurkowyZatwierdzenia (
                Rok SHORT NOT NULL,
                Miesiac SHORT NOT NULL,
                Zatwierdzony YESNO NOT NULL,
                ZatwierdzonyPrzez TEXT(100),
                DataZatwierdzenia DATETIME
            )
            """,
            cancellationToken);
    }

    /// <summary>
    /// Dodaje domyślny kolor oznaczenia dnia służby w planie urlopów.
    /// </summary>
    private static async Task MigrateDzienSluzbyColorAsync(
        OleDbConnection connection,
        CancellationToken cancellationToken)
    {
        const string migrationKey = "MigratedDzienSluzbyColor20260710";

        try
        {
            await using var checkCmd = new OleDbCommand(
                "SELECT COUNT(*) FROM Ustawienia WHERE Klucz = ? AND Wartosc = '1'",
                connection);
            checkCmd.Parameters.AddWithValue("@p1", migrationKey);
            if (Convert.ToInt32(await checkCmd.ExecuteScalarAsync(cancellationToken)) > 0)
                return;

            await using (var existsCmd = new OleDbCommand(
                "SELECT COUNT(*) FROM KoloryStanowisk WHERE KluczRoli = ?",
                connection))
            {
                existsCmd.Parameters.AddWithValue("@p1", RoleKeys.DzienSluzby);
                if (Convert.ToInt32(await existsCmd.ExecuteScalarAsync(cancellationToken)) == 0)
                {
                    await using var insertCmd = new OleDbCommand(
                        "INSERT INTO KoloryStanowisk (KluczRoli, KolorHex) VALUES (?, ?)",
                        connection);
                    insertCmd.Parameters.AddWithValue("@p1", RoleKeys.DzienSluzby);
                    insertCmd.Parameters.AddWithValue("@p2", RoleKeys.DomyslneKoloryWpisow[RoleKeys.DzienSluzby]);
                    await insertCmd.ExecuteNonQueryAsync(cancellationToken);
                }
            }

            await using var flagCmd = new OleDbCommand(
                "INSERT INTO Ustawienia (Klucz, Wartosc) VALUES (?, ?)",
                connection);
            flagCmd.Parameters.AddWithValue("@p1", migrationKey);
            flagCmd.Parameters.AddWithValue("@p2", "1");
            await flagCmd.ExecuteNonQueryAsync(cancellationToken);
        }
        catch { /* seed uzupełni brakujące dane */ }
    }

    private static async Task MigrateUrlopPlanWpisyTableAsync(
        OleDbConnection connection,
        CancellationToken cancellationToken)
    {
        await ExecuteDdlAsync(connection,
            """
            CREATE TABLE UrlopPlanWpisy (
                Id AUTOINCREMENT PRIMARY KEY,
                FunkcjonariuszId LONG NOT NULL,
                ZmianaId SHORT NOT NULL,
                Rok SHORT NOT NULL,
                Miesiac SHORT NOT NULL,
                Dzien SHORT NOT NULL,
                TypUrlopu TEXT(1) NOT NULL
            )
            """,
            cancellationToken);
    }

    private static void MigrateLegacyDatabaseFile(string newFullPath)
    {
        if (File.Exists(newFullPath))
            return;

        var directory = Path.GetDirectoryName(newFullPath) ?? AppContext.BaseDirectory;
        var legacyPath = Path.Combine(directory, "SkrybekDatabase.accdb");
        if (File.Exists(legacyPath))
            File.Move(legacyPath, newFullPath);
    }

    /// <summary>
    /// Kopiuje użytkowników ze starej tabeli SKRYBEK do UzytkownicyBOBER.
    /// </summary>
    private static async Task MigrateUsersTableFromSkrybekAsync(
        OleDbConnection connection,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var newCountCmd = new OleDbCommand(
                "SELECT COUNT(*) FROM UzytkownicyBOBER", connection);
            if (Convert.ToInt32(await newCountCmd.ExecuteScalarAsync(cancellationToken)) > 0)
                return;

            await using var oldCountCmd = new OleDbCommand(
                "SELECT COUNT(*) FROM UzytkownicySKRYBEK", connection);
            if (Convert.ToInt32(await oldCountCmd.ExecuteScalarAsync(cancellationToken)) == 0)
                return;

            await using var copyCmd = new OleDbCommand(
                """
                INSERT INTO UzytkownicyBOBER (Login, NumerZmiany, HasloHash, HasloSol)
                SELECT Login, NumerZmiany, HasloHash, HasloSol FROM UzytkownicySKRYBEK
                """,
                connection);
            await copyCmd.ExecuteNonQueryAsync(cancellationToken);

            await using var dropCmd = new OleDbCommand("DROP TABLE UzytkownicySKRYBEK", connection);
            await dropCmd.ExecuteNonQueryAsync(cancellationToken);
        }
        catch { /* brak starej tabeli albo schemat jeszcze nie istnieje */ }
    }

    /// <summary>
    /// Jeśli DataReferencyjna jest ustawiona na 2025-01-01 (stary seed), nadpisuje ją na 2026-01-01.
    /// Zmiana jest jednorazowa i bezpieczna — offsety zmian pozostają bez zmian.
    /// </summary>
    private static async Task MigrateReferenceDate2026Async(
        OleDbConnection connection,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var cmd = new OleDbCommand(
                "UPDATE Ustawienia SET Wartosc = '2026-01-01' WHERE Klucz = 'DataReferencyjna' AND Wartosc = '2025-01-01'",
                connection);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
        catch { /* tabela może nie istnieć jeszcze — seed ją zaraz wypełni */ }
    }

    /// <summary>
    /// Jednorazowa aktualizacja domyślnych kolorów ról (maj 2026).
    /// </summary>
    private static async Task MigrateDefaultRoleColorsAsync(
        OleDbConnection connection,
        CancellationToken cancellationToken)
    {
        const string migrationKey = "MigratedKoloryRol20260524";

        try
        {
            await using var checkCmd = new OleDbCommand(
                "SELECT COUNT(*) FROM Ustawienia WHERE Klucz = ? AND Wartosc = '1'",
                connection);
            checkCmd.Parameters.AddWithValue("@p1", migrationKey);
            if (Convert.ToInt32(await checkCmd.ExecuteScalarAsync(cancellationToken)) > 0)
                return;

            await using var deleteCmd = new OleDbCommand("DELETE FROM KoloryStanowisk", connection);
            await deleteCmd.ExecuteNonQueryAsync(cancellationToken);

            foreach (var (klucz, kolor) in RoleKeys.DomyslneKolory)
            {
                await using var insertCmd = new OleDbCommand(
                    "INSERT INTO KoloryStanowisk (KluczRoli, KolorHex) VALUES (?, ?)",
                    connection);
                insertCmd.Parameters.AddWithValue("@p1", klucz);
                insertCmd.Parameters.AddWithValue("@p2", kolor);
                await insertCmd.ExecuteNonQueryAsync(cancellationToken);
            }

            await using var flagCmd = new OleDbCommand(
                "INSERT INTO Ustawienia (Klucz, Wartosc) VALUES (?, ?)",
                connection);
            flagCmd.Parameters.AddWithValue("@p1", migrationKey);
            flagCmd.Parameters.AddWithValue("@p2", "1");
            await flagCmd.ExecuteNonQueryAsync(cancellationToken);
        }
        catch { /* seed / schemat uzupełni brakujące dane */ }
    }

    /// <summary>
    /// Łączy KierowcaC i KierowcaCE w jedną kategorię Kierowca.
    /// </summary>
    private static async Task MigrateKierowcaRoleMergeAsync(
        OleDbConnection connection,
        CancellationToken cancellationToken)
    {
        const string migrationKey = "MigratedKoloryRolKierowca20260524";

        try
        {
            await using var checkCmd = new OleDbCommand(
                "SELECT COUNT(*) FROM Ustawienia WHERE Klucz = ? AND Wartosc = '1'",
                connection);
            checkCmd.Parameters.AddWithValue("@p1", migrationKey);
            if (Convert.ToInt32(await checkCmd.ExecuteScalarAsync(cancellationToken)) > 0)
                return;

            await using (var deleteOld = new OleDbCommand(
                "DELETE FROM KoloryStanowisk WHERE KluczRoli IN ('KierowcaC', 'KierowcaCE')",
                connection))
            {
                await deleteOld.ExecuteNonQueryAsync(cancellationToken);
            }

            var kierowcaColor = RoleKeys.DomyslneKolory[RoleKeys.Kierowca];

            await using (var deleteNew = new OleDbCommand(
                "DELETE FROM KoloryStanowisk WHERE KluczRoli = 'Kierowca'",
                connection))
            {
                await deleteNew.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var insertCmd = new OleDbCommand(
                "INSERT INTO KoloryStanowisk (KluczRoli, KolorHex) VALUES (?, ?)",
                connection))
            {
                insertCmd.Parameters.AddWithValue("@p1", RoleKeys.Kierowca);
                insertCmd.Parameters.AddWithValue("@p2", kierowcaColor);
                await insertCmd.ExecuteNonQueryAsync(cancellationToken);
            }

            await using var flagCmd = new OleDbCommand(
                "INSERT INTO Ustawienia (Klucz, Wartosc) VALUES (?, ?)",
                connection);
            flagCmd.Parameters.AddWithValue("@p1", migrationKey);
            flagCmd.Parameters.AddWithValue("@p2", "1");
            await flagCmd.ExecuteNonQueryAsync(cancellationToken);
        }
        catch { /* seed uzupełni brakujące dane */ }
    }

    /// <summary>
    /// Dodaje domyślny kolor WS (Wolna służba) do ustawień kolorów.
    /// </summary>
    private static async Task MigrateWolnaSluzbaColorAsync(
        OleDbConnection connection,
        CancellationToken cancellationToken)
    {
        const string migrationKey = "MigratedWolnaSluzbaColor20260524";

        try
        {
            await using var checkCmd = new OleDbCommand(
                "SELECT COUNT(*) FROM Ustawienia WHERE Klucz = ? AND Wartosc = '1'",
                connection);
            checkCmd.Parameters.AddWithValue("@p1", migrationKey);
            if (Convert.ToInt32(await checkCmd.ExecuteScalarAsync(cancellationToken)) > 0)
                return;

            await using (var existsCmd = new OleDbCommand(
                "SELECT COUNT(*) FROM KoloryStanowisk WHERE KluczRoli = ?",
                connection))
            {
                existsCmd.Parameters.AddWithValue("@p1", RoleKeys.WolnaSluzba);
                if (Convert.ToInt32(await existsCmd.ExecuteScalarAsync(cancellationToken)) == 0)
                {
                    await using var insertCmd = new OleDbCommand(
                        "INSERT INTO KoloryStanowisk (KluczRoli, KolorHex) VALUES (?, ?)",
                        connection);
                    insertCmd.Parameters.AddWithValue("@p1", RoleKeys.WolnaSluzba);
                    insertCmd.Parameters.AddWithValue("@p2", RoleKeys.DomyslneKoloryWpisow[RoleKeys.WolnaSluzba]);
                    await insertCmd.ExecuteNonQueryAsync(cancellationToken);
                }
            }

            await using var flagCmd = new OleDbCommand(
                "INSERT INTO Ustawienia (Klucz, Wartosc) VALUES (?, ?)",
                connection);
            flagCmd.Parameters.AddWithValue("@p1", migrationKey);
            flagCmd.Parameters.AddWithValue("@p2", "1");
            await flagCmd.ExecuteNonQueryAsync(cancellationToken);
        }
        catch { /* seed uzupełni brakujące dane */ }
    }

    /// <summary>
    /// Scala KPP z Nurkiem, dodaje kolor czcionki nurka do ustawień.
    /// </summary>
    private static async Task MigrateNurekRoleMergeAsync(
        OleDbConnection connection,
        CancellationToken cancellationToken)
    {
        const string migrationKey = "MigratedNurekRoleMerge20260524";

        try
        {
            await using var checkCmd = new OleDbCommand(
                "SELECT COUNT(*) FROM Ustawienia WHERE Klucz = ? AND Wartosc = '1'",
                connection);
            checkCmd.Parameters.AddWithValue("@p1", migrationKey);
            if (Convert.ToInt32(await checkCmd.ExecuteScalarAsync(cancellationToken)) > 0)
                return;

            await using (var deleteKpp = new OleDbCommand(
                "DELETE FROM KoloryStanowisk WHERE KluczRoli = ?",
                connection))
            {
                deleteKpp.Parameters.AddWithValue("@p1", RoleKeys.KierownikPracPodwodnych);
                await deleteKpp.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var existsCmd = new OleDbCommand(
                "SELECT COUNT(*) FROM KoloryStanowisk WHERE KluczRoli = ?",
                connection))
            {
                existsCmd.Parameters.AddWithValue("@p1", RoleKeys.NurekCzcionka);
                if (Convert.ToInt32(await existsCmd.ExecuteScalarAsync(cancellationToken)) == 0)
                {
                    await using var insertCmd = new OleDbCommand(
                        "INSERT INTO KoloryStanowisk (KluczRoli, KolorHex) VALUES (?, ?)",
                        connection);
                    insertCmd.Parameters.AddWithValue("@p1", RoleKeys.NurekCzcionka);
                    insertCmd.Parameters.AddWithValue("@p2", RoleKeys.DomyslneKoloryWpisow[RoleKeys.NurekCzcionka]);
                    await insertCmd.ExecuteNonQueryAsync(cancellationToken);
                }
            }

            await using var flagCmd = new OleDbCommand(
                "INSERT INTO Ustawienia (Klucz, Wartosc) VALUES (?, ?)",
                connection);
            flagCmd.Parameters.AddWithValue("@p1", migrationKey);
            flagCmd.Parameters.AddWithValue("@p2", "1");
            await flagCmd.ExecuteNonQueryAsync(cancellationToken);
        }
        catch { /* seed uzupełni brakujące dane */ }
    }

    /// <summary>
    /// Usuwa osobny kolor D — D i WS korzystają z WolnaSluzba.
    /// </summary>
    private static async Task MigrateRemoveDyzurColorAsync(
        OleDbConnection connection,
        CancellationToken cancellationToken)
    {
        const string migrationKey = "MigratedRemoveDyzurColor20260524";

        try
        {
            await using var checkCmd = new OleDbCommand(
                "SELECT COUNT(*) FROM Ustawienia WHERE Klucz = ? AND Wartosc = '1'",
                connection);
            checkCmd.Parameters.AddWithValue("@p1", migrationKey);
            if (Convert.ToInt32(await checkCmd.ExecuteScalarAsync(cancellationToken)) > 0)
                return;

            await using (var deleteCmd = new OleDbCommand(
                "DELETE FROM KoloryStanowisk WHERE KluczRoli = ?",
                connection))
            {
                deleteCmd.Parameters.AddWithValue("@p1", RoleKeys.Dyzur);
                await deleteCmd.ExecuteNonQueryAsync(cancellationToken);
            }

            await using var flagCmd = new OleDbCommand(
                "INSERT INTO Ustawienia (Klucz, Wartosc) VALUES (?, ?)",
                connection);
            flagCmd.Parameters.AddWithValue("@p1", migrationKey);
            flagCmd.Parameters.AddWithValue("@p2", "1");
            await flagCmd.ExecuteNonQueryAsync(cancellationToken);
        }
        catch { /* seed uzupełni brakujące dane */ }
    }

    /// <summary>
    /// Dodaje domyślne kolory nagłówka i stopki eksportu Excel.
    /// </summary>
    private static async Task MigrateExportBandColorsAsync(
        OleDbConnection connection,
        CancellationToken cancellationToken)
    {
        const string migrationKey = "MigratedExportBandColors20260524";

        try
        {
            await using var checkCmd = new OleDbCommand(
                "SELECT COUNT(*) FROM Ustawienia WHERE Klucz = ? AND Wartosc = '1'",
                connection);
            checkCmd.Parameters.AddWithValue("@p1", migrationKey);
            if (Convert.ToInt32(await checkCmd.ExecuteScalarAsync(cancellationToken)) > 0)
                return;

            foreach (var (klucz, kolor) in RoleKeys.DomyslneKoloryEksportu)
            {
                await using var existsCmd = new OleDbCommand(
                    "SELECT COUNT(*) FROM KoloryStanowisk WHERE KluczRoli = ?",
                    connection);
                existsCmd.Parameters.AddWithValue("@p1", klucz);
                if (Convert.ToInt32(await existsCmd.ExecuteScalarAsync(cancellationToken)) > 0)
                    continue;

                await using var insertCmd = new OleDbCommand(
                    "INSERT INTO KoloryStanowisk (KluczRoli, KolorHex) VALUES (?, ?)",
                    connection);
                insertCmd.Parameters.AddWithValue("@p1", klucz);
                insertCmd.Parameters.AddWithValue("@p2", kolor);
                await insertCmd.ExecuteNonQueryAsync(cancellationToken);
            }

            await using var flagCmd = new OleDbCommand(
                "INSERT INTO Ustawienia (Klucz, Wartosc) VALUES (?, ?)",
                connection);
            flagCmd.Parameters.AddWithValue("@p1", migrationKey);
            flagCmd.Parameters.AddWithValue("@p2", "1");
            await flagCmd.ExecuteNonQueryAsync(cancellationToken);
        }
        catch { /* seed uzupełni brakujące dane */ }
    }

    /// <summary>
    /// Jeśli tabela Ustawienia istnieje z kolumną Wartosc TEXT(500) (Access jej nie obsługuje),
    /// usuwa tabelę i tworzy ponownie z TEXT(255).
    /// </summary>
    private static async Task MigrateUstawieniaTabelaAsync(
        OleDbConnection connection,
        CancellationToken cancellationToken)
    {
        try
        {
            // Próba INSERT testowego — jeśli rzuca wyjątek o rozmiarze, tabela ma zły schemat
            await using var testCmd = new OleDbCommand(
                "INSERT INTO Ustawienia (Klucz, Wartosc) VALUES ('__test__', 'ok')",
                connection);
            await testCmd.ExecuteNonQueryAsync(cancellationToken);

            // Usuń rekord testowy
            await using var delCmd = new OleDbCommand(
                "DELETE FROM Ustawienia WHERE Klucz = '__test__'",
                connection);
            await delCmd.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (OleDbException ex) when (ex.Message.Contains("za duż", StringComparison.OrdinalIgnoreCase)
                                      || ex.Message.Contains("too large", StringComparison.OrdinalIgnoreCase)
                                      || ex.Message.Contains("field size", StringComparison.OrdinalIgnoreCase))
        {
            // Tabela ma nieprawidłowy schemat — usuń i utwórz ponownie
            await using var drop = new OleDbCommand("DROP TABLE Ustawienia", connection);
            await drop.ExecuteNonQueryAsync(cancellationToken);

            await ExecuteDdlAsync(connection,
                """
                CREATE TABLE Ustawienia (
                    Klucz TEXT(100) NOT NULL,
                    Wartosc TEXT(255) NOT NULL
                )
                """,
                cancellationToken);
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
            // Tabela już istnieje — pomijamy.
        }
    }
}
