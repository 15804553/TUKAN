using System.Data.OleDb;

namespace BOBER.Data;

public sealed class BoberConnectionFactory(BoberDatabaseOptions options)
{
    public OleDbConnection CreateOpenConnection()
    {
        var connection = new OleDbConnection(options.BuildConnectionString());
        connection.Open();
        return connection;
    }
}

public sealed class ChomikConnectionFactory(ChomikDatabaseOptions options)
{
    public OleDbConnection CreateOpenConnection()
    {
        var connection = new OleDbConnection(options.BuildConnectionString());
        connection.Open();
        return connection;
    }
}
