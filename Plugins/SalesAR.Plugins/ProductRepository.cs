namespace SalesAR.Plugins.DataStore.SQL;

using System.Data;
using Dapper;
using SalesAR.CoreBusiness.Models;
using SalesAR.UseCases.PluginInterfaces;

public class ProductRepository : IProductRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public ProductRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<Product>> GetAllAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "SELECT Id, Code, Name, Unit, Price, StockQuantity, IsActive FROM dbo.Products ORDER BY Code ASC;";
        return await connection.QueryAsync<Product>(sql);
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "SELECT Id, Code, Name, Unit, Price, StockQuantity, IsActive FROM dbo.Products WHERE Id = @Id;";
        return await connection.QuerySingleOrDefaultAsync<Product>(sql, new { Id = id });
    }

    public async Task<Product?> GetByCodeAsync(string code)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "SELECT Id, Code, Name, Unit, Price, StockQuantity, IsActive FROM dbo.Products WHERE Code = @Code;";
        return await connection.QuerySingleOrDefaultAsync<Product>(sql, new { Code = code });
    }

    public async Task<int> AddAsync(Product product)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            INSERT INTO dbo.Products (Code, Name, Unit, Price, StockQuantity, IsActive)
            OUTPUT INSERTED.Id
            VALUES (@Code, @Name, @Unit, @Price, @StockQuantity, @IsActive);";
        return await connection.ExecuteScalarAsync<int>(sql, product);
    }

    public async Task UpdateAsync(Product product)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            UPDATE dbo.Products
            SET Code = @Code,
                Name = @Name,
                Unit = @Unit,
                Price = @Price,
                StockQuantity = @StockQuantity,
                IsActive = @IsActive
            WHERE Id = @Id;";
        await connection.ExecuteAsync(sql, product);
    }

    public async Task DeleteAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "UPDATE dbo.Products SET IsActive = 0 WHERE Id = @Id;";
        await connection.ExecuteAsync(sql, new { Id = id });
    }
}
