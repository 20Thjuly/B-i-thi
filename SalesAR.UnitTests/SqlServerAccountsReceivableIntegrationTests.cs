namespace SalesAR.UnitTests;

using Xunit;
using Microsoft.Data.SqlClient;
using Dapper;

public class SqlServerAccountsReceivableIntegrationTests
{
    private const string ConnectionString = "Server=.;Database=SalesARDB;Trusted_Connection=True;TrustServerCertificate=True;";

    private bool IsSqlServerAvailable()
    {
        try
        {
            using var conn = new SqlConnection(ConnectionString);
            conn.Open();
            return true;
        }
        catch
        {
            return false;
        }
    }

    [Fact]
    public async Task SqlServer_EnforcesUniqueConstraint_OnPaymentNumber()
    {
        if (!IsSqlServerAvailable()) return;

        using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();

        string uniquePaymentNo = $"TEST-REC-{Guid.NewGuid():N}".Substring(0, 20);

        // Lấy 1 Customer hợp lệ
        var customerId = await connection.ExecuteScalarAsync<int>("SELECT TOP 1 Id FROM dbo.Customers;");

        // Chèn payment lần 1
        await connection.ExecuteAsync(@"
            INSERT INTO dbo.Payments (PaymentNumber, CustomerId, Amount, PaymentDate, Note)
            VALUES (@PaymentNumber, @CustomerId, 100000, SYSDATETIME(), 'Test Chống trùng 1');",
            new { PaymentNumber = uniquePaymentNo, CustomerId = customerId });

        try
        {
            // Chèn payment lần 2 với cùng PaymentNumber -> CSDL bắt buộc phải ném lỗi SqlException (Violation of UNIQUE KEY constraint)
            await Assert.ThrowsAsync<SqlException>(async () =>
            {
                await connection.ExecuteAsync(@"
                    INSERT INTO dbo.Payments (PaymentNumber, CustomerId, Amount, PaymentDate, Note)
                    VALUES (@PaymentNumber, @CustomerId, 200000, SYSDATETIME(), 'Test Chống trùng 2');",
                    new { PaymentNumber = uniquePaymentNo, CustomerId = customerId });
            });
        }
        finally
        {
            // Dọn dẹp dữ liệu test
            await connection.ExecuteAsync("DELETE FROM dbo.Payments WHERE PaymentNumber = @PaymentNumber;", new { PaymentNumber = uniquePaymentNo });
        }
    }

