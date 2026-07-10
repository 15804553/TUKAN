using System.Data;
using System.Data.Common;
using System.Data.OleDb;
using Chomik.Core.Models;
using Chomik.Data;

namespace Chomik.Data.Repositories;

public sealed class FunkcjonariuszRepository(AccessConnectionFactory connectionFactory)
    : IFunkcjonariuszRepository
{
    private const string SelectSql = """
        SELECT f.Id, f.NumerZmiany, f.NumerPorzadkowy, f.StopienId, f.StanowiskoId, s.Nazwa AS Stopien, f.Imie, f.Nazwisko,
               st.Nazwa AS Stanowisko, f.Telefon, f.StazLat, f.BadaniaOkresoweDo, f.KomoraDymowaDo, f.KppDo,
               f.DataWstepieniaDoSluzby, f.InformacjaDodatkowa, f.DataAwansuStopien, f.DataAwansuGrupa, f.DodatekMotywacyjny
        FROM ((Funkcjonariusze AS f
        INNER JOIN StopnieSlownik AS s ON s.Id = f.StopienId)
        INNER JOIN StanowiskaSlownik AS st ON st.Id = f.StanowiskoId)
        """;

    private const string OrderByListSql = " ORDER BY f.NumerZmiany, f.NumerPorzadkowy, f.Id";

    private const string NamesSelectPrefix = """
        SELECT f.Imie, f.Nazwisko
        """;

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        return connection.State == ConnectionState.Open;
    }

    public async Task<IReadOnlyList<Funkcjonariusz>> GetAllAsync(
        FunkcjonariuszLoadOptions? loadOptions = null,
        CancellationToken cancellationToken = default)
    {
        loadOptions ??= FunkcjonariuszLoadOptions.Full;

        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new OleDbCommand(
            SelectSql + OrderByListSql,
            connection);

        var result = await ReadAllAsync(command, cancellationToken);

        if (loadOptions.IncludeUprawnienia)
        {
            await AttachUprawnieniaAsync(connection, result, cancellationToken);
        }

        if (loadOptions.IncludeOdznaczenia)
        {
            await AttachOdznaczeniaAsync(connection, result, cancellationToken);
        }

        return result;
    }

    public async Task<IReadOnlyList<Funkcjonariusz>> GetListAsync(
        FunkcjonariuszListQuery query,
        FunkcjonariuszLoadOptions? loadOptions = null,
        CancellationToken cancellationToken = default)
    {
        loadOptions ??= FunkcjonariuszLoadOptions.Full;

        await using var connection = connectionFactory.CreateOpenConnection();
        var (sql, parameters) = BuildListQuery(query);
        await using var command = new OleDbCommand(sql, connection);
        foreach (var parameter in parameters)
        {
            command.Parameters.Add(parameter);
        }

        var result = await ReadAllAsync(command, cancellationToken);

        if (loadOptions.IncludeUprawnienia)
        {
            await AttachUprawnieniaAsync(connection, result, cancellationToken);
        }

        if (loadOptions.IncludeOdznaczenia)
        {
            await AttachOdznaczeniaAsync(connection, result, cancellationToken);
        }

        return result;
    }

    public async Task<GeneralViewPersonnelBundle> LoadGeneralViewBundleAsync(
        FunkcjonariuszListQuery query,
        bool includeSensitiveRelations,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        var (sql, parameters) = BuildListQuery(query);
        await using var listCommand = new OleDbCommand(sql, connection);
        foreach (var parameter in parameters)
        {
            listCommand.Parameters.Add(parameter);
        }

        var personnel = await ReadAllAsync(listCommand, cancellationToken);
        var uprawnieniaByPersonId = await ReadUprawnieniaDictionaryAsync(connection, personnel, query, cancellationToken);

        if (includeSensitiveRelations)
        {
            await AttachOdznaczeniaAsync(connection, personnel, cancellationToken);
        }

        return new GeneralViewPersonnelBundle
        {
            Personnel = personnel,
            UprawnieniaByPersonId = uprawnieniaByPersonId
        };
    }

    public async Task<IReadOnlyList<string>> GetPersonnelFullNamesAsync(
        FunkcjonariuszListQuery query,
        CancellationToken cancellationToken = default)
    {
        var exportQuery = new FunkcjonariuszListQuery
        {
            NumerZmiany = query.NumerZmiany
        };

        var (listSql, parameters) = BuildListQuery(exportQuery);
        var fromIndex = listSql.IndexOf("\nFROM ", StringComparison.Ordinal);
        if (fromIndex < 0)
        {
            throw new InvalidOperationException("Nie można zbudować zapytania listy imion i nazwisk.");
        }

        var sql = NamesSelectPrefix + listSql[fromIndex..];

        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new OleDbCommand(sql, connection);
        foreach (var parameter in parameters)
        {
            command.Parameters.Add(parameter);
        }

        var names = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var imie = reader.GetString(0);
            var nazwisko = reader.GetString(1);
            names.Add($"{imie} {nazwisko}".Trim());
        }

        return names;
    }

    public async Task<Funkcjonariusz?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new OleDbCommand(SelectSql + " WHERE f.Id = ?", connection);
        command.Parameters.AddWithValue("@p1", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var entity = Map(reader);
        await AttachUprawnieniaAsync(connection, [entity], cancellationToken);
        await AttachOdznaczeniaAsync(connection, [entity], cancellationToken);
        return entity;
    }

    public async Task<int> InsertAsync(Funkcjonariusz entity, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = BuildInsertCommand(connection, entity);
        await command.ExecuteNonQueryAsync(cancellationToken);

        await using var idCommand = new OleDbCommand("SELECT @@IDENTITY", connection);
        return Convert.ToInt32(await idCommand.ExecuteScalarAsync(cancellationToken));
    }

    public async Task UpdateAsync(Funkcjonariusz entity, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new OleDbCommand(
            """
            UPDATE Funkcjonariusze SET
                NumerZmiany = ?, NumerPorzadkowy = ?, StopienId = ?, Imie = ?, Nazwisko = ?, StanowiskoId = ?,
                Telefon = ?, StazLat = ?, BadaniaOkresoweDo = ?, KomoraDymowaDo = ?, KppDo = ?,
                DataWstepieniaDoSluzby = ?, InformacjaDodatkowa = ?, DataAwansuStopien = ?, DataAwansuGrupa = ?, DodatekMotywacyjny = ?
            WHERE Id = ?
            """,
            connection);
        AddCommonParameters(command, entity);
        command.Parameters.AddWithValue("@id", entity.Id);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateNumerZmianyAsync(
        int funkcjonariuszId,
        int numerZmiany,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new OleDbCommand(
            "UPDATE Funkcjonariusze SET NumerZmiany = ? WHERE Id = ?",
            connection);
        command.Parameters.AddWithValue("@p1", (short)numerZmiany);
        command.Parameters.AddWithValue("@p2", funkcjonariuszId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateStopienIdAsync(
        int funkcjonariuszId,
        int stopienId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new OleDbCommand(
            "UPDATE Funkcjonariusze SET StopienId = ? WHERE Id = ?",
            connection);
        command.Parameters.AddWithValue("@p1", stopienId);
        command.Parameters.AddWithValue("@p2", funkcjonariuszId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateTerminyMedyczneAsync(
        int funkcjonariuszId,
        DateTime? badaniaOkresoweDo,
        DateTime? komoraDymowaDo,
        DateTime? kppDo,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new OleDbCommand(
            """
            UPDATE Funkcjonariusze SET
                BadaniaOkresoweDo = ?, KomoraDymowaDo = ?, KppDo = ?
            WHERE Id = ?
            """,
            connection);
        command.Parameters.AddWithValue("@p1", badaniaOkresoweDo ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@p2", komoraDymowaDo ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@p3", kppDo ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@p4", funkcjonariuszId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateUprawnienieWazneDoAsync(
        int uprawnieniePrzypisanieId,
        DateTime? wazneDo,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new OleDbCommand(
            "UPDATE FunkcjonariuszUprawnienia SET WazneDo = ? WHERE Id = ?",
            connection);
        command.Parameters.AddWithValue("@p1", wazneDo ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@p2", uprawnieniePrzypisanieId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        await DeleteChildRowsAsync(connection, id, cancellationToken);

        await using var command = new OleDbCommand("DELETE FROM Funkcjonariusze WHERE Id = ?", connection);
        command.Parameters.AddWithValue("@p1", id);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task ReplaceUprawnieniaAsync(
        int funkcjonariuszId,
        IReadOnlyList<int> typUprawnieniaIds,
        IReadOnlyDictionary<int, DateTime?> datyWaznosci,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        await using var deleteCommand = new OleDbCommand(
            "DELETE FROM FunkcjonariuszUprawnienia WHERE FunkcjonariuszId = ?",
            connection);
        deleteCommand.Parameters.AddWithValue("@p1", funkcjonariuszId);
        await deleteCommand.ExecuteNonQueryAsync(cancellationToken);

        foreach (var typId in typUprawnieniaIds)
        {
            datyWaznosci.TryGetValue(typId, out var data);
            await using var insertCommand = new OleDbCommand(
                "INSERT INTO FunkcjonariuszUprawnienia (FunkcjonariuszId, TypUprawnieniaId, WazneDo) VALUES (?, ?, ?)",
                connection);
            insertCommand.Parameters.AddWithValue("@p1", funkcjonariuszId);
            insertCommand.Parameters.AddWithValue("@p2", typId);
            insertCommand.Parameters.AddWithValue("@p3", data ?? (object)DBNull.Value);
            await insertCommand.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public async Task ReplaceOdznaczeniaAsync(
        int funkcjonariuszId,
        IReadOnlyDictionary<int, DateTime> datyNadania,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        await using var deleteCommand = new OleDbCommand(
            "DELETE FROM FunkcjonariuszOdznaczenia WHERE FunkcjonariuszId = ?",
            connection);
        deleteCommand.Parameters.AddWithValue("@p1", funkcjonariuszId);
        await deleteCommand.ExecuteNonQueryAsync(cancellationToken);

        foreach (var (typId, dataNadania) in datyNadania)
        {
            await using var insertCommand = new OleDbCommand(
                "INSERT INTO FunkcjonariuszOdznaczenia (FunkcjonariuszId, TypOdznaczeniaId, DataNadania) VALUES (?, ?, ?)",
                connection);
            insertCommand.Parameters.AddWithValue("@p1", funkcjonariuszId);
            insertCommand.Parameters.AddWithValue("@p2", typId);
            insertCommand.Parameters.AddWithValue("@p3", dataNadania);
            await insertCommand.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static OleDbCommand BuildInsertCommand(OleDbConnection connection, Funkcjonariusz entity)
    {
        var command = new OleDbCommand(
            """
            INSERT INTO Funkcjonariusze
            (NumerZmiany, NumerPorzadkowy, StopienId, Imie, Nazwisko, StanowiskoId, Telefon, StazLat,
             BadaniaOkresoweDo, KomoraDymowaDo, KppDo, DataWstepieniaDoSluzby, InformacjaDodatkowa,
             DataAwansuStopien, DataAwansuGrupa, DodatekMotywacyjny)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
            """,
            connection);
        AddCommonParameters(command, entity);
        return command;
    }

    private static void AddCommonParameters(OleDbCommand command, Funkcjonariusz entity)
    {
        command.Parameters.AddWithValue("@p1", (short)entity.NumerZmiany);
        command.Parameters.AddWithValue("@p2", (short)entity.NumerPorzadkowy);
        command.Parameters.AddWithValue("@p3", entity.StopienId);
        command.Parameters.AddWithValue("@p4", entity.Imie);
        command.Parameters.AddWithValue("@p5", entity.Nazwisko);
        command.Parameters.AddWithValue("@p6", entity.StanowiskoId);
        command.Parameters.AddWithValue("@p7", entity.Telefon ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@p8", entity.StazLat ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@p9", entity.BadaniaOkresoweDo ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@p10", entity.KomoraDymowaDo ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@p11", entity.KppDo ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@p12", entity.DataWstepieniaDoSluzby ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@p13", entity.InformacjaDodatkowa ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@p14", entity.DataAwansuStopien ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@p15", entity.DataAwansuGrupa ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@p16", entity.DodatekMotywacyjny ?? (object)DBNull.Value);
    }

    private static (string Sql, List<OleDbParameter> Parameters) BuildListQuery(FunkcjonariuszListQuery query)
    {
        var conditions = new List<string>();
        var parameters = new List<OleDbParameter>();
        var sql = SelectSql;

        if (!string.IsNullOrWhiteSpace(query.UprawnienieNazwa))
        {
            var (permissionWhere, permissionParameters) = BuildUprawnienieTypeWhere(query);
            sql += $"""

                INNER JOIN (
                    SELECT DISTINCT fu.FunkcjonariuszId
                    FROM FunkcjonariuszUprawnienia AS fu
                    INNER JOIN TypyUprawnien AS tu ON tu.Id = fu.TypUprawnieniaId
                    WHERE {permissionWhere}
                ) AS upr_sel ON upr_sel.FunkcjonariuszId = f.Id
                """;
            parameters.AddRange(permissionParameters);
        }

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var term = $"%{query.SearchTerm.Trim()}%";
            conditions.Add("(f.Imie LIKE ? OR f.Nazwisko LIKE ? OR s.Nazwa LIKE ?)");
            parameters.Add(CreateStringParameter(term));
            parameters.Add(CreateStringParameter(term));
            parameters.Add(CreateStringParameter(term));
        }

        if (query.NumerZmiany is int shift)
        {
            conditions.Add("f.NumerZmiany = ?");
            parameters.Add(CreateIntegerParameter(shift));
        }

        conditions.Add("f.NumerZmiany <> 4");

        if (conditions.Count > 0)
        {
            sql += " WHERE " + string.Join(" AND ", conditions);
        }

        sql += OrderByListSql;
        return (sql, parameters);
    }

    public async Task<int> GetNextNumerPorzadkowyAsync(
        int numerZmiany,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new OleDbCommand(
            "SELECT MAX(NumerPorzadkowy) FROM Funkcjonariusze WHERE NumerZmiany = ?",
            connection);
        command.Parameters.AddWithValue("@p1", (short)numerZmiany);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        if (result is null or DBNull)
        {
            return 1;
        }

        return Convert.ToInt32(result) + 1;
    }

    public async Task<bool> IsNumerPorzadkowyTakenAsync(
        int numerZmiany,
        int numerPorzadkowy,
        int excludeFunkcjonariuszId = 0,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new OleDbCommand(
            """
            SELECT COUNT(*)
            FROM Funkcjonariusze
            WHERE NumerZmiany = ? AND NumerPorzadkowy = ? AND Id <> ?
            """,
            connection);
        command.Parameters.AddWithValue("@p1", (short)numerZmiany);
        command.Parameters.AddWithValue("@p2", (short)numerPorzadkowy);
        command.Parameters.AddWithValue("@p3", excludeFunkcjonariuszId);
        var count = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
        return count > 0;
    }

    private static (string Where, List<OleDbParameter> Parameters) BuildUprawnienieTypeWhere(FunkcjonariuszListQuery query)
    {
        var parts = new List<string>();
        var parameters = new List<OleDbParameter>();
        var nazwa = query.UprawnienieNazwa!.Trim();

        parts.Add("tu.Nazwa = ?");
        parameters.Add(CreateStringParameter(nazwa));

        if (string.IsNullOrWhiteSpace(query.UprawnieniePodtyp))
        {
            parts.Add("(tu.Podtyp IS NULL OR tu.Podtyp = '' OR tu.Podtyp = ' ')");
        }
        else
        {
            parts.Add("tu.Podtyp = ?");
            parameters.Add(CreateStringParameter(query.UprawnieniePodtyp.Trim()));
        }

        return (string.Join(" AND ", parts), parameters);
    }

    private static OleDbParameter CreateStringParameter(string value) =>
        new() { OleDbType = OleDbType.VarWChar, Value = value };

    private static OleDbParameter CreateIntegerParameter(int value) =>
        new() { OleDbType = OleDbType.Integer, Value = value };

    private static async Task<List<Funkcjonariusz>> ReadAllAsync(
        OleDbCommand command,
        CancellationToken cancellationToken)
    {
        var result = new List<Funkcjonariusz>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(Map(reader));
        }

        return result;
    }

    private static async Task<IReadOnlyDictionary<int, IReadOnlyList<UprawnieniePrzypisanie>>> ReadUprawnieniaDictionaryAsync(
        OleDbConnection connection,
        IReadOnlyList<Funkcjonariusz> funkcjonariusze,
        FunkcjonariuszListQuery query,
        CancellationToken cancellationToken)
    {
        var lookup = new Dictionary<int, List<UprawnieniePrzypisanie>>();
        foreach (var funkcjonariusz in funkcjonariusze)
        {
            lookup[funkcjonariusz.Id] = [];
        }

        if (funkcjonariusze.Count == 0)
        {
            return new Dictionary<int, IReadOnlyList<UprawnieniePrzypisanie>>();
        }

        var idFilter = BuildPersonnelIdFilter(funkcjonariusze, "fu.FunkcjonariuszId");
        var permissionFilter = BuildUprawnieniaTypeFilter(query);
        await using var command = new OleDbCommand(
            $"""
            SELECT fu.FunkcjonariuszId, fu.Id, tu.Nazwa, tu.Podtyp, fu.WazneDo
            FROM FunkcjonariuszUprawnienia fu
            INNER JOIN TypyUprawnien tu ON tu.Id = fu.TypUprawnieniaId
            {idFilter}{permissionFilter}
            ORDER BY fu.FunkcjonariuszId, tu.Nazwa, tu.Podtyp
            """,
            connection);

        if (!string.IsNullOrWhiteSpace(query.UprawnienieNazwa))
        {
            var (_, permissionParameters) = BuildUprawnienieTypeWhere(query);
            foreach (var parameter in permissionParameters)
            {
                command.Parameters.Add(parameter);
            }
        }

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var funkcjonariuszId = reader.GetFieldInt32(0);
            if (!lookup.TryGetValue(funkcjonariuszId, out var list))
            {
                continue;
            }

            list.Add(new UprawnieniePrzypisanie
            {
                Id = reader.GetFieldInt32(1),
                FunkcjonariuszId = funkcjonariuszId,
                Nazwa = reader.GetString(2),
                Podtyp = reader.IsDBNull(3) ? null : reader.GetString(3),
                WazneDo = reader.GetNullableFieldDateTime(4)
            });
        }

        return lookup.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyList<UprawnieniePrzypisanie>)pair.Value);
    }

    private static async Task AttachUprawnieniaAsync(
        OleDbConnection connection,
        IReadOnlyList<Funkcjonariusz> funkcjonariusze,
        CancellationToken cancellationToken)
    {
        if (funkcjonariusze.Count == 0)
        {
            return;
        }

        var idFilter = BuildPersonnelIdFilter(funkcjonariusze, "fu.FunkcjonariuszId");
        await using var command = new OleDbCommand(
            $"""
            SELECT fu.FunkcjonariuszId, fu.Id, tu.Nazwa, tu.Podtyp, fu.WazneDo
            FROM FunkcjonariuszUprawnienia fu
            INNER JOIN TypyUprawnien tu ON tu.Id = fu.TypUprawnieniaId
            {idFilter}
            ORDER BY fu.FunkcjonariuszId, tu.Nazwa, tu.Podtyp
            """,
            connection);

        var lookup = funkcjonariusze.ToDictionary(f => f.Id);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var funkcjonariuszId = reader.GetFieldInt32(0);
            if (!lookup.TryGetValue(funkcjonariuszId, out var funkcjonariusz))
            {
                continue;
            }

            funkcjonariusz.Uprawnienia.Add(new UprawnieniePrzypisanie
            {
                Id = reader.GetFieldInt32(1),
                FunkcjonariuszId = funkcjonariuszId,
                Nazwa = reader.GetString(2),
                Podtyp = reader.IsDBNull(3) ? null : reader.GetString(3),
                WazneDo = reader.GetNullableFieldDateTime(4)
            });
        }
    }

    private static async Task AttachOdznaczeniaAsync(
        OleDbConnection connection,
        IReadOnlyList<Funkcjonariusz> funkcjonariusze,
        CancellationToken cancellationToken)
    {
        if (funkcjonariusze.Count == 0)
        {
            return;
        }

        var idFilter = BuildPersonnelIdFilter(funkcjonariusze, "fo.FunkcjonariuszId");
        await using var command = new OleDbCommand(
            $"""
            SELECT fo.FunkcjonariuszId, fo.Id, fo.TypOdznaczeniaId, typOdz.Nazwa, fo.DataNadania
            FROM FunkcjonariuszOdznaczenia fo
            INNER JOIN TypyOdznaczen typOdz ON typOdz.Id = fo.TypOdznaczeniaId
            {idFilter}
            ORDER BY fo.FunkcjonariuszId, typOdz.Nazwa
            """,
            connection);

        var lookup = funkcjonariusze.ToDictionary(f => f.Id);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var funkcjonariuszId = reader.GetFieldInt32(0);
            if (!lookup.TryGetValue(funkcjonariuszId, out var funkcjonariusz))
            {
                continue;
            }

            funkcjonariusz.Odznaczenia.Add(new OdznaczeniePrzypisanie
            {
                Id = reader.GetFieldInt32(1),
                FunkcjonariuszId = funkcjonariuszId,
                TypOdznaczeniaId = reader.GetFieldInt32(2),
                Nazwa = reader.GetString(3),
                DataNadania = reader.GetFieldDateTime(4)
            });
        }
    }

    private static async Task DeleteChildRowsAsync(
        OleDbConnection connection,
        int id,
        CancellationToken cancellationToken)
    {
        await using var upr = new OleDbCommand(
            "DELETE FROM FunkcjonariuszUprawnienia WHERE FunkcjonariuszId = ?",
            connection);
        upr.Parameters.AddWithValue("@p1", id);
        await upr.ExecuteNonQueryAsync(cancellationToken);

        await using var odz = new OleDbCommand(
            "DELETE FROM FunkcjonariuszOdznaczenia WHERE FunkcjonariuszId = ?",
            connection);
        odz.Parameters.AddWithValue("@p1", id);
        await odz.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string BuildUprawnieniaTypeFilter(FunkcjonariuszListQuery query)
    {
        if (string.IsNullOrWhiteSpace(query.UprawnienieNazwa))
        {
            return string.Empty;
        }

        var (permissionWhere, _) = BuildUprawnienieTypeWhere(query);
        return $" AND {permissionWhere}";
    }

    private static string BuildPersonnelIdFilter(IReadOnlyList<Funkcjonariusz> funkcjonariusze, string columnName)
    {
        if (funkcjonariusze.Count is 0 or > 200)
        {
            return string.Empty;
        }

        var ids = string.Join(',', funkcjonariusze.Select(f => f.Id));
        return $" WHERE {columnName} IN ({ids})";
    }

    private static Funkcjonariusz Map(DbDataReader reader) => new()
    {
        Id = reader.GetFieldInt32(0),
        NumerZmiany = reader.GetFieldInt32(1),
        NumerPorzadkowy = reader.GetFieldInt32(2),
        StopienId = reader.GetFieldInt32(3),
        StanowiskoId = reader.GetFieldInt32(4),
        Stopien = reader.GetString(5),
        Imie = reader.GetString(6),
        Nazwisko = reader.GetString(7),
        Stanowisko = reader.GetString(8),
        Telefon = reader.IsDBNull(9) ? null : reader.GetString(9),
        StazLat = reader.GetNullableFieldInt32(10),
        BadaniaOkresoweDo = reader.GetNullableFieldDateTime(11),
        KomoraDymowaDo = reader.GetNullableFieldDateTime(12),
        KppDo = reader.GetNullableFieldDateTime(13),
        DataWstepieniaDoSluzby = reader.GetNullableFieldDateTime(14),
        InformacjaDodatkowa = reader.IsDBNull(15) ? null : reader.GetString(15),
        DataAwansuStopien = reader.GetNullableFieldDateTime(16),
        DataAwansuGrupa = reader.GetNullableFieldDateTime(17),
        DodatekMotywacyjny = reader.GetNullableFieldDecimal(18)
    };
}
