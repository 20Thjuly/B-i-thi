namespace SalesAR.Plugins.DataStore.SQL;

using Dapper;
using SalesAR.CoreBusiness.Models;
using SalesAR.UseCases.PluginInterfaces;

public class DebtAdjustmentRepository : IDebtAdjustmentRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public DebtAdjustmentRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<DebtAdjustment>> GetAllAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT Id, AdjustmentNumber, CustomerId, InvoiceId, AdjustmentDate, AdjustmentType, Amount, Reason, CreatedBy, CreatedAt
            FROM dbo.DebtAdjustments
            ORDER BY AdjustmentDate DESC;";
        return await connection.QueryAsync<DebtAdjustment>(sql);
    }

    public async Task<IEnumerable<DebtAdjustment>> GetByCustomerIdAsync(int customerId)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT Id, AdjustmentNumber, CustomerId, InvoiceId, AdjustmentDate, AdjustmentType, Amount, Reason, CreatedBy, CreatedAt
            FROM dbo.DebtAdjustments
            WHERE CustomerId = @CustomerId
            ORDER BY AdjustmentDate DESC;";
        return await connection.QueryAsync<DebtAdjustment>(sql, new { CustomerId = customerId });
    }

    public async Task<DebtAdjustment?> GetByIdAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT Id, AdjustmentNumber, CustomerId, InvoiceId, AdjustmentDate, AdjustmentType, Amount, Reason, CreatedBy, CreatedAt
            FROM dbo.DebtAdjustments
            WHERE Id = @Id;";
        return await connection.QuerySingleOrDefaultAsync<DebtAdjustment>(sql, new { Id = id });
    }

    public async Task<int> AddAsync(DebtAdjustment adjustment)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();

        try
        {
            const string sql = @"
                INSERT INTO dbo.DebtAdjustments (AdjustmentNumber, CustomerId, InvoiceId, AdjustmentDate, AdjustmentType, Amount, Reason, CreatedBy, CreatedAt)
                OUTPUT INSERTED.Id
                VALUES (@AdjustmentNumber, @CustomerId, @InvoiceId, @AdjustmentDate, @AdjustmentType, @Amount, @Reason, @CreatedBy, @CreatedAt);";

            var id = await connection.ExecuteScalarAsync<int>(sql, adjustment, transaction);

            // Tự động ghi nhật ký kiểm toán cho thao tác điều chỉnh công nợ
            const string auditSql = @"
                INSERT INTO dbo.AuditLogs (TableName, RecordId, Action, NewValues, ChangedBy, ChangedAt, Reason)
                VALUES ('DebtAdjustments', @RecordId, 'INSERT', @NewValues, @ChangedBy, SYSDATETIME(), @Reason);";

            var newValuesJson = $"{{\"Amount\":{adjustment.Amount},\"Type\":\"{adjustment.AdjustmentType}\",\"CustomerId\":{adjustment.CustomerId}}}";
            await connection.ExecuteAsync(auditSql, new
            {
                RecordId = id,
                NewValues = newValuesJson,
                ChangedBy = adjustment.CreatedBy,
                Reason = adjustment.Reason
            }, transaction);

            transaction.Commit();
            return id;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}
