namespace SalesAR.UnitTests;

using Xunit;
using SalesAR.CoreBusiness.Models;
using SalesAR.UseCases.Invoices;
using SalesAR.UseCases.Models;
using SalesAR.UseCases.PluginInterfaces;

public class FakeCustomerRepository : ICustomerRepository
{
    private readonly Customer? _customer;
    private readonly decimal _currentDebt;

    public FakeCustomerRepository(Customer? customer, decimal currentDebt)
    {
        _customer = customer;
        _currentDebt = currentDebt;
    }

    public Task<IEnumerable<Customer>> GetAllAsync() => Task.FromResult<IEnumerable<Customer>>(_customer != null ? new[] { _customer } : Array.Empty<Customer>());
    public Task<Customer?> GetByIdAsync(int id) => Task.FromResult(_customer);
    public Task<Customer?> GetByCodeAsync(string code) => Task.FromResult(_customer);
    public Task<int> AddAsync(Customer customer) => Task.FromResult(1);
    public Task UpdateAsync(Customer customer) => Task.CompletedTask;
    public Task DeleteAsync(int id) => Task.CompletedTask;
    public Task<decimal> GetCurrentDebtAsync(int customerId) => Task.FromResult(_currentDebt);
    public Task<CustomerDebtDto?> GetCustomerDebtDetailsAsync(int customerId) => Task.FromResult<CustomerDebtDto?>(null);
}

public class FakeInvoiceRepository : IInvoiceRepository
{
    public int CreatedInvoiceId { get; private set; } = 100;
    public Invoice? LastCreatedInvoice { get; private set; }
    public List<InvoiceLine>? LastCreatedLines { get; private set; }

    public Task<IEnumerable<Invoice>> GetAllAsync() => Task.FromResult<IEnumerable<Invoice>>(Array.Empty<Invoice>());
    public Task<Invoice?> GetByIdAsync(int id) => Task.FromResult<Invoice?>(null);
    public Task<Invoice?> GetByInvoiceNumberAsync(string invoiceNumber) => Task.FromResult<Invoice?>(null);
    public Task<IEnumerable<InvoiceLine>> GetLinesByInvoiceIdAsync(int invoiceId) => Task.FromResult<IEnumerable<InvoiceLine>>(Array.Empty<InvoiceLine>());
    public Task<IEnumerable<Invoice>> GetUnpaidInvoicesByCustomerAsync(int customerId) => Task.FromResult<IEnumerable<Invoice>>(Array.Empty<Invoice>());
    public Task<IEnumerable<Invoice>> GetInvoicesByCustomerAsync(int customerId) => Task.FromResult<IEnumerable<Invoice>>(Array.Empty<Invoice>());
    public Task<IEnumerable<InvoiceOutstandingDto>> GetOutstandingInvoicesByCustomerAsync(int customerId) => Task.FromResult<IEnumerable<InvoiceOutstandingDto>>(Array.Empty<InvoiceOutstandingDto>());
    
    public Task<int> CreateInvoiceWithLinesAsync(Invoice invoice, IEnumerable<InvoiceLine> lines)
    {
        LastCreatedInvoice = invoice;
        LastCreatedLines = lines.ToList();
        return Task.FromResult(CreatedInvoiceId);
    }

    public Task UpdateStatusAsync(int invoiceId, string status) => Task.CompletedTask;
}

public class CreditLimitTests
{
    [Fact]
    public async Task CreateInvoice_WhenTotalExceedsCreditLimit_ThrowsExactBusinessRuleException()
    {
        // ARRANGE: Khách hàng có hạn mức tín dụng 10,000,000 đ
        // Đang nợ hiện tại: 8,000,000 đ
        var customer = new Customer
        {
            Id = 1,
            Code = "KH001",
            Name = "Khách Hàng Test",
            CreditLimit = 10000000m,
            IsActive = true
        };
        decimal currentDebt = 8000000m;

        var fakeCustRepo = new FakeCustomerRepository(customer, currentDebt);
        var fakeInvRepo = new FakeInvoiceRepository();
        var useCase = new CreateInvoiceUseCase(fakeInvRepo, fakeCustRepo);

        // Hóa đơn mới: 3,000,000 đ -> Tổng nợ mới = 8tr + 3tr = 11tr > 10tr (Vượt hạn mức!)
        var invoice = new Invoice
        {
            InvoiceNumber = "HD-TEST-01",
            CustomerId = 1,
            InvoiceDate = DateTime.Now,
            DueDate = DateTime.Now.AddDays(30)
        };
        var lines = new List<InvoiceLine>
        {
            new InvoiceLine { ProductId = 1, Quantity = 3, UnitPrice = 1000000m }
        };

        // ACT & ASSERT: Phải ném ngoại lệ đúng thông báo yêu cầu
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.ExecuteAsync(invoice, lines));
        Assert.Equal("Không thể tạo hóa đơn. Công nợ hiện tại và giá trị hóa đơn vượt quá hạn mức tín dụng của khách hàng.", ex.Message);
    }

    [Fact]
    public async Task CreateInvoice_WhenTotalWithinCreditLimit_CreatesSuccessfully()
    {
        // ARRANGE: Khách hàng có hạn mức 10,000,000 đ
        // Đang nợ: 5,000,000 đ
        var customer = new Customer
        {
            Id = 1,
            Code = "KH001",
            Name = "Khách Hàng Test",
            CreditLimit = 10000000m,
            IsActive = true
        };
        decimal currentDebt = 5000000m;

        var fakeCustRepo = new FakeCustomerRepository(customer, currentDebt);
        var fakeInvRepo = new FakeInvoiceRepository();
        var useCase = new CreateInvoiceUseCase(fakeInvRepo, fakeCustRepo);

        // Hóa đơn mới: 4,000,000 đ -> Tổng nợ = 5tr + 4tr = 9tr <= 10tr (Hợp lệ)
        var invoice = new Invoice
        {
            InvoiceNumber = "HD-TEST-02",
            CustomerId = 1,
            InvoiceDate = DateTime.Now,
            DueDate = DateTime.Now.AddDays(30)
        };
        var lines = new List<InvoiceLine>
        {
            new InvoiceLine { ProductId = 1, Quantity = 2, UnitPrice = 2000000m }
        };

        // ACT
        var resultId = await useCase.ExecuteAsync(invoice, lines);

        // ASSERT
        Assert.Equal(100, resultId);
        Assert.NotNull(fakeInvRepo.LastCreatedInvoice);
        Assert.Equal(4000000m, fakeInvRepo.LastCreatedInvoice.TotalAmount);
    }
}
