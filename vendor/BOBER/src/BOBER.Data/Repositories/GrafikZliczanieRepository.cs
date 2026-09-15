using System.Data.OleDb;
using BOBER.Core.Enums;
using BOBER.Core.Models;

namespace BOBER.Data.Repositories;

public sealed partial class GrafikZliczanieRepository(BoberConnectionFactory connectionFactory)
    : IGrafikZliczanieRepository
{
    public async Task<IReadOnlyList<GrafikZliczanieWiersz>> GetAllAsync(
        int zmianaId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        var wiersze = await LoadWierszeAsync(connection, zmianaId, cancellationToken);
        if (wiersze.Count == 0)
            return wiersze;

        var wierszIds = wiersze.Select(w => w.Id).ToHashSet();
        var poziomy = await LoadPoziomyAsync(connection, wierszIds, cancellationToken);
        var poziomIds = poziomy.Select(p => p.Id).ToHashSet();
        var sloty = await LoadSlotyAsync(connection, poziomIds, cancellationToken);
        var slotIds = sloty.Select(s => s.Id).ToHashSet();
        var grupy = await LoadGrupyAsync(connection, wierszIds, slotIds, cancellationToken);

        var wierszeById = wiersze.ToDictionary(w => w.Id);
        foreach (var poziom in poziomy.OrderBy(p => p.Kolejnosc))
        {
            if (wierszeById.TryGetValue(poziom.WierszId, out var wiersz))
                wiersz.Poziomy.Add(poziom);
        }

        var poziomyById = poziomy.ToDictionary(p => p.Id);
        foreach (var slot in sloty.OrderBy(s => s.Kolejnosc))
        {
            if (poziomyById.TryGetValue(slot.PoziomId, out var poziom))
                poziom.Sloty.Add(slot);
        }

        var slotyById = sloty.ToDictionary(s => s.Id);
        foreach (var grupa in grupy.OrderBy(g => g.Kolejnosc))
        {
            if (grupa.WierszId is int wid && wierszeById.TryGetValue(wid, out var wiersz))
                wiersz.Grupy.Add(grupa);
            else if (grupa.SlotId is int sid && slotyById.TryGetValue(sid, out var slot))
                slot.Grupy.Add(grupa);
        }

        return wiersze.OrderBy(w => w.Kolejnosc).ToList();
    }

    public async Task SaveAllAsync(
        int zmianaId,
        IReadOnlyList<GrafikZliczanieWiersz> items,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        await DeleteZmianaAsync(connection, zmianaId, cancellationToken);

        foreach (var wiersz in items.OrderBy(w => w.Kolejnosc))
        {
            var wierszId = await InsertWierszAsync(connection, zmianaId, wiersz, cancellationToken);
            foreach (var grupa in wiersz.Grupy.OrderBy(g => g.Kolejnosc))
                await InsertGrupaAsync(connection, wierszId, slotId: null, grupa, cancellationToken);

            foreach (var poziom in wiersz.Poziomy.OrderBy(p => p.Kolejnosc))
            {
                var poziomId = await InsertPoziomAsync(connection, wierszId, poziom, cancellationToken);
                foreach (var slot in poziom.Sloty.OrderBy(s => s.Kolejnosc))
                {
                    var slotId = await InsertSlotAsync(connection, poziomId, slot, cancellationToken);
                    foreach (var grupa in slot.Grupy.OrderBy(g => g.Kolejnosc))
                        await InsertGrupaAsync(connection, wierszId: null, slotId, grupa, cancellationToken);
                }
            }
        }
    }

    private static async Task<List<GrafikZliczanieWiersz>> LoadWierszeAsync(
        OleDbConnection connection,
        int zmianaId,
        CancellationToken cancellationToken)
    {
        await using var cmd = new OleDbCommand(
            """
            SELECT Id, ZmianaId, Nazwa, Typ, Zrodlo, Kolejnosc
            FROM GrafikZliczanieWiersze
            WHERE ZmianaId = ?
            ORDER BY Kolejnosc, Id
            """,
            connection);
        cmd.Parameters.AddWithValue("@p1", (short)zmianaId);

        var result = new List<GrafikZliczanieWiersz>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new GrafikZliczanieWiersz
            {
                Id = Convert.ToInt32(reader["Id"]),
                ZmianaId = Convert.ToInt32(reader["ZmianaId"]),
                Nazwa = reader["Nazwa"]?.ToString() ?? string.Empty,
                Typ = (GrafikZliczanieTyp)Convert.ToInt16(reader["Typ"]),
                Zrodlo = (GrafikZliczanieZrodlo)Convert.ToInt16(reader["Zrodlo"]),
                Kolejnosc = Convert.ToInt16(reader["Kolejnosc"])
            });
        }

        return result;
    }

    private static async Task<List<GrafikZliczaniePoziom>> LoadPoziomyAsync(
        OleDbConnection connection,
        IReadOnlySet<int> wierszIds,
        CancellationToken cancellationToken)
    {
        var result = new List<GrafikZliczaniePoziom>();
        await using var cmd = new OleDbCommand(
            "SELECT Id, WierszId, Kod, Kolejnosc FROM GrafikZliczaniePoziomy ORDER BY Kolejnosc, Id",
            connection);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var wierszId = Convert.ToInt32(reader["WierszId"]);
            if (!wierszIds.Contains(wierszId))
                continue;
            result.Add(new GrafikZliczaniePoziom
            {
                Id = Convert.ToInt32(reader["Id"]),
                WierszId = wierszId,
                Kod = reader["Kod"]?.ToString() ?? string.Empty,
                Kolejnosc = Convert.ToInt16(reader["Kolejnosc"])
            });
        }

        return result;
    }

    private static async Task<List<GrafikZliczanieSlot>> LoadSlotyAsync(
        OleDbConnection connection,
        IReadOnlySet<int> poziomIds,
        CancellationToken cancellationToken)
    {
        var result = new List<GrafikZliczanieSlot>();
        await using var cmd = new OleDbCommand(
            """
            SELECT Id, PoziomId, Nazwa, Zrodlo, Liczba, Kolejnosc, WspoldzielSlotKolejnosc
            FROM GrafikZliczanieSloty
            ORDER BY Kolejnosc, Id
            """,
            connection);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var poziomId = Convert.ToInt32(reader["PoziomId"]);
            if (!poziomIds.Contains(poziomId))
                continue;
            result.Add(new GrafikZliczanieSlot
            {
                Id = Convert.ToInt32(reader["Id"]),
                PoziomId = poziomId,
                Nazwa = reader["Nazwa"]?.ToString() ?? string.Empty,
                Zrodlo = (GrafikZliczanieZrodlo)Convert.ToInt16(reader["Zrodlo"]),
                Liczba = Convert.ToInt16(reader["Liczba"]),
                Kolejnosc = Convert.ToInt16(reader["Kolejnosc"]),
                WspoldzielSlotKolejnosc = reader["WspoldzielSlotKolejnosc"] is DBNull
                    ? null
                    : Convert.ToInt16(reader["WspoldzielSlotKolejnosc"])
            });
        }

        return result;
    }

    private static async Task<List<GrafikZliczanieGrupa>> LoadGrupyAsync(
        OleDbConnection connection,
        IReadOnlySet<int> wierszIds,
        IReadOnlySet<int> slotIds,
        CancellationToken cancellationToken)
    {
        var grupy = new List<GrafikZliczanieGrupa>();
        await using (var cmd = new OleDbCommand(
            "SELECT Id, WierszId, SlotId, Kolejnosc FROM GrafikZliczanieGrupy",
            connection))
        await using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var wierszId = reader["WierszId"] is DBNull ? (int?)null : Convert.ToInt32(reader["WierszId"]);
                var slotId = reader["SlotId"] is DBNull ? (int?)null : Convert.ToInt32(reader["SlotId"]);
                var pasuje = (wierszId is int wid && wierszIds.Contains(wid))
                    || (slotId is int sid && slotIds.Contains(sid));
                if (!pasuje)
                    continue;

                grupy.Add(new GrafikZliczanieGrupa
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Kolejnosc = Convert.ToInt16(reader["Kolejnosc"]),
                    WierszId = wierszId,
                    SlotId = slotId
                });
            }
        }

        if (grupy.Count == 0)
            return grupy;

        var byId = grupy.ToDictionary(g => g.Id);
        await using var pozCmd = new OleDbCommand(
            "SELECT GrupaId, RefId FROM GrafikZliczaniePozycje",
            connection);
        await using var pozReader = await pozCmd.ExecuteReaderAsync(cancellationToken);
        while (await pozReader.ReadAsync(cancellationToken))
        {
            var grupaId = Convert.ToInt32(pozReader["GrupaId"]);
            if (byId.TryGetValue(grupaId, out var grupa))
                grupa.RefIds.Add(Convert.ToInt32(pozReader["RefId"]));
        }

        return grupy;
    }
}
