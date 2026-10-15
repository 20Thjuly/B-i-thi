namespace SalesAR.UseCases.Invoices;

using SalesAR.CoreBusiness.Models;
using SalesAR.UseCases.PluginInterfaces;

public interface ICreateInvoiceUseCase
{
    Task<int> ExecuteAsync(Invoice invoice, List<InvoiceLine> lines, int? approvalRequestId = null);
}

public class CreateInvoiceUseCase : ICreateInvoiceUseCase
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ICreditLimitRequestRepository? _creditLimitRequestRepository;

    public CreateInvoiceUseCase(
        IInvoiceRepository invoiceRepository, 
        ICustomerRepository customerRepository,
        ICreditLimitRequestRepository? creditLimitRequestRepository = null)
    {
        _invoiceRepository = invoiceRepository;
        _customerRepository = customerRepository;
        _creditLimitRequestRepository = creditLimitRequestRepository;
    }

    public async Task<int> ExecuteAsync(Invoice invoice, List<InvoiceLine> lines, int? approvalRequestId = null)
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

        // BƯỚC 3: Tính công nợ hiện tại & Validate Credit Limit (Rule 1, 2, 3, 4 - Part 5)
        decimal currentDebt = await _customerRepository.GetCurrentDebtAsync(invoice.CustomerId);
        CreditLimitRequest? approvedRequest = null;

        if (currentDebt + invoice.TotalAmount > customer.CreditLimit)
        {
            // Vượt hạn mức: Kiểm tra xem có yêu cầu phê duyệt hợp lệ hay không
            if (_creditLimitRequestRepository != null)
            {
                if (approvalRequestId.HasValue)
                {
                    approvedRequest = await _creditLimitRequestRepository.GetByIdAsync(approvalRequestId.Value);
                }
                else
                {
                    approvedRequest = await _creditLimitRequestRepository.GetValidApprovedRequestAsync(invoice.CustomerId, invoice.TotalAmount);
                }
            }

            if (approvedRequest == null || approvedRequest.Status != "Approved" || approvedRequest.IsUsed || 
                (approvedRequest.ExpiresAt.HasValue && approvedRequest.ExpiresAt.Value < DateTime.UtcNow) ||
                approvedRequest.TransactionAmount < invoice.TotalAmount)
            {
                throw new InvalidOperationException("Không thể tạo hóa đơn. Công nợ hiện tại và giá trị hóa đơn vượt quá hạn mức tín dụng của khách hàng.");
            }
        }

        // BƯỚC 4, 5, 6: Tạo Invoice, InvoiceLine và Cập nhật Stock trong cùng 1 Transaction
        invoice.Status = "Unpaid";
        int invoiceId = await _invoiceRepository.CreateInvoiceWithLinesAsync(invoice, lines);

        // Đánh dấu yêu cầu phê duyệt đã được sử dụng cho hóa đơn này (Chống sử dụng lại approval)
        if (approvedRequest != null && _creditLimitRequestRepository != null)
        {
            await _creditLimitRequestRepository.MarkAsUsedAsync(approvedRequest.Id, invoiceId);
        }

        return invoiceId;
    }
}
