using System.Data.OleDb;
using BOBER.Core.Models;

namespace BOBER.Data.Repositories;

public sealed class GrafikNurkowyRepository(BoberConnectionFactory connectionFactory) : IGrafikNurkowyRepository
{
    public async Task<GrafikNurkowyZatwierdzenie?> GetAsync(
        int rok,
        int miesiac,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new OleDbCommand(
            """
            SELECT Rok, Miesiac, Zatwierdzony, ZatwierdzonyPrzez, DataZatwierdzenia
            FROM GrafikNurkowyZatwierdzenia
            WHERE Rok = ? AND Miesiac = ?
            """,
            connection);
        AddShort(command, rok);
        AddShort(command, miesiac);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return new GrafikNurkowyZatwierdzenie
        {
            Rok = reader.GetFieldInt32(0),
            Miesiac = reader.GetFieldInt32(1),
            Zatwierdzony = !reader.IsDBNull(2) && Convert.ToBoolean(reader.GetValue(2)),
            ZatwierdzonyPrzez = reader.IsDBNull(3) ? null : reader.GetString(3),
            DataZatwierdzenia = reader.IsDBNull(4) ? null : reader.GetDateTime(4)
        };
    }

    public async Task SetZatwierdzenieAsync(
        int rok,
        int miesiac,
        bool zatwierdzony,
        string? zatwierdzonyPrzez,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();

        await using var checkCmd = new OleDbCommand(
            "SELECT COUNT(*) FROM GrafikNurkowyZatwierdzenia WHERE Rok = ? AND Miesiac = ?",
            connection);
        AddShort(checkCmd, rok);
        AddShort(checkCmd, miesiac);
        var exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync(cancellationToken)) > 0;

        if (exists)
        {
            await using var update = new OleDbCommand(
                """
                UPDATE GrafikNurkowyZatwierdzenia
                SET Zatwierdzony = ?, ZatwierdzonyPrzez = ?, DataZatwierdzenia = ?
                WHERE Rok = ? AND Miesiac = ?
                """,
                connection);
            AddYesNo(update, zatwierdzony);
            AddNullableString(update, zatwierdzonyPrzez);
            AddNullableDate(update, zatwierdzony ? DateTime.Now : null);
            AddShort(update, rok);
            AddShort(update, miesiac);
            await update.ExecuteNonQueryAsync(cancellationToken);
            return;
        }

        await using var insert = new OleDbCommand(
            """
            INSERT INTO GrafikNurkowyZatwierdzenia
                (Rok, Miesiac, Zatwierdzony, ZatwierdzonyPrzez, DataZatwierdzenia)
            VALUES (?, ?, ?, ?, ?)
            """,
            connection);
        AddShort(insert, rok);
        AddShort(insert, miesiac);
        AddYesNo(insert, zatwierdzony);
        AddNullableString(insert, zatwierdzonyPrzez);
        AddNullableDate(insert, zatwierdzony ? DateTime.Now : null);
        await insert.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddShort(OleDbCommand command, int value) =>
        command.Parameters.Add(new OleDbParameter { OleDbType = OleDbType.SmallInt, Value = (short)value });

    /// <summary>Access YESNO — bezpieczniej jako Boolean z jawnym OleDbType.</summary>
    private static void AddYesNo(OleDbCommand command, bool value) =>
        command.Parameters.Add(new OleDbParameter { OleDbType = OleDbType.Boolean, Value = value });

    private static void AddNullableString(OleDbCommand command, string? value) =>
        command.Parameters.Add(new OleDbParameter
        {
            OleDbType = OleDbType.VarWChar,
            Value = string.IsNullOrWhiteSpace(value) ? DBNull.Value : value
        });

    private static void AddNullableDate(OleDbCommand command, DateTime? value) =>
        command.Parameters.Add(new OleDbParameter
        {
            OleDbType = OleDbType.Date,
            Value = value.HasValue ? value.Value : DBNull.Value
        });
}
