namespace SalesAR.UseCases.Dashboard.Models;

public class ArDashboardViewModel
{
    public ArDashboardFilterDto Filter { get; set; } = new();
    public ArKpiSummaryDto Kpi { get; set; } = new();
    public AgingBucketSummaryDto AgingBuckets { get; set; } = new();
    public List<TopDebtorCustomerDto> TopDebtors { get; set; } = new();
    public List<MonthlyCashFlowTrendDto> MonthlyTrends { get; set; } = new();
    public List<CashCollectionForecastDto> Forecasts { get; set; } = new();
    public PagedInvoiceDetailDto Invoices { get; set; } = new();
}
