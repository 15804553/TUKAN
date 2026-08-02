using System.Data.OleDb;

namespace SKRYBEK.Data.Connections;

public sealed class SkrybekConnectionFactory
{
    private static readonly string[] DefaultMigrationPasswords = ["5359"];

    private readonly string _databasePath;
    private readonly string _password;

    public SkrybekConnectionFactory(string databasePath, string? preferredPassword = null)
    {
        _databasePath = databasePath;
        _password = preferredPassword ?? DefaultMigrationPasswords[0];
    }

    public OleDbConnection Create()
    {
        var connectionString =
            $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={_databasePath};Jet OLEDB:Database Password={_password};";
        return new OleDbConnection(connectionString);
    }

    public string DatabasePath => _databasePath;
}
