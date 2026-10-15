namespace SalesAR.Plugins.DataStore.SQL;

using System.Data;
using Dapper;
using SalesAR.CoreBusiness.Models;
using SalesAR.UseCases.Models;
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

    public async Task<IEnumerable<Invoice>> GetInvoicesByCustomerAsync(int customerId)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "SELECT Id, InvoiceNumber, CustomerId, InvoiceDate, DueDate, TotalAmount, Status FROM dbo.Invoices WHERE CustomerId = @CustomerId ORDER BY InvoiceDate DESC;";
        return await connection.QueryAsync<Invoice>(sql, new { CustomerId = customerId });
    }

    public async Task<IEnumerable<Invoice>> GetUnpaidInvoicesByCustomerAsync(int customerId)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT Id, InvoiceNumber, CustomerId, InvoiceDate, DueDate, TotalAmount, Status 
            FROM dbo.Invoices 
            WHERE CustomerId = @CustomerId AND Status <> 'Paid' AND Status <> 'Cancelled'
            ORDER BY DueDate ASC, InvoiceDate ASC, Id ASC;";
        return await connection.QueryAsync<Invoice>(sql, new { CustomerId = customerId });
    }

    /// <summary>
    /// Lấy danh sách hóa đơn còn dư nợ của khách hàng:
    /// Thống nhất công thức: RemainingAmount = TotalAmount - ValidAllocatedPayments - ValidAdjustments
    /// Sắp xếp theo thứ tự FIFO: DueDate ASC, InvoiceDate ASC, Id ASC.
    /// </summary>
    public async Task<IEnumerable<InvoiceOutstandingDto>> GetOutstandingInvoicesByCustomerAsync(int customerId)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT 
                i.Id,
                i.InvoiceNumber,
                i.CustomerId,
                i.InvoiceDate,
                i.DueDate,
                i.TotalAmount,
                ISNULL(pa.Allocated, 0) AS PaidAmount,
                ISNULL(da.Adjusted, 0) AS AdjustedAmount,
                i.Status
            FROM dbo.Invoices i
            LEFT JOIN (
                SELECT InvoiceId, SUM(AllocatedAmount) AS Allocated
                FROM dbo.PaymentAllocations
                GROUP BY InvoiceId
            ) pa ON i.Id = pa.InvoiceId
            LEFT JOIN (
                SELECT InvoiceId, SUM(Amount) AS Adjusted
                FROM dbo.DebtAdjustments
                WHERE InvoiceId IS NOT NULL
                GROUP BY InvoiceId
            ) da ON i.Id = da.InvoiceId
            WHERE i.CustomerId = @CustomerId AND i.Status <> 'Paid' AND i.Status <> 'Cancelled'
            ORDER BY i.DueDate ASC, i.InvoiceDate ASC, i.Id ASC;";
        return await connection.QueryAsync<InvoiceOutstandingDto>(sql, new { CustomerId = customerId });
    }

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

            const string updateStockSql = @"
                UPDATE dbo.Products
                SET StockQuantity = StockQuantity - @Quantity
                WHERE Id = @ProductId AND StockQuantity >= @Quantity;";

            var lineList = lines.ToList();
            foreach (var line in lineList)
            {
                line.InvoiceId = invoiceId;
                await connection.ExecuteAsync(insertLineSql, line, transaction);

                int rowsAffected = await connection.ExecuteAsync(updateStockSql, new { Quantity = line.Quantity, ProductId = line.ProductId }, transaction);
                if (rowsAffected == 0)
                {
                    throw new InvalidOperationException($"Sản phẩm (ID: {line.ProductId}) không đủ số lượng tồn kho để xuất hóa đơn.");
                }
            }

            // Ghi nhận AuditLog tự động khi tạo hóa đơn (Part 4 - Requirement 4)
            const string auditSql = @"
                INSERT INTO dbo.AuditLogs (TableName, RecordId, Action, NewValues, ChangedBy, ChangedAt, Reason)
                VALUES ('Invoices', @RecordId, 'INSERT', @NewValues, 'sales', SYSDATETIME(), N'Lập hóa đơn bán hàng mới');";

            var auditJson = $"{{\"InvoiceNumber\":\"{invoice.InvoiceNumber}\",\"CustomerId\":{invoice.CustomerId},\"TotalAmount\":{invoice.TotalAmount},\"LinesCount\":{lineList.Count}}}";
            await connection.ExecuteAsync(auditSql, new { RecordId = invoiceId, NewValues = auditJson }, transaction);

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
