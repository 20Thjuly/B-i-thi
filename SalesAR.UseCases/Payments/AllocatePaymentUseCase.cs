namespace SalesAR.UseCases.Payments;

using SalesAR.CoreBusiness.Models;
using SalesAR.UseCases.Models;

public interface IAllocatePaymentUseCase
{
    List<PaymentAllocation> Execute(int paymentId, decimal paymentAmount, IEnumerable<InvoiceOutstandingDto> unpaidInvoices);
}

public class AllocatePaymentUseCase : IAllocatePaymentUseCase
{
    public List<PaymentAllocation> Execute(int paymentId, decimal paymentAmount, IEnumerable<InvoiceOutstandingDto> unpaidInvoices)
    {
        if (paymentAmount <= 0)
            throw new InvalidOperationException("Số tiền thanh toán phải lớn hơn 0.");

        if (unpaidInvoices == null)
            return new List<PaymentAllocation>();

        // Sắp xếp InvoiceDate tăng dần, Id tăng dần (Hóa đơn cũ nhất lên đầu - FIFO)
        var sortedInvoices = unpaidInvoices
            .Where(i => i.RemainingAmount > 0)
            .OrderBy(i => i.InvoiceDate)
            .ThenBy(i => i.Id)
            .ToList();

        decimal remainingPayment = paymentAmount;
        var allocations = new List<PaymentAllocation>();

        foreach (var inv in sortedInvoices)
        {
            if (remainingPayment <= 0)
                break;

            // Không phân bổ vượt số dư Invoice
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

        // Tổng allocation không vượt Payment.Amount
        decimal totalAllocated = allocations.Sum(a => a.AllocatedAmount);
        if (totalAllocated > paymentAmount)
        {
            throw new InvalidOperationException($"Lỗi phân bổ: Tổng phân bổ ({totalAllocated:N0}) vượt quá số tiền thu ({paymentAmount:N0}).");
        }

        return allocations;
    }
}
