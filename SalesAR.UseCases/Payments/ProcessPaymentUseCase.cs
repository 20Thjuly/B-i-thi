namespace SalesAR.UseCases.Payments;

using SalesAR.CoreBusiness.Models;
using SalesAR.UseCases.PluginInterfaces;

public interface IProcessPaymentUseCase
{
    Task<int> ExecuteAsync(Payment payment);
}

public class ProcessPaymentUseCase : IProcessPaymentUseCase
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly ICustomerRepository _customerRepository;

    public ProcessPaymentUseCase(
        IPaymentRepository paymentRepository,
        IInvoiceRepository invoiceRepository,
        ICustomerRepository customerRepository)
    {
        _paymentRepository = paymentRepository;
        _invoiceRepository = invoiceRepository;
        _customerRepository = customerRepository;
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

        // LUẬT NGHIỆP VỤ 2 (K1.3): Phân bổ thanh toán vào hóa đơn cũ nhất trước (FIFO), Tổng phân bổ <= số tiền thu
        var unpaidInvoices = (await _invoiceRepository.GetUnpaidInvoicesByCustomerAsync(payment.CustomerId))
            .OrderBy(i => i.InvoiceDate)
            .ThenBy(i => i.Id)
            .ToList();

        if (unpaidInvoices.Count == 0)
            throw new InvalidOperationException("Khách hàng hiện không có hóa đơn nào chưa thanh toán.");

        decimal remainingPayment = payment.Amount;
        var allocations = new List<PaymentAllocation>();

        foreach (var inv in unpaidInvoices)
        {
            if (remainingPayment <= 0)
                break;

            // Tính số tiền còn nợ của hóa đơn này
            // Lấy tất cả phân bổ trước đây của hóa đơn nếu có
            decimal alreadyAllocated = 0;
            // Ở tầng Plugin/Database đã tính hoặc lấy TotalAmount
            decimal invoiceBalance = inv.TotalAmount - alreadyAllocated;
            if (invoiceBalance <= 0)
                continue;

            decimal allocatedToThisInvoice = Math.Min(remainingPayment, invoiceBalance);
            allocations.Add(new PaymentAllocation
            {
                InvoiceId = inv.Id,
                AllocatedAmount = allocatedToThisInvoice
            });

            remainingPayment -= allocatedToThisInvoice;
        }

        // Kiểm tra ràng buộc: Tổng phân bổ <= số tiền thu
        decimal totalAllocated = allocations.Sum(a => a.AllocatedAmount);
        if (totalAllocated > payment.Amount)
        {
            throw new InvalidOperationException($"Lỗi nghiệp vụ: Tổng phân bổ ({totalAllocated:N0} đ) vượt quá số tiền thu ({payment.Amount:N0} đ).");
        }

        // Lưu vào CSDL trong 1 transaction an toàn (K2.2)
        return await _paymentRepository.CreatePaymentWithAllocationsAsync(payment, allocations);
    }
}
