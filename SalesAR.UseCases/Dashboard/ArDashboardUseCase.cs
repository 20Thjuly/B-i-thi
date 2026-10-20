namespace SalesAR.UseCases.Dashboard;

using System;
using System.Linq;
using System.Threading.Tasks;
using SalesAR.UseCases.Dashboard.Models;
using SalesAR.UseCases.PluginInterfaces;

public class ArDashboardUseCase : IArDashboardUseCase
{
    private readonly IArDashboardRepository _repository;

    public ArDashboardUseCase(IArDashboardRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<ArDashboardViewModel> ExecuteAsync(ArDashboardFilterDto filter)
    {
        if (filter == null)
            filter = new ArDashboardFilterDto();

        if (filter.ToDate < filter.FromDate)
        {
            // Tự động hoán vị nếu người dùng nhập ngược khoảng ngày
            (filter.FromDate, filter.ToDate) = (filter.ToDate, filter.FromDate);
        }

        var kpi = await _repository.GetKpiSummaryAsync(filter);
        var aging = await _repository.GetAgingBucketsAsync(filter.AsOfDate, filter.CustomerId);
        var topDebtors = (await _repository.GetTopDebtorsAsync(filter.AsOfDate, 5)).ToList();
        var trends = (await _repository.GetMonthlyCashFlowTrendAsync(filter.AsOfDate, 6)).ToList();
        var forecast = (await _repository.GetCashCollectionForecastAsync(filter.AsOfDate, filter.CustomerId)).ToList();
        var invoices = await _repository.GetPagedInvoicesAsync(filter);

        // 1. TÍNH TOÁN VÀ ĐỊNH NGHĨA CHỈ SỐ DSO (Days Sales Outstanding)
        int daysInPeriod = Math.Max(1, (filter.ToDate.Date - filter.FromDate.Date).Days + 1);

        if (kpi.TotalCreditSalesInPeriod > 0)
        {
            double dso = (double)(kpi.TotalArOutstanding / kpi.TotalCreditSalesInPeriod) * daysInPeriod;
            kpi.DsoDays = Math.Round(dso, 1);
            kpi.DsoExplanation = $"DSO = (Tổng phải thu {kpi.TotalArOutstanding:N0} đ / Doanh thu kỳ {kpi.TotalCreditSalesInPeriod:N0} đ) × {daysInPeriod} ngày = {kpi.DsoDays} ngày. Doanh nghiệp mất trung bình {kpi.DsoDays} ngày để chuyển doanh số bán hàng thành tiền mặt.";
        }
        else
        {
            kpi.DsoDays = null;
            kpi.DsoExplanation = "Không có doanh thu xuất bán trong kỳ phân tích [Từ ngày - Đến ngày], không thể tính chỉ số DSO.";
        }

        // 2. GẮN TỶ TRỌNG CÔNG NỢ CHO TOP DEBTORS
        decimal systemDebt = kpi.TotalArOutstanding;
        foreach (var debtor in topDebtors)
        {
            debtor.DebtSharePercentage = systemDebt > 0 
                ? Math.Round(debtor.TotalDebt / systemDebt * 100m, 1) 
                : 0m;
        }

        return new ArDashboardViewModel
        {
            Filter = filter,
            Kpi = kpi,
            AgingBuckets = aging,
            TopDebtors = topDebtors,
            MonthlyTrends = trends,
            Forecasts = forecast,
            Invoices = invoices
        };
    }
}
