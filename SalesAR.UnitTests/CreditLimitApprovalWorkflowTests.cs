namespace SalesAR.UnitTests;

using Xunit;
using SalesAR.CoreBusiness.Models;
using SalesAR.UseCases.CreditLimitRequests;
using SalesAR.UseCases.Invoices;
using SalesAR.UseCases.Models;
using SalesAR.UseCases.PluginInterfaces;

public class FakeCreditLimitRequestRepository : ICreditLimitRequestRepository
{
    public List<CreditLimitRequest> Requests { get; } = new();
    public List<CreditLimitApproval> Approvals { get; } = new();

    public Task<IEnumerable<CreditLimitRequest>> GetAllAsync() => Task.FromResult<IEnumerable<CreditLimitRequest>>(Requests);
    public Task<IEnumerable<CreditLimitRequest>> GetByCustomerIdAsync(int customerId) => Task.FromResult(Requests.Where(r => r.CustomerId == customerId));
    public Task<IEnumerable<CreditLimitRequest>> GetPendingRequestsAsync() => Task.FromResult(Requests.Where(r => r.Status == "Pending"));
    public Task<CreditLimitRequest?> GetByIdAsync(int id) => Task.FromResult(Requests.FirstOrDefault(r => r.Id == id));
    public Task<CreditLimitRequest?> GetByRequestNumberAsync(string requestNumber) => Task.FromResult(Requests.FirstOrDefault(r => r.RequestNumber == requestNumber));

    public Task<CreditLimitRequest?> GetValidApprovedRequestAsync(int customerId, decimal transactionAmount)
    {
        var req = Requests.FirstOrDefault(r => 
            r.CustomerId == customerId && 
            r.Status == "Approved" && 
            !r.IsUsed && 
            (!r.ExpiresAt.HasValue || r.ExpiresAt.Value > DateTime.UtcNow) && 
            r.TransactionAmount >= transactionAmount);
        return Task.FromResult(req);
    }

    public Task<int> CreateRequestAsync(CreditLimitRequest request)
    {
        request.Id = Requests.Count + 1;
        Requests.Add(request);
        Approvals.Add(new CreditLimitApproval
        {
            Id = Approvals.Count + 1,
            RequestId = request.Id,
            Action = "SUBMIT",
            Actor = request.RequestedBy,
            CurrentDebtAtDecision = request.CurrentDebtSnapshot,
            CreditLimitAtDecision = request.CreditLimitSnapshot
        });
        return Task.FromResult(request.Id);
    }

    public Task ApproveRequestAsync(int requestId, string approver, string reason, decimal liveDebt, decimal liveLimit, TimeSpan validDuration)
    {
        var req = Requests.First(r => r.Id == requestId);
        req.Status = "Approved";
        req.ApprovedBy = approver;
        req.ApprovedAt = DateTime.UtcNow;
        req.DecisionReason = reason;
        req.ExpiresAt = DateTime.UtcNow.Add(validDuration);

        Approvals.Add(new CreditLimitApproval
        {
            Id = Approvals.Count + 1,
            RequestId = requestId,
            Action = "APPROVE",
            Actor = approver,
            Comment = reason,
            CurrentDebtAtDecision = liveDebt,
            CreditLimitAtDecision = liveLimit
        });
        return Task.CompletedTask;
    }

    public Task RejectRequestAsync(int requestId, string approver, string reason, decimal liveDebt, decimal liveLimit)
    {
        var req = Requests.First(r => r.Id == requestId);
        req.Status = "Rejected";
        req.ApprovedBy = approver;
        req.ApprovedAt = DateTime.UtcNow;
        req.DecisionReason = reason;

        Approvals.Add(new CreditLimitApproval
        {
            Id = Approvals.Count + 1,
            RequestId = requestId,
            Action = "REJECT",
            Actor = approver,
            Comment = reason,
            CurrentDebtAtDecision = liveDebt,
            CreditLimitAtDecision = liveLimit
        });
        return Task.CompletedTask;
    }

    public Task CancelRequestAsync(int requestId, string actor, string reason)
    {
        var req = Requests.First(r => r.Id == requestId);
        req.Status = "Cancelled";
        return Task.CompletedTask;
    }

    public Task MarkAsUsedAsync(int requestId, int invoiceId)
    {
        var req = Requests.First(r => r.Id == requestId);
        if (req.IsUsed)
            throw new InvalidOperationException("Yêu cầu duyệt đã được sử dụng trước đó.");
        req.IsUsed = true;
        req.UsedInvoiceId = invoiceId;
        req.UsedAt = DateTime.UtcNow;
        return Task.CompletedTask;
    }

