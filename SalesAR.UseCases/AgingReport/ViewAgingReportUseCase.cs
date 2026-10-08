namespace SalesAR.UseCases.AgingReport;

using SalesAR.UseCases.Models;
using SalesAR.UseCases.PluginInterfaces;

public interface IViewAgingReportUseCase
{
    Task<IEnumerable<AgingReportItem>> ExecuteAsync(DateTime asOfDate);
}

public class ViewAgingReportUseCase : IViewAgingReportUseCase
{
    private readonly IAgingReportRepository _agingReportRepository;

    public ViewAgingReportUseCase(IAgingReportRepository agingReportRepository)
    {
        _agingReportRepository = agingReportRepository;
    }

    public async Task<IEnumerable<AgingReportItem>> ExecuteAsync(DateTime asOfDate)
    {
        return await _agingReportRepository.GetAgingReportAsync(asOfDate);
    }
}
