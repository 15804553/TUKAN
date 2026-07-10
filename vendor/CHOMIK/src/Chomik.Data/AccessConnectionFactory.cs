using System.Data.OleDb;

namespace Chomik.Data;

public sealed class AccessConnectionFactory(DatabaseOptions options)
{
    public OleDbConnection CreateOpenConnection()
    {
        var connection = new OleDbConnection(options.BuildConnectionString());
        connection.Open();
        return connection;
    }
}
