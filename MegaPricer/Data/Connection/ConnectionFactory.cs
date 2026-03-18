using System.Data;
using Microsoft.Data.Sqlite;

namespace MegaPricer.Data;

public class SqlConnectionFactory : IConnectionFactory
{
    private string _defaultConnectionString = ConfigurationSettings.ConnectionString;

    public IDbConnection GetConnection()
    {
        return GetConnection(_defaultConnectionString);
    }

    public IDbConnection GetConnection(string connectionString)
    {
        return new SqliteConnection(connectionString);
    }
}
