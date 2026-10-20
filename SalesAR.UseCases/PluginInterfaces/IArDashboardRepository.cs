namespace SalesAR.UseCases.PluginInterfaces;

using SalesAR.UseCases.Dashboard.Models;

public interface IArDashboardRepository
{
    Task<ArKpiSummaryDto> GetKpiSummaryAsync(ArDashboardFilterDto filter);
    Task<AgingBucketSummaryDto> GetAgingBucketsAsync(DateTime asOfDate, int? customerId = null);
    Task<IEnumerable<TopDebtorCustomerDto>> GetTopDebtorsAsync(DateTime asOfDate, int limit = 5);
    Task<IEnumerable<MonthlyCashFlowTrendDto>> GetMonthlyCashFlowTrendAsync(DateTime asOfDate, int months = 6);
    Task<IEnumerable<CashCollectionForecastDto>> GetCashCollectionForecastAsync(DateTime asOfDate, int? customerId = null);
    Task<PagedInvoiceDetailDto> GetPagedInvoicesAsync(ArDashboardFilterDto filter);
}
