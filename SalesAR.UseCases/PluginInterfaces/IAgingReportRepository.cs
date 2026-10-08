namespace SalesAR.UseCases.PluginInterfaces;

using SalesAR.UseCases.Models;

public interface IAgingReportRepository
{
    Task<AgingReportSummary> GetAgingReportAsync(DateTime asOfDate);
}
