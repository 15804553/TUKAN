using System.Data.OleDb;
using Chomik.Core.Enums;

namespace Chomik.Data.Database;

/// <summary>Przywraca model PA bez hasła (czyści hash, jeśli został ustawiony).</summary>
internal static class PaPasswordMigration
{
    public static async Task ApplyAsync(OleDbConnection connection, CancellationToken cancellationToken)
    {
        await using var update = new OleDbCommand(
            "UPDATE Uzytkownicy SET HasloHash = ?, HasloSol = ? WHERE Rola = ?",
            connection);
        update.Parameters.AddWithValue("@p1", string.Empty);
        update.Parameters.AddWithValue("@p2", string.Empty);
        update.Parameters.AddWithValue("@p3", (short)UserRole.Pa);
        await update.ExecuteNonQueryAsync(cancellationToken);
    }
}
