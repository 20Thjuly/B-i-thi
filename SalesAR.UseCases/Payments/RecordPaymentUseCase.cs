namespace SalesAR.UseCases.Payments;

using SalesAR.CoreBusiness.Models;
using SalesAR.UseCases.PluginInterfaces;

public interface IRecordPaymentUseCase
{
    Task<int> ExecuteAsync(Payment payment);
}

public class RecordPaymentUseCase : IRecordPaymentUseCase
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IAllocatePaymentUseCase _allocatePaymentUseCase;

    public RecordPaymentUseCase(
        IPaymentRepository paymentRepository,
        IInvoiceRepository invoiceRepository,
        ICustomerRepository customerRepository,
        IAllocatePaymentUseCase allocatePaymentUseCase)
    {
        _paymentRepository = paymentRepository;
        _invoiceRepository = invoiceRepository;
        _customerRepository = customerRepository;
        _allocatePaymentUseCase = allocatePaymentUseCase;
    }

    public async Task<int> ExecuteAsync(Payment payment)
    {
        if (payment == null)
            throw new ArgumentNullException(nameof(payment));

        if (payment.Amount <= 0)
            throw new InvalidOperationException("Số tiền thanh toán phải lớn hơn 0.");

        // 1. Chống ghi nhận trùng lặp payment ở tầng UseCase
        var existing = await _paymentRepository.GetByPaymentNumberAsync(payment.PaymentNumber);
        if (existing != null)
        {
            throw new InvalidOperationException($"Số phiếu thu '{payment.PaymentNumber}' đã tồn tại trong hệ thống. Không thể ghi nhận trùng lặp.");
        }

        // 2. Validate khách hàng
        var customer = await _customerRepository.GetByIdAsync(payment.CustomerId);
        if (customer == null)
            throw new InvalidOperationException($"Không tìm thấy khách hàng với mã ID: {payment.CustomerId}.");

        // 3. Lấy các hóa đơn còn dư nợ của khách hàng (theo công thức: TotalAmount - PaidAmount - AdjustedAmount)
        var outstandingInvoices = await _invoiceRepository.GetOutstandingInvoicesByCustomerAsync(payment.CustomerId);
        var unpaidList = outstandingInvoices.ToList();

        // 4. Áp dụng Business Rule (FIFO allocation theo DueDate)
        // Nếu khách hàng thanh toán dư (overpayment) hoặc đặt cọc trước (unpaidList rỗng), 
        // phiếu thu vẫn được ghi nhận an toàn, phần tiền dư không bị mất.
        var allocations = unpaidList.Count > 0 
            ? _allocatePaymentUseCase.Execute(0, payment.Amount, unpaidList)
            : new List<PaymentAllocation>();

        // 5. Ghi nhận Payment, Allocations, cập nhật trạng thái hóa đơn & AuditLog trong 1 Transaction tại Repository
        return await _paymentRepository.CreatePaymentWithAllocationsAsync(payment, allocations);
    }
}
