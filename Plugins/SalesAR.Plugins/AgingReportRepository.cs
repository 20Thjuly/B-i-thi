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
                    (i.TotalAmount - ISNULL(pa.Allocated, 0)) AS RemainingAmount,
                    DATEDIFF(day, i.DueDate, @AsOfDate) AS DaysOverdue
                FROM dbo.Invoices i
                LEFT JOIN (
                    SELECT InvoiceId, SUM(AllocatedAmount) AS Allocated
                    FROM dbo.PaymentAllocations
                    GROUP BY InvoiceId
                ) pa ON i.Id = pa.InvoiceId
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
