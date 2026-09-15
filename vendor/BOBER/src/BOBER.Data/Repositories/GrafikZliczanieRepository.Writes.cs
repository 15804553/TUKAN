using System.Data.OleDb;
using BOBER.Core.Models;

namespace BOBER.Data.Repositories;

public sealed partial class GrafikZliczanieRepository
{
    private static async Task DeleteZmianaAsync(
        OleDbConnection connection,
        int zmianaId,
        CancellationToken cancellationToken)
    {
        var wierszIds = new List<int>();
        await using (var cmd = new OleDbCommand(
            "SELECT Id FROM GrafikZliczanieWiersze WHERE ZmianaId = ?",
            connection))
        {
            cmd.Parameters.AddWithValue("@p1", (short)zmianaId);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                wierszIds.Add(Convert.ToInt32(reader["Id"]));
        }

        if (wierszIds.Count == 0)
            return;

        var poziomIds = new List<int>();
        foreach (var wierszId in wierszIds)
        {
            await using var cmd = new OleDbCommand(
                "SELECT Id FROM GrafikZliczaniePoziomy WHERE WierszId = ?",
                connection);
            cmd.Parameters.AddWithValue("@p1", wierszId);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                poziomIds.Add(Convert.ToInt32(reader["Id"]));
        }

        var slotIds = new List<int>();
        foreach (var poziomId in poziomIds)
        {
            await using var cmd = new OleDbCommand(
                "SELECT Id FROM GrafikZliczanieSloty WHERE PoziomId = ?",
                connection);
            cmd.Parameters.AddWithValue("@p1", poziomId);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                slotIds.Add(Convert.ToInt32(reader["Id"]));
        }

        var grupaIds = new List<int>();
        foreach (var wierszId in wierszIds)
            await CollectGrupaIdsAsync(connection, "WierszId", wierszId, grupaIds, cancellationToken);
        foreach (var slotId in slotIds)
            await CollectGrupaIdsAsync(connection, "SlotId", slotId, grupaIds, cancellationToken);

        foreach (var grupaId in grupaIds)
            await ExecuteNonQueryAsync(connection, "DELETE FROM GrafikZliczaniePozycje WHERE GrupaId = ?", grupaId, cancellationToken);
        foreach (var grupaId in grupaIds)
            await ExecuteNonQueryAsync(connection, "DELETE FROM GrafikZliczanieGrupy WHERE Id = ?", grupaId, cancellationToken);
        foreach (var slotId in slotIds)
            await ExecuteNonQueryAsync(connection, "DELETE FROM GrafikZliczanieSloty WHERE Id = ?", slotId, cancellationToken);
        foreach (var poziomId in poziomIds)
            await ExecuteNonQueryAsync(connection, "DELETE FROM GrafikZliczaniePoziomy WHERE Id = ?", poziomId, cancellationToken);
        foreach (var wierszId in wierszIds)
            await ExecuteNonQueryAsync(connection, "DELETE FROM GrafikZliczanieWiersze WHERE Id = ?", wierszId, cancellationToken);
    }

    private static async Task CollectGrupaIdsAsync(
        OleDbConnection connection,
        string column,
        int parentId,
        List<int> target,
        CancellationToken cancellationToken)
    {
        await using var cmd = new OleDbCommand(
            $"SELECT Id FROM GrafikZliczanieGrupy WHERE {column} = ?",
            connection);
        cmd.Parameters.AddWithValue("@p1", parentId);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            target.Add(Convert.ToInt32(reader["Id"]));
    }

    private static async Task<int> InsertWierszAsync(
        OleDbConnection connection,
        int zmianaId,
        GrafikZliczanieWiersz wiersz,
        CancellationToken cancellationToken)
    {
        await using var cmd = new OleDbCommand(
            """
            INSERT INTO GrafikZliczanieWiersze (ZmianaId, Nazwa, Typ, Zrodlo, Kolejnosc)
            VALUES (?, ?, ?, ?, ?)
            """,
            connection);
        cmd.Parameters.AddWithValue("@p0", (short)zmianaId);
        cmd.Parameters.AddWithValue("@p1", wiersz.Nazwa.Trim());
        cmd.Parameters.AddWithValue("@p2", (short)wiersz.Typ);
        cmd.Parameters.AddWithValue("@p3", (short)wiersz.Zrodlo);
        cmd.Parameters.AddWithValue("@p4", wiersz.Kolejnosc);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
        return await ReadIdentityAsync(connection, cancellationToken);
    }

