using BOBER.Data;
using BOBER.Data.Database;

namespace BOBER.Services.Database;

public sealed class DatabaseService(
    DatabaseBootstrapper bootstrapper,
    ChomikDatabaseOptions chomikOptions)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await bootstrapper.EnsureReadyAsync(cancellationToken);
    }

    public bool IsChomikDbAvailable() => chomikOptions.FileExists();

    public string GetChomikDbPath() => chomikOptions.FilePath;

    public void UpdateChomikDbPath(string path)
    {
        chomikOptions.FilePath = path;
    }
}
