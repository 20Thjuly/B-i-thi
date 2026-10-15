namespace SalesAR.UseCases.CreditLimitRequests;

using SalesAR.CoreBusiness.Models;
using SalesAR.UseCases.PluginInterfaces;

public interface IApproveCreditLimitRequestUseCase
{
    Task ExecuteAsync(int requestId, string approverUsername, string approverRole, string approvalReason, TimeSpan? validDuration = null);
}

public class ApproveCreditLimitRequestUseCase : IApproveCreditLimitRequestUseCase
{
    private readonly ICreditLimitRequestRepository _requestRepository;
    private readonly ICustomerRepository _customerRepository;

    public ApproveCreditLimitRequestUseCase(
        ICreditLimitRequestRepository requestRepository,
        ICustomerRepository customerRepository)
    {
        _requestRepository = requestRepository;
        _customerRepository = customerRepository;
    }

    public async Task ExecuteAsync(int requestId, string approverUsername, string approverRole, string approvalReason, TimeSpan? validDuration = null)
    {
        // 1. Phân quyền: Chỉ CREDIT_MANAGER hoặc ADMIN mới có quyền duyệt
        if (string.IsNullOrWhiteSpace(approverRole) || (!approverRole.Equals("CREDIT_MANAGER", StringComparison.OrdinalIgnoreCase) && !approverRole.Equals("ADMIN", StringComparison.OrdinalIgnoreCase)))
        {
            throw new UnauthorizedAccessException("Chỉ có Quản lý Tín dụng (CREDIT_MANAGER) hoặc Quản trị viên (ADMIN) mới có quyền phê duyệt.");
        }

        if (string.IsNullOrWhiteSpace(approvalReason))
        {
            throw new InvalidOperationException("Bắt buộc phải nhập ý kiến / lý do phê duyệt.");
        }

        // 2. Kiểm tra tồn tại yêu cầu
        var request = await _requestRepository.GetByIdAsync(requestId);
        if (request == null)
            throw new InvalidOperationException($"Không tìm thấy yêu cầu duyệt ID: {requestId}.");

        // 3. Quy tắc tách biệt nhiệm vụ (Segregation of Duties): Người tạo không được tự phê duyệt yêu cầu của mình
        if (request.RequestedBy.Equals(approverUsername, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Vi phạm chính sách tách biệt nhiệm vụ (Segregation of Duties): Người tạo yêu cầu không được phép tự phê duyệt yêu cầu của chính mình.");
        }

        // 4. Kiểm tra trạng thái hiện tại
        if (request.Status != "Pending")
        {
            throw new InvalidOperationException($"Yêu cầu này đã ở trạng thái '{request.Status}', không thể phê duyệt lại.");
        }

        // 5. Kiểm tra lại công nợ và hạn mức thực tế TẠI THỜI ĐIỂM DUYỆT (Rule 7: Không tin snapshot cũ)
        var liveCustomer = await _customerRepository.GetByIdAsync(request.CustomerId);
        if (liveCustomer == null)
            throw new InvalidOperationException($"Không tìm thấy khách hàng với mã ID: {request.CustomerId}.");

        decimal liveDebt = await _customerRepository.GetCurrentDebtAsync(request.CustomerId);
        decimal liveLimit = liveCustomer.CreditLimit;

        // Thời hạn có hiệu lực mặc định là 48 giờ
        var duration = validDuration ?? TimeSpan.FromHours(48);

        // 6. Thực hiện phê duyệt thông qua repository có kiểm soát giao dịch nguyên tử & khóa chống tranh chấp
        await _requestRepository.ApproveRequestAsync(requestId, approverUsername, approvalReason, liveDebt, liveLimit, duration);
    }
}
