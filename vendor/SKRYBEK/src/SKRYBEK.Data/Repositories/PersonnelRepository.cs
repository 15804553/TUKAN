using System.Data.OleDb;
using SKRYBEK.Core.Chomik;
using SKRYBEK.Core.Models;
using SKRYBEK.Core.Rules;
using SKRYBEK.Data.Connections;
using SKRYBEK.Data.Grafik;

namespace SKRYBEK.Data.Repositories;

/// <summary>
/// Personel, stanowiska i uprawnienia — z bazy CHOMIK.
/// Dostępność w danym dniu (grafik, nieobecności) — z bazy BOBER.
/// </summary>
public sealed class PersonnelRepository
{
    private readonly BoberConnectionFactory _bober;
    private readonly ChomikConnectionFactory _chomik;
    private readonly ShiftCalendarEngine _calendar;

    public PersonnelRepository(
        BoberConnectionFactory bober,
        ChomikConnectionFactory chomik,
        ShiftCalendarEngine calendar)
    {
        _bober    = bober;
        _chomik   = chomik;
        _calendar = calendar;
    }

    /// <summary>Zwraca wszystkich funkcjonariuszy danej zmiany z danymi z CHOMIK.</summary>
    public async Task<List<Funkcjonariusz>> GetByZmianaAsync(int nrZmiany)
    {
        var list = new List<Funkcjonariusz>();

        await using var conn = _chomik.Create();
        await conn.OpenAsync();

        const string sql =
            "SELECT f.Id, f.NumerZmiany, f.StopienId, f.Imie, f.Nazwisko, f.StanowiskoId, f.Telefon, f.StazLat," +
            " ss.Nazwa AS Stopien, st.Nazwa AS Stanowisko" +
            " FROM (Funkcjonariusze AS f INNER JOIN StopnieSlownik AS ss ON ss.Id = f.StopienId)" +
            " INNER JOIN StanowiskaSlownik AS st ON st.Id = f.StanowiskoId" +
            " WHERE f.NumerZmiany = ?" +
            " ORDER BY f.Id";

        await using var cmd = new OleDbCommand(sql, conn);
        cmd.Parameters.AddWithValue("@p1", (short)nrZmiany);
        await using var r = await cmd.ExecuteReaderAsync();

        while (await r.ReadAsync())
        {
            list.Add(new Funkcjonariusz
            {
                Id           = r.GetIntSafe(0),
                NumerZmiany  = r.GetIntSafe(1),
                StopienId    = r.GetIntSafe(2),
                Imie         = r.GetStringSafe(3),
                Nazwisko     = r.GetStringSafe(4),
                StanowiskoId = r.GetIntSafe(5),
                Telefon      = r.IsDBNull(6) ? null : r.GetStringSafe(6),
                StazLat      = r.GetIntOrNull(7),
                Stopien      = r.GetStringSafe(8),
                Stanowisko   = r.GetStringSafe(9)
            });
        }

        await AttachUprawnieniaAsync(conn, list);

        return list;
    }

    /// <summary>
    /// Zwraca funkcjonariuszy obecnych w danym dniu służby zmiany.
    /// W BOBER nieobecność = dowolny wpis w GrafikWpisy (pusta komórka = w pracy).
    /// </summary>
    public async Task<List<Funkcjonariusz>> GetDostepniWDniuAsync(
        DateOnly data,
        int nrZmiany,
        IReadOnlyList<Funkcjonariusz>? wszyscyZmiany = null)
    {
        var wszyscy = wszyscyZmiany?.ToList() ?? await GetByZmianaAsync(nrZmiany);
        if (wszyscy.Count == 0)
            return wszyscy;

        if (!await _calendar.IsWorkDayAsync(nrZmiany, data))
            return [];

        if (string.IsNullOrWhiteSpace(_bober.DatabasePath))
            return wszyscy;

        try
        {
            var nieobecniIds = await PobierzNieobecnychZBoberAsync(data, nrZmiany);
            return wszyscy.Where(f => !nieobecniIds.Contains(f.Id)).ToList();
        }
        catch
        {
            return wszyscy;
        }
    }

