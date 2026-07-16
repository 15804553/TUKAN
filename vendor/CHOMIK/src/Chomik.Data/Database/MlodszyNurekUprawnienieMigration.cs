using System.Data.OleDb;

namespace Chomik.Data.Database;

/// <summary>
/// Dopisuje typ uprawnienia „Mł.nurek” do istniejących baz (seed dotyczy tylko nowych).
/// </summary>
internal static class MlodszyNurekUprawnienieMigration
{
    private const string Nazwa = "Mł.nurek";

    public static async Task ApplyAsync(
        OleDbConnection connection,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (await ExistsAsync(connection, cancellationToken))
                return;

            await using var command = new OleDbCommand(
                "INSERT INTO TypyUprawnien (Nazwa, Podtyp, WymagaDaty) VALUES (?, ?, ?)",
                connection);
            command.Parameters.AddWithValue("@p1", Nazwa);
            command.Parameters.AddWithValue("@p2", DBNull.Value);
            command.Parameters.AddWithValue("@p3", true);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (OleDbException)
        {
            // Tabela niedostępna lub baza w trakcie inicjalizacji — seed pokryje nowe bazy.
        }
    }

    private static async Task<bool> ExistsAsync(
        OleDbConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = new OleDbCommand(
            "SELECT COUNT(*) FROM TypyUprawnien WHERE Nazwa = ?",
            connection);
        command.Parameters.AddWithValue("@p1", Nazwa);
        var count = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
        return count > 0;
    }
}
