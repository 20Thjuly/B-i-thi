namespace SalesAR.UnitTests;

using Xunit;
using SalesAR.CoreBusiness.Models;
using SalesAR.UseCases.Invoices;

public class InvoiceValidationTests
{
    [Fact]
    public async Task CreateInvoice_WithoutInvoiceLines_ThrowsException()
    {
        // ARRANGE:
        var customer = new Customer { Id = 1, Code = "KH01", Name = "Khách Hàng", CreditLimit = 50000000m, IsActive = true };
        var fakeCustRepo = new FakeCustomerRepository(customer, 0m);
        var fakeInvRepo = new FakeInvoiceRepository();
        var useCase = new CreateInvoiceUseCase(fakeInvRepo, fakeCustRepo);

        var invoice = new Invoice { CustomerId = 1, InvoiceNumber = "HD-EMPTY" };

        // ACT & ASSERT: Hóa đơn không có dòng chi tiết phải bị từ chối
        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.ExecuteAsync(invoice, new List<InvoiceLine>()));
        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.ExecuteAsync(invoice, null!));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public async Task CreateInvoice_WithQuantityZeroOrNegative_ThrowsException(int invalidQuantity)
    {
        // ARRANGE:
        var customer = new Customer { Id = 1, Code = "KH01", Name = "Khách Hàng", CreditLimit = 50000000m, IsActive = true };
        var fakeCustRepo = new FakeCustomerRepository(customer, 0m);
        var fakeInvRepo = new FakeInvoiceRepository();
        var useCase = new CreateInvoiceUseCase(fakeInvRepo, fakeCustRepo);

        var invoice = new Invoice { CustomerId = 1, InvoiceNumber = "HD-BAD-QTY" };
        var lines = new List<InvoiceLine>
        {
            new InvoiceLine { ProductId = 1, Quantity = invalidQuantity, UnitPrice = 500000m }
        };

        // ACT & ASSERT: Số lượng <= 0 phải bị từ chối
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.ExecuteAsync(invoice, lines));
        Assert.Equal("Số lượng sản phẩm phải lớn hơn 0.", ex.Message);
    }
}
