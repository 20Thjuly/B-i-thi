namespace SalesAR.Plugins.DataStore.SQL;

using System.Data;
using Dapper;
using SalesAR.CoreBusiness.Models;
using SalesAR.UseCases.PluginInterfaces;

public class CreditLimitRequestRepository : ICreditLimitRequestRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public CreditLimitRequestRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<CreditLimitRequest>> GetAllAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT Id, RequestNumber, CustomerId, TransactionAmount, CurrentDebtSnapshot, CreditLimitSnapshot,
                   Reason, Status, RequestedBy, RequestedAt, ApprovedBy, ApprovedAt, DecisionReason,
                   ExpiresAt, IsUsed, UsedAt, UsedInvoiceId
            FROM dbo.CreditLimitRequests
            ORDER BY RequestedAt DESC;";
        return await connection.QueryAsync<CreditLimitRequest>(sql);
    }

    public async Task<IEnumerable<CreditLimitRequest>> GetByCustomerIdAsync(int customerId)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT Id, RequestNumber, CustomerId, TransactionAmount, CurrentDebtSnapshot, CreditLimitSnapshot,
                   Reason, Status, RequestedBy, RequestedAt, ApprovedBy, ApprovedAt, DecisionReason,
                   ExpiresAt, IsUsed, UsedAt, UsedInvoiceId
            FROM dbo.CreditLimitRequests
            WHERE CustomerId = @CustomerId
            ORDER BY RequestedAt DESC;";
        return await connection.QueryAsync<CreditLimitRequest>(sql, new { CustomerId = customerId });
    }

    public async Task<IEnumerable<CreditLimitRequest>> GetPendingRequestsAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT Id, RequestNumber, CustomerId, TransactionAmount, CurrentDebtSnapshot, CreditLimitSnapshot,
                   Reason, Status, RequestedBy, RequestedAt, ApprovedBy, ApprovedAt, DecisionReason,
                   ExpiresAt, IsUsed, UsedAt, UsedInvoiceId
            FROM dbo.CreditLimitRequests
            WHERE Status = 'Pending'
            ORDER BY RequestedAt ASC;";
        return await connection.QueryAsync<CreditLimitRequest>(sql);
    }

    public async Task<CreditLimitRequest?> GetByIdAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT Id, RequestNumber, CustomerId, TransactionAmount, CurrentDebtSnapshot, CreditLimitSnapshot,
                   Reason, Status, RequestedBy, RequestedAt, ApprovedBy, ApprovedAt, DecisionReason,
                   ExpiresAt, IsUsed, UsedAt, UsedInvoiceId
            FROM dbo.CreditLimitRequests
            WHERE Id = @Id;";
        return await connection.QuerySingleOrDefaultAsync<CreditLimitRequest>(sql, new { Id = id });
    }

    public async Task<CreditLimitRequest?> GetByRequestNumberAsync(string requestNumber)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT Id, RequestNumber, CustomerId, TransactionAmount, CurrentDebtSnapshot, CreditLimitSnapshot,
                   Reason, Status, RequestedBy, RequestedAt, ApprovedBy, ApprovedAt, DecisionReason,
                   ExpiresAt, IsUsed, UsedAt, UsedInvoiceId
            FROM dbo.CreditLimitRequests
            WHERE RequestNumber = @RequestNumber;";
        return await connection.QuerySingleOrDefaultAsync<CreditLimitRequest>(sql, new { RequestNumber = requestNumber });
    }

    public async Task<CreditLimitRequest?> GetValidApprovedRequestAsync(int customerId, decimal transactionAmount)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT TOP 1 Id, RequestNumber, CustomerId, TransactionAmount, CurrentDebtSnapshot, CreditLimitSnapshot,
                   Reason, Status, RequestedBy, RequestedAt, ApprovedBy, ApprovedAt, DecisionReason,
                   ExpiresAt, IsUsed, UsedAt, UsedInvoiceId
            FROM dbo.CreditLimitRequests WITH (UPDLOCK)
            WHERE CustomerId = @CustomerId 
              AND Status = 'Approved' 
              AND IsUsed = 0 
              AND (ExpiresAt IS NULL OR ExpiresAt > SYSDATETIME())
              AND TransactionAmount >= @TransactionAmount
            ORDER BY RequestedAt ASC;";
        return await connection.QuerySingleOrDefaultAsync<CreditLimitRequest>(sql, new { CustomerId = customerId, TransactionAmount = transactionAmount });
    }

    public async Task<int> CreateRequestAsync(CreditLimitRequest request)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

        try
        {
            const string insertRequestSql = @"
                INSERT INTO dbo.CreditLimitRequests 
                (RequestNumber, CustomerId, TransactionAmount, CurrentDebtSnapshot, CreditLimitSnapshot, Reason, Status, RequestedBy, RequestedAt, ExpiresAt, IsUsed)
                OUTPUT INSERTED.Id
                VALUES (@RequestNumber, @CustomerId, @TransactionAmount, @CurrentDebtSnapshot, @CreditLimitSnapshot, @Reason, 'Pending', @RequestedBy, @RequestedAt, @ExpiresAt, 0);";

            var requestId = await connection.ExecuteScalarAsync<int>(insertRequestSql, request, transaction);

            // Ghi nhật ký vào CreditLimitApprovals
            const string insertApprovalSql = @"
                INSERT INTO dbo.CreditLimitApprovals (RequestId, Action, Actor, ActionAt, Comment, CurrentDebtAtDecision, CreditLimitAtDecision)
                VALUES (@RequestId, 'SUBMIT', @Actor, SYSDATETIME(), @Comment, @CurrentDebtAtDecision, @CreditLimitAtDecision);";

            await connection.ExecuteAsync(insertApprovalSql, new
            {
                RequestId = requestId,
                Actor = request.RequestedBy,
                Comment = request.Reason,
                CurrentDebtAtDecision = request.CurrentDebtSnapshot,
                CreditLimitAtDecision = request.CreditLimitSnapshot
            }, transaction);

            // Ghi nhật ký vào AuditLogs
            const string auditSql = @"
                INSERT INTO dbo.AuditLogs (TableName, RecordId, Action, NewValues, ChangedBy, ChangedAt, Reason)
                VALUES ('CreditLimitRequests', @RecordId, 'INSERT', @NewValues, @ChangedBy, SYSDATETIME(), N'Tạo yêu cầu duyệt vượt hạn mức tín dụng');";

            var newValues = System.Text.Json.JsonSerializer.Serialize(new
            {
                request.RequestNumber,
                request.CustomerId,
                request.TransactionAmount,
                request.Reason
            });

            await connection.ExecuteAsync(auditSql, new
            {
                RecordId = requestId,
                NewValues = newValues,
                ChangedBy = request.RequestedBy
            }, transaction);

            transaction.Commit();
            return requestId;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task ApproveRequestAsync(int requestId, string approver, string reason, decimal liveDebt, decimal liveLimit, TimeSpan validDuration)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

        try
        {
            // Kiểm tra trạng thái hiện tại với UPDLOCK
            var request = await connection.QuerySingleOrDefaultAsync<CreditLimitRequest>(
                "SELECT * FROM dbo.CreditLimitRequests WITH (UPDLOCK, HOLDLOCK) WHERE Id = @Id;",
                new { Id = requestId }, transaction);

            if (request == null)
                throw new InvalidOperationException($"Không tìm thấy yêu cầu duyệt với ID: {requestId}.");

            if (request.Status != "Pending")
                throw new InvalidOperationException($"Yêu cầu này đã ở trạng thái '{request.Status}', không thể phê duyệt.");

            DateTime expiresAt = DateTime.UtcNow.Add(validDuration);

            // Cập nhật trạng thái sang Approved
            const string updateSql = @"
                UPDATE dbo.CreditLimitRequests
                SET Status = 'Approved',
                    ApprovedBy = @ApprovedBy,
                    ApprovedAt = SYSDATETIME(),
                    DecisionReason = @DecisionReason,
                    ExpiresAt = @ExpiresAt
                WHERE Id = @Id;";

            await connection.ExecuteAsync(updateSql, new
            {
                Id = requestId,
                ApprovedBy = approver,
                DecisionReason = reason,
                ExpiresAt = expiresAt
            }, transaction);

            // Ghi nhận quyết định vào CreditLimitApprovals (Lưu dư nợ và hạn mức thực tế lúc quyết định)
            const string approvalSql = @"
                INSERT INTO dbo.CreditLimitApprovals (RequestId, Action, Actor, ActionAt, Comment, CurrentDebtAtDecision, CreditLimitAtDecision)
                VALUES (@RequestId, 'APPROVE', @Actor, SYSDATETIME(), @Comment, @CurrentDebtAtDecision, @CreditLimitAtDecision);";

            await connection.ExecuteAsync(approvalSql, new
            {
                RequestId = requestId,
                Actor = approver,
                Comment = reason,
                CurrentDebtAtDecision = liveDebt,
                CreditLimitAtDecision = liveLimit
            }, transaction);

            // Ghi vết vào AuditLogs
            const string auditSql = @"
                INSERT INTO dbo.AuditLogs (TableName, RecordId, Action, OldValues, NewValues, ChangedBy, ChangedAt, Reason)
                VALUES ('CreditLimitRequests', @RecordId, 'STATUS_CHANGE', '{""Status"":""Pending""}', '{""Status"":""Approved""}', @ChangedBy, SYSDATETIME(), @Reason);";

            await connection.ExecuteAsync(auditSql, new
            {
                RecordId = requestId,
                ChangedBy = approver,
                Reason = reason
            }, transaction);

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task RejectRequestAsync(int requestId, string approver, string reason, decimal liveDebt, decimal liveLimit)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

        try
        {
            var request = await connection.QuerySingleOrDefaultAsync<CreditLimitRequest>(
                "SELECT * FROM dbo.CreditLimitRequests WITH (UPDLOCK, HOLDLOCK) WHERE Id = @Id;",
                new { Id = requestId }, transaction);

            if (request == null)
                throw new InvalidOperationException($"Không tìm thấy yêu cầu duyệt với ID: {requestId}.");

            if (request.Status != "Pending")
                throw new InvalidOperationException($"Yêu cầu này đã ở trạng thái '{request.Status}', không thể từ chối.");

            const string updateSql = @"
                UPDATE dbo.CreditLimitRequests
                SET Status = 'Rejected',
                    ApprovedBy = @ApprovedBy,
                    ApprovedAt = SYSDATETIME(),
                    DecisionReason = @DecisionReason
                WHERE Id = @Id;";

            await connection.ExecuteAsync(updateSql, new
            {
                Id = requestId,
                ApprovedBy = approver,
                DecisionReason = reason
            }, transaction);

            const string approvalSql = @"
                INSERT INTO dbo.CreditLimitApprovals (RequestId, Action, Actor, ActionAt, Comment, CurrentDebtAtDecision, CreditLimitAtDecision)
                VALUES (@RequestId, 'REJECT', @Actor, SYSDATETIME(), @Comment, @CurrentDebtAtDecision, @CreditLimitAtDecision);";

            await connection.ExecuteAsync(approvalSql, new
            {
                RequestId = requestId,
                Actor = approver,
                Comment = reason,
                CurrentDebtAtDecision = liveDebt,
                CreditLimitAtDecision = liveLimit
            }, transaction);

            const string auditSql = @"
                INSERT INTO dbo.AuditLogs (TableName, RecordId, Action, OldValues, NewValues, ChangedBy, ChangedAt, Reason)
                VALUES ('CreditLimitRequests', @RecordId, 'STATUS_CHANGE', '{""Status"":""Pending""}', '{""Status"":""Rejected""}', @ChangedBy, SYSDATETIME(), @Reason);";

            await connection.ExecuteAsync(auditSql, new
            {
                RecordId = requestId,
                ChangedBy = approver,
                Reason = reason
            }, transaction);

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task CancelRequestAsync(int requestId, string actor, string reason)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();

        try
        {
            var request = await connection.QuerySingleOrDefaultAsync<CreditLimitRequest>(
                "SELECT * FROM dbo.CreditLimitRequests WITH (UPDLOCK) WHERE Id = @Id;",
                new { Id = requestId }, transaction);

            if (request == null)
                throw new InvalidOperationException($"Không tìm thấy yêu cầu với ID: {requestId}.");

            if (request.Status != "Pending")
                throw new InvalidOperationException($"Không thể hủy yêu cầu đang ở trạng thái '{request.Status}'.");

            await connection.ExecuteAsync(
                "UPDATE dbo.CreditLimitRequests SET Status = 'Cancelled', DecisionReason = @Reason WHERE Id = @Id;",
                new { Id = requestId, Reason = reason }, transaction);

            const string approvalSql = @"
                INSERT INTO dbo.CreditLimitApprovals (RequestId, Action, Actor, ActionAt, Comment, CurrentDebtAtDecision, CreditLimitAtDecision)
                VALUES (@RequestId, 'CANCEL', @Actor, SYSDATETIME(), @Comment, @CurrentDebt, @CreditLimit);";

            await connection.ExecuteAsync(approvalSql, new
            {
                RequestId = requestId,
                Actor = actor,
                Comment = reason,
                CurrentDebt = request.CurrentDebtSnapshot,
                CreditLimit = request.CreditLimitSnapshot
            }, transaction);

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task MarkAsUsedAsync(int requestId, int invoiceId)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            UPDATE dbo.CreditLimitRequests
            SET IsUsed = 1,
                UsedAt = SYSDATETIME(),
                UsedInvoiceId = @InvoiceId
            WHERE Id = @Id AND IsUsed = 0;";
        
        int rows = await connection.ExecuteAsync(sql, new { Id = requestId, InvoiceId = invoiceId });
        if (rows == 0)
        {
            throw new InvalidOperationException($"Yêu cầu duyệt ID {requestId} đã được sử dụng hoặc không hợp lệ.");
        }
    }

    public async Task<IEnumerable<CreditLimitApproval>> GetApprovalHistoryAsync(int requestId)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT Id, RequestId, Action, Actor, ActionAt, Comment, CurrentDebtAtDecision, CreditLimitAtDecision
            FROM dbo.CreditLimitApprovals
            WHERE RequestId = @RequestId
            ORDER BY ActionAt ASC;";
        return await connection.QueryAsync<CreditLimitApproval>(sql, new { RequestId = requestId });
    }
}
