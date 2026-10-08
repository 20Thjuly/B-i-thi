namespace SalesAR.Plugins.DataStore.SQL;

using System.Data;
using Dapper;
using SalesAR.CoreBusiness.Models;
using SalesAR.UseCases.PluginInterfaces;

public class InvoiceRepository : IInvoiceRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public InvoiceRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<Invoice>> GetAllAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "SELECT Id, InvoiceNumber, CustomerId, InvoiceDate, DueDate, TotalAmount, Status FROM dbo.Invoices ORDER BY InvoiceDate DESC;";
        return await connection.QueryAsync<Invoice>(sql);
    }

    public async Task<Invoice?> GetByIdAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "SELECT Id, InvoiceNumber, CustomerId, InvoiceDate, DueDate, TotalAmount, Status FROM dbo.Invoices WHERE Id = @Id;";
        return await connection.QuerySingleOrDefaultAsync<Invoice>(sql, new { Id = id });
    }

    public async Task<Invoice?> GetByInvoiceNumberAsync(string invoiceNumber)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "SELECT Id, InvoiceNumber, CustomerId, InvoiceDate, DueDate, TotalAmount, Status FROM dbo.Invoices WHERE InvoiceNumber = @InvoiceNumber;";
        return await connection.QuerySingleOrDefaultAsync<Invoice>(sql, new { InvoiceNumber = invoiceNumber });
    }

    public async Task<IEnumerable<InvoiceLine>> GetLinesByInvoiceIdAsync(int invoiceId)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "SELECT Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount FROM dbo.InvoiceLines WHERE InvoiceId = @InvoiceId ORDER BY Id ASC;";
        return await connection.QueryAsync<InvoiceLine>(sql, new { InvoiceId = invoiceId });
    }

    public async Task<IEnumerable<Invoice>> GetUnpaidInvoicesByCustomerAsync(int customerId)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT Id, InvoiceNumber, CustomerId, InvoiceDate, DueDate, TotalAmount, Status 
            FROM dbo.Invoices 
            WHERE CustomerId = @CustomerId AND Status <> 'Paid'
            ORDER BY InvoiceDate ASC, Id ASC;";
        return await connection.QueryAsync<Invoice>(sql, new { CustomerId = customerId });
    }

    // Luồng giao dịch chính: Cặp header-line ghi trong 1 transaction (Rubric K2.2 & K2.3)
    public async Task<int> CreateInvoiceWithLinesAsync(Invoice invoice, IEnumerable<InvoiceLine> lines)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();

        try
        {
            const string insertInvoiceSql = @"
                INSERT INTO dbo.Invoices (InvoiceNumber, CustomerId, InvoiceDate, DueDate, TotalAmount, Status)
                OUTPUT INSERTED.Id
                VALUES (@InvoiceNumber, @CustomerId, @InvoiceDate, @DueDate, @TotalAmount, @Status);";

            var invoiceId = await connection.ExecuteScalarAsync<int>(insertInvoiceSql, invoice, transaction);

            const string insertLineSql = @"
                INSERT INTO dbo.InvoiceLines (InvoiceId, ProductId, Quantity, UnitPrice, Amount)
                VALUES (@InvoiceId, @ProductId, @Quantity, @UnitPrice, @Amount);";

            foreach (var line in lines)
            {
                line.InvoiceId = invoiceId;
                await connection.ExecuteAsync(insertLineSql, line, transaction);
            }

            transaction.Commit();
            return invoiceId;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task UpdateStatusAsync(int invoiceId, string status)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "UPDATE dbo.Invoices SET Status = @Status WHERE Id = @Id;";
        await connection.ExecuteAsync(sql, new { Id = invoiceId, Status = status });
    }
}
