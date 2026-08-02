using System.Data.OleDb;
using Chomik.Core.Constants;
using Chomik.Data;
using Chomik.Data.Database;
using Chomik.Data.Repositories;

namespace Chomik.Services.Database;

public sealed class DatabaseService(
    DatabaseOptions options,
    DatabaseBootstrapper bootstrapper,
    IFunkcjonariuszRepository funkcjonariuszRepository)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        options.MigrateLegacyDatabaseIfNeeded();

        Exception? lastError = null;
        foreach (var password in GetPasswordCandidates())
        {
            options.DatabasePassword = password;
            options.UseDatabasePassword = true;

            try
            {
                await bootstrapper.EnsureReadyAsync(cancellationToken);
                await funkcjonariuszRepository.TestConnectionAsync(cancellationToken);
                return;
            }
            catch (Exception ex) when (IsInvalidPasswordError(ex))
            {
                lastError = ex;
            }
        }

        throw new InvalidOperationException(
            BuildConnectionErrorMessage(lastError),
            lastError);
    }

    private IEnumerable<string> GetPasswordCandidates()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        if (!string.IsNullOrEmpty(options.DatabasePassword) && seen.Add(options.DatabasePassword))
            yield return options.DatabasePassword;

        foreach (var candidate in DefaultCredentials.DatabasePasswordMigrationCandidates)
        {
            if (seen.Add(candidate))
                yield return candidate;
        }
    }

    private string BuildConnectionErrorMessage(Exception? inner) =>
        "Nie można otworzyć bazy danych CHOMIK.\n\n" +
        $"Plik: {options.GetFullPath()}\n\n" +
        "Sprawdź, czy plik istnieje i czy hasło bazy Access jest zgodne z konfiguracją aplikacji.\n" +
        "Hasło nie jest wyświetlane w komunikacie z powodów bezpieczeństwa.\n\n" +
        $"Szczegóły: {inner?.Message}";

    private static bool IsInvalidPasswordError(Exception ex)
    {
        for (var current = ex; current is not null; current = current.InnerException)
        {
            if (current is OleDbException oleDb
                && (oleDb.Message.Contains("hasło", StringComparison.OrdinalIgnoreCase)
                    || oleDb.Message.Contains("password", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        return false;
    }
}