    private async Task<HashSet<int>> PobierzNieobecnychZBoberAsync(DateOnly data, int nrZmiany)
    {
        await using var boberConn = _bober.Create();
        await boberConn.OpenAsync();

        const string sql = """
            SELECT FunkcjonariuszId, TypWpisu FROM GrafikWpisy
            WHERE ZmianaId=? AND Rok=? AND Miesiac=? AND Dzien=?
            """;

        await using var cmd = new OleDbCommand(sql, boberConn);
        cmd.Parameters.AddWithValue("@p1", (short)nrZmiany);
        cmd.Parameters.AddWithValue("@p2", (short)data.Year);
        cmd.Parameters.AddWithValue("@p3", (short)data.Month);
        cmd.Parameters.AddWithValue("@p4", (short)data.Day);
        await using var reader = await cmd.ExecuteReaderAsync();

        var nieobecniIds = new HashSet<int>();
        while (await reader.ReadAsync())
        {
            var typStr = reader.IsDBNull(1) ? null : reader.GetStringSafe(1);
            // Oddaje / „?” = osoba dostępna — nie wykluczaj z obsady.
            if (BoberTypWpisuMapper.MapLubPominOddal(typStr) is null)
                continue;

            nieobecniIds.Add(reader.GetIntSafe(0));
        }

        return nieobecniIds;
    }

    /// <summary>
    /// Pobiera nieobecnych z BOBER wraz z typem nieobecności.
    /// Kolumna TypWpisu: U, Del, WS, D, S, C (+ opcjonalnie Oddał „/”).
    /// </summary>
    public async Task<List<(int FunkcjonariuszId, Core.Enums.TypNieobecnosci Typ)>> PobierzNieobecnychZTypemAsync(
        DateOnly data, int nrZmiany)
    {
        if (string.IsNullOrWhiteSpace(_bober.DatabasePath))
            return [];

        try
        {
            await using var conn = _bober.Create();
            await conn.OpenAsync();

            if (!await _calendar.IsWorkDayAsync(nrZmiany, data))
                return [];

            const string sqlZTypem = """
                SELECT FunkcjonariuszId, TypWpisu FROM GrafikWpisy
                WHERE ZmianaId=? AND Rok=? AND Miesiac=? AND Dzien=?
                """;

            await using var cmd = new OleDbCommand(sqlZTypem, conn);
            cmd.Parameters.AddWithValue("@p1", (short)nrZmiany);
            cmd.Parameters.AddWithValue("@p2", (short)data.Year);
            cmd.Parameters.AddWithValue("@p3", (short)data.Month);
            cmd.Parameters.AddWithValue("@p4", (short)data.Day);

            await using var reader = await cmd.ExecuteReaderAsync();
            var wynik = new List<(int, Core.Enums.TypNieobecnosci)>();
            while (await reader.ReadAsync())
            {
                var fid = reader.GetIntSafe(0);
                var typStr = reader.IsDBNull(1) ? null : reader.GetStringSafe(1);
                var typ = BoberTypWpisuMapper.MapLubPominOddal(typStr);
                if (typ is null)
                    continue;

                wynik.Add((fid, typ.Value));
            }
            return wynik;
        }
        catch
        {
            return [];
        }
    }

    /// <summary>Pobiera wszystkie typy uprawnień ze słownika CHOMIK.</summary>
    public async Task<List<(int Id, string Nazwa)>> GetTypyUprawnienAsync()
    {
        var list = new List<(int, string)>();
        try
        {
            await using var conn = _chomik.Create();
            await conn.OpenAsync();
            await using var cmd = new OleDbCommand(
                "SELECT Id, Nazwa, Podtyp FROM TypyUprawnien ORDER BY Nazwa, Podtyp", conn);
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                var nazwa = r.GetStringSafe(1);
                var podtyp = r.IsDBNull(2) ? null : r.GetStringSafe(2);
                list.Add((r.GetIntSafe(0), ChomikSlowniki.FormatUprawnienie(nazwa, podtyp)));
            }
        }
        catch { /* CHOMIK niedostępny lub brak tabeli */ }
        return list;
    }

    private static async Task AttachUprawnieniaAsync(OleDbConnection conn, List<Funkcjonariusz> list)
    {
        if (list.Count == 0) return;
        var ids = string.Join(",", list.Select(f => f.Id));
        var sql = $"""
            SELECT fu.FunkcjonariuszId, fu.TypUprawnieniaId, tu.Nazwa, tu.Podtyp
            FROM FunkcjonariuszUprawnienia fu
            INNER JOIN TypyUprawnien tu ON tu.Id = fu.TypUprawnieniaId
            WHERE fu.FunkcjonariuszId IN ({ids})
            """;
        await using var cmd = new OleDbCommand(sql, conn);
        await using var r = await cmd.ExecuteReaderAsync();
        var dict = list.ToDictionary(f => f.Id);
        while (await r.ReadAsync())
        {
            var fid = r.GetIntSafe(0);
            if (!dict.TryGetValue(fid, out var f)) continue;

            var typId  = r.GetIntSafe(1);
            var nazwa  = r.GetStringSafe(2);
            var podtyp = r.IsDBNull(3) ? null : r.GetStringSafe(3);

            f.IdUprawnien.Add(typId);
            f.NazwyUprawnien.Add(ChomikSlowniki.FormatUprawnienie(nazwa, podtyp));
        }
    }
}