    private static async Task<int> InsertPoziomAsync(
        OleDbConnection connection,
        int wierszId,
        GrafikZliczaniePoziom poziom,
        CancellationToken cancellationToken)
    {
        await using var cmd = new OleDbCommand(
            "INSERT INTO GrafikZliczaniePoziomy (WierszId, Kod, Kolejnosc) VALUES (?, ?, ?)",
            connection);
        cmd.Parameters.AddWithValue("@p0", wierszId);
        cmd.Parameters.AddWithValue("@p1", poziom.Kod.Trim());
        cmd.Parameters.AddWithValue("@p2", poziom.Kolejnosc);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
        return await ReadIdentityAsync(connection, cancellationToken);
    }

    private static async Task<int> InsertSlotAsync(
        OleDbConnection connection,
        int poziomId,
        GrafikZliczanieSlot slot,
        CancellationToken cancellationToken)
    {
        await using var cmd = new OleDbCommand(
            """
            INSERT INTO GrafikZliczanieSloty
                (PoziomId, Nazwa, Zrodlo, Liczba, Kolejnosc, WspoldzielSlotKolejnosc)
            VALUES (?, ?, ?, ?, ?, ?)
            """,
            connection);
        cmd.Parameters.AddWithValue("@p0", poziomId);
        cmd.Parameters.AddWithValue("@p1", slot.Nazwa.Trim());
        cmd.Parameters.AddWithValue("@p2", (short)slot.Zrodlo);
        cmd.Parameters.AddWithValue("@p3", slot.Liczba);
        cmd.Parameters.AddWithValue("@p4", slot.Kolejnosc);
        var share = cmd.Parameters.Add("@p5", OleDbType.SmallInt);
        share.Value = slot.WspoldzielSlotKolejnosc.HasValue
            ? slot.WspoldzielSlotKolejnosc.Value
            : DBNull.Value;
        await cmd.ExecuteNonQueryAsync(cancellationToken);
        return await ReadIdentityAsync(connection, cancellationToken);
    }

    private static async Task InsertGrupaAsync(
        OleDbConnection connection,
        int? wierszId,
        int? slotId,
        GrafikZliczanieGrupa grupa,
        CancellationToken cancellationToken)
    {
        await using var cmd = new OleDbCommand(
            "INSERT INTO GrafikZliczanieGrupy (WierszId, SlotId, Kolejnosc) VALUES (?, ?, ?)",
            connection);
        AddNullableLong(cmd, wierszId);
        AddNullableLong(cmd, slotId);
        cmd.Parameters.AddWithValue("@p2", grupa.Kolejnosc);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
        var grupaId = await ReadIdentityAsync(connection, cancellationToken);

        foreach (var refId in grupa.RefIds.Distinct())
        {
            await using var poz = new OleDbCommand(
                "INSERT INTO GrafikZliczaniePozycje (GrupaId, RefId) VALUES (?, ?)",
                connection);
            poz.Parameters.AddWithValue("@p0", grupaId);
            poz.Parameters.AddWithValue("@p1", refId);
            await poz.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static async Task<int> ReadIdentityAsync(OleDbConnection connection, CancellationToken cancellationToken)
    {
        await using var cmd = new OleDbCommand("SELECT @@IDENTITY", connection);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync(cancellationToken));
    }

    private static async Task ExecuteNonQueryAsync(
        OleDbConnection connection,
        string sql,
        int id,
        CancellationToken cancellationToken)
    {
        await using var cmd = new OleDbCommand(sql, connection);
        cmd.Parameters.AddWithValue("@p1", id);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddNullableLong(OleDbCommand command, int? value)
    {
        var p = command.Parameters.Add("@p", OleDbType.Integer);
        p.Value = value.HasValue ? value.Value : DBNull.Value;
    }
}
