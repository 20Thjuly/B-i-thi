namespace SalesAR.UseCases.PluginInterfaces;

using SalesAR.CoreBusiness.Models;

public interface ICreditLimitRequestRepository
{
    Task<IEnumerable<CreditLimitRequest>> GetAllAsync();
    Task<IEnumerable<CreditLimitRequest>> GetByCustomerIdAsync(int customerId);
    Task<IEnumerable<CreditLimitRequest>> GetPendingRequestsAsync();
    Task<CreditLimitRequest?> GetByIdAsync(int id);
    Task<CreditLimitRequest?> GetByRequestNumberAsync(string requestNumber);
    Task<CreditLimitRequest?> GetValidApprovedRequestAsync(int customerId, decimal transactionAmount);
    Task<int> CreateRequestAsync(CreditLimitRequest request);
    Task ApproveRequestAsync(int requestId, string approver, string reason, decimal liveDebt, decimal liveLimit, TimeSpan validDuration);
    Task RejectRequestAsync(int requestId, string approver, string reason, decimal liveDebt, decimal liveLimit);
    Task CancelRequestAsync(int requestId, string actor, string reason);
    Task MarkAsUsedAsync(int requestId, int invoiceId);
    Task<IEnumerable<CreditLimitApproval>> GetApprovalHistoryAsync(int requestId);
}
