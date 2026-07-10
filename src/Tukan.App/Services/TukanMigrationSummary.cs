using System.Text;

namespace Tukan.App.Services;

/// <summary>Formatuje podsumowanie migracji dla użytkownika.</summary>
public static class TukanMigrationSummary
{
    public static string FormatForUser(TukanDatabaseMigrator.MigrationResult result)
    {
        if (!result.Any)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        builder.AppendLine("TUKAN scalił dane w jednej bazie TukanDatabase.accdb:");
        builder.AppendLine();
        builder.AppendLine($"Plik: {TukanDatabaseOptions.FileName}");
        builder.AppendLine();

        foreach (var entry in result.Migrated)
        {
            builder.AppendLine($"• {entry.FileName}");
            if (!string.Equals(entry.SourcePath, entry.TargetPath, StringComparison.OrdinalIgnoreCase))
            {
                builder.AppendLine($"  z: {entry.SourcePath}");
            }
        }

        builder.AppendLine();
        builder.AppendLine("Stare pliki ChomikDatabase / BoberDatabase / SkrybekDatabase zostały zarchiwizowane jako *.legacy.bak (jeśli były w katalogu programu).");
        builder.AppendLine("Szczegóły zapisano w pliku TukanMigration.log obok programu.");

        return builder.ToString().TrimEnd();
    }
}
