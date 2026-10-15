namespace SalesAR.UseCases.CreditLimitRequests;

using SalesAR.CoreBusiness.Models;
using SalesAR.UseCases.PluginInterfaces;

public interface ICreateCreditLimitRequestUseCase
{
    Task<int> ExecuteAsync(int customerId, decimal transactionAmount, string reason, string requestedBy, string userRole);
}

public class CreateCreditLimitRequestUseCase : ICreateCreditLimitRequestUseCase
{
    private readonly ICreditLimitRequestRepository _requestRepository;
    private readonly ICustomerRepository _customerRepository;

    public CreateCreditLimitRequestUseCase(
        ICreditLimitRequestRepository requestRepository,
        ICustomerRepository customerRepository)
    {
        _requestRepository = requestRepository;
        _customerRepository = customerRepository;
    }

    public async Task<int> ExecuteAsync(int customerId, decimal transactionAmount, string reason, string requestedBy, string userRole)
    {
        // 1. Phân quyền: Nhân viên bán hàng (SALES) hoặc ADMIN có quyền lập yêu cầu duyệt
        if (string.IsNullOrWhiteSpace(userRole) || (!userRole.Equals("SALES", StringComparison.OrdinalIgnoreCase) && !userRole.Equals("ADMIN", StringComparison.OrdinalIgnoreCase)))
        {
            throw new UnauthorizedAccessException("Chỉ có nhân viên bán hàng (SALES) hoặc Quản trị viên mới có quyền tạo yêu cầu duyệt vượt hạn mức.");
        }

        if (transactionAmount <= 0)
            throw new InvalidOperationException("Số tiền giao dịch yêu cầu duyệt phải lớn hơn 0.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Bắt buộc phải cung cấp lý do đề xuất duyệt vượt hạn mức.");

        // 2. Lấy thông tin khách hàng và số dư công nợ thực tế tại thời điểm tạo
        var customer = await _customerRepository.GetByIdAsync(customerId);
        if (customer == null)
            throw new InvalidOperationException($"Không tìm thấy khách hàng với mã ID: {customerId}.");

        if (!customer.IsActive)
            throw new InvalidOperationException($"Khách hàng '{customer.Name}' đang ngừng hoạt động.");

        decimal liveDebt = await _customerRepository.GetCurrentDebtAsync(customerId);

        // 3. Khởi tạo đối tượng yêu cầu với snapshot ghi nhận bối cảnh
        var request = new CreditLimitRequest
        {
            RequestNumber = $"CLR-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper()}",
            CustomerId = customerId,
            TransactionAmount = transactionAmount,
            CurrentDebtSnapshot = liveDebt,
            CreditLimitSnapshot = customer.CreditLimit,
            Reason = reason.Trim(),
            Status = "Pending",
            RequestedBy = string.IsNullOrWhiteSpace(requestedBy) ? "sales" : requestedBy,
            RequestedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(3), // Hiệu lực chờ duyệt tối đa 3 ngày
            IsUsed = false
        };

        return await _requestRepository.CreateRequestAsync(request);
    }
}
