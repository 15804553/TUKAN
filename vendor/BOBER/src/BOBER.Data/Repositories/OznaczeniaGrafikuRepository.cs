using System.Data.OleDb;
using BOBER.Core.Constants;
using BOBER.Core.Enums;
using BOBER.Core.Models;

namespace BOBER.Data.Repositories;

public sealed class OznaczeniaGrafikuRepository(BoberConnectionFactory connectionFactory)
    : IOznaczeniaGrafikuRepository
{
    public async Task<IReadOnlyList<OznaczenieGrafiku>> GetAllAsync(
        int zmianaId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new OleDbCommand(
            """
            SELECT Id, ZmianaId, Kod, Nazwa, KolorHex, WPracy, SekcjaRozkazu, SkrotKlawiszowy,
                   TekstWyswietlany, MoznaOddac, MoznaKropke, ZachowajTloWsPrzyBraku,
                   DodatkowaSekcjaRozkazu, RolaNalozania, Kolejnosc,
                   EksportDoExcela, KolorExcelHex, AdnotacjaRozkazu,
                   StylWyswietlania, FlagaPozycja
            FROM OznaczeniaGrafiku
            WHERE ZmianaId = ?
            ORDER BY Kolejnosc, Kod
            """,
            connection);
        command.Parameters.AddWithValue("@p1", (short)zmianaId);

        var result = new List<OznaczenieGrafiku>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            result.Add(ReadRow(reader, zmianaId));

        return result;
    }

    public async Task SaveAllAsync(
        int zmianaId,
        IReadOnlyList<OznaczenieGrafiku> items,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();

        await using (var deleteCmd = new OleDbCommand(
                         "DELETE FROM OznaczeniaGrafiku WHERE ZmianaId = ?",
                         connection))
        {
            deleteCmd.Parameters.AddWithValue("@p1", (short)zmianaId);
            await deleteCmd.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var item in items)
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

            insertCmd.Parameters.AddWithValue("@p0", (short)zmianaId);
            insertCmd.Parameters.AddWithValue("@p1", item.Kod);
            insertCmd.Parameters.AddWithValue("@p2", item.Nazwa);
            insertCmd.Parameters.AddWithValue("@p3", item.KolorHex);
            insertCmd.Parameters.AddWithValue("@p4", item.WPracy);
            AddNullableShort(insertCmd, item.SekcjaRozkazu is null ? null : (short?)item.SekcjaRozkazu.Value);
            insertCmd.Parameters.AddWithValue("@p6", item.SkrotKlawiszowy ?? string.Empty);
            insertCmd.Parameters.AddWithValue(
                "@p7",
                string.IsNullOrWhiteSpace(item.TekstWyswietlany)
                    ? DBNull.Value
                    : item.TekstWyswietlany);
            insertCmd.Parameters.AddWithValue("@p8", item.MoznaOddac);
            insertCmd.Parameters.AddWithValue("@p9", item.MoznaKropke);
            insertCmd.Parameters.AddWithValue("@p10", item.ZachowajTloWsPrzyBraku);
            AddNullableShort(
                insertCmd,
                item.DodatkowaSekcjaRozkazu is null ? null : (short?)item.DodatkowaSekcjaRozkazu.Value);
            insertCmd.Parameters.AddWithValue("@p12", (short)item.RolaNalozania);
            insertCmd.Parameters.AddWithValue("@p13", item.Kolejnosc);
            insertCmd.Parameters.AddWithValue("@p14", item.EksportDoExcela);
            insertCmd.Parameters.AddWithValue(
                "@p15",
                string.IsNullOrWhiteSpace(item.KolorExcelHex) ? item.KolorHex : item.KolorExcelHex);
            insertCmd.Parameters.AddWithValue("@p16", item.AdnotacjaRozkazu ?? string.Empty);
            insertCmd.Parameters.AddWithValue("@p17", (short)item.StylWyswietlania);
            insertCmd.Parameters.AddWithValue("@p18", (short)item.FlagaPozycja);
            await insertCmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public async Task<int> CountWpisowZKodemAsync(
        string kod,
        int zmianaId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new OleDbCommand(
            """
            SELECT COUNT(*) FROM GrafikWpisy
            WHERE ZmianaId = ? AND (TypWpisu = ? OR TypWpisu LIKE ?)
            """,
            connection);
        command.Parameters.AddWithValue("@p0", (short)zmianaId);
        command.Parameters.AddWithValue("@p1", kod);
        command.Parameters.AddWithValue("@p2", kod + "%");
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    private static OznaczenieGrafiku ReadRow(System.Data.Common.DbDataReader reader, int fallbackZmianaId)
    {
        var kolor = reader["KolorHex"]?.ToString() ?? RoleKeys.BrakWypelnienia;
        var excelRaw = HasColumn(reader, "KolorExcelHex")
            ? reader["KolorExcelHex"]?.ToString()
            : null;
        var eksport = true;
        if (HasColumn(reader, "EksportDoExcela") && reader["EksportDoExcela"] is not DBNull)
            eksport = Convert.ToBoolean(reader["EksportDoExcela"]);

        var zmiana = fallbackZmianaId;
        if (HasColumn(reader, "ZmianaId") && reader["ZmianaId"] is not DBNull)
            zmiana = Convert.ToInt32(reader["ZmianaId"]);

        var tekstRaw = reader["TekstWyswietlany"] is DBNull
            ? null
            : reader["TekstWyswietlany"]?.ToString();

        var adnotacja = HasColumn(reader, "AdnotacjaRozkazu")
            ? reader["AdnotacjaRozkazu"]?.ToString() ?? string.Empty
            : string.Empty;

        var styl = StylWyswietlaniaOznaczenia.Normalny;
        if (HasColumn(reader, "StylWyswietlania") && reader["StylWyswietlania"] is not DBNull)
            styl = (StylWyswietlaniaOznaczenia)Convert.ToInt16(reader["StylWyswietlania"]);

        var flaga = FlagaPozycjaOznaczenia.Nie;
        if (HasColumn(reader, "FlagaPozycja") && reader["FlagaPozycja"] is not DBNull)
            flaga = (FlagaPozycjaOznaczenia)Convert.ToInt16(reader["FlagaPozycja"]);

        return new OznaczenieGrafiku
        {
            Id = Convert.ToInt32(reader["Id"]),
            ZmianaId = zmiana,
            Kod = reader["Kod"]?.ToString() ?? string.Empty,
            Nazwa = reader["Nazwa"]?.ToString() ?? string.Empty,
            KolorHex = kolor,
            WPracy = Convert.ToBoolean(reader["WPracy"]),
            SekcjaRozkazu = ReadSekcja(reader["SekcjaRozkazu"]),
            SkrotKlawiszowy = reader["SkrotKlawiszowy"]?.ToString() ?? string.Empty,
            TekstWyswietlany = string.IsNullOrWhiteSpace(tekstRaw) ? null : tekstRaw,
            MoznaOddac = Convert.ToBoolean(reader["MoznaOddac"]),
            MoznaKropke = Convert.ToBoolean(reader["MoznaKropke"]),
            ZachowajTloWsPrzyBraku = Convert.ToBoolean(reader["ZachowajTloWsPrzyBraku"]),
            DodatkowaSekcjaRozkazu = ReadSekcja(reader["DodatkowaSekcjaRozkazu"]),
            RolaNalozania = (RolaNalozaniaOznaczenia)Convert.ToInt16(reader["RolaNalozania"]),
            Kolejnosc = Convert.ToInt16(reader["Kolejnosc"]),
            EksportDoExcela = eksport,
            KolorExcelHex = string.IsNullOrWhiteSpace(excelRaw) ? kolor : excelRaw,
            AdnotacjaRozkazu = adnotacja,
            StylWyswietlania = styl,
            FlagaPozycja = flaga
        };
    }

    private static bool HasColumn(System.Data.Common.DbDataReader reader, string name)
    {
        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (reader.GetName(i).Equals(name, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static SekcjaRozkazuGrafiku? ReadSekcja(object? raw)
    {
        if (raw is null or DBNull)
            return null;
        var v = Convert.ToInt16(raw);
        if (v < 1 || v > 5)
            return null;
        return (SekcjaRozkazuGrafiku)v;
    }

    private static void AddNullableShort(OleDbCommand command, short? value)
    {
        var p = command.Parameters.Add("@p", OleDbType.SmallInt);
        p.Value = value.HasValue ? value.Value : DBNull.Value;
    }
}
