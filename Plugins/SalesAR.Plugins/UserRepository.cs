namespace SalesAR.Plugins.DataStore.SQL;

using System.Data;
using Dapper;
using SalesAR.CoreBusiness.Models;
using SalesAR.UseCases.PluginInterfaces;

public class UserRepository : IUserRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public UserRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT u.Id, u.Username, u.PasswordHash, u.FullName, u.Email, u.RoleId, r.Name AS RoleName, u.IsActive, u.CreatedAt
            FROM dbo.Users u
            INNER JOIN dbo.Roles r ON u.RoleId = r.Id
            WHERE u.Username = @Username;";
        return await connection.QuerySingleOrDefaultAsync<User>(sql, new { Username = username });
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT u.Id, u.Username, u.PasswordHash, u.FullName, u.Email, u.RoleId, r.Name AS RoleName, u.IsActive, u.CreatedAt
            FROM dbo.Users u
            INNER JOIN dbo.Roles r ON u.RoleId = r.Id
            WHERE u.Id = @Id;";
        return await connection.QuerySingleOrDefaultAsync<User>(sql, new { Id = id });
    }

    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT u.Id, u.Username, u.PasswordHash, u.FullName, u.Email, u.RoleId, r.Name AS RoleName, u.IsActive, u.CreatedAt
            FROM dbo.Users u
            INNER JOIN dbo.Roles r ON u.RoleId = r.Id
            ORDER BY u.Id ASC;";
        return await connection.QueryAsync<User>(sql);
    }

    public async Task<int> CreateUserAsync(User user)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            INSERT INTO dbo.Users (Username, PasswordHash, FullName, Email, RoleId, IsActive, CreatedAt)
            VALUES (@Username, @PasswordHash, @FullName, @Email, @RoleId, @IsActive, SYSDATETIME());
            SELECT CAST(SCOPE_IDENTITY() AS INT);";
        return await connection.ExecuteScalarAsync<int>(sql, user);
    }

    public async Task UpdateUserAsync(User user)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            UPDATE dbo.Users
            SET FullName = @FullName,
                Email = @Email,
                RoleId = @RoleId,
                IsActive = @IsActive
            WHERE Id = @Id;";
        await connection.ExecuteAsync(sql, user);
    }

    public async Task ToggleActiveAsync(int userId, bool isActive)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "UPDATE dbo.Users SET IsActive = @IsActive WHERE Id = @Id;";
        await connection.ExecuteAsync(sql, new { Id = userId, IsActive = isActive });
    }
}
