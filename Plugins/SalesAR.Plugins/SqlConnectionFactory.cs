namespace SalesAR.Plugins.DataStore.SQL;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

public interface ISqlConnectionFactory
{
    SqlConnection CreateConnection();
}

public class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Server=.;Database=SalesARDB;Trusted_Connection=True;TrustServerCertificate=True;";
    }

    public SqlConnection CreateConnection() => new SqlConnection(_connectionString);
}
