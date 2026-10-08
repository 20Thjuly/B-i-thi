namespace SalesAR.UseCases.PluginInterfaces;

using SalesAR.UseCases.Models;

public interface IAgingReportRepository
{
    Task<IEnumerable<AgingReportItem>> GetAgingReportAsync(DateTime asOfDate);
}
