namespace SalesAR.UseCases.CreditLimitRequests;

using SalesAR.CoreBusiness.Models;
using SalesAR.UseCases.PluginInterfaces;

public interface IRejectCreditLimitRequestUseCase
{
    Task ExecuteAsync(int requestId, string rejecterUsername, string rejecterRole, string rejectionReason);
}

public class RejectCreditLimitRequestUseCase : IRejectCreditLimitRequestUseCase
{
    private readonly ICreditLimitRequestRepository _requestRepository;
    private readonly ICustomerRepository _customerRepository;

    public RejectCreditLimitRequestUseCase(
        ICreditLimitRequestRepository requestRepository,
        ICustomerRepository customerRepository)
    {
        _requestRepository = requestRepository;
        _customerRepository = customerRepository;
    }

    public async Task ExecuteAsync(int requestId, string rejecterUsername, string rejecterRole, string rejectionReason)
    {
        // 1. Phân quyền: Chỉ CREDIT_MANAGER hoặc ADMIN mới có quyền từ chối
        if (string.IsNullOrWhiteSpace(rejecterRole) || (!rejecterRole.Equals("CREDIT_MANAGER", StringComparison.OrdinalIgnoreCase) && !rejecterRole.Equals("ADMIN", StringComparison.OrdinalIgnoreCase)))
        {
            throw new UnauthorizedAccessException("Chỉ có Quản lý Tín dụng (CREDIT_MANAGER) hoặc Quản trị viên (ADMIN) mới có quyền từ chối yêu cầu.");
        }

        if (string.IsNullOrWhiteSpace(rejectionReason))
        {
            throw new InvalidOperationException("Bắt buộc phải nhập lý do từ chối yêu cầu.");
        }

        var request = await _requestRepository.GetByIdAsync(requestId);
        if (request == null)
            throw new InvalidOperationException($"Không tìm thấy yêu cầu duyệt ID: {requestId}.");

        // Segregation of duties
        if (request.RequestedBy.Equals(rejecterUsername, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Vi phạm chính sách tách biệt nhiệm vụ: Người tạo yêu cầu không được tự xử lý yêu cầu của mình.");
        }

        if (request.Status != "Pending")
        {
            throw new InvalidOperationException($"Yêu cầu này đã ở trạng thái '{request.Status}', không thể từ chối.");
        }

        var liveCustomer = await _customerRepository.GetByIdAsync(request.CustomerId);
        decimal liveDebt = await _customerRepository.GetCurrentDebtAsync(request.CustomerId);
        decimal liveLimit = liveCustomer?.CreditLimit ?? 0;

        await _requestRepository.RejectRequestAsync(requestId, rejecterUsername, rejectionReason, liveDebt, liveLimit);
    }
}
