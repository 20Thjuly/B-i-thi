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

    public async Task<Payment?> GetByPaymentNumberAsync(string paymentNumber)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "SELECT Id, PaymentNumber, CustomerId, PaymentDate, Amount, Note FROM dbo.Payments WHERE PaymentNumber = @PaymentNumber;";
        return await connection.QuerySingleOrDefaultAsync<Payment>(sql, new { PaymentNumber = paymentNumber });
    }

    public async Task<IEnumerable<Payment>> GetPaymentsByCustomerAsync(int customerId)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "SELECT Id, PaymentNumber, CustomerId, PaymentDate, Amount, Note FROM dbo.Payments WHERE CustomerId = @CustomerId ORDER BY PaymentDate DESC;";
        return await connection.QueryAsync<Payment>(sql, new { CustomerId = customerId });
    }

    public async Task<IEnumerable<PaymentAllocation>> GetAllocationsByPaymentIdAsync(int paymentId)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "SELECT Id, PaymentId, InvoiceId, AllocatedAmount FROM dbo.PaymentAllocations WHERE PaymentId = @PaymentId ORDER BY Id ASC;";
        return await connection.QueryAsync<PaymentAllocation>(sql, new { PaymentId = paymentId });
    }

    public async Task<IEnumerable<PaymentAllocation>> GetAllocationsByCustomerAsync(int customerId)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT pa.Id, pa.PaymentId, pa.InvoiceId, pa.AllocatedAmount
            FROM dbo.PaymentAllocations pa
            INNER JOIN dbo.Payments p ON pa.PaymentId = p.Id
            WHERE p.CustomerId = @CustomerId
            ORDER BY pa.Id DESC;";
        return await connection.QueryAsync<PaymentAllocation>(sql, new { CustomerId = customerId });
    }

    public async Task<int> CreatePaymentWithAllocationsAsync(Payment payment, IEnumerable<PaymentAllocation> allocations)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

        try
        {
            // 1. Chống ghi nhận trùng lặp payment: Kiểm tra số phiếu thu với UPDLOCK
            const string checkDuplicateSql = @"
                SELECT COUNT(1) 
                FROM dbo.Payments WITH (UPDLOCK, HOLDLOCK)
                WHERE PaymentNumber = @PaymentNumber;";
            
            int exists = await connection.ExecuteScalarAsync<int>(checkDuplicateSql, new { payment.PaymentNumber }, transaction);
            if (exists > 0)
            {
                throw new InvalidOperationException($"Số phiếu thu '{payment.PaymentNumber}' đã tồn tại trong hệ thống. Không thể tạo trùng lặp.");
            }

            // 2. Kiểm tra tổng phân bổ không được vượt quá số tiền thanh toán khả dụng
            var allocList = allocations.ToList();
            decimal totalAlloc = allocList.Sum(a => a.AllocatedAmount);
            if (totalAlloc > payment.Amount)
            {
                throw new InvalidOperationException($"Lỗi phân bổ: Tổng phân bổ ({totalAlloc:N2}) vượt quá số tiền phiếu thu ({payment.Amount:N2}).");
            }

            const string insertPaymentSql = @"
                INSERT INTO dbo.Payments (PaymentNumber, CustomerId, PaymentDate, Amount, Note)
                OUTPUT INSERTED.Id
                VALUES (@PaymentNumber, @CustomerId, @PaymentDate, @Amount, @Note);";

            var paymentId = await connection.ExecuteScalarAsync<int>(insertPaymentSql, payment, transaction);

            const string insertAllocationSql = @"
                INSERT INTO dbo.PaymentAllocations (PaymentId, InvoiceId, AllocatedAmount)
                VALUES (@PaymentId, @InvoiceId, @AllocatedAmount);";

            // Kiểm tra số dư nợ hóa đơn trước khi phân bổ với khóa UPDLOCK để chống tranh chấp (concurrency race condition)
            const string checkInvoiceBalanceSql = @"
                SELECT 
                    (i.TotalAmount - ISNULL(pa.Allocated, 0) - ISNULL(da.Adjusted, 0)) AS RemainingAmount
                FROM dbo.Invoices i WITH (UPDLOCK, ROWLOCK)
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
                WHERE i.Id = @InvoiceId;";

            const string updateInvoiceStatusSql = @"
                UPDATE i
                SET i.Status = CASE 
                    WHEN (ISNULL(alloc.TotalAllocated, 0) + ISNULL(adj.TotalAdjusted, 0)) >= i.TotalAmount THEN 'Paid'
                    WHEN (ISNULL(alloc.TotalAllocated, 0) + ISNULL(adj.TotalAdjusted, 0)) > 0 THEN 'PartiallyPaid'
                    ELSE 'Unpaid'
                END
                FROM dbo.Invoices i
                LEFT JOIN (
                    SELECT InvoiceId, SUM(AllocatedAmount) AS TotalAllocated
                    FROM dbo.PaymentAllocations
                    GROUP BY InvoiceId
                ) alloc ON i.Id = alloc.InvoiceId
                LEFT JOIN (
                    SELECT InvoiceId, SUM(Amount) AS TotalAdjusted
                    FROM dbo.DebtAdjustments
                    WHERE InvoiceId IS NOT NULL
                    GROUP BY InvoiceId
                ) adj ON i.Id = adj.InvoiceId
                WHERE i.Id = @InvoiceId;";

            foreach (var alloc in allocList)
            {
                // Kiểm tra phân bổ không vượt số dư nợ có thể thu của hóa đơn
                var currentRemaining = await connection.ExecuteScalarAsync<decimal>(checkInvoiceBalanceSql, new { InvoiceId = alloc.InvoiceId }, transaction);
                if (alloc.AllocatedAmount > currentRemaining)
                {
                    throw new InvalidOperationException($"Số tiền phân bổ ({alloc.AllocatedAmount:N2}) vượt quá số dư nợ còn lại của hóa đơn ID {alloc.InvoiceId} ({currentRemaining:N2}).");
                }

                alloc.PaymentId = paymentId;
                await connection.ExecuteAsync(insertAllocationSql, alloc, transaction);
                await connection.ExecuteAsync(updateInvoiceStatusSql, new { InvoiceId = alloc.InvoiceId }, transaction);
            }

            // Ghi nhận AuditLog tự động trong cùng Transaction (Part 4 - Requirement 4)
            const string auditSql = @"
                INSERT INTO dbo.AuditLogs (TableName, RecordId, Action, NewValues, ChangedBy, ChangedAt, Reason)
                VALUES ('Payments', @RecordId, 'INSERT', @NewValues, 'accountant', SYSDATETIME(), N'Ghi nhận phiếu thu tiền và phân bổ công nợ FIFO');";

            var auditJson = $"{{\"PaymentNumber\":\"{payment.PaymentNumber}\",\"Amount\":{payment.Amount},\"AllocatedTotal\":{totalAlloc},\"AllocationsCount\":{allocList.Count}}}";
            await connection.ExecuteAsync(auditSql, new { RecordId = paymentId, NewValues = auditJson }, transaction);

            transaction.Commit();
            return paymentId;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task ReallocatePaymentAsync(int paymentId, IEnumerable<PaymentAllocation> newAllocations, string performedBy, string reason)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

        try
        {
            // 1. Kiểm tra tồn tại payment
            var payment = await connection.QuerySingleOrDefaultAsync<Payment>(
                "SELECT Id, PaymentNumber, CustomerId, Amount FROM dbo.Payments WITH (UPDLOCK) WHERE Id = @Id;", 
                new { Id = paymentId }, transaction);
            
            if (payment == null)
                throw new InvalidOperationException($"Không tìm thấy phiếu thu với ID: {paymentId}.");

            var newAllocList = newAllocations.ToList();
            decimal totalNewAlloc = newAllocList.Sum(a => a.AllocatedAmount);
            if (totalNewAlloc > payment.Amount)
                throw new InvalidOperationException($"Tổng phân bổ mới ({totalNewAlloc:N2}) vượt quá số tiền của phiếu thu ({payment.Amount:N2}).");

            // 2. Lấy danh sách phân bổ cũ để ghi Audit Log và phục hồi trạng thái hóa đơn
            const string oldAllocSql = "SELECT InvoiceId, AllocatedAmount FROM dbo.PaymentAllocations WHERE PaymentId = @PaymentId;";
            var oldAllocations = (await connection.QueryAsync<PaymentAllocation>(oldAllocSql, new { PaymentId = paymentId }, transaction)).ToList();

            var affectedInvoiceIds = oldAllocations.Select(a => a.InvoiceId).Union(newAllocList.Select(a => a.InvoiceId)).Distinct().ToList();

            // 3. Xóa các phân bổ cũ của phiếu thu
            await connection.ExecuteAsync("DELETE FROM dbo.PaymentAllocations WHERE PaymentId = @PaymentId;", new { PaymentId = paymentId }, transaction);

            // 4. Chèn các phân bổ mới
            const string insertAllocationSql = @"
                INSERT INTO dbo.PaymentAllocations (PaymentId, InvoiceId, AllocatedAmount)
                VALUES (@PaymentId, @InvoiceId, @AllocatedAmount);";

            foreach (var alloc in newAllocList)
            {
                alloc.PaymentId = paymentId;
                await connection.ExecuteAsync(insertAllocationSql, alloc, transaction);
            }

            // 5. Cập nhật lại trạng thái cho toàn bộ hóa đơn bị ảnh hưởng (cả cũ và mới)
            const string updateInvoiceStatusSql = @"
                UPDATE i
                SET i.Status = CASE 
                    WHEN (ISNULL(alloc.TotalAllocated, 0) + ISNULL(adj.TotalAdjusted, 0)) >= i.TotalAmount THEN 'Paid'
                    WHEN (ISNULL(alloc.TotalAllocated, 0) + ISNULL(adj.TotalAdjusted, 0)) > 0 THEN 'PartiallyPaid'
                    ELSE 'Unpaid'
                END
                FROM dbo.Invoices i
                LEFT JOIN (
                    SELECT InvoiceId, SUM(AllocatedAmount) AS TotalAllocated
                    FROM dbo.PaymentAllocations
                    GROUP BY InvoiceId
                ) alloc ON i.Id = alloc.InvoiceId
                LEFT JOIN (
                    SELECT InvoiceId, SUM(Amount) AS TotalAdjusted
                    FROM dbo.DebtAdjustments
                    WHERE InvoiceId IS NOT NULL
                    GROUP BY InvoiceId
                ) adj ON i.Id = adj.InvoiceId
                WHERE i.Id = @InvoiceId;";

            foreach (var invId in affectedInvoiceIds)
            {
                await connection.ExecuteAsync(updateInvoiceStatusSql, new { InvoiceId = invId }, transaction);
            }

            // 6. Ghi Audit Log với đầy đủ OldValues và NewValues (Part 4 - Requirement 4)
            const string auditSql = @"
                INSERT INTO dbo.AuditLogs (TableName, RecordId, Action, OldValues, NewValues, ChangedBy, ChangedAt, Reason)
                VALUES ('PaymentAllocations', @RecordId, 'REALLOCATE', @OldValues, @NewValues, @ChangedBy, SYSDATETIME(), @Reason);";

            var oldJson = System.Text.Json.JsonSerializer.Serialize(oldAllocations.Select(o => new { o.InvoiceId, o.AllocatedAmount }));
            var newJson = System.Text.Json.JsonSerializer.Serialize(newAllocList.Select(n => new { n.InvoiceId, n.AllocatedAmount }));

            await connection.ExecuteAsync(auditSql, new
            {
                RecordId = paymentId,
                OldValues = oldJson,
                NewValues = newJson,
                ChangedBy = string.IsNullOrWhiteSpace(performedBy) ? "accountant" : performedBy,
                Reason = string.IsNullOrWhiteSpace(reason) ? "Phân bổ lại phiếu thu công nợ" : reason
            }, transaction);

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}
