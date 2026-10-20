namespace SalesAR.UnitTests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SalesAR.CoreBusiness.Models;
using SalesAR.CoreBusiness.Security;
using SalesAR.UseCases.CreditLimitRequests;
using SalesAR.UseCases.Invoices;
using SalesAR.UseCases.Payments;
using SalesAR.UseCases.PluginInterfaces;
using Xunit;

public class AuthorizationAndSecurityTests
{
    #region In-Memory Test Fakes

    private class FakeCreditLimitRequestRepository : ICreditLimitRequestRepository
    {
        public List<CreditLimitRequest> Requests { get; } = new();

        public Task<IEnumerable<CreditLimitRequest>> GetAllAsync() => Task.FromResult<IEnumerable<CreditLimitRequest>>(Requests);
        public Task<IEnumerable<CreditLimitRequest>> GetByCustomerIdAsync(int customerId) => Task.FromResult<IEnumerable<CreditLimitRequest>>(Requests.Where(r => r.CustomerId == customerId));
        public Task<IEnumerable<CreditLimitRequest>> GetPendingRequestsAsync() => Task.FromResult<IEnumerable<CreditLimitRequest>>(Requests.Where(r => r.Status == "Pending"));
        public Task<CreditLimitRequest?> GetByIdAsync(int id) => Task.FromResult(Requests.FirstOrDefault(r => r.Id == id));
        public Task<CreditLimitRequest?> GetByRequestNumberAsync(string number) => Task.FromResult(Requests.FirstOrDefault(r => r.RequestNumber == number));
        public Task<CreditLimitRequest?> GetValidApprovedRequestAsync(int customerId, decimal transactionAmount) =>
            Task.FromResult(Requests.FirstOrDefault(r => r.CustomerId == customerId && r.Status == "Approved" && !r.IsUsed));

        public Task<int> CreateRequestAsync(CreditLimitRequest request) { Requests.Add(request); return Task.FromResult(request.Id); }

        public Task ApproveRequestAsync(int requestId, string approver, string reason, decimal liveDebt, decimal liveLimit, TimeSpan validDuration)
        {
            var r = Requests.FirstOrDefault(x => x.Id == requestId);
            if (r != null)
            {
                r.Status = "Approved";
                r.ApprovedBy = approver;
                r.DecisionReason = reason;
                r.ApprovedAt = DateTime.UtcNow;
                r.ExpiresAt = DateTime.UtcNow.Add(validDuration);
            }
            return Task.CompletedTask;
        }

        public Task RejectRequestAsync(int requestId, string approver, string reason, decimal liveDebt, decimal liveLimit)
        {
            var r = Requests.FirstOrDefault(x => x.Id == requestId);
            if (r != null)
            {
                r.Status = "Rejected";
                r.ApprovedBy = approver;
                r.DecisionReason = reason;
                r.ApprovedAt = DateTime.UtcNow;
            }
            return Task.CompletedTask;
        }

        public Task CancelRequestAsync(int requestId, string actor, string reason)
        {
            var r = Requests.FirstOrDefault(x => x.Id == requestId);
            if (r != null) r.Status = "Cancelled";
            return Task.CompletedTask;
        }

        public Task MarkAsUsedAsync(int requestId, int invoiceId)
        {
            var r = Requests.FirstOrDefault(x => x.Id == requestId);
            if (r != null) r.IsUsed = true;
            return Task.CompletedTask;
        }

        public Task<IEnumerable<CreditLimitApproval>> GetApprovalHistoryAsync(int requestId) =>
            Task.FromResult<IEnumerable<CreditLimitApproval>>(new List<CreditLimitApproval>());
    }

    private class FakeCustomerRepository : ICustomerRepository
    {
        public List<Customer> Customers { get; } = new();

        public Task<IEnumerable<Customer>> GetAllAsync() => Task.FromResult<IEnumerable<Customer>>(Customers);
        public Task<Customer?> GetByIdAsync(int id) => Task.FromResult(Customers.FirstOrDefault(c => c.Id == id));
        public Task<Customer?> GetByCodeAsync(string code) => Task.FromResult(Customers.FirstOrDefault(c => c.Code == code));
        public Task<int> AddAsync(Customer customer) { Customers.Add(customer); return Task.FromResult(customer.Id); }
        public Task UpdateAsync(Customer customer) => Task.CompletedTask;
        public Task DeleteAsync(int id) => Task.CompletedTask;
        public Task<decimal> GetCurrentDebtAsync(int customerId) => Task.FromResult(20_000_000m);
        public Task<SalesAR.UseCases.Models.CustomerDebtDto?> GetCustomerDebtDetailsAsync(int customerId) => Task.FromResult<SalesAR.UseCases.Models.CustomerDebtDto?>(null);
        public Task UpdateCreditLimitAsync(int customerId, decimal newCreditLimit, string changedBy, string reason)
        {
            var c = Customers.FirstOrDefault(x => x.Id == customerId);
            if (c != null) c.CreditLimit = newCreditLimit;
            return Task.CompletedTask;
        }
    }

