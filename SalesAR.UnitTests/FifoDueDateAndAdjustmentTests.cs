namespace SalesAR.UnitTests;

using Xunit;
using SalesAR.CoreBusiness.Models;
using SalesAR.UseCases.Models;
using SalesAR.UseCases.Payments;

public class FifoDueDateAndAdjustmentTests
{
    [Fact]
    public void Fifo_OrdersByDueDateFirst_ThenInvoiceDate()
    {
        // ARRANGE:
        // Invoice 1: Lập trước (10 ngày trước) nhưng DueDate xa hơn (sau 30 ngày)
        // Invoice 2: Lập sau (5 ngày trước) nhưng DueDate gần hơn (sau 5 ngày) -> Phải thanh toán trước!
        var now = DateTime.Today;
        var invoices = new List<InvoiceOutstandingDto>
        {
            new InvoiceOutstandingDto 
            { 
                Id = 1, 
                InvoiceNumber = "HD-001", 
                InvoiceDate = now.AddDays(-10), 
                DueDate = now.AddDays(30), 
                TotalAmount = 5000000m, 
                PaidAmount = 0m 
            },
            new InvoiceOutstandingDto 
            { 
                Id = 2, 
                InvoiceNumber = "HD-002", 
                InvoiceDate = now.AddDays(-5), 
                DueDate = now.AddDays(5), 
                TotalAmount = 3000000m, 
                PaidAmount = 0m 
            }
        };

        var allocator = new AllocatePaymentUseCase();

        // ACT: Thanh toán 4.000.000 đ
        var allocations = allocator.Execute(paymentId: 100, paymentAmount: 4000000m, unpaidInvoices: invoices);

        // ASSERT:
        // HD-002 (đến hạn trước) được trả hết 3.000.000 đ
        // HD-001 (đến hạn sau) nhận phần còn lại là 1.000.000 đ
        var alloc2 = allocations.FirstOrDefault(a => a.InvoiceId == 2)?.AllocatedAmount ?? 0m;
        var alloc1 = allocations.FirstOrDefault(a => a.InvoiceId == 1)?.AllocatedAmount ?? 0m;

        Assert.Equal(3000000m, alloc2);
        Assert.Equal(1000000m, alloc1);
    }

    [Fact]
    public void OutstandingAmount_CalculatesCorrectlyWithAdjustments_NoDoubleCounting()
    {
        // Công thức chuẩn: OutstandingAmount = InvoiceAmount - ValidAllocatedPayments - ValidAdjustments
        var inv = new InvoiceOutstandingDto
        {
            Id = 5,
            InvoiceNumber = "HD-005",
            TotalAmount = 10000000m,
            PaidAmount = 3000000m,
            AdjustedAmount = 1000000m // Giảm giá / chiết khấu / trả hàng
        };

        // Còn nợ = 10tr - 3tr - 1tr = 6tr
        Assert.Equal(6000000m, inv.RemainingAmount);
    }

    [Fact]
    public void Overpayment_AllocatesUpToTotalDebt_LeftoverPaymentKeptSafe()
    {
        // ARRANGE: Tổng nợ 4.000.000 đ, khách trả 7.000.000 đ (thanh toán dư 3.000.000 đ)
        var invoices = new List<InvoiceOutstandingDto>
        {
            new InvoiceOutstandingDto { Id = 1, TotalAmount = 2000000m, PaidAmount = 0m },
            new InvoiceOutstandingDto { Id = 2, TotalAmount = 2000000m, PaidAmount = 0m }
        };

        var allocator = new AllocatePaymentUseCase();

        // ACT:
        var allocations = allocator.Execute(1, 7000000m, invoices);

        // ASSERT:
        // Cả 2 hóa đơn đều được phân bổ đủ 2.000.000 đ (tổng phân bổ = 4.000.000 đ)
        // Không lỗi exception, tiền thừa 3.000.000 đ sẽ được giữ an toàn trên phiếu thu
        Assert.Equal(4000000m, allocations.Sum(a => a.AllocatedAmount));
        Assert.Equal(2, allocations.Count);
    }
}
