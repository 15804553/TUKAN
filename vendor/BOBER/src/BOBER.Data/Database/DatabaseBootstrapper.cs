using System.Data.OleDb;
using System.Runtime.InteropServices;
using BOBER.Core.Constants;
using BOBER.Core.Enums;
using BOBER.Core.Models;

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
        await MigrateKolorAktywnyColumnAsync(connection, cancellationToken);
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
        await MigrateObsadaFunkcjiUwagiMiesieczneTableAsync(connection, cancellationToken);
        await MigrateKalendarzTablesAsync(connection, cancellationToken);
        await MigrateKalendarzKoloryAsync(connection, cancellationToken);
        await MigrateKalendarzEntryTypesAsync(connection, cancellationToken);
        await MigrateOznaczeniaGrafikuAsync(connection, cancellationToken);
        await MigrateGrafikTypWpisuText20Async(connection, cancellationToken);
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
                TypWpisu TEXT(30) NOT NULL,
                AutorZmianaId SHORT,
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

    /// <summary>
    /// Dodaje kolumnę Aktywny do KoloryStanowisk — istniejące kolory pozostają włączone.
    /// </summary>
    private static async Task MigrateKolorAktywnyColumnAsync(
        OleDbConnection connection,
        CancellationToken cancellationToken)
    {
        const string migrationKey = "MigratedKolorAktywny20260909";

        try
        {
            await using var checkCmd = new OleDbCommand(
                "SELECT COUNT(*) FROM Ustawienia WHERE Klucz = ? AND Wartosc = '1'",
                connection);
            checkCmd.Parameters.AddWithValue("@p1", migrationKey);
            if (Convert.ToInt32(await checkCmd.ExecuteScalarAsync(cancellationToken)) > 0)
                return;

            await TryAlterTableAsync(
                connection,
                "ALTER TABLE KoloryStanowisk ADD COLUMN Aktywny YESNO",
                cancellationToken);

            await using (var updateCmd = new OleDbCommand(
                "UPDATE KoloryStanowisk SET Aktywny = True",
                connection))
            {
                await updateCmd.ExecuteNonQueryAsync(cancellationToken);
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

    private static async Task InsertKolorStanowiskaAsync(
        OleDbConnection connection,
        string klucz,
        string hex,
        CancellationToken cancellationToken)
    {
        await using var insertCmd = new OleDbCommand(
            "INSERT INTO KoloryStanowisk (KluczRoli, KolorHex, Aktywny) VALUES (?, ?, ?)",
            connection);
        insertCmd.Parameters.AddWithValue("@p1", klucz);
        insertCmd.Parameters.AddWithValue("@p2", hex);
        insertCmd.Parameters.Add("@p3", OleDbType.Boolean).Value = true;
        await insertCmd.ExecuteNonQueryAsync(cancellationToken);
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

                await InsertKolorStanowiskaAsync(connection, klucz, kolor, cancellationToken);
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

    private static async Task MigrateKalendarzEntryTypesAsync(
        OleDbConnection connection,
        CancellationToken cancellationToken)
    {
        const string migrationKey = "MigratedKalendarzEntryTypes20260728";

        try
        {
            await using var checkCmd = new OleDbCommand(
                "SELECT COUNT(*) FROM Ustawienia WHERE Klucz = ? AND Wartosc = '1'",
                connection);
            checkCmd.Parameters.AddWithValue("@p1", migrationKey);
            if (Convert.ToInt32(await checkCmd.ExecuteScalarAsync(cancellationToken)) > 0)
                return;

            await TryAlterTableAsync(
                connection,
                "ALTER TABLE KalendarzWpisy ADD COLUMN TypWpisu TEXT(30)",
                cancellationToken);
            await TryAlterTableAsync(
                connection,
                "ALTER TABLE KalendarzWpisy ADD COLUMN AutorZmianaId SHORT",
                cancellationToken);

            await using (var updateCmd = new OleDbCommand(
                "UPDATE KalendarzWpisy SET TypWpisu = 'Dca' WHERE TypWpisu IS NULL OR TypWpisu = ''",
                connection))
            {
                await updateCmd.ExecuteNonQueryAsync(cancellationToken);
            }

            await using var flagCmd = new OleDbCommand(
                "INSERT INTO Ustawienia (Klucz, Wartosc) VALUES (?, ?)",
                connection);
            flagCmd.Parameters.AddWithValue("@p1", migrationKey);
            flagCmd.Parameters.AddWithValue("@p2", "1");
            await flagCmd.ExecuteNonQueryAsync(cancellationToken);
        }
        catch
        {
            // Przy pierwszym uruchomieniu pełny schemat utworzy tabele już z nowymi kolumnami.
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

    private static async Task MigrateObsadaFunkcjiUwagiMiesieczneTableAsync(
        OleDbConnection connection,
        CancellationToken cancellationToken)
    {
        await ExecuteDdlAsync(connection,
            """
            CREATE TABLE ObsadaFunkcjiUwagiMiesieczne (
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
                    await InsertKolorStanowiskaAsync(
                        connection,
                        RoleKeys.DzienSluzby,
                        RoleKeys.DomyslneKoloryWpisow[RoleKeys.DzienSluzby],
                        cancellationToken);
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
                await InsertKolorStanowiskaAsync(connection, klucz, kolor, cancellationToken);

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

            await InsertKolorStanowiskaAsync(
                connection, RoleKeys.Kierowca, kierowcaColor, cancellationToken);

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
                    await InsertKolorStanowiskaAsync(
                        connection,
                        RoleKeys.WolnaSluzba,
                        RoleKeys.DomyslneKoloryWpisow[RoleKeys.WolnaSluzba],
                        cancellationToken);
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
                    await InsertKolorStanowiskaAsync(
                        connection,
                        RoleKeys.NurekCzcionka,
                        RoleKeys.DomyslneKoloryWpisow[RoleKeys.NurekCzcionka],
                        cancellationToken);
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

                await InsertKolorStanowiskaAsync(connection, klucz, kolor, cancellationToken);
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

    private static async Task MigrateOznaczeniaGrafikuAsync(
        OleDbConnection connection,
        CancellationToken cancellationToken)
    {
        await ExecuteDdlAsync(connection,
            """
            CREATE TABLE OznaczeniaGrafiku (
                Id AUTOINCREMENT PRIMARY KEY,
                ZmianaId SHORT NOT NULL,
                Kod TEXT(20) NOT NULL,
                Nazwa TEXT(50) NOT NULL,
                KolorHex TEXT(10) NOT NULL,
                WPracy YESNO NOT NULL,
                SekcjaRozkazu SHORT,
                SkrotKlawiszowy TEXT(20),
                TekstWyswietlany TEXT(20),
                MoznaOddac YESNO NOT NULL,
                MoznaKropke YESNO NOT NULL,
                ZachowajTloWsPrzyBraku YESNO NOT NULL,
                DodatkowaSekcjaRozkazu SHORT,
                RolaNalozania SHORT NOT NULL,
                Kolejnosc SHORT NOT NULL,
                EksportDoExcela YESNO NOT NULL,
                KolorExcelHex TEXT(10),
                AdnotacjaRozkazu TEXT(40),
                StylWyswietlania SHORT NOT NULL,
                FlagaPozycja SHORT NOT NULL
            )
            """,
            cancellationToken);

        await TryAlterTableAsync(
            connection,
            "ALTER TABLE OznaczeniaGrafiku ADD COLUMN EksportDoExcela YESNO",
            cancellationToken);
        await TryAlterTableAsync(
            connection,
            "ALTER TABLE OznaczeniaGrafiku ADD COLUMN KolorExcelHex TEXT(10)",
            cancellationToken);
        await TryAlterTableAsync(
            connection,
            "ALTER TABLE OznaczeniaGrafiku ADD COLUMN ZmianaId SHORT",
            cancellationToken);
        await TryAlterTableAsync(
            connection,
            "ALTER TABLE OznaczeniaGrafiku ADD COLUMN AdnotacjaRozkazu TEXT(40)",
            cancellationToken);
        await TryAlterTableAsync(
            connection,
            "ALTER TABLE OznaczeniaGrafiku ADD COLUMN StylWyswietlania SHORT",
            cancellationToken);
        await TryAlterTableAsync(
            connection,
            "ALTER TABLE OznaczeniaGrafiku ADD COLUMN FlagaPozycja SHORT",
            cancellationToken);

        // Uzupełnij brakujące wartości dla wierszy sprzed migracji kolumn.
        try
        {
            await using var fillExcel = new OleDbCommand(
                """
                UPDATE OznaczeniaGrafiku
                SET KolorExcelHex = KolorHex
                WHERE KolorExcelHex IS NULL OR KolorExcelHex = ''
                """,
                connection);
            await fillExcel.ExecuteNonQueryAsync(cancellationToken);
        }
        catch { /* ignore */ }

        try
        {
            await using var fillExport = new OleDbCommand(
                "UPDATE OznaczeniaGrafiku SET EksportDoExcela = True WHERE EksportDoExcela IS NULL",
                connection);
            await fillExport.ExecuteNonQueryAsync(cancellationToken);
        }
        catch { /* ignore */ }

        try
        {
            await using var fillZmiana = new OleDbCommand(
                "UPDATE OznaczeniaGrafiku SET ZmianaId = 1 WHERE ZmianaId IS NULL OR ZmianaId = 0",
                connection);
            await fillZmiana.ExecuteNonQueryAsync(cancellationToken);
        }
        catch { /* ignore */ }

        try
        {
            await using var fillStyl = new OleDbCommand(
                "UPDATE OznaczeniaGrafiku SET StylWyswietlania = 0 WHERE StylWyswietlania IS NULL",
                connection);
            await fillStyl.ExecuteNonQueryAsync(cancellationToken);
        }
        catch { /* ignore */ }

        try
        {
            await using var fillFlaga = new OleDbCommand(
                "UPDATE OznaczeniaGrafiku SET FlagaPozycja = 0 WHERE FlagaPozycja IS NULL",
                connection);
            await fillFlaga.ExecuteNonQueryAsync(cancellationToken);
        }
        catch { /* ignore */ }

        try
        {
            await using var pytajnikPrawa = new OleDbCommand(
                """
                UPDATE OznaczeniaGrafiku
                SET FlagaPozycja = 2, KolorHex = '#000000'
                WHERE Kod = '?'
                """,
                connection);
            await pytajnikPrawa.ExecuteNonQueryAsync(cancellationToken);
        }
        catch { /* ignore */ }

        await EnsureOddajeOznaczenieAsync(connection, cancellationToken);
        await EnsureChceOddacOznaczenieAsync(connection, cancellationToken);
        await CloneOznaczeniaToMissingZmianyAsync(connection, cancellationToken);
    }

    private static async Task EnsureOddajeOznaczenieAsync(
        OleDbConnection connection,
        CancellationToken cancellationToken)
    {
        try
        {
            for (short zmiana = 1; zmiana <= 3; zmiana++)
            {
                await using var countCmd = new OleDbCommand(
                    "SELECT COUNT(*) FROM OznaczeniaGrafiku WHERE ZmianaId = ? AND Kod = ?",
                    connection);
                countCmd.Parameters.AddWithValue("@z", zmiana);
                countCmd.Parameters.AddWithValue("@k", OznaczeniaGrafikuSeed.KodOddaje);
                if (Convert.ToInt32(await countCmd.ExecuteScalarAsync(cancellationToken)) > 0)
                {
                    await using var upd = new OleDbCommand(
                        """
                        UPDATE OznaczeniaGrafiku
                        SET FlagaPozycja = 3, StylWyswietlania = 2, WPracy = True,
                            KolorHex = '#000000'
                        WHERE ZmianaId = ? AND Kod = ?
                        """,
                        connection);
                    upd.Parameters.AddWithValue("@z", zmiana);
                    upd.Parameters.AddWithValue("@k", OznaczeniaGrafikuSeed.KodOddaje);
                    await upd.ExecuteNonQueryAsync(cancellationToken);
                    continue;
                }

                await InsertSystemOznaczenieAsync(
                    connection,
                    zmiana,
                    OznaczeniaGrafikuSeed.KodOddaje,
                    "Oddaje",
                    skrot: "O",
                    tekst: null,
                    styl: StylWyswietlaniaOznaczenia.Przekreslenie,
                    flaga: FlagaPozycjaOznaczenia.Centrum,
                    cancellationToken);
            }
        }
        catch
        {
            /* seed uzupełni przy pustej zmianie */
        }
    }

    private static async Task EnsureChceOddacOznaczenieAsync(
        OleDbConnection connection,
        CancellationToken cancellationToken)
    {
        try
        {
            for (short zmiana = 1; zmiana <= 3; zmiana++)
            {
                // Migracja starego kodu CHCEODDAC → symbol •.
                await using (var rename = new OleDbCommand(
                                 """
                                 UPDATE OznaczeniaGrafiku
                                 SET Kod = ?, TekstWyswietlany = NULL, FlagaPozycja = 2, WPracy = True,
                                     SkrotKlawiszowy = '.', Nazwa = 'Chce oddać', KolorHex = '#000000'
                                 WHERE ZmianaId = ? AND Kod = ?
                                 """,
                                 connection))
                {
                    rename.Parameters.AddWithValue("@new", OznaczeniaGrafikuSeed.KodChceOddac);
                    rename.Parameters.AddWithValue("@z", zmiana);
                    rename.Parameters.AddWithValue("@old", OznaczeniaGrafikuSeed.KodChceOddacLegacy);
                    await rename.ExecuteNonQueryAsync(cancellationToken);
                }

                await using var countCmd = new OleDbCommand(
                    "SELECT COUNT(*) FROM OznaczeniaGrafiku WHERE ZmianaId = ? AND Kod = ?",
                    connection);
                countCmd.Parameters.AddWithValue("@z", zmiana);
                countCmd.Parameters.AddWithValue("@k", OznaczeniaGrafikuSeed.KodChceOddac);
                if (Convert.ToInt32(await countCmd.ExecuteScalarAsync(cancellationToken)) > 0)
                {
                    await using var upd = new OleDbCommand(
                        """
                        UPDATE OznaczeniaGrafiku
                        SET FlagaPozycja = 2, WPracy = True, TekstWyswietlany = NULL,
                            SkrotKlawiszowy = '.', Nazwa = 'Chce oddać', KolorHex = '#000000'
                        WHERE ZmianaId = ? AND Kod = ?
                        """,
                        connection);
                    upd.Parameters.AddWithValue("@z", zmiana);
                    upd.Parameters.AddWithValue("@k", OznaczeniaGrafikuSeed.KodChceOddac);
                    await upd.ExecuteNonQueryAsync(cancellationToken);
                    continue;
                }

                await InsertSystemOznaczenieAsync(
                    connection,
                    zmiana,
                    OznaczeniaGrafikuSeed.KodChceOddac,
                    "Chce oddać",
                    skrot: ".",
                    tekst: null,
                    styl: StylWyswietlaniaOznaczenia.Normalny,
                    flaga: FlagaPozycjaOznaczenia.Prawa,
                    cancellationToken);
            }
        }
        catch
        {
            /* seed uzupełni przy pustej zmianie */
        }
    }

    private static async Task InsertSystemOznaczenieAsync(
        OleDbConnection connection,
        short zmianaId,
        string kod,
        string nazwa,
        string skrot,
        string? tekst,
        StylWyswietlaniaOznaczenia styl,
        FlagaPozycjaOznaczenia flaga,
        CancellationToken cancellationToken)
    {
        await using var maxCmd = new OleDbCommand(
            "SELECT MAX(Kolejnosc) FROM OznaczeniaGrafiku WHERE ZmianaId = ?",
            connection);
        maxCmd.Parameters.AddWithValue("@z", zmianaId);
        var maxObj = await maxCmd.ExecuteScalarAsync(cancellationToken);
        var next = maxObj is null or DBNull ? (short)1 : (short)(Convert.ToInt16(maxObj) + 1);

        await using var insert = new OleDbCommand(
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
        insert.Parameters.AddWithValue("@p0", zmianaId);
        insert.Parameters.AddWithValue("@p1", kod);
        insert.Parameters.AddWithValue("@p2", nazwa);
        insert.Parameters.AddWithValue("@p3",
            flaga == FlagaPozycjaOznaczenia.Nie ? RoleKeys.BrakWypelnienia : OznaczenieGrafiku.DomyslnyKolorCzcionkiFlagi);
        insert.Parameters.AddWithValue("@p4", true);
        insert.Parameters.AddWithValue("@p5", DBNull.Value);
        insert.Parameters.AddWithValue("@p6", skrot);
        insert.Parameters.AddWithValue("@p7", (object?)tekst ?? DBNull.Value);
        insert.Parameters.AddWithValue("@p8", false);
        insert.Parameters.AddWithValue("@p9", false);
        insert.Parameters.AddWithValue("@p10", false);
        insert.Parameters.AddWithValue("@p11", DBNull.Value);
        insert.Parameters.AddWithValue("@p12", (short)0);
        insert.Parameters.AddWithValue("@p13", next);
        insert.Parameters.AddWithValue("@p14", true);
        insert.Parameters.AddWithValue("@p15", RoleKeys.BrakWypelnienia);
        insert.Parameters.AddWithValue("@p16", string.Empty);
        insert.Parameters.AddWithValue("@p17", (short)styl);
        insert.Parameters.AddWithValue("@p18", (short)flaga);
        await insert.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Kopiuje oznaczenia ze zmiany 1 do zmian 2 i 3, jeśli te jeszcze nie mają własnej konfiguracji.
    /// </summary>
    private static async Task CloneOznaczeniaToMissingZmianyAsync(
        OleDbConnection connection,
        CancellationToken cancellationToken)
    {
        try
        {
            for (short target = 2; target <= 3; target++)
            {
                await using var countCmd = new OleDbCommand(
                    "SELECT COUNT(*) FROM OznaczeniaGrafiku WHERE ZmianaId = ?",
                    connection);
                countCmd.Parameters.AddWithValue("@p1", target);
                if (Convert.ToInt32(await countCmd.ExecuteScalarAsync(cancellationToken)) > 0)
                    continue;

                await using var selectCmd = new OleDbCommand(
                    """
                    SELECT Kod, Nazwa, KolorHex, WPracy, SekcjaRozkazu, SkrotKlawiszowy,
                           TekstWyswietlany, MoznaOddac, MoznaKropke, ZachowajTloWsPrzyBraku,
                           DodatkowaSekcjaRozkazu, RolaNalozania, Kolejnosc,
                           EksportDoExcela, KolorExcelHex, AdnotacjaRozkazu,
                           StylWyswietlania, FlagaPozycja
                    FROM OznaczeniaGrafiku
                    WHERE ZmianaId = 1
                    ORDER BY Kolejnosc, Kod
                    """,
                    connection);

                var rows = new List<object[]>();
                await using (var reader = await selectCmd.ExecuteReaderAsync(cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        var values = new object[reader.FieldCount];
                        reader.GetValues(values);
                        rows.Add(values);
                    }
                }

                foreach (var values in rows)
                {
                    await using var insertCmd = new OleDbCommand(
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
                    insertCmd.Parameters.AddWithValue("@z", target);
                    foreach (var v in values)
                        insertCmd.Parameters.AddWithValue("@p", v is null or DBNull ? DBNull.Value : v);
                    await insertCmd.ExecuteNonQueryAsync(cancellationToken);
                }
            }
        }
        catch
        {
            /* tabela / kolumna niedostępna — seed uzupełni później */
        }
    }

    /// <summary>
    /// Poszerza TypWpisu z TEXT(5) do TEXT(20) (własne kody + sufiksy).
    /// </summary>
    private static async Task MigrateGrafikTypWpisuText20Async(
        OleDbConnection connection,
        CancellationToken cancellationToken)
    {
        const string migrationKey = "MigratedGrafikTypWpisuText20";

        try
        {
            await using var checkCmd = new OleDbCommand(
                "SELECT COUNT(*) FROM Ustawienia WHERE Klucz = ? AND Wartosc = '1'",
                connection);
            checkCmd.Parameters.AddWithValue("@p1", migrationKey);
            if (Convert.ToInt32(await checkCmd.ExecuteScalarAsync(cancellationToken)) > 0)
                return;

            try
            {
                await using var alterCmd = new OleDbCommand(
                    "ALTER TABLE GrafikWpisy ALTER COLUMN TypWpisu TEXT(20)",
                    connection);
                await alterCmd.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (OleDbException)
            {
                // ACE czasem nie zmienia rozmiaru — nowe bazy i tak mają TEXT(20) ze schematu.
            }

            await using var flagCmd = new OleDbCommand(
                "INSERT INTO Ustawienia (Klucz, Wartosc) VALUES (?, ?)",
                connection);
            flagCmd.Parameters.AddWithValue("@p1", migrationKey);
            flagCmd.Parameters.AddWithValue("@p2", "1");
            await flagCmd.ExecuteNonQueryAsync(cancellationToken);
        }
        catch
        {
            /* starsze bazy — TEXT(5) nadal mieści Del* */
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

    private static async Task TryAlterTableAsync(
        OleDbConnection connection,
        string ddl,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var command = new OleDbCommand(ddl, connection);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (OleDbException ex) when (ex.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase)
                                         || ex.Message.Contains("już istnieje", StringComparison.OrdinalIgnoreCase)
                                         || ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
        {
            // Kolumna już istnieje — pomijamy.
        }
    }
}
