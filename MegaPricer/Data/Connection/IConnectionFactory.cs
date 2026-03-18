using System.Data;

public interface IConnectionFactory
{
    public IDbConnection GetConnection();
    public IDbConnection GetConnection(string ConnectionString);
}
