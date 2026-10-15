namespace SalesAR.Plugins.DataStore.SQL;

using System.Data;
using Dapper;
using SalesAR.UseCases.Models;
using SalesAR.UseCases.PluginInterfaces;

public class AgingReportRepository : IAgingReportRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public AgingReportRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <summary>
    /// Tính tuổi nợ từ dữ liệu thật dựa trên DueDate và ngày tham chiếu @AsOfDate.
    /// Công thức số dư hóa đơn chuẩn: RemainingAmount = TotalAmount - ValidAllocatedPayments - ValidAdjustments.
    /// Nhóm tuổi nợ thống nhất toàn hệ thống:
    /// - Bucket 0-30: Trong hạn nợ hoặc quá hạn đến 30 ngày (DaysOverdue <= 30)
    /// - Bucket 31-60: Quá hạn 31 đến 60 ngày
    /// - Bucket 61-90: Quá hạn 61 đến 90 ngày
    /// - Bucket >90: Quá hạn trên 90 ngày (Nợ khó đòi)
    /// </summary>
    public async Task<AgingReportSummary> GetAgingReportAsync(DateTime asOfDate)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT 
                c.Id AS CustomerId,
                c.Code AS CustomerCode,
                c.Name AS CustomerName,
                c.CreditLimit,
                ISNULL(SUM(inv.RemainingAmount), 0) AS TotalDebt,
                ISNULL(SUM(CASE WHEN inv.DaysOverdue <= 30 THEN inv.RemainingAmount ELSE 0 END), 0) AS Bucket0To30,
                ISNULL(SUM(CASE WHEN inv.DaysOverdue BETWEEN 31 AND 60 THEN inv.RemainingAmount ELSE 0 END), 0) AS Bucket31To60,
                ISNULL(SUM(CASE WHEN inv.DaysOverdue BETWEEN 61 AND 90 THEN inv.RemainingAmount ELSE 0 END), 0) AS Bucket61To90,
                ISNULL(SUM(CASE WHEN inv.DaysOverdue > 90 THEN inv.RemainingAmount ELSE 0 END), 0) AS BucketOver90
            FROM dbo.Customers c
            LEFT JOIN (
                SELECT 
                    i.Id,
                    i.CustomerId,
                    (i.TotalAmount - ISNULL(pa.Allocated, 0) - ISNULL(da.Adjusted, 0)) AS RemainingAmount,
                    DATEDIFF(day, i.DueDate, @AsOfDate) AS DaysOverdue
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
                WHERE i.Status <> 'Paid' AND i.Status <> 'Cancelled'
            ) inv ON c.Id = inv.CustomerId AND inv.RemainingAmount > 0
            GROUP BY c.Id, c.Code, c.Name, c.CreditLimit
            ORDER BY c.Code ASC;";

        var items = (await connection.QueryAsync<AgingReportItem>(sql, new { AsOfDate = asOfDate })).ToList();
        return new AgingReportSummary
        {
            Items = items
        };
    }
}
