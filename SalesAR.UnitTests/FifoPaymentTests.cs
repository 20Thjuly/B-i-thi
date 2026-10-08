namespace SalesAR.UnitTests;

using Xunit;
using SalesAR.CoreBusiness.Models;
using SalesAR.UseCases.Models;
using SalesAR.UseCases.Payments;

public class FifoPaymentTests
{
    [Fact]
    public void AllocatePayment_MatchesExactSpecificationExample()
    {
        // ARRANGE:
        // Invoice A = 2.000.000 (ngày cũ nhất)
        // Invoice B = 3.000.000 (ngày kế tiếp)
        // Invoice C = 1.000.000 (ngày mới nhất)
        // Payment = 4.000.000
        var now = DateTime.Now;
        var invoices = new List<InvoiceOutstandingDto>
        {
            new InvoiceOutstandingDto { Id = 1, InvoiceNumber = "Invoice A", InvoiceDate = now.AddDays(-30), TotalAmount = 2000000m, PaidAmount = 0m },
            new InvoiceOutstandingDto { Id = 2, InvoiceNumber = "Invoice B", InvoiceDate = now.AddDays(-20), TotalAmount = 3000000m, PaidAmount = 0m },
            new InvoiceOutstandingDto { Id = 3, InvoiceNumber = "Invoice C", InvoiceDate = now.AddDays(-10), TotalAmount = 1000000m, PaidAmount = 0m }
        };

        var allocator = new AllocatePaymentUseCase();

        // ACT:
        var allocations = allocator.Execute(paymentId: 1, paymentAmount: 4000000m, unpaidInvoices: invoices);

        // ASSERT:
        // Kết quả yêu cầu: A = 2.000.000, B = 2.000.000, C = 0
        var allocA = allocations.FirstOrDefault(a => a.InvoiceId == 1)?.AllocatedAmount ?? 0m;
        var allocB = allocations.FirstOrDefault(a => a.InvoiceId == 2)?.AllocatedAmount ?? 0m;
        var allocC = allocations.FirstOrDefault(a => a.InvoiceId == 3)?.AllocatedAmount ?? 0m;

        Assert.Equal(2000000m, allocA);
        Assert.Equal(2000000m, allocB);
        Assert.Equal(0m, allocC);
        Assert.Equal(4000000m, allocations.Sum(a => a.AllocatedAmount));
    }

    [Fact]
    public void AllocatePayment_DoesNotExceedInvoiceRemainingBalance()
    {
        // ARRANGE:
        // Invoice còn nợ 1.500.000 đ
        // Khách thanh toán 5.000.000 đ
        var invoices = new List<InvoiceOutstandingDto>
        {
            new InvoiceOutstandingDto { Id = 10, InvoiceNumber = "HD-10", InvoiceDate = DateTime.Now.AddDays(-5), TotalAmount = 2000000m, PaidAmount = 500000m } // Còn nợ: 1.5tr
        };

        var allocator = new AllocatePaymentUseCase();

        // ACT:
        var allocations = allocator.Execute(paymentId: 1, paymentAmount: 5000000m, unpaidInvoices: invoices);

        // ASSERT: Chỉ phân bổ đúng số dư còn lại của Invoice (1.5tr), không vượt quá
        Assert.Single(allocations);
        Assert.Equal(1500000m, allocations[0].AllocatedAmount);
    }

    [Fact]
    public void AllocatePayment_TotalAllocationDoesNotExceedPaymentAmount()
    {
        // ARRANGE:
        // Nhiều Invoice tổng nợ lớn: 10.000.000 đ
        // Payment chỉ trả: 3.500.000 đ
        var invoices = new List<InvoiceOutstandingDto>
        {
            new InvoiceOutstandingDto { Id = 1, InvoiceNumber = "HD-01", InvoiceDate = DateTime.Now.AddDays(-10), TotalAmount = 5000000m, PaidAmount = 0m },
            new InvoiceOutstandingDto { Id = 2, InvoiceNumber = "HD-02", InvoiceDate = DateTime.Now.AddDays(-5), TotalAmount = 5000000m, PaidAmount = 0m }
        };

        var allocator = new AllocatePaymentUseCase();

        // ACT:
        var allocations = allocator.Execute(paymentId: 1, paymentAmount: 3500000m, unpaidInvoices: invoices);

        // ASSERT: Tổng phân bổ = 3.500.000 đ <= Payment.Amount
        decimal totalAllocated = allocations.Sum(a => a.AllocatedAmount);
        Assert.Equal(3500000m, totalAllocated);
        Assert.True(totalAllocated <= 3500000m);
    }

    [Fact]
    public void AllocatePayment_WhenPaymentAmountZeroOrNegative_ThrowsException()
    {
        var allocator = new AllocatePaymentUseCase();
        var invoices = new List<InvoiceOutstandingDto>
        {
            new InvoiceOutstandingDto { Id = 1, TotalAmount = 1000000m, PaidAmount = 0m }
        };

        // Payment = 0 hoặc âm phải bị từ chối
        Assert.Throws<InvalidOperationException>(() => allocator.Execute(paymentId: 1, paymentAmount: 0m, unpaidInvoices: invoices));
        Assert.Throws<InvalidOperationException>(() => allocator.Execute(paymentId: 1, paymentAmount: -500000m, unpaidInvoices: invoices));
    }
}
