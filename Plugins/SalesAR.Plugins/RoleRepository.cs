namespace SalesAR.Plugins.DataStore.SQL;

using System.Data;
using Dapper;
using SalesAR.CoreBusiness.Models;
using SalesAR.UseCases.PluginInterfaces;

public class RoleRepository : IRoleRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public RoleRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<Role>> GetAllRolesAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "SELECT Id, Name, Description FROM dbo.Roles ORDER BY Id ASC;";
        return await connection.QueryAsync<Role>(sql);
    }

    public async Task<Role?> GetByIdAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "SELECT Id, Name, Description FROM dbo.Roles WHERE Id = @Id;";
        return await connection.QuerySingleOrDefaultAsync<Role>(sql, new { Id = id });
    }

    public async Task<Role?> GetByNameAsync(string name)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "SELECT Id, Name, Description FROM dbo.Roles WHERE Name = @Name;";
        return await connection.QuerySingleOrDefaultAsync<Role>(sql, new { Name = name });
    }
}
