namespace SalesAR.Plugins.DataStore.SQL;

using System.Data;
using Dapper;
using SalesAR.CoreBusiness.Models;
using SalesAR.UseCases.PluginInterfaces;

public class CustomerRepository : ICustomerRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public CustomerRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<Customer>> GetAllAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "SELECT Id, Code, Name, Phone, Address, CreditLimit, PaymentTermDays, IsActive FROM dbo.Customers ORDER BY Code ASC;";
        return await connection.QueryAsync<Customer>(sql);
    }

    public async Task<Customer?> GetByIdAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "SELECT Id, Code, Name, Phone, Address, CreditLimit, PaymentTermDays, IsActive FROM dbo.Customers WHERE Id = @Id;";
        return await connection.QuerySingleOrDefaultAsync<Customer>(sql, new { Id = id });
    }

    public async Task<Customer?> GetByCodeAsync(string code)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "SELECT Id, Code, Name, Phone, Address, CreditLimit, PaymentTermDays, IsActive FROM dbo.Customers WHERE Code = @Code;";
        return await connection.QuerySingleOrDefaultAsync<Customer>(sql, new { Code = code });
    }

    public async Task<int> AddAsync(Customer customer)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            INSERT INTO dbo.Customers (Code, Name, Phone, Address, CreditLimit, PaymentTermDays, IsActive)
            OUTPUT INSERTED.Id
            VALUES (@Code, @Name, @Phone, @Address, @CreditLimit, @PaymentTermDays, @IsActive);";
        return await connection.ExecuteScalarAsync<int>(sql, customer);
    }

    public async Task UpdateAsync(Customer customer)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            UPDATE dbo.Customers
            SET Code = @Code,
                Name = @Name,
                Phone = @Phone,
                Address = @Address,
                CreditLimit = @CreditLimit,
                PaymentTermDays = @PaymentTermDays,
                IsActive = @IsActive
            WHERE Id = @Id;";
        await connection.ExecuteAsync(sql, customer);
    }

    public async Task<decimal> GetCurrentDebtAsync(int customerId)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT ISNULL(SUM(i.TotalAmount - ISNULL(pa.Allocated, 0)), 0)
            FROM dbo.Invoices i
            LEFT JOIN (
                SELECT InvoiceId, SUM(AllocatedAmount) AS Allocated
                FROM dbo.PaymentAllocations
                GROUP BY InvoiceId
            ) pa ON i.Id = pa.InvoiceId
            WHERE i.CustomerId = @CustomerId AND i.Status <> 'Paid';";
        return await connection.ExecuteScalarAsync<decimal>(sql, new { CustomerId = customerId });
    }
}
