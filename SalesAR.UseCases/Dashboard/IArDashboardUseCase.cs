namespace SalesAR.UseCases.Dashboard;

using SalesAR.UseCases.Dashboard.Models;

public interface IArDashboardUseCase
{
    Task<ArDashboardViewModel> ExecuteAsync(ArDashboardFilterDto filter);
}