    [Fact]
    public async Task SqlServer_RollbacksTransaction_WhenAllocationFails()
    {
        if (!IsSqlServerAvailable()) return;

        using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();

        string testPaymentNo = $"TEST-ROLLBACK-{Guid.NewGuid():N}".Substring(0, 20);
        var customerId = await connection.ExecuteScalarAsync<int>("SELECT TOP 1 Id FROM dbo.Customers;", transaction: transaction);

        try
        {
            // 1. Chèn payment
            await connection.ExecuteAsync(@"
                INSERT INTO dbo.Payments (PaymentNumber, CustomerId, Amount, PaymentDate, Note)
                VALUES (@PaymentNumber, @CustomerId, 5000000, SYSDATETIME(), 'Test Rollback');",
                new { PaymentNumber = testPaymentNo, CustomerId = customerId }, transaction);

            // 2. Cố tình vi phạm Check constraint trên PaymentAllocations (AllocatedAmount = -100)
            await connection.ExecuteAsync(@"
                INSERT INTO dbo.PaymentAllocations (PaymentId, InvoiceId, AllocatedAmount)
                VALUES (999999, 999999, -100);", transaction: transaction);

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
        }

        // Sau khi rollback, Payment tuyệt đối không được tồn tại trong DB!
        int count = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM dbo.Payments WHERE PaymentNumber = @PaymentNumber;",
            new { PaymentNumber = testPaymentNo });

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task SqlServer_AuditLogs_RecordsActionsAndMaintainsHistory()
    {
        if (!IsSqlServerAvailable()) return;

        using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();

        // Kiểm tra xem bảng AuditLogs có hoạt động và có bản ghi
        var logCount = await connection.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM dbo.AuditLogs;");
        Assert.True(logCount > 0, "Bảng AuditLogs phải có bản ghi ghi nhận lịch sử.");

        // Kiểm tra cấu trúc audit log không chứa mật khẩu hoặc token
        var containsPassword = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM dbo.AuditLogs WHERE OldValues LIKE '%password%' OR NewValues LIKE '%password%';");
        Assert.Equal(0, containsPassword);
    }

    private class TestSqlConnectionFactory : SalesAR.Plugins.DataStore.SQL.ISqlConnectionFactory
    {
        private readonly string _cs;
        public TestSqlConnectionFactory(string cs) => _cs = cs;
        public SqlConnection CreateConnection() => new SqlConnection(_cs);
    }

    [Fact]
    public async Task SqlServer_Scenario06_ConcurrentApprovals_EnforcesLockingAndPreventsDoubleApproval()
    {
        if (!IsSqlServerAvailable()) return;

        using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();

        // 1. Tạo 1 yêu cầu duyệt tín dụng ở trạng thái Pending
        var customerId = await connection.ExecuteScalarAsync<int>("SELECT TOP 1 Id FROM dbo.Customers;");
        string testReqNo = $"TEST-REQ-CONCUR-{Guid.NewGuid():N}".Substring(0, 20);

        var requestId = await connection.ExecuteScalarAsync<int>(@"
            INSERT INTO dbo.CreditLimitRequests 
            (RequestNumber, CustomerId, TransactionAmount, CurrentDebtSnapshot, CreditLimitSnapshot, Reason, Status, RequestedBy, RequestedAt, ExpiresAt, IsUsed)
            OUTPUT INSERTED.Id
            VALUES (@RequestNumber, @CustomerId, 50000000, 20000000, 40000000, 'Test Race Condition', 'Pending', 'sales_tester', SYSDATETIME(), DATEADD(day, 3, SYSDATETIME()), 0);",
            new { RequestNumber = testReqNo, CustomerId = customerId });

        var factory = new TestSqlConnectionFactory(ConnectionString);
        var repo = new SalesAR.Plugins.DataStore.SQL.CreditLimitRequestRepository(factory);

        try
        {
            // 2. Chạy 2 tác vụ phê duyệt đồng thời (Concurrent Execution via Task.WhenAll)
            int successCount = 0;
            int failureCount = 0;

            var task1 = Task.Run(async () =>
            {
                try
                {
                    await repo.ApproveRequestAsync(requestId, "manager_1", "Phê duyệt luồng 1", 20000000, 40000000, TimeSpan.FromDays(3));
                    Interlocked.Increment(ref successCount);
                }
                catch (InvalidOperationException)
                {
                    Interlocked.Increment(ref failureCount);
                }
            });

            var task2 = Task.Run(async () =>
            {
                try
                {
                    await repo.ApproveRequestAsync(requestId, "manager_2", "Phê duyệt luồng 2", 20000000, 40000000, TimeSpan.FromDays(3));
                    Interlocked.Increment(ref successCount);
                }
                catch (InvalidOperationException)
                {
                    Interlocked.Increment(ref failureCount);
                }
            });

            await Task.WhenAll(task1, task2);

            // Assert: Đúng duy nhất 1 tác vụ thành công, tác vụ còn lại bị chặn bởi UPDLOCK/HOLDLOCK
            Assert.Equal(1, successCount);
            Assert.Equal(1, failureCount);

            // Kiểm tra trạng thái cuối cùng trong database
            var finalStatus = await connection.ExecuteScalarAsync<string>(
                "SELECT Status FROM dbo.CreditLimitRequests WHERE Id = @Id;", new { Id = requestId });
            Assert.Equal("Approved", finalStatus);
        }
        finally
        {
            // Dọn dẹp dữ liệu test
            await connection.ExecuteAsync("DELETE FROM dbo.CreditLimitApprovals WHERE RequestId = @Id;", new { Id = requestId });
            await connection.ExecuteAsync("DELETE FROM dbo.AuditLogs WHERE TableName = 'CreditLimitRequests' AND RecordId = @Id;", new { Id = requestId });
            await connection.ExecuteAsync("DELETE FROM dbo.CreditLimitRequests WHERE Id = @Id;", new { Id = requestId });
        }
    }

    [Fact]
    public async Task SqlServer_Scenario06_ConcurrentUseOfApproval_AtomicOptimisticLocking()
    {
        if (!IsSqlServerAvailable()) return;

        using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();

        var customerId = await connection.ExecuteScalarAsync<int>("SELECT TOP 1 Id FROM dbo.Customers;");
        var existingInvoiceId = await connection.ExecuteScalarAsync<int>("SELECT TOP 1 Id FROM dbo.Invoices;");
        string testReqNo = $"TEST-USE-CONCUR-{Guid.NewGuid():N}".Substring(0, 20);

        var requestId = await connection.ExecuteScalarAsync<int>(@"
            INSERT INTO dbo.CreditLimitRequests 
            (RequestNumber, CustomerId, TransactionAmount, CurrentDebtSnapshot, CreditLimitSnapshot, Reason, Status, RequestedBy, RequestedAt, ExpiresAt, IsUsed)
            OUTPUT INSERTED.Id
            VALUES (@RequestNumber, @CustomerId, 30000000, 10000000, 20000000, 'Test Single Use', 'Approved', 'sales_tester', SYSDATETIME(), DATEADD(day, 3, SYSDATETIME()), 0);",
            new { RequestNumber = testReqNo, CustomerId = customerId });

        var factory = new TestSqlConnectionFactory(ConnectionString);
        var repo = new SalesAR.Plugins.DataStore.SQL.CreditLimitRequestRepository(factory);

        try
        {
            int successCount = 0;
            int failureCount = 0;

            var task1 = Task.Run(async () =>
            {
                try
                {
                    await repo.MarkAsUsedAsync(requestId, existingInvoiceId);
                    Interlocked.Increment(ref successCount);
                }
                catch (InvalidOperationException)
                {
                    Interlocked.Increment(ref failureCount);
                }
            });

            var task2 = Task.Run(async () =>
            {
                try
                {
                    await repo.MarkAsUsedAsync(requestId, existingInvoiceId);
                    Interlocked.Increment(ref successCount);
                }
                catch (InvalidOperationException)
                {
                    Interlocked.Increment(ref failureCount);
                }
            });

            await Task.WhenAll(task1, task2);

            // Duy nhất 1 giao dịch được phép sử dụng phiếu duyệt
            Assert.Equal(1, successCount);
            Assert.Equal(1, failureCount);

            var isUsed = await connection.ExecuteScalarAsync<bool>(
                "SELECT IsUsed FROM dbo.CreditLimitRequests WHERE Id = @Id;", new { Id = requestId });
            Assert.True(isUsed);
        }
        finally
        {
            await connection.ExecuteAsync("DELETE FROM dbo.CreditLimitRequests WHERE Id = @Id;", new { Id = requestId });
        }
    }

    [Fact]
    public async Task SqlServer_Scenario15_DashboardReports_MatchRealDatabaseAggregates()
    {
        if (!IsSqlServerAvailable()) return;

        using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();

        // 1. Tính toán trực tiếp bằng câu lệnh SQL thô trên SalesARDB chuẩn hóa
        const string rawSql = @"
            WITH InvAgg AS (
                SELECT 
                    i.Id,
                    i.DueDate,
                    (i.TotalAmount - ISNULL(pa.Allocated, 0) - ISNULL(da.Adjusted, 0)) AS Rem
                FROM dbo.Invoices i
                LEFT JOIN (SELECT InvoiceId, SUM(AllocatedAmount) AS Allocated FROM dbo.PaymentAllocations GROUP BY InvoiceId) pa ON i.Id = pa.InvoiceId
                LEFT JOIN (SELECT InvoiceId, SUM(Amount) AS Adjusted FROM dbo.DebtAdjustments WHERE InvoiceId IS NOT NULL GROUP BY InvoiceId) da ON i.Id = da.InvoiceId
                WHERE i.Status <> 'Paid' AND i.Status <> 'Cancelled'
            )
            SELECT 
                ISNULL(SUM(CASE WHEN Rem > 0 THEN Rem ELSE 0 END), 0) AS TotalAr,
                ISNULL(SUM(CASE WHEN Rem > 0 AND DueDate < CAST(SYSDATETIME() AS DATE) THEN Rem ELSE 0 END), 0) AS TotalOverdue,
                COUNT(CASE WHEN Rem > 0 THEN 1 END) AS UnpaidCount
            FROM InvAgg;";

        var directAgg = await connection.QuerySingleAsync<(decimal TotalAr, decimal TotalOverdue, int UnpaidCount)>(rawSql);

        // 2. Lấy dữ liệu qua Repository của tầng Plugins
        var factory = new TestSqlConnectionFactory(ConnectionString);
        var dashboardRepo = new SalesAR.Plugins.Dashboard.ArDashboardRepository(factory);

        var kpiSummary = await dashboardRepo.GetKpiSummaryAsync(new SalesAR.UseCases.Dashboard.Models.ArDashboardFilterDto());

        // 3. Đối chiếu khớp 100% giữa Dashboard và dữ liệu thật trong SQL Server
        Assert.Equal(directAgg.TotalAr, kpiSummary.TotalArOutstanding);
        Assert.Equal(directAgg.TotalOverdue, kpiSummary.TotalOverdueAr);
        Assert.Equal(directAgg.UnpaidCount, kpiSummary.UnpaidInvoicesCount);
    }
}
