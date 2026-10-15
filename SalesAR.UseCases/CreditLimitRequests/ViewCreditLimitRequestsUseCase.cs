namespace SalesAR.UseCases.CreditLimitRequests;

using SalesAR.CoreBusiness.Models;
using SalesAR.UseCases.PluginInterfaces;

public interface IViewCreditLimitRequestsUseCase
{
    Task<IEnumerable<CreditLimitRequest>> GetAllAsync();
    Task<IEnumerable<CreditLimitRequest>> GetPendingAsync();
    Task<IEnumerable<CreditLimitRequest>> GetByCustomerIdAsync(int customerId);
    Task<CreditLimitRequest?> GetByIdAsync(int id);
    Task<IEnumerable<CreditLimitApproval>> GetApprovalHistoryAsync(int requestId);
}

public class ViewCreditLimitRequestsUseCase : IViewCreditLimitRequestsUseCase
{
    private readonly ICreditLimitRequestRepository _repository;

    public ViewCreditLimitRequestsUseCase(ICreditLimitRequestRepository repository)
    {
        _repository = repository;
    }

    public Task<IEnumerable<CreditLimitRequest>> GetAllAsync() => _repository.GetAllAsync();
    public Task<IEnumerable<CreditLimitRequest>> GetPendingAsync() => _repository.GetPendingRequestsAsync();
    public Task<IEnumerable<CreditLimitRequest>> GetByCustomerIdAsync(int customerId) => _repository.GetByCustomerIdAsync(customerId);
    public Task<CreditLimitRequest?> GetByIdAsync(int id) => _repository.GetByIdAsync(id);
    public Task<IEnumerable<CreditLimitApproval>> GetApprovalHistoryAsync(int requestId) => _repository.GetApprovalHistoryAsync(requestId);
}
