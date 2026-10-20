namespace SalesAR.Plugins.Reminders;

using System.Data;
using Dapper;
using SalesAR.CoreBusiness.Models;
using SalesAR.Plugins.DataStore.SQL;
using SalesAR.UseCases.PluginInterfaces;

public class ReminderRepository : IReminderRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public ReminderRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<ReminderRule>> GetActiveRulesAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT Id, Name, DaysOffset, TriggerType, Tone, TemplateSubject, TemplateBody, AutoSendApproved, IsActive
            FROM dbo.ReminderRules
            WHERE IsActive = 1
            ORDER BY DaysOffset ASC;";
        return await connection.QueryAsync<ReminderRule>(sql);
    }

    public async Task<ReminderRule?> GetRuleByIdAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT Id, Name, DaysOffset, TriggerType, Tone, TemplateSubject, TemplateBody, AutoSendApproved, IsActive
            FROM dbo.ReminderRules
            WHERE Id = @Id;";
        return await connection.QuerySingleOrDefaultAsync<ReminderRule>(sql, new { Id = id });
    }

    public async Task<ReminderLog?> GetByIdempotencyKeyAsync(string idempotencyKey)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT l.Id, l.IdempotencyKey, l.CustomerId, c.Name AS CustomerName, l.ReminderRuleId, r.Name AS RuleName,
                   l.RecipientEmail, l.Subject, l.Body, l.Status, l.RetryCount, l.MaxRetries, l.LastError,
                   l.SentAt, l.ConfirmedBy, l.ConfirmedAt, l.InvoiceIdsCsv, l.TotalDebtAmount, l.CreatedAt
            FROM dbo.ReminderLogs l
            INNER JOIN dbo.Customers c ON l.CustomerId = c.Id
            LEFT JOIN dbo.ReminderRules r ON l.ReminderRuleId = r.Id
            WHERE l.IdempotencyKey = @IdempotencyKey;";
        return await connection.QuerySingleOrDefaultAsync<ReminderLog>(sql, new { IdempotencyKey = idempotencyKey });
    }

    public async Task<ReminderLog?> GetLogByIdAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT l.Id, l.IdempotencyKey, l.CustomerId, c.Name AS CustomerName, l.ReminderRuleId, r.Name AS RuleName,
                   l.RecipientEmail, l.Subject, l.Body, l.Status, l.RetryCount, l.MaxRetries, l.LastError,
                   l.SentAt, l.ConfirmedBy, l.ConfirmedAt, l.InvoiceIdsCsv, l.TotalDebtAmount, l.CreatedAt
            FROM dbo.ReminderLogs l
            INNER JOIN dbo.Customers c ON l.CustomerId = c.Id
            LEFT JOIN dbo.ReminderRules r ON l.ReminderRuleId = r.Id
            WHERE l.Id = @Id;";
        return await connection.QuerySingleOrDefaultAsync<ReminderLog>(sql, new { Id = id });
    }

    public async Task<int> SaveReminderLogAsync(ReminderLog log)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            INSERT INTO dbo.ReminderLogs (
                IdempotencyKey, CustomerId, ReminderRuleId, RecipientEmail, Subject, Body,
                Status, RetryCount, MaxRetries, LastError, SentAt, ConfirmedBy, ConfirmedAt,
                InvoiceIdsCsv, TotalDebtAmount, CreatedAt
            )
            OUTPUT INSERTED.Id
            VALUES (
                @IdempotencyKey, @CustomerId, @ReminderRuleId, @RecipientEmail, @Subject, @Body,
                @Status, @RetryCount, @MaxRetries, @LastError, @SentAt, @ConfirmedBy, @ConfirmedAt,
                @InvoiceIdsCsv, @TotalDebtAmount, SYSDATETIME()
            );";
        return await connection.ExecuteScalarAsync<int>(sql, log);
    }

    public async Task UpdateReminderLogStatusAsync(int id, string status, int retryCount, string? lastError, DateTime? sentAt = null, string? confirmedBy = null)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            UPDATE dbo.ReminderLogs
            SET Status = @Status,
                RetryCount = @RetryCount,
                LastError = @LastError,
                SentAt = ISNULL(@SentAt, SentAt),
                ConfirmedBy = ISNULL(@ConfirmedBy, ConfirmedBy),
                ConfirmedAt = CASE WHEN @ConfirmedBy IS NOT NULL AND ConfirmedAt IS NULL THEN SYSDATETIME() ELSE ConfirmedAt END
            WHERE Id = @Id;";
        await connection.ExecuteAsync(sql, new
        {
            Id = id,
            Status = status,
            RetryCount = retryCount,
            LastError = lastError,
            SentAt = sentAt,
            ConfirmedBy = confirmedBy
        });
    }

    public async Task<IEnumerable<ReminderLog>> GetReminderLogsAsync(int? customerId = null, string? status = null, int limit = 100)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT TOP (@Limit)
                   l.Id, l.IdempotencyKey, l.CustomerId, c.Name AS CustomerName, l.ReminderRuleId, r.Name AS RuleName,
                   l.RecipientEmail, l.Subject, l.Body, l.Status, l.RetryCount, l.MaxRetries, l.LastError,
                   l.SentAt, l.ConfirmedBy, l.ConfirmedAt, l.InvoiceIdsCsv, l.TotalDebtAmount, l.CreatedAt
            FROM dbo.ReminderLogs l
            INNER JOIN dbo.Customers c ON l.CustomerId = c.Id
            LEFT JOIN dbo.ReminderRules r ON l.ReminderRuleId = r.Id
            WHERE (@CustomerId IS NULL OR l.CustomerId = @CustomerId)
              AND (@Status IS NULL OR l.Status = @Status)
            ORDER BY l.CreatedAt DESC;";
        return await connection.QueryAsync<ReminderLog>(sql, new { CustomerId = customerId, Status = status, Limit = limit });
    }

    public async Task<IEnumerable<ReminderLog>> GetPendingRetryLogsAsync(int maxRetries = 3)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT l.Id, l.IdempotencyKey, l.CustomerId, c.Name AS CustomerName, l.ReminderRuleId, r.Name AS RuleName,
                   l.RecipientEmail, l.Subject, l.Body, l.Status, l.RetryCount, l.MaxRetries, l.LastError,
                   l.SentAt, l.ConfirmedBy, l.ConfirmedAt, l.InvoiceIdsCsv, l.TotalDebtAmount, l.CreatedAt
            FROM dbo.ReminderLogs l
            INNER JOIN dbo.Customers c ON l.CustomerId = c.Id
            LEFT JOIN dbo.ReminderRules r ON l.ReminderRuleId = r.Id
            WHERE (l.Status = 'RetryPending' OR l.Status = 'Queued')
              AND l.RetryCount < @MaxRetries
            ORDER BY l.CreatedAt ASC;";
        return await connection.QueryAsync<ReminderLog>(sql, new { MaxRetries = maxRetries });
    }

    public async Task SetCustomerDisputeStatusAsync(int customerId, bool isInDispute, string? reason)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            UPDATE dbo.Customers
            SET IsInDispute = @IsInDispute,
                DisputeReason = @DisputeReason
            WHERE Id = @CustomerId;";
        await connection.ExecuteAsync(sql, new { CustomerId = customerId, IsInDispute = isInDispute, DisputeReason = reason });
    }

    public async Task SetInvoiceDisputeStatusAsync(int invoiceId, bool isInDispute, string? reason)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            UPDATE dbo.Invoices
            SET IsInDispute = @IsInDispute,
                DisputeReason = @DisputeReason
            WHERE Id = @InvoiceId;";
        await connection.ExecuteAsync(sql, new { InvoiceId = invoiceId, IsInDispute = isInDispute, DisputeReason = reason });
    }
}
