namespace SalesAR.UseCases.Payments;

using SalesAR.CoreBusiness.Models;
using SalesAR.UseCases.PluginInterfaces;

public interface IReallocatePaymentUseCase
{
    Task ExecuteAsync(int paymentId, IEnumerable<PaymentAllocation> newAllocations, string performedBy, string userRole, string reason);
}

public class ReallocatePaymentUseCase : IReallocatePaymentUseCase
{
    private readonly IPaymentRepository _paymentRepository;

    public ReallocatePaymentUseCase(IPaymentRepository paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task ExecuteAsync(int paymentId, IEnumerable<PaymentAllocation> newAllocations, string performedBy, string userRole, string reason)
    {
        // Kiểm tra quyền nghiệp vụ: Chỉ ADMIN hoặc ACCOUNTANT mới được phép phân bổ lại thanh toán
        if (string.IsNullOrWhiteSpace(userRole) || (!userRole.Equals("ADMIN", StringComparison.OrdinalIgnoreCase) && !userRole.Equals("ACCOUNTANT", StringComparison.OrdinalIgnoreCase)))
        {
            throw new UnauthorizedAccessException("Bạn không có quyền thực hiện phân bổ lại thanh toán. Yêu cầu vai trò ADMIN hoặc ACCOUNTANT.");
        }

        if (paymentId <= 0)
            throw new ArgumentException("Mã phiếu thu không hợp lệ.", nameof(paymentId));

        if (newAllocations == null)
            throw new ArgumentNullException(nameof(newAllocations));

        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Bắt buộc phải cung cấp lý do phân bổ lại để lưu vết kiểm toán (Audit Trail).");

        await _paymentRepository.ReallocatePaymentAsync(paymentId, newAllocations, performedBy, reason);
    }
}
