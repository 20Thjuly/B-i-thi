namespace SalesAR.Plugins.Dashboard;

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using SalesAR.Plugins.DataStore.SQL;
using SalesAR.UseCases.Dashboard.Models;
using SalesAR.UseCases.PluginInterfaces;

public class ArDashboardRepository : IArDashboardRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public ArDashboardRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<ArKpiSummaryDto> GetKpiSummaryAsync(ArDashboardFilterDto filter)
    {
        using var connection = _connectionFactory.CreateConnection();

        // 1. Tổng công nợ, nợ quá hạn, số lượng hóa đơn, số khách hàng nợ
        const string arSql = @"
            WITH UnpaidInv AS (
                SELECT 
                    i.Id,
                    i.CustomerId,
                    i.InvoiceDate,
                    i.DueDate,
                    i.TotalAmount,
                    (i.TotalAmount - ISNULL(pa.Allocated, 0) - ISNULL(da.Adjusted, 0)) AS RemainingAmount
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
                  AND (@CustomerId IS NULL OR i.CustomerId = @CustomerId)
            )
            SELECT 
                ISNULL(SUM(CASE WHEN RemainingAmount > 0 THEN RemainingAmount ELSE 0 END), 0) AS TotalArOutstanding,
                ISNULL(SUM(CASE WHEN RemainingAmount > 0 AND DueDate < @AsOfDate THEN RemainingAmount ELSE 0 END), 0) AS TotalOverdueAr,
                COUNT(CASE WHEN RemainingAmount > 0 THEN 1 END) AS UnpaidInvoicesCount,
                COUNT(CASE WHEN RemainingAmount > 0 AND DueDate < @AsOfDate THEN 1 END) AS OverdueInvoicesCount,
                COUNT(DISTINCT CASE WHEN RemainingAmount > 0 THEN CustomerId END) AS IndebtedCustomersCount
            FROM UnpaidInv;";

        var kpi = await connection.QuerySingleAsync<ArKpiSummaryDto>(arSql, new
        {
            AsOfDate = filter.AsOfDate,
            CustomerId = filter.CustomerId
        });

        // 2. Tổng dòng tiền thực thu trong kỳ [FromDate, ToDate]
        const string cashSql = @"
            SELECT ISNULL(SUM(Amount), 0)
            FROM dbo.Payments
            WHERE PaymentDate >= @FromDate AND PaymentDate <= @ToDate
              AND (@CustomerId IS NULL OR CustomerId = @CustomerId);";

        kpi.TotalCashCollectedInPeriod = await connection.ExecuteScalarAsync<decimal>(cashSql, new
        {
            FromDate = filter.FromDate.Date,
            ToDate = filter.ToDate.Date.AddDays(1).AddTicks(-1),
            CustomerId = filter.CustomerId
        });

        // 3. Tổng doanh thu xuất bán trong kỳ [FromDate, ToDate]
        const string salesSql = @"
            SELECT ISNULL(SUM(TotalAmount), 0)
            FROM dbo.Invoices
            WHERE InvoiceDate >= @FromDate AND InvoiceDate <= @ToDate
              AND Status <> 'Cancelled'
              AND (@CustomerId IS NULL OR CustomerId = @CustomerId);";

        kpi.TotalCreditSalesInPeriod = await connection.ExecuteScalarAsync<decimal>(salesSql, new
        {
            FromDate = filter.FromDate.Date,
            ToDate = filter.ToDate.Date.AddDays(1).AddTicks(-1),
            CustomerId = filter.CustomerId
        });

        // 4. Tổng số khách hàng
        const string custCountSql = "SELECT COUNT(*) FROM dbo.Customers WHERE IsActive = 1;";
        kpi.TotalCustomersCount = await connection.ExecuteScalarAsync<int>(custCountSql);

        kpi.UnpaidInvoicesAmount = kpi.TotalArOutstanding;

        return kpi;
    }

    public async Task<AgingBucketSummaryDto> GetAgingBucketsAsync(DateTime asOfDate, int? customerId = null)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            WITH InvWithDays AS (
                SELECT 
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
                  AND (@CustomerId IS NULL OR i.CustomerId = @CustomerId)
            )
            SELECT 
                ISNULL(SUM(CASE WHEN DaysOverdue <= 0 THEN RemainingAmount ELSE 0 END), 0) AS CurrentAmount,
                COUNT(CASE WHEN DaysOverdue <= 0 AND RemainingAmount > 0 THEN 1 END) AS CurrentCount,

                ISNULL(SUM(CASE WHEN DaysOverdue BETWEEN 1 AND 30 THEN RemainingAmount ELSE 0 END), 0) AS Overdue1To30Amount,
                COUNT(CASE WHEN DaysOverdue BETWEEN 1 AND 30 AND RemainingAmount > 0 THEN 1 END) AS Overdue1To30Count,

                ISNULL(SUM(CASE WHEN DaysOverdue BETWEEN 31 AND 60 THEN RemainingAmount ELSE 0 END), 0) AS Overdue31To60Amount,
                COUNT(CASE WHEN DaysOverdue BETWEEN 31 AND 60 AND RemainingAmount > 0 THEN 1 END) AS Overdue31To60Count,

                ISNULL(SUM(CASE WHEN DaysOverdue BETWEEN 61 AND 90 THEN RemainingAmount ELSE 0 END), 0) AS Overdue61To90Amount,
                COUNT(CASE WHEN DaysOverdue BETWEEN 61 AND 90 AND RemainingAmount > 0 THEN 1 END) AS Overdue61To90Count,

                ISNULL(SUM(CASE WHEN DaysOverdue > 90 THEN RemainingAmount ELSE 0 END), 0) AS OverdueOver90Amount,
                COUNT(CASE WHEN DaysOverdue > 90 AND RemainingAmount > 0 THEN 1 END) AS OverdueOver90Count
            FROM InvWithDays
            WHERE RemainingAmount > 0;";

        return await connection.QuerySingleAsync<AgingBucketSummaryDto>(sql, new
        {
            AsOfDate = asOfDate,
            CustomerId = customerId
        });
    }

    public async Task<IEnumerable<TopDebtorCustomerDto>> GetTopDebtorsAsync(DateTime asOfDate, int limit = 5)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT TOP (@Limit)
                c.Id AS CustomerId,
                c.Code AS CustomerCode,
                c.Name AS CustomerName,
                c.CreditLimit,
                ISNULL(SUM(inv.RemainingAmount), 0) AS TotalDebt,
                ISNULL(SUM(CASE WHEN inv.DueDate < @AsOfDate THEN inv.RemainingAmount ELSE 0 END), 0) AS OverdueDebt,
                COUNT(CASE WHEN inv.RemainingAmount > 0 THEN 1 END) AS UnpaidInvoiceCount,
                COUNT(CASE WHEN inv.RemainingAmount > 0 AND inv.DueDate < @AsOfDate THEN 1 END) AS OverdueInvoiceCount
            FROM dbo.Customers c
            INNER JOIN (
                SELECT 
                    i.CustomerId,
                    i.DueDate,
                    (i.TotalAmount - ISNULL(pa.Allocated, 0) - ISNULL(da.Adjusted, 0)) AS RemainingAmount
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
            ORDER BY TotalDebt DESC;";

        return await connection.QueryAsync<TopDebtorCustomerDto>(sql, new
        {
            AsOfDate = asOfDate,
            Limit = limit
        });
    }

    public async Task<IEnumerable<MonthlyCashFlowTrendDto>> GetMonthlyCashFlowTrendAsync(DateTime asOfDate, int months = 6)
    {
        using var connection = _connectionFactory.CreateConnection();
        DateTime startDate = asOfDate.AddMonths(-(months - 1));
        startDate = new DateTime(startDate.Year, startDate.Month, 1);

        const string sql = @"
            WITH MonthList AS (
                SELECT 0 AS Offset
                UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4 UNION ALL SELECT 5
            ),
            Months AS (
                SELECT 
                    FORMAT(DATEADD(month, Offset, @StartDate), 'yyyy-MM') AS YearMonth,
                    N'Tháng ' + FORMAT(DATEADD(month, Offset, @StartDate), 'MM/yyyy') AS Label
                FROM MonthList
            ),
            MonthlySales AS (
                SELECT 
                    FORMAT(InvoiceDate, 'yyyy-MM') AS YearMonth,
                    SUM(TotalAmount) AS InvoicedAmount
                FROM dbo.Invoices
                WHERE Status <> 'Cancelled' AND InvoiceDate >= @StartDate
                GROUP BY FORMAT(InvoiceDate, 'yyyy-MM')
            ),
            MonthlyCash AS (
                SELECT 
                    FORMAT(PaymentDate, 'yyyy-MM') AS YearMonth,
                    SUM(Amount) AS CashCollectedAmount
                FROM dbo.Payments
                WHERE PaymentDate >= @StartDate
                GROUP BY FORMAT(PaymentDate, 'yyyy-MM')
            )
            SELECT 
                m.YearMonth,
                m.Label,
                ISNULL(s.InvoicedAmount, 0) AS InvoicedAmount,
                ISNULL(c.CashCollectedAmount, 0) AS CashCollectedAmount
            FROM Months m
            LEFT JOIN MonthlySales s ON m.YearMonth = s.YearMonth
            LEFT JOIN MonthlyCash c ON m.YearMonth = c.YearMonth
            ORDER BY m.YearMonth ASC;";

        return await connection.QueryAsync<MonthlyCashFlowTrendDto>(sql, new { StartDate = startDate });
    }

    public async Task<IEnumerable<CashCollectionForecastDto>> GetCashCollectionForecastAsync(DateTime asOfDate, int? customerId = null)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            WITH UnpaidInv AS (
                SELECT 
                    i.Id,
                    i.CustomerId,
                    i.DueDate,
                    (i.TotalAmount - ISNULL(pa.Allocated, 0) - ISNULL(da.Adjusted, 0)) AS RemainingAmount,
                    DATEDIFF(day, @AsOfDate, i.DueDate) AS DaysUntilDue,
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
                  AND (@CustomerId IS NULL OR i.CustomerId = @CustomerId)
            )
            SELECT 
                'Next7Days' AS PeriodKey,
                N'7 ngày tới' AS PeriodLabel,
                FORMAT(@AsOfDate, 'dd/MM') + ' - ' + FORMAT(DATEADD(day, 7, @AsOfDate), 'dd/MM/yyyy') AS DateRangeText,
                COUNT(CASE WHEN DaysUntilDue BETWEEN 0 AND 7 AND RemainingAmount > 0 THEN 1 END) AS InvoiceCount,
                ISNULL(SUM(CASE WHEN DaysUntilDue BETWEEN 0 AND 7 AND RemainingAmount > 0 THEN RemainingAmount ELSE 0 END), 0) AS NominalDueAmount,
                0.90 AS CollectionProbability,
                N'Cao' AS ConfidenceLevel,
                N'Hóa đơn sắp đến hạn trong tuần tới; cam kết thanh toán hợp đồng chuẩn.' AS BasisOfEstimation
            FROM UnpaidInv
            UNION ALL
            SELECT 
                'Next15Days',
                N'8 - 15 ngày tới',
                FORMAT(DATEADD(day, 8, @AsOfDate), 'dd/MM') + ' - ' + FORMAT(DATEADD(day, 15, @AsOfDate), 'dd/MM/yyyy'),
                COUNT(CASE WHEN DaysUntilDue BETWEEN 8 AND 15 AND RemainingAmount > 0 THEN 1 END),
                ISNULL(SUM(CASE WHEN DaysUntilDue BETWEEN 8 AND 15 AND RemainingAmount > 0 THEN RemainingAmount ELSE 0 END), 0),
                0.85,
                N'Khá',
                N'Hóa đơn đáo hạn nửa đầu tháng kế tiếp, trong kỳ đối soát luân chuyển chứng từ.'
            FROM UnpaidInv
            UNION ALL
            SELECT 
                'Next30Days',
                N'16 - 30 ngày tới',
                FORMAT(DATEADD(day, 16, @AsOfDate), 'dd/MM') + ' - ' + FORMAT(DATEADD(day, 30, @AsOfDate), 'dd/MM/yyyy'),
                COUNT(CASE WHEN DaysUntilDue BETWEEN 16 AND 30 AND RemainingAmount > 0 THEN 1 END),
                ISNULL(SUM(CASE WHEN DaysUntilDue BETWEEN 16 AND 30 AND RemainingAmount > 0 THEN RemainingAmount ELSE 0 END), 0),
                0.80,
                N'Trung bình',
                N'Kỳ hạn thanh toán cuối tháng; phụ thuộc tiến độ thu tiền từ khách hàng của đối tác.'
            FROM UnpaidInv
            UNION ALL
            SELECT 
                'OverdueRecoverable',
                N'Thu hồi nợ quá hạn (1 - 30 ngày)',
                N'Đang quá hạn đợt 1',
                COUNT(CASE WHEN DaysOverdue BETWEEN 1 AND 30 AND RemainingAmount > 0 THEN 1 END),
                ISNULL(SUM(CASE WHEN DaysOverdue BETWEEN 1 AND 30 AND RemainingAmount > 0 THEN RemainingAmount ELSE 0 END), 0),
                0.70,
                N'Trung bình',
                N'Nợ quá hạn nhẹ; dự báo thu hồi được phần lớn qua quy trình nhắc nợ tự động.'
            FROM UnpaidInv
            UNION ALL
            SELECT 
                'BadDebtAllowance',
                N'Nợ khó đòi (> 30 ngày)',
                N'Quá hạn nghiêm trọng',
                COUNT(CASE WHEN DaysOverdue > 30 AND RemainingAmount > 0 THEN 1 END),
                ISNULL(SUM(CASE WHEN DaysOverdue > 30 AND RemainingAmount > 0 THEN RemainingAmount ELSE 0 END), 0),
                0.20,
                N'Thận trọng',
                N'Nợ quá hạn kéo dài; cần áp dụng biện pháp chế tài hoặc chuyển bộ phận pháp lý.'
            FROM UnpaidInv;";

        return await connection.QueryAsync<CashCollectionForecastDto>(sql, new
        {
            AsOfDate = asOfDate,
            CustomerId = customerId
        });
    }

    public async Task<PagedInvoiceDetailDto> GetPagedInvoicesAsync(ArDashboardFilterDto filter)
    {
        using var connection = _connectionFactory.CreateConnection();

        int offset = Math.Max(0, (filter.PageIndex - 1) * filter.PageSize);

        // Xây dựng điều kiện lọc linh hoạt
        string whereClause = @"
            WHERE inv.RemainingAmount > 0
              AND (@CustomerId IS NULL OR i.CustomerId = @CustomerId)";

        // Lọc trạng thái hóa đơn
        if (!string.IsNullOrWhiteSpace(filter.InvoiceStatus) && filter.InvoiceStatus != "All")
        {
            if (filter.InvoiceStatus == "Unpaid")
                whereClause += " AND inv.PaidAmount = 0";
            else if (filter.InvoiceStatus == "PartiallyPaid")
                whereClause += " AND inv.PaidAmount > 0 AND inv.RemainingAmount > 0";
            else if (filter.InvoiceStatus == "Overdue")
                whereClause += " AND i.DueDate < @AsOfDate";
            else if (filter.InvoiceStatus == "InDispute")
                whereClause += " AND i.IsInDispute = 1";
        }

        // Lọc nhóm tuổi nợ
        if (!string.IsNullOrWhiteSpace(filter.AgingBucket) && filter.AgingBucket != "All")
        {
            if (filter.AgingBucket == "Current")
                whereClause += " AND DATEDIFF(day, i.DueDate, @AsOfDate) <= 0";
            else if (filter.AgingBucket == "1To30")
                whereClause += " AND DATEDIFF(day, i.DueDate, @AsOfDate) BETWEEN 1 AND 30";
            else if (filter.AgingBucket == "31To60")
                whereClause += " AND DATEDIFF(day, i.DueDate, @AsOfDate) BETWEEN 31 AND 60";
            else if (filter.AgingBucket == "61To90")
                whereClause += " AND DATEDIFF(day, i.DueDate, @AsOfDate) BETWEEN 61 AND 90";
            else if (filter.AgingBucket == "Over90")
                whereClause += " AND DATEDIFF(day, i.DueDate, @AsOfDate) > 90";
        }

        string baseSql = $@"
            FROM dbo.Invoices i
            INNER JOIN dbo.Customers c ON i.CustomerId = c.Id
            CROSS APPLY (
                SELECT 
                    ISNULL((SELECT SUM(AllocatedAmount) FROM dbo.PaymentAllocations WHERE InvoiceId = i.Id), 0) AS PaidAmount,
                    ISNULL((SELECT SUM(Amount) FROM dbo.DebtAdjustments WHERE InvoiceId = i.Id), 0) AS AdjustedAmount
            ) alloc
            CROSS APPLY (
                SELECT (i.TotalAmount - alloc.PaidAmount - alloc.AdjustedAmount) AS RemainingAmount
            ) inv
            {whereClause}";

        string countSql = $"SELECT COUNT(*) {baseSql};";

        string selectSql = $@"
            SELECT 
                i.Id,
                i.InvoiceNumber,
                i.CustomerId,
                c.Code AS CustomerCode,
                c.Name AS CustomerName,
                i.InvoiceDate,
                i.DueDate,
                i.TotalAmount,
                alloc.PaidAmount,
                alloc.AdjustedAmount,
                DATEDIFF(day, i.DueDate, @AsOfDate) AS DaysOverdue,
                CASE 
                    WHEN DATEDIFF(day, i.DueDate, @AsOfDate) <= 0 THEN N'Trong hạn'
                    WHEN DATEDIFF(day, i.DueDate, @AsOfDate) <= 30 THEN N'Quá hạn 1 - 30 ngày'
                    WHEN DATEDIFF(day, i.DueDate, @AsOfDate) <= 60 THEN N'Quá hạn 31 - 60 ngày'
                    WHEN DATEDIFF(day, i.DueDate, @AsOfDate) <= 90 THEN N'Quá hạn 61 - 90 ngày'
                    ELSE N'Quá hạn > 90 ngày'
                END AS AgingCategory,
                i.Status,
                ISNULL(i.IsInDispute, 0) AS IsInDispute,
                i.DisputeReason
            {baseSql}
            ORDER BY i.DueDate ASC, i.Id ASC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, new
        {
            AsOfDate = filter.AsOfDate,
            CustomerId = filter.CustomerId
        });

        var items = (await connection.QueryAsync<DashboardInvoiceItemDto>(selectSql, new
        {
            AsOfDate = filter.AsOfDate,
            CustomerId = filter.CustomerId,
            Offset = offset,
            PageSize = filter.PageSize
        })).ToList();

        return new PagedInvoiceDetailDto
        {
            TotalCount = totalCount,
            PageIndex = filter.PageIndex,
            PageSize = filter.PageSize,
            Items = items
        };
    }
}