    public Task<IEnumerable<CreditLimitApproval>> GetApprovalHistoryAsync(int requestId)
    {
        return Task.FromResult<IEnumerable<CreditLimitApproval>>(Approvals.Where(a => a.RequestId == requestId));
    }
}

public class CreditLimitApprovalWorkflowTests
{
    [Fact]
    public async Task CreateRequest_Succeeds_WhenRoleIsSales()
    {
        var repo = new FakeCreditLimitRequestRepository();
        var custRepo = new FakeCustomerRepository(new Customer { Id = 1, Name = "Công ty ABC", CreditLimit = 100000000m, IsActive = true }, 80000000m);
        var useCase = new CreateCreditLimitRequestUseCase(repo, custRepo);

        int id = await useCase.ExecuteAsync(1, 50000000m, "Dự án mới đợt 2", "salesman1", "SALES");

        Assert.True(id > 0);
        Assert.Single(repo.Requests);
        Assert.Equal("Pending", repo.Requests[0].Status);
        Assert.Equal(80000000m, repo.Requests[0].CurrentDebtSnapshot);
    }

    [Fact]
    public async Task CreateRequest_ThrowsException_WhenRoleIsNotSalesOrAdmin()
    {
        var repo = new FakeCreditLimitRequestRepository();
        var custRepo = new FakeCustomerRepository(new Customer { Id = 1, Name = "ABC", CreditLimit = 100000000m, IsActive = true }, 80000000m);
        var useCase = new CreateCreditLimitRequestUseCase(repo, custRepo);

        // Kế toán không được lập yêu cầu duyệt bán hàng vượt hạn mức
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => 
            useCase.ExecuteAsync(1, 50000000m, "Lý do", "accuser", "ACCOUNTANT"));
    }

    [Fact]
    public async Task ApproveRequest_ThrowsException_WhenApproverIsTheCreator_SegregationOfDuties()
    {
        var repo = new FakeCreditLimitRequestRepository();
        var custRepo = new FakeCustomerRepository(new Customer { Id = 1, CreditLimit = 100000000m, IsActive = true }, 80000000m);
        
        // Yêu cầu do 'salesman_john' tạo
        repo.Requests.Add(new CreditLimitRequest
        {
            Id = 1,
            CustomerId = 1,
            RequestedBy = "salesman_john",
            Status = "Pending",
            TransactionAmount = 50000000m
        });

        var useCase = new ApproveCreditLimitRequestUseCase(repo, custRepo);

        // 'salesman_john' cố tình tự duyệt yêu cầu của mình -> Phải bị chặn theo nguyên tắc tách nhiệm vụ
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            useCase.ExecuteAsync(1, "salesman_john", "CREDIT_MANAGER", "Tự duyệt"));

        Assert.Contains("tách biệt nhiệm vụ", ex.Message);
    }

    [Fact]
    public async Task ApproveRequest_ThrowsException_WhenRoleIsNotCreditManagerOrAdmin()
    {
        var repo = new FakeCreditLimitRequestRepository();
        var custRepo = new FakeCustomerRepository(new Customer { Id = 1, CreditLimit = 100000000m, IsActive = true }, 80000000m);
        
        repo.Requests.Add(new CreditLimitRequest
        {
            Id = 1,
            CustomerId = 1,
            RequestedBy = "sales1",
            Status = "Pending",
            TransactionAmount = 50000000m
        });

        var useCase = new ApproveCreditLimitRequestUseCase(repo, custRepo);

        // Người dùng có vai trò SALES cố tình duyệt -> Bị chặn phân quyền
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => 
            useCase.ExecuteAsync(1, "sales2", "SALES", "Duyệt hộ"));
    }

    [Fact]
    public async Task ApproveRequest_Succeeds_AndRechecksLiveDebt_NotOldSnapshot()
    {
        var repo = new FakeCreditLimitRequestRepository();
        // Snapshot lúc tạo nợ là 80tr, nhưng hiện tại nợ thực tế trong DB đã tăng lên 95tr
        var custRepo = new FakeCustomerRepository(new Customer { Id = 1, CreditLimit = 100000000m, IsActive = true }, 95000000m);
        
        repo.Requests.Add(new CreditLimitRequest
        {
            Id = 10,
            CustomerId = 1,
            RequestedBy = "sales1",
            Status = "Pending",
            CurrentDebtSnapshot = 80000000m,
            CreditLimitSnapshot = 100000000m,
            TransactionAmount = 30000000m
        });

        var useCase = new ApproveCreditLimitRequestUseCase(repo, custRepo);
        await useCase.ExecuteAsync(10, "credit_boss", "CREDIT_MANAGER", "Chấp thuận dựa trên dư nợ mới nhất");

        var req = repo.Requests.First(r => r.Id == 10);
        Assert.Equal("Approved", req.Status);
        Assert.Equal("credit_boss", req.ApprovedBy);

        // Kiểm tra lịch sử quyết định lưu dư nợ thực tế lúc duyệt (95tr), KHÔNG phải snapshot cũ (80tr)
        var approvalEvent = repo.Approvals.First(a => a.RequestId == 10 && a.Action == "APPROVE");
        Assert.Equal(95000000m, approvalEvent.CurrentDebtAtDecision);
    }

    [Fact]
    public async Task RejectRequest_Succeeds_WhenAuthorized()
    {
        var repo = new FakeCreditLimitRequestRepository();
        var custRepo = new FakeCustomerRepository(new Customer { Id = 1, CreditLimit = 100000000m, IsActive = true }, 50000000m);
        
        repo.Requests.Add(new CreditLimitRequest
        {
            Id = 20,
            CustomerId = 1,
            RequestedBy = "sales1",
            Status = "Pending",
            TransactionAmount = 80000000m
        });

        var useCase = new RejectCreditLimitRequestUseCase(repo, custRepo);
        await useCase.ExecuteAsync(20, "credit_boss", "CREDIT_MANAGER", "Khách hàng có lịch sử trễ nợ quá 60 ngày");

        var req = repo.Requests.First(r => r.Id == 20);
        Assert.Equal("Rejected", req.Status);
        Assert.Equal("Khách hàng có lịch sử trễ nợ quá 60 ngày", req.DecisionReason);
    }

    [Fact]
    public async Task CreateInvoice_AllowsExceedingLimit_WhenValidApprovalExists_AndMarksApprovalUsed()
    {
        var reqRepo = new FakeCreditLimitRequestRepository();
        var invRepo = new FakeInvoiceRepository();
        // Khách hàng nợ 90tr / hạn mức 100tr. Hóa đơn mới 40tr -> Tổng 130tr > 100tr
        var custRepo = new FakeCustomerRepository(new Customer { Id = 1, CreditLimit = 100000000m, IsActive = true }, 90000000m);

        // Đã có phiếu duyệt hợp lệ trị giá 50tr
        reqRepo.Requests.Add(new CreditLimitRequest
        {
            Id = 1,
            CustomerId = 1,
            Status = "Approved",
            TransactionAmount = 50000000m,
            IsUsed = false,
            ExpiresAt = DateTime.UtcNow.AddDays(2)
        });

        var createInvoiceUseCase = new CreateInvoiceUseCase(invRepo, custRepo, reqRepo);

        var invoice = new Invoice { CustomerId = 1, InvoiceNumber = "INV-APPROVED-01" };
        var lines = new List<InvoiceLine> { new InvoiceLine { ProductId = 1, Quantity = 2, UnitPrice = 20000000m, Amount = 40000000m } };

        int newInvId = await createInvoiceUseCase.ExecuteAsync(invoice, lines);

        Assert.True(newInvId > 0);
        // Kiểm tra phiếu duyệt đã bị đánh dấu đã sử dụng (chống tái sử dụng approval)
        Assert.True(reqRepo.Requests[0].IsUsed);
        Assert.Equal(newInvId, reqRepo.Requests[0].UsedInvoiceId);
    }

    [Fact]
    public async Task CreateInvoice_RejectsSecondUseOfSameApproval_AntiReplay()
    {
        var reqRepo = new FakeCreditLimitRequestRepository();
        var invRepo = new FakeInvoiceRepository();
        var custRepo = new FakeCustomerRepository(new Customer { Id = 1, CreditLimit = 100000000m, IsActive = true }, 90000000m);

        // Phiếu duyệt ĐÃ BỊ SỬ DỤNG (IsUsed = true)
        reqRepo.Requests.Add(new CreditLimitRequest
        {
            Id = 1,
            CustomerId = 1,
            Status = "Approved",
            TransactionAmount = 50000000m,
            IsUsed = true, // Đã dùng rồi
            ExpiresAt = DateTime.UtcNow.AddDays(2)
        });

        var createInvoiceUseCase = new CreateInvoiceUseCase(invRepo, custRepo, reqRepo);

        var invoice = new Invoice { CustomerId = 1, InvoiceNumber = "INV-REPLAY" };
        var lines = new List<InvoiceLine> { new InvoiceLine { ProductId = 1, Quantity = 1, UnitPrice = 30000000m, Amount = 30000000m } };

        // Cố tình dùng lại approval đã qua sử dụng -> Phải bị từ chối
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            createInvoiceUseCase.ExecuteAsync(invoice, lines, approvalRequestId: 1));

        Assert.Contains("vượt quá hạn mức tín dụng", ex.Message);
    }
}
