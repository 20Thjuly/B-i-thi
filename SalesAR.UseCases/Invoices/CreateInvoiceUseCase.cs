namespace SalesAR.UseCases.Invoices;

using SalesAR.CoreBusiness.Models;
using SalesAR.UseCases.PluginInterfaces;

public interface ICreateInvoiceUseCase
{
    Task<int> ExecuteAsync(Invoice invoice, List<InvoiceLine> lines);
}

public class CreateInvoiceUseCase : ICreateInvoiceUseCase
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly ICustomerRepository _customerRepository;

    public CreateInvoiceUseCase(
        IInvoiceRepository invoiceRepository, 
        ICustomerRepository customerRepository)
    {
        _invoiceRepository = invoiceRepository;
        _customerRepository = customerRepository;
    }

    public async Task<int> ExecuteAsync(Invoice invoice, List<InvoiceLine> lines)
    {
        if (invoice == null)
            throw new ArgumentNullException(nameof(invoice));

        if (lines == null || lines.Count == 0)
            throw new InvalidOperationException("Hóa đơn phải có ít nhất 1 dòng chi tiết sản phẩm.");

        var customer = await _customerRepository.GetByIdAsync(invoice.CustomerId);
        if (customer == null)
            throw new InvalidOperationException($"Không tìm thấy khách hàng với mã ID: {invoice.CustomerId}.");

        if (!customer.IsActive)
            throw new InvalidOperationException($"Khách hàng {customer.Name} đang ở trạng thái ngừng hoạt động.");

        // Tính tổng tiền hóa đơn từ các dòng chi tiết
        decimal calculatedTotal = 0;
        foreach (var line in lines)
        {
            if (line.Quantity <= 0)
                throw new InvalidOperationException("Số lượng sản phẩm phải lớn hơn 0.");
            if (line.UnitPrice < 0)
                throw new InvalidOperationException("Đơn giá không được âm.");

            line.Amount = line.Quantity * line.UnitPrice;
            calculatedTotal += line.Amount;
        }
        invoice.TotalAmount = calculatedTotal;

        // LUẬT NGHIỆP VỤ 1 (K1.3): Chặn hóa đơn mới khi công nợ hiện tại + giá trị hóa đơn > hạn mức tín dụng
        decimal currentDebt = await _customerRepository.GetCurrentDebtAsync(invoice.CustomerId);
        if (currentDebt + invoice.TotalAmount > customer.CreditLimit)
        {
            throw new InvalidOperationException(
                $"[Vi phạm hạn mức tín dụng] Công nợ hiện tại ({currentDebt:N0} đ) + Giá trị đơn hàng ({invoice.TotalAmount:N0} đ) = {(currentDebt + invoice.TotalAmount):N0} đ vượt quá Hạn mức tín dụng cho phép ({customer.CreditLimit:N0} đ). Đơn hàng bị từ chối!");
        }

        // Thiết lập trạng thái ban đầu và ghi dữ liệu trong 1 Transaction (K2.2)
        invoice.Status = "Unpaid";
        return await _invoiceRepository.CreateInvoiceWithLinesAsync(invoice, lines);
    }
}
