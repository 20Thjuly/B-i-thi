namespace SalesAR.UnitTests;

using Xunit;
using SalesAR.CoreBusiness.Models;
using SalesAR.UseCases.Models;
using SalesAR.UseCases.Payments;
using SalesAR.UseCases.PluginInterfaces;

public class FakePaymentRepositoryForTest : IPaymentRepository
{
    public List<Payment> Payments { get; } = new();
    public List<PaymentAllocation> Allocations { get; } = new();
    public bool ReallocateCalled { get; private set; }

    public Task<IEnumerable<Payment>> GetAllAsync() => Task.FromResult<IEnumerable<Payment>>(Payments);
    public Task<Payment?> GetByIdAsync(int id) => Task.FromResult(Payments.FirstOrDefault(p => p.Id == id));
    public Task<Payment?> GetByPaymentNumberAsync(string paymentNumber) => Task.FromResult(Payments.FirstOrDefault(p => p.PaymentNumber == paymentNumber));
    public Task<IEnumerable<Payment>> GetPaymentsByCustomerAsync(int customerId) => Task.FromResult(Payments.Where(p => p.CustomerId == customerId));
    public Task<IEnumerable<PaymentAllocation>> GetAllocationsByPaymentIdAsync(int paymentId) => Task.FromResult(Allocations.Where(a => a.PaymentId == paymentId));
    public Task<IEnumerable<PaymentAllocation>> GetAllocationsByCustomerAsync(int customerId) => Task.FromResult<IEnumerable<PaymentAllocation>>(Allocations);

    public Task<int> CreatePaymentWithAllocationsAsync(Payment payment, IEnumerable<PaymentAllocation> allocations)
    {
        if (Payments.Any(p => p.PaymentNumber == payment.PaymentNumber))
            throw new InvalidOperationException($"Số phiếu thu '{payment.PaymentNumber}' đã tồn tại.");

        payment.Id = Payments.Count + 1;
        Payments.Add(payment);
        Allocations.AddRange(allocations);
        return Task.FromResult(payment.Id);
    }

    public Task ReallocatePaymentAsync(int paymentId, IEnumerable<PaymentAllocation> newAllocations, string performedBy, string reason)
    {
        ReallocateCalled = true;
        Allocations.RemoveAll(a => a.PaymentId == paymentId);
        Allocations.AddRange(newAllocations);
        return Task.CompletedTask;
    }
}

public class PaymentValidationAndLimitsTests
{
    [Fact]
    public async Task RecordPayment_ThrowsException_WhenPaymentNumberIsDuplicate()
    {
        // ARRANGE:
        var repo = new FakePaymentRepositoryForTest();
        repo.Payments.Add(new Payment { Id = 1, PaymentNumber = "REC-DUPLICATE", CustomerId = 1, Amount = 1000000m });

        var customerRepo = new FakeCustomerRepository(new Customer { Id = 1, Code = "KH001", Name = "Test", CreditLimit = 10000000m }, 1000000m);
        var invoiceRepo = new FakeInvoiceRepository();
        var allocator = new AllocatePaymentUseCase();

        var useCase = new RecordPaymentUseCase(repo, invoiceRepo, customerRepo, allocator);

        // ACT & ASSERT: Cố tình ghi nhận lại PaymentNumber đã có
        var dupPayment = new Payment { PaymentNumber = "REC-DUPLICATE", CustomerId = 1, Amount = 500000m };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.ExecuteAsync(dupPayment));
        Assert.Contains("đã tồn tại trong hệ thống", ex.Message);
    }

    [Fact]
    public async Task ReallocatePayment_RequiresAdminOrAccountantRole()
    {
        var repo = new FakePaymentRepositoryForTest();
        var useCase = new ReallocatePaymentUseCase(repo);

        var newAllocations = new List<PaymentAllocation>
        {
            new PaymentAllocation { InvoiceId = 1, AllocatedAmount = 1000000m }
        };

        // Vai trò SALES không được phép phân bổ lại
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => 
            useCase.ExecuteAsync(1, newAllocations, "salesuser", "SALES", "Sửa phân bổ"));

        // Vai trò ACCOUNTANT được phép
        await useCase.ExecuteAsync(1, newAllocations, "accuser", "ACCOUNTANT", "Kế toán điều chỉnh phân bổ đúng chứng từ");
        Assert.True(repo.ReallocateCalled);
    }

    [Fact]
    public async Task ReallocatePayment_RequiresReasonForAuditTrail()
    {
        var repo = new FakePaymentRepositoryForTest();
        var useCase = new ReallocatePaymentUseCase(repo);

        var newAllocations = new List<PaymentAllocation>
        {
            new PaymentAllocation { InvoiceId = 1, AllocatedAmount = 1000000m }
        };

        // Lý do để trống hoặc khoảng trắng phải bị từ chối
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            useCase.ExecuteAsync(1, newAllocations, "accuser", "ACCOUNTANT", "   "));

        Assert.Contains("lý do phân bổ lại để lưu vết kiểm toán", ex.Message);
    }
}
