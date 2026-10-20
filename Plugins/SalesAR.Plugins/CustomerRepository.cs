namespace SalesAR.Plugins.DataStore.SQL;

using System.Data;
using Dapper;
using SalesAR.CoreBusiness.Models;
using SalesAR.UseCases.Models;
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
        const string sql = "SELECT Id, Code, Name, Phone, Address, CreditLimit, PaymentTermDays, Email, IsInDispute, DisputeReason, SpecialAgreementNotes, IsActive FROM dbo.Customers ORDER BY Code ASC;";
        return await connection.QueryAsync<Customer>(sql);
    }

    public async Task<Customer?> GetByIdAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "SELECT Id, Code, Name, Phone, Address, CreditLimit, PaymentTermDays, Email, IsInDispute, DisputeReason, SpecialAgreementNotes, IsActive FROM dbo.Customers WHERE Id = @Id;";
        return await connection.QuerySingleOrDefaultAsync<Customer>(sql, new { Id = id });
    }

    public async Task<Customer?> GetByCodeAsync(string code)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "SELECT Id, Code, Name, Phone, Address, CreditLimit, PaymentTermDays, Email, IsInDispute, DisputeReason, SpecialAgreementNotes, IsActive FROM dbo.Customers WHERE Code = @Code;";
        return await connection.QuerySingleOrDefaultAsync<Customer>(sql, new { Code = code });
    }

    public async Task<int> AddAsync(Customer customer)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            INSERT INTO dbo.Customers (Code, Name, Phone, Address, CreditLimit, PaymentTermDays, Email, IsInDispute, DisputeReason, SpecialAgreementNotes, IsActive)
            OUTPUT INSERTED.Id
            VALUES (@Code, @Name, @Phone, @Address, @CreditLimit, @PaymentTermDays, @Email, @IsInDispute, @DisputeReason, @SpecialAgreementNotes, @IsActive);";
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
                Email = @Email,
                IsInDispute = @IsInDispute,
                DisputeReason = @DisputeReason,
                SpecialAgreementNotes = @SpecialAgreementNotes,
                IsActive = @IsActive
            WHERE Id = @Id;";
        await connection.ExecuteAsync(sql, customer);
    }

    public async Task DeleteAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "UPDATE dbo.Customers SET IsActive = 0 WHERE Id = @Id;";
        await connection.ExecuteAsync(sql, new { Id = id });
    }

    /// <summary>
    /// Công thức tính công nợ chuẩn: OutstandingAmount = TotalInvoiced - TotalPaid - TotalAdjusted
    /// Đảm bảo tính toán nhất quán từ dữ liệu thực tế và không bao giờ tính trùng một khoản điều chỉnh.
    /// </summary>
    public async Task<decimal> GetCurrentDebtAsync(int customerId)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT 
                (ISNULL(inv.TotalInvoiced, 0) - ISNULL(pmt.TotalPaid, 0) - ISNULL(adj.TotalAdjusted, 0)) AS CurrentDebt
            FROM dbo.Customers c
            LEFT JOIN (
                SELECT CustomerId, SUM(TotalAmount) AS TotalInvoiced
                FROM dbo.Invoices
                WHERE Status <> 'Cancelled'
                GROUP BY CustomerId
            ) inv ON c.Id = inv.CustomerId
            LEFT JOIN (
                SELECT CustomerId, SUM(Amount) AS TotalPaid
                FROM dbo.Payments
                GROUP BY CustomerId
            ) pmt ON c.Id = pmt.CustomerId
            LEFT JOIN (
                SELECT CustomerId, SUM(Amount) AS TotalAdjusted
                FROM dbo.DebtAdjustments
                GROUP BY CustomerId
            ) adj ON c.Id = adj.CustomerId
            WHERE c.Id = @CustomerId;";
        var debt = await connection.ExecuteScalarAsync<decimal?>(sql, new { CustomerId = customerId });
        return debt ?? 0;
    }

    public async Task<CustomerDebtDto?> GetCustomerDebtDetailsAsync(int customerId)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT 
                c.Id AS CustomerId,
                c.Code AS CustomerCode,
                c.Name AS CustomerName,
                c.CreditLimit AS CreditLimit,
                ISNULL(inv.TotalInvoiced, 0) AS TotalInvoiced,
                ISNULL(pmt.TotalPaid, 0) AS TotalPaid,
                ISNULL(adj.TotalAdjusted, 0) AS TotalAdjusted,
                (ISNULL(inv.TotalInvoiced, 0) - ISNULL(pmt.TotalPaid, 0) - ISNULL(adj.TotalAdjusted, 0)) AS CurrentDebt
            FROM dbo.Customers c
            LEFT JOIN (
                SELECT CustomerId, SUM(TotalAmount) AS TotalInvoiced
                FROM dbo.Invoices
                WHERE Status <> 'Cancelled'
                GROUP BY CustomerId
            ) inv ON c.Id = inv.CustomerId
            LEFT JOIN (
                SELECT CustomerId, SUM(Amount) AS TotalPaid
                FROM dbo.Payments
                GROUP BY CustomerId
            ) pmt ON c.Id = pmt.CustomerId
            LEFT JOIN (
                SELECT CustomerId, SUM(Amount) AS TotalAdjusted
                FROM dbo.DebtAdjustments
                GROUP BY CustomerId
            ) adj ON c.Id = adj.CustomerId
            WHERE c.Id = @CustomerId;";

        return await connection.QuerySingleOrDefaultAsync<CustomerDebtDto>(sql, new { CustomerId = customerId });
    }

    public async Task UpdateCreditLimitAsync(int customerId, decimal newCreditLimit, string changedBy, string reason)
    {
        if (newCreditLimit < 0)
            throw new InvalidOperationException("Hạn mức tín dụng không được nhỏ hơn 0.");

        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();

        try
        {
            var oldLimit = await connection.ExecuteScalarAsync<decimal?>(
                "SELECT CreditLimit FROM dbo.Customers WITH (UPDLOCK) WHERE Id = @Id;", 
                new { Id = customerId }, transaction);

            if (oldLimit == null)
                throw new InvalidOperationException($"Không tìm thấy khách hàng với ID: {customerId}.");

            await connection.ExecuteAsync(
                "UPDATE dbo.Customers SET CreditLimit = @CreditLimit WHERE Id = @Id;",
                new { Id = customerId, CreditLimit = newCreditLimit }, transaction);

            // Ghi nhận AuditLog cho thay đổi hạn mức tín dụng (Part 4 - Requirement 4)
            const string auditSql = @"
                INSERT INTO dbo.AuditLogs (TableName, RecordId, Action, OldValues, NewValues, ChangedBy, ChangedAt, Reason)
                VALUES ('Customers', @RecordId, 'UPDATE', @OldValues, @NewValues, @ChangedBy, SYSDATETIME(), @Reason);";

            var oldJson = $"{{\"CreditLimit\":{oldLimit.Value}}}";
            var newJson = $"{{\"CreditLimit\":{newCreditLimit}}}";

            await connection.ExecuteAsync(auditSql, new
            {
                RecordId = customerId,
                OldValues = oldJson,
                NewValues = newJson,
                ChangedBy = string.IsNullOrWhiteSpace(changedBy) ? "admin" : changedBy,
                Reason = string.IsNullOrWhiteSpace(reason) ? "Điều chỉnh hạn mức công nợ khách hàng" : reason
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
