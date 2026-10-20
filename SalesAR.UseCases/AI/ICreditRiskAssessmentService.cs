namespace SalesAR.UseCases.AI;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SalesAR.CoreBusiness.Models;
using SalesAR.UseCases.AI.Models;

public interface ICreditRiskAssessmentService
{
    Task<CreditRiskAssessmentResultDto> AssessCustomerRiskAsync(int customerId, string assessedBy = "AI Assistant", CancellationToken cancellationToken = default);
    Task<IEnumerable<CreditRiskAssessment>> GetAssessmentHistoryAsync(int customerId);
    Task<bool> ReviewAssessmentAsync(int assessmentId, string reviewedBy, string reviewNotes);
}
