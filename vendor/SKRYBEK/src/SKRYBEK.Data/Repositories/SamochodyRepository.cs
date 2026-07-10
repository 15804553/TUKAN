using System.Data.OleDb;
using SKRYBEK.Core.Enums;
using SKRYBEK.Core.Models;
using SKRYBEK.Data.Connections;

namespace SKRYBEK.Data.Repositories;

public sealed class SamochodyRepository
{
    private readonly SkrybekConnectionFactory _factory;

    public SamochodyRepository(SkrybekConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<List<Samochod>> GetAllAsync()
    {
        var list = new List<Samochod>();
        await using var conn = _factory.Create();
        await conn.OpenAsync();
        await using var cmd = new OleDbCommand(
            "SELECT Id, Nazwa, LiczbaPozycji, Typ, Kolejnosc, CzyAktywny FROM Samochody ORDER BY Kolejnosc", conn);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            list.Add(new Samochod
            {
                Id            = r.GetIntSafe(0),
                Nazwa         = r.GetStringSafe(1),
                LiczbaPozycji = r.GetIntSafe(2),
                Typ           = (TypSamochodu)r.GetIntSafe(3),
                Kolejnosc     = r.GetIntSafe(4),
                CzyAktywny    = r.GetBoolSafe(5)
            });
        }
        await r.CloseAsync();

        await AttachWymaganiaAsync(conn, list);
        return list;
    }

    private static async Task AttachWymaganiaAsync(OleDbConnection conn, List<Samochod> list)
    {
        if (list.Count == 0) return;
        try
        {
            await using var cmd = new OleDbCommand(
                "SELECT SamochodId, TypUprawnieniaId FROM SamochodWymagania", conn);
            await using var r = await cmd.ExecuteReaderAsync();
            var dict = list.ToDictionary(s => s.Id);
            while (await r.ReadAsync())
            {
                var samId = r.GetIntSafe(0);
                var typId = r.GetIntSafe(1);
                if (dict.TryGetValue(samId, out var s))
                    s.WymaganeUprawnieniaIds.Add(typId);
            }
        }
        catch
        {
            // Tabela SamochodWymagania może jeszcze nie istnieć na starych bazach.
        }
    }

    public async Task<List<Samochod>> GetAktywneAsync()
    {
        var all = await GetAllAsync();
        return all.Where(s => s.CzyAktywny).ToList();
    }

    public async Task UpsertAsync(Samochod s)
    {
        await using var conn = _factory.Create();
        await conn.OpenAsync();

        if (s.Id == 0)
        {
            await using var cmd = new OleDbCommand(
                "INSERT INTO Samochody (Nazwa, LiczbaPozycji, Typ, Kolejnosc, CzyAktywny) VALUES (?, ?, ?, ?, ?)", conn);
            AddParams(cmd, s);
            await cmd.ExecuteNonQueryAsync();

            s.Id = await ReadInsertedIdentityAsync(conn);
        }
        else
        {
            await using var cmd = new OleDbCommand(
                "UPDATE Samochody SET Nazwa=?, LiczbaPozycji=?, Typ=?, Kolejnosc=?, CzyAktywny=? WHERE Id=?", conn);
            AddParams(cmd, s);
            cmd.Parameters.AddInteger(s.Id);
            await cmd.ExecuteNonQueryAsync();
        }

        await SaveWymaganiaAsync(conn, s);
    }

    private static async Task SaveWymaganiaAsync(OleDbConnection conn, Samochod s)
    {
        if (s.Id == 0) return;

        await using var del = new OleDbCommand("DELETE FROM SamochodWymagania WHERE SamochodId=?", conn);
        del.Parameters.AddInteger(s.Id);
        await del.ExecuteNonQueryAsync();

        foreach (var typId in s.WymaganeUprawnieniaIds.Distinct())
        {
            await using var ins = new OleDbCommand(
                "INSERT INTO SamochodWymagania (SamochodId, TypUprawnieniaId) VALUES (?, ?)", conn);
            ins.Parameters.AddInteger(s.Id);
            ins.Parameters.AddInteger(typId);
            await ins.ExecuteNonQueryAsync();
        }
    }

    public async Task DeleteAsync(int id)
    {
        if (id <= 0)
            throw new InvalidOperationException("Pojazd nie ma przypisanego identyfikatora w bazie danych.");

        await using var conn = _factory.Create();
        await conn.OpenAsync();

        try
        {
            await using var delPodzial = new OleDbCommand(
                "DELETE FROM RozkazPodzialBojowy WHERE SamochodId=?", conn);
            delPodzial.Parameters.AddWithValue("SamochodId", id);
            await delPodzial.ExecuteNonQueryAsync();
        }
        catch
        {
            // Tabela lub kolumna może nie istnieć na starszych bazach — ignoruj.
        }

        try
        {
            await using var delWym = new OleDbCommand("DELETE FROM SamochodWymagania WHERE SamochodId=?", conn);
            delWym.Parameters.AddWithValue("SamochodId", id);
            await delWym.ExecuteNonQueryAsync();
        }
        catch
        {
            // Tabela SamochodWymagania może jeszcze nie istnieć na starych bazach
        }

        await using var cmd = new OleDbCommand("DELETE FROM Samochody WHERE Id=?", conn);
        cmd.Parameters.AddWithValue("Id", id);
        var affected = await cmd.ExecuteNonQueryAsync();
        if (affected == 0)
            throw new InvalidOperationException($"Nie znaleziono pojazdu o identyfikatorze {id}.");
    }

    private static async Task<int> ReadInsertedIdentityAsync(OleDbConnection conn)
    {
        await using var idCmd = new OleDbCommand("SELECT @@IDENTITY", conn);
        var scalar = await idCmd.ExecuteScalarAsync();
        if (scalar is null or DBNull)
            throw new InvalidOperationException("Nie udało się odczytać identyfikatora nowego pojazdu.");

        return Convert.ToInt32(Convert.ToDecimal(scalar));
    }

    private static void AddParams(OleDbCommand cmd, Samochod s)
    {
        cmd.Parameters.AddWithValue("Nazwa", s.Nazwa);
        cmd.Parameters.AddWithValue("LiczbaPozycji", s.LiczbaPozycji);
        cmd.Parameters.AddWithValue("Typ", (int)s.Typ);
        cmd.Parameters.AddWithValue("Kolejnosc", s.Kolejnosc);
        cmd.Parameters.AddWithValue("CzyAktywny", s.CzyAktywny);
    }
}