    #endregion

    [Fact]
    public async Task SalesRole_Forbidden_FromApprovingCreditLimitRequest()
    {
        // Arrange
        var reqRepo = new FakeCreditLimitRequestRepository();
        var custRepo = new FakeCustomerRepository();
        var useCase = new ApproveCreditLimitRequestUseCase(reqRepo, custRepo);

        // Act & Assert: Nhân viên SALES cố tình gọi use case duyệt hạn mức
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            useCase.ExecuteAsync(1, "sales_user", "SALES", "Tôi muốn duyệt đơn này"));

        Assert.Contains("CREDIT_MANAGER", ex.Message);
    }

    [Fact]
    public async Task AccountantRole_Forbidden_FromApprovingCreditLimitRequest()
    {
        // Arrange
        var reqRepo = new FakeCreditLimitRequestRepository();
        var custRepo = new FakeCustomerRepository();
        var useCase = new ApproveCreditLimitRequestUseCase(reqRepo, custRepo);

        // Act & Assert: Nhân viên ACCOUNTANT không có thẩm quyền duyệt hạn mức
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            useCase.ExecuteAsync(1, "accountant_user", "ACCOUNTANT", "Tôi duyệt"));

        Assert.Contains("CREDIT_MANAGER", ex.Message);
    }

    [Fact]
    public async Task CreditManagerRole_Allowed_ToApproveValidRequest()
    {
        // Arrange
        var reqRepo = new FakeCreditLimitRequestRepository();
        var custRepo = new FakeCustomerRepository();
        custRepo.Customers.Add(new Customer { Id = 10, Code = "KH10", Name = "Công ty Hải Nam", CreditLimit = 50_000_000m });

        reqRepo.Requests.Add(new CreditLimitRequest
        {
            Id = 1,
            CustomerId = 10,
            RequestedBy = "sales_user", // Người tạo khác người duyệt
            TransactionAmount = 80_000_000m,
            CurrentDebtSnapshot = 20_000_000m,
            CreditLimitSnapshot = 50_000_000m,
            Status = "Pending"
        });

        var useCase = new ApproveCreditLimitRequestUseCase(reqRepo, custRepo);

        // Act: CREDIT_MANAGER thực hiện phê duyệt
        await useCase.ExecuteAsync(1, "credit_manager", "CREDIT_MANAGER", "Đối tác thanh toán đúng hạn, duyệt tăng hạn mức tạm thời");

        // Assert: Trạng thái cập nhật thành Approved
        var updated = await reqRepo.GetByIdAsync(1);
        Assert.NotNull(updated);
        Assert.Equal("Approved", updated.Status);
        Assert.Equal("credit_manager", updated.ApprovedBy);
    }

    [Fact]
    public async Task AdminRole_Allowed_ToApproveValidRequest()
    {
        // Arrange
        var reqRepo = new FakeCreditLimitRequestRepository();
        var custRepo = new FakeCustomerRepository();
        custRepo.Customers.Add(new Customer { Id = 11, Code = "KH11", Name = "Công ty Toàn Cầu", CreditLimit = 100_000_000m });

        reqRepo.Requests.Add(new CreditLimitRequest
        {
            Id = 2,
            CustomerId = 11,
            RequestedBy = "sales_user",
            TransactionAmount = 150_000_000m,
            CurrentDebtSnapshot = 30_000_000m,
            CreditLimitSnapshot = 100_000_000m,
            Status = "Pending"
        });

        var useCase = new ApproveCreditLimitRequestUseCase(reqRepo, custRepo);

        // Act: ADMIN toàn quyền phê duyệt
        await useCase.ExecuteAsync(2, "admin_user", "ADMIN", "Ban Giám đốc duyệt hợp đồng lớn");

        var updated = await reqRepo.GetByIdAsync(2);
        Assert.NotNull(updated);
        Assert.Equal("Approved", updated.Status);
        Assert.Equal("admin_user", updated.ApprovedBy);
    }

    [Fact]
    public async Task SegregationOfDuties_CreatorCannotApproveOwnRequest()
    {
        // Arrange: Người tạo cố tình tự phê duyệt yêu cầu của mình
        var reqRepo = new FakeCreditLimitRequestRepository();
        var custRepo = new FakeCustomerRepository();
        custRepo.Customers.Add(new Customer { Id = 12, Code = "KH12", Name = "Công ty Tự Duyệt", CreditLimit = 50_000_000m });

        reqRepo.Requests.Add(new CreditLimitRequest
        {
            Id = 3,
            CustomerId = 12,
            RequestedBy = "manager_john",
            TransactionAmount = 70_000_000m,
            CurrentDebtSnapshot = 10_000_000m,
            CreditLimitSnapshot = 50_000_000m,
            Status = "Pending"
        });

        var useCase = new ApproveCreditLimitRequestUseCase(reqRepo, custRepo);

        // Act & Assert: Bị chặn bởi nguyên tắc Tách biệt nhiệm vụ (Segregation of Duties)
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.ExecuteAsync(3, "manager_john", "CREDIT_MANAGER", "Tự duyệt cho nhanh"));

        Assert.Contains("Segregation of Duties", ex.Message);
    }

    [Fact]
    public void PasswordHasher_ProtectsPasswords_WithPbkdf2SaltedHashes()
    {
        // Arrange
        var hasher = new PasswordHasher();
        string rawPassword = "SecurePassword@2026";

        // Act
        string hash1 = hasher.HashPassword(rawPassword);
        string hash2 = hasher.HashPassword(rawPassword);

        // Assert: 
        // 1. Mật khẩu không lưu ở dạng văn bản thô
        Assert.NotEqual(rawPassword, hash1);
        Assert.Contains(":", hash1); // Định dạng Salt:Hash

        // 2. Không tạo chuỗi hash cố định (mỗi lần hash sử dụng salt ngẫu nhiên khác nhau)
        Assert.NotEqual(hash1, hash2);

        // 3. Xác thực đúng với mật khẩu chuẩn
        Assert.True(hasher.VerifyPassword(rawPassword, hash1));
        Assert.True(hasher.VerifyPassword(rawPassword, hash2));

        // 4. Từ chối mật khẩu sai
        Assert.False(hasher.VerifyPassword("WrongPassword!123", hash1));
    }

    [Fact]
    public void AuditLogFilterDto_OffsetCalculation_IsSafeForPagination()
    {
        var filterPage1 = new AuditLogFilterDto { PageIndex = 1, PageSize = 20 };
        var filterPage2 = new AuditLogFilterDto { PageIndex = 2, PageSize = 20 };
        var filterInvalid = new AuditLogFilterDto { PageIndex = -1, PageSize = 10 };

        Assert.Equal(0, filterPage1.Offset);
        Assert.Equal(20, filterPage2.Offset);
        Assert.Equal(0, filterInvalid.Offset);
    }

    [Fact]
    public void PagedAuditLogsDto_CalculatesTotalPagesAccurately()
    {
        var paged1 = new PagedAuditLogsDto { TotalCount = 45, PageSize = 20 };
        var paged2 = new PagedAuditLogsDto { TotalCount = 0, PageSize = 20 };
        var paged3 = new PagedAuditLogsDto { TotalCount = 20, PageSize = 20 };

        Assert.Equal(3, paged1.TotalPages);
        Assert.Equal(0, paged2.TotalPages);
        Assert.Equal(1, paged3.TotalPages);
    }

    [Fact]
    public async Task InputValidation_CreateInvoice_RejectsNegativeAmountAndZeroLines()
    {
        // Arrange
        var invRepo = new FakeInvoiceRepository();
        var custRepo = new FakeCustomerRepository();
        custRepo.Customers.Add(new Customer { Id = 1, Code = "KH01", Name = "A", IsActive = true, CreditLimit = 100_000_000m });
        var useCase = new CreateInvoiceUseCase(invRepo, custRepo);

        // Act & Assert 1: Hóa đơn rỗng không có dòng sản phẩm
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.ExecuteAsync(new Invoice { CustomerId = 1, InvoiceNumber = "HD-999" }, new List<InvoiceLine>()));

        // Act & Assert 2: Dòng sản phẩm có số lượng <= 0
        var invalidLines = new List<InvoiceLine>
        {
            new() { ProductId = 1, Quantity = -5, UnitPrice = 100_000m }
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.ExecuteAsync(new Invoice { CustomerId = 1, InvoiceNumber = "HD-999" }, invalidLines));
    }

    [Fact]
    public async Task InputValidation_RecordPayment_RejectsNegativeOrZeroAmount()
    {
        // Arrange
        var payRepo = new FakePaymentRepository();
        var invRepo = new FakeInvoiceRepository();
        var custRepo = new FakeCustomerRepository();
        var allocUseCase = new FakeAllocatePaymentUseCase();
        var useCase = new RecordPaymentUseCase(payRepo, invRepo, custRepo, allocUseCase);

        // Act & Assert: Phiếu thu có số tiền âm hoặc bằng 0
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.ExecuteAsync(new Payment { CustomerId = 1, PaymentNumber = "PT-001", Amount = -50_000m }));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.ExecuteAsync(new Payment { CustomerId = 1, PaymentNumber = "PT-001", Amount = 0m }));
    }

    #region Helper Mock
    private class FakeInvoiceRepository : IInvoiceRepository
    {
        public Task<IEnumerable<Invoice>> GetAllAsync() => Task.FromResult<IEnumerable<Invoice>>(new List<Invoice>());
        public Task<Invoice?> GetByIdAsync(int id) => Task.FromResult<Invoice?>(null);
        public Task<Invoice?> GetByInvoiceNumberAsync(string invoiceNumber) => Task.FromResult<Invoice?>(null);
        public Task<IEnumerable<InvoiceLine>> GetLinesByInvoiceIdAsync(int invoiceId) => Task.FromResult<IEnumerable<InvoiceLine>>(new List<InvoiceLine>());
        public Task<IEnumerable<Invoice>> GetInvoicesByCustomerAsync(int customerId) => Task.FromResult<IEnumerable<Invoice>>(new List<Invoice>());
        public Task<IEnumerable<Invoice>> GetUnpaidInvoicesByCustomerAsync(int customerId) => Task.FromResult<IEnumerable<Invoice>>(new List<Invoice>());
        public Task<IEnumerable<SalesAR.UseCases.Models.InvoiceOutstandingDto>> GetOutstandingInvoicesByCustomerAsync(int customerId) =>
            Task.FromResult<IEnumerable<SalesAR.UseCases.Models.InvoiceOutstandingDto>>(new List<SalesAR.UseCases.Models.InvoiceOutstandingDto>());
        public Task<int> CreateInvoiceWithLinesAsync(Invoice invoice, IEnumerable<InvoiceLine> lines) => Task.FromResult(1);
        public Task UpdateStatusAsync(int invoiceId, string status) => Task.CompletedTask;
    }

    private class FakePaymentRepository : IPaymentRepository
    {
        public Task<IEnumerable<Payment>> GetAllAsync() => Task.FromResult<IEnumerable<Payment>>(new List<Payment>());
        public Task<Payment?> GetByIdAsync(int id) => Task.FromResult<Payment?>(null);
        public Task<Payment?> GetByPaymentNumberAsync(string paymentNumber) => Task.FromResult<Payment?>(null);
        public Task<IEnumerable<Payment>> GetPaymentsByCustomerAsync(int customerId) => Task.FromResult<IEnumerable<Payment>>(new List<Payment>());
        public Task<IEnumerable<PaymentAllocation>> GetAllocationsByPaymentIdAsync(int paymentId) => Task.FromResult<IEnumerable<PaymentAllocation>>(new List<PaymentAllocation>());
        public Task<IEnumerable<PaymentAllocation>> GetAllocationsByCustomerAsync(int customerId) => Task.FromResult<IEnumerable<PaymentAllocation>>(new List<PaymentAllocation>());
        public Task<int> CreatePaymentWithAllocationsAsync(Payment payment, IEnumerable<PaymentAllocation> allocations) => Task.FromResult(1);
        public Task ReallocatePaymentAsync(int paymentId, IEnumerable<PaymentAllocation> newAllocations, string performedBy, string reason) => Task.CompletedTask;
    }

    private class FakeAllocatePaymentUseCase : IAllocatePaymentUseCase
    {
        public List<PaymentAllocation> Execute(int paymentId, decimal paymentAmount, IEnumerable<SalesAR.UseCases.Models.InvoiceOutstandingDto> unpaidInvoices) => new();
    }
    #endregion
}
