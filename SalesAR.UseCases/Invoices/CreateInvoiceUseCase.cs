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

        // RÀNG BUỘC: Hóa đơn phải có chi tiết
        if (lines == null || lines.Count == 0)
            throw new InvalidOperationException("Hóa đơn phải có ít nhất một dòng sản phẩm chi tiết.");

        // BƯỚC 1: Validate Customer
        var customer = await _customerRepository.GetByIdAsync(invoice.CustomerId);
        if (customer == null)
            throw new InvalidOperationException($"Không tìm thấy khách hàng với mã ID: {invoice.CustomerId}.");

        if (!customer.IsActive)
            throw new InvalidOperationException($"Khách hàng '{customer.Name}' đang ở trạng thái ngừng hoạt động.");

        // BƯỚC 2: Tính tổng tiền hóa đơn & Validate từng dòng sản phẩm
        decimal calculatedTotal = 0;
        foreach (var line in lines)
        {
            if (line.Quantity <= 0)
                throw new InvalidOperationException("Số lượng sản phẩm phải lớn hơn 0.");

            if (line.UnitPrice < 0)
                throw new InvalidOperationException("Đơn giá sản phẩm không được âm.");

            line.Amount = line.Quantity * line.UnitPrice;
            calculatedTotal += line.Amount;
        }
        invoice.TotalAmount = calculatedTotal;

        // BƯỚC 3: Tính công nợ hiện tại & Validate Credit Limit (BUSINESS RULE 1 - K1.3)
        decimal currentDebt = await _customerRepository.GetCurrentDebtAsync(invoice.CustomerId);
        if (currentDebt + invoice.TotalAmount > customer.CreditLimit)
        {
            throw new InvalidOperationException("Không thể tạo hóa đơn. Công nợ hiện tại và giá trị hóa đơn vượt quá hạn mức tín dụng của khách hàng.");
        }

        // BƯỚC 4, 5, 6: Tạo Invoice, InvoiceLine và Cập nhật Stock trong cùng 1 Transaction (K2.2)
        invoice.Status = "Unpaid";
        return await _invoiceRepository.CreateInvoiceWithLinesAsync(invoice, lines);
    }
}
