namespace SalesAR.UseCases.AI;

using System.Threading;
using System.Threading.Tasks;
using SalesAR.UseCases.AI.Models;

public interface IReminderDraftGenerator
{
    Task<AiReminderDraftResultDto> GenerateDraftAsync(ReminderDraftRequestDto request, CancellationToken cancellationToken = default);
}
