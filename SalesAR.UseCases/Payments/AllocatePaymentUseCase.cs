namespace SalesAR.UseCases.Payments;

using SalesAR.CoreBusiness.Models;
using SalesAR.UseCases.Models;

public interface IAllocatePaymentUseCase
{
    List<PaymentAllocation> Execute(int paymentId, decimal paymentAmount, IEnumerable<InvoiceOutstandingDto> unpaidInvoices);
}

public class AllocatePaymentUseCase : IAllocatePaymentUseCase
{
    /// <summary>
    /// Thuật toán phân bổ thanh toán FIFO chuẩn:
    /// - Sắp xếp theo ngày đến hạn (DueDate ASC) tăng dần (hóa đơn đến hạn trước trả trước).
    /// - Sau đó đến ngày lập hóa đơn (InvoiceDate ASC), cuối cùng là Id để đảm bảo tính tất định.
    /// - Tổng phân bổ cho từng hóa đơn không vượt số dư nợ còn lại (RemainingAmount).
    /// - Tổng phân bổ không vượt số tiền thanh toán khả dụng.
    /// - Hỗ trợ thanh toán một phần, thanh toán nhiều hóa đơn và thanh toán dư (overpayment).
    /// </summary>
    public List<PaymentAllocation> Execute(int paymentId, decimal paymentAmount, IEnumerable<InvoiceOutstandingDto> unpaidInvoices)
    {
        if (paymentAmount <= 0)
            throw new InvalidOperationException("Số tiền thanh toán phải lớn hơn 0.");

        if (unpaidInvoices == null)
            return new List<PaymentAllocation>();

        // Sắp xếp DueDate ASC, InvoiceDate ASC, Id ASC (Chuẩn FIFO theo hạn nợ)
        var sortedInvoices = unpaidInvoices
            .Where(i => i.RemainingAmount > 0)
            .OrderBy(i => i.DueDate)
            .ThenBy(i => i.InvoiceDate)
            .ThenBy(i => i.Id)
            .ToList();

        decimal remainingPayment = paymentAmount;
        var allocations = new List<PaymentAllocation>();

        foreach (var inv in sortedInvoices)
        {
            if (remainingPayment <= 0)
                break;

            // Không phân bổ vượt số dư nợ có thể thu của Invoice (RemainingAmount)
            decimal allocated = Math.Min(remainingPayment, inv.RemainingAmount);
            if (allocated > 0)
            {
                allocations.Add(new PaymentAllocation
                {
                    PaymentId = paymentId,
                    InvoiceId = inv.Id,
                    AllocatedAmount = allocated
                });

                remainingPayment -= allocated;
            }
        }

        // Kiểm tra ràng buộc bảo vệ: Tổng allocation không vượt Payment.Amount
        decimal totalAllocated = allocations.Sum(a => a.AllocatedAmount);
        if (totalAllocated > paymentAmount)
        {
            throw new InvalidOperationException($"Lỗi phân bổ: Tổng phân bổ ({totalAllocated:N0}) vượt quá số tiền thu ({paymentAmount:N0}).");
        }

        return allocations;
    }
}
