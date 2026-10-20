namespace SalesAR.UseCases.AI;

using System.Threading;
using System.Threading.Tasks;
using SalesAR.UseCases.AI.Models;

public interface ICustomerArSummaryUseCase
{
    Task<CustomerArSummaryResultDto> SummarizeCustomerAsync(int customerId, CancellationToken cancellationToken = default);
}
