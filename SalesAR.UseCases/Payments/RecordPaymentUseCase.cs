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

        var customer = await _customerRepository.GetByIdAsync(payment.CustomerId);
        if (customer == null)
            throw new InvalidOperationException($"Không tìm thấy khách hàng với mã ID: {payment.CustomerId}.");

        // Lấy các hóa đơn còn dư nợ của khách hàng
        var outstandingInvoices = await _invoiceRepository.GetOutstandingInvoicesByCustomerAsync(payment.CustomerId);
        var unpaidList = outstandingInvoices.ToList();

        if (unpaidList.Count == 0)
            throw new InvalidOperationException("Khách hàng hiện không có hóa đơn nào còn nợ để thanh toán.");

        // Áp dụng Business Rule 2 (FIFO allocation)
        var allocations = _allocatePaymentUseCase.Execute(0, payment.Amount, unpaidList);

        if (allocations.Count == 0)
            throw new InvalidOperationException("Không thể phân bổ thanh toán cho các hóa đơn hiện tại.");

        // Ghi nhận Payment và Allocations trong 1 Transaction tại Repository (K2.2)
        return await _paymentRepository.CreatePaymentWithAllocationsAsync(payment, allocations);
    }
}
