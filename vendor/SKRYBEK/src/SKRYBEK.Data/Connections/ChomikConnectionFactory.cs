using System.Data.OleDb;

namespace SKRYBEK.Data.Connections;

public sealed class ChomikConnectionFactory
{
    private static readonly string[] DefaultMigrationPasswords = ["5359", "5393"];

    private readonly string _databasePath;
    private readonly string[] _passwordCandidates;

    public ChomikConnectionFactory(string databasePath, string? preferredPassword = null)
    {
        _databasePath = databasePath;
        _passwordCandidates = BuildCandidates(preferredPassword);
    }

    public OleDbConnection Create()
    {
        if (string.IsNullOrWhiteSpace(_databasePath))
            throw new InvalidOperationException(
                "Nie ustawiono ścieżki bazy personelu (CHOMIK / TukanDatabase).");

        Exception? lastError = null;
        foreach (var pwd in _passwordCandidates)
        {
            try
            {
                using var probe = new OleDbConnection(BuildConnectionString(pwd));
                probe.Open();
                return new OleDbConnection(BuildConnectionString(pwd));
            }
            catch (Exception ex)
            {
                lastError = ex;
            }
        }

        throw new InvalidOperationException(
            $"Nie można otworzyć bazy CHOMIK:\n{_databasePath}",
            lastError);
    }

    private static string[] BuildCandidates(string? preferred)
    {
        var list = new List<string>();
        if (!string.IsNullOrEmpty(preferred))
            list.Add(preferred);
        foreach (var p in DefaultMigrationPasswords)
        {
            if (!list.Contains(p, StringComparer.Ordinal))
                list.Add(p);
        }

        return list.ToArray();
    }

    private string BuildConnectionString(string databasePassword) =>
        $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={_databasePath};Jet OLEDB:Database Password={databasePassword};";

    public string DatabasePath => _databasePath;

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            await using var conn = Create();
            await conn.OpenAsync();
            return conn.State == System.Data.ConnectionState.Open;
        }
        catch
        {
            return false;
        }
    }
}
