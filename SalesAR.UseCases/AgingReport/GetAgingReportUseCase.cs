namespace SalesAR.UseCases.AgingReport;

using SalesAR.UseCases.Models;
using SalesAR.UseCases.PluginInterfaces;

public interface IGetAgingReportUseCase
{
    Task<AgingReportSummary> ExecuteAsync(DateTime asOfDate);
}

public class GetAgingReportUseCase : IGetAgingReportUseCase
{
    private readonly IAgingReportRepository _agingReportRepository;

    public GetAgingReportUseCase(IAgingReportRepository agingReportRepository)
    {
        _agingReportRepository = agingReportRepository;
    }

    public async Task<AgingReportSummary> ExecuteAsync(DateTime asOfDate)
    {
        return await _agingReportRepository.GetAgingReportAsync(asOfDate);
    }
}
