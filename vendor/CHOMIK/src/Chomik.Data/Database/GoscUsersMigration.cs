using System.Data.OleDb;
using Chomik.Core.Constants;
using Chomik.Core.Enums;
using Chomik.Core.Security;

namespace Chomik.Data.Database;

/// <summary>Dopisuje konta Gość 1–3 do istniejących baz (seed dotyczy tylko nowych).</summary>
internal static class GoscUsersMigration
{
    private static readonly (string Login, UserRole Role, int Shift)[] Guests =
    [
        ("Gość 1", UserRole.Gosc1, 1),
        ("Gość 2", UserRole.Gosc2, 2),
        ("Gość 3", UserRole.Gosc3, 3)
    ];

    public static async Task ApplyAsync(
        OleDbConnection connection,
        CancellationToken cancellationToken = default)
    {
        foreach (var (login, role, shift) in Guests)
        {
            try
            {
                if (await ExistsAsync(connection, login, cancellationToken))
                    continue;

                var defaultPassword = DefaultCredentials.DefaultPasswords[role]
                    ?? throw new InvalidOperationException($"Brak domyślnego hasła dla {role}.");
                var (hash, salt) = PasswordHasher.HashPassword(defaultPassword);

                await using var command = new OleDbCommand(
                    "INSERT INTO Uzytkownicy (Login, Rola, NumerZmiany, HasloHash, HasloSol) VALUES (?, ?, ?, ?, ?)",
                    connection);
                command.Parameters.AddWithValue("@p1", login);
                command.Parameters.AddWithValue("@p2", (short)role);
                command.Parameters.AddWithValue("@p3", shift);
                command.Parameters.AddWithValue("@p4", hash);
                command.Parameters.AddWithValue("@p5", salt);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (OleDbException)
            {
                // Tabela niedostępna — seed pokryje nowe bazy.
            }
        }
    }

    private static async Task<bool> ExistsAsync(
        OleDbConnection connection,
        string login,
        CancellationToken cancellationToken)
    {
        await using var command = new OleDbCommand(
            "SELECT COUNT(*) FROM Uzytkownicy WHERE Login = ?",
            connection);
        command.Parameters.AddWithValue("@p1", login);
        var count = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
        return count > 0;
    }
}
