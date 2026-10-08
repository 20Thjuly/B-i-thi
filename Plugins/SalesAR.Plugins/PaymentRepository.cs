namespace SalesAR.Plugins.DataStore.SQL;

using System.Data;
using Dapper;
using SalesAR.CoreBusiness.Models;
using SalesAR.UseCases.PluginInterfaces;

public class PaymentRepository : IPaymentRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public PaymentRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<Payment>> GetAllAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "SELECT Id, PaymentNumber, CustomerId, PaymentDate, Amount, Note FROM dbo.Payments ORDER BY PaymentDate DESC;";
        return await connection.QueryAsync<Payment>(sql);
    }

    public async Task<Payment?> GetByIdAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "SELECT Id, PaymentNumber, CustomerId, PaymentDate, Amount, Note FROM dbo.Payments WHERE Id = @Id;";
        return await connection.QuerySingleOrDefaultAsync<Payment>(sql, new { Id = id });
    }

    public async Task<IEnumerable<PaymentAllocation>> GetAllocationsByPaymentIdAsync(int paymentId)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "SELECT Id, PaymentId, InvoiceId, AllocatedAmount FROM dbo.PaymentAllocations WHERE PaymentId = @PaymentId ORDER BY Id ASC;";
        return await connection.QueryAsync<PaymentAllocation>(sql, new { PaymentId = paymentId });
    }

    // Ghi nhận thanh toán và phân bổ theo 1 transaction
    public async Task<int> CreatePaymentWithAllocationsAsync(Payment payment, IEnumerable<PaymentAllocation> allocations)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();

        try
        {
            const string insertPaymentSql = @"
                INSERT INTO dbo.Payments (PaymentNumber, CustomerId, PaymentDate, Amount, Note)
                OUTPUT INSERTED.Id
                VALUES (@PaymentNumber, @CustomerId, @PaymentDate, @Amount, @Note);";

            var paymentId = await connection.ExecuteScalarAsync<int>(insertPaymentSql, payment, transaction);

            const string insertAllocationSql = @"
                INSERT INTO dbo.PaymentAllocations (PaymentId, InvoiceId, AllocatedAmount)
                VALUES (@PaymentId, @InvoiceId, @AllocatedAmount);";

            const string updateInvoiceStatusSql = @"
                UPDATE i
                SET i.Status = CASE 
                    WHEN (ISNULL(alloc.TotalAllocated, 0)) >= i.TotalAmount THEN 'Paid'
                    WHEN (ISNULL(alloc.TotalAllocated, 0)) > 0 THEN 'PartiallyPaid'
                    ELSE 'Unpaid'
                END
                FROM dbo.Invoices i
                LEFT JOIN (
                    SELECT InvoiceId, SUM(AllocatedAmount) AS TotalAllocated
                    FROM dbo.PaymentAllocations
                    GROUP BY InvoiceId
                ) alloc ON i.Id = alloc.InvoiceId
                WHERE i.Id = @InvoiceId;";

            foreach (var alloc in allocations)
            {
                alloc.PaymentId = paymentId;
                await connection.ExecuteAsync(insertAllocationSql, alloc, transaction);
                await connection.ExecuteAsync(updateInvoiceStatusSql, new { InvoiceId = alloc.InvoiceId }, transaction);
            }

            transaction.Commit();
            return paymentId;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}
