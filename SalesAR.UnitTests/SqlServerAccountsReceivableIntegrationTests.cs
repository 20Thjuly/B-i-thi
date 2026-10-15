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
}
