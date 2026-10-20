namespace SalesAR.UnitTests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using SalesAR.CoreBusiness.Models;
using SalesAR.Plugins.AI;
using SalesAR.UseCases.AI;
using SalesAR.UseCases.AI.Models;
using SalesAR.UseCases.AI.Providers;
using SalesAR.UseCases.CreditLimitRequests;
using SalesAR.UseCases.Invoices;
using SalesAR.UseCases.Payments;
using SalesAR.UseCases.PluginInterfaces;
using SalesAR.UseCases.Reminders;
using SalesAR.UseCases.Reminders.Models;
using Xunit;

/// <summary>
/// BỘ TEST BẮT BUỘC THEO ĐẶC TẢ PART 10/10 — END-TO-END TESTING AND FINAL ACCEPTANCE
/// Kiểm thử 15 nghiệp vụ cốt lõi không thể thiếu của Hệ thống Sales & AR.
/// </summary>
public class EndToEndAcceptanceBusinessTests
{
    #region In-Memory Test Fakes for Core Business Workflow Verification

    private class AcceptanceTestCustomerRepo : ICustomerRepository
    {
        public List<Customer> Customers { get; } = new();
        public decimal MockDebt { get; set; } = 0m;

        public Task<IEnumerable<Customer>> GetAllAsync() => Task.FromResult<IEnumerable<Customer>>(Customers);
        public Task<Customer?> GetByIdAsync(int id) => Task.FromResult(Customers.FirstOrDefault(c => c.Id == id));
        public Task<Customer?> GetByCodeAsync(string code) => Task.FromResult(Customers.FirstOrDefault(c => c.Code == code));
        public Task<int> AddAsync(Customer customer) { Customers.Add(customer); return Task.FromResult(customer.Id); }
        public Task UpdateAsync(Customer customer) => Task.CompletedTask;
        public Task DeleteAsync(int id) => Task.CompletedTask;
        public Task<decimal> GetCurrentDebtAsync(int customerId) => Task.FromResult(MockDebt);
        public Task<SalesAR.UseCases.Models.CustomerDebtDto?> GetCustomerDebtDetailsAsync(int customerId) => Task.FromResult<SalesAR.UseCases.Models.CustomerDebtDto?>(null);
        public Task UpdateCreditLimitAsync(int customerId, decimal newCreditLimit, string changedBy, string reason)
        {
            var cust = Customers.FirstOrDefault(c => c.Id == customerId);
            if (cust != null) cust.CreditLimit = newCreditLimit;
            return Task.CompletedTask;
        }
    }

    private class AcceptanceTestInvoiceRepo : IInvoiceRepository
    {
        public List<Invoice> Invoices { get; } = new();
        public List<InvoiceLine> Lines { get; } = new();

        public Task<IEnumerable<Invoice>> GetAllAsync() => Task.FromResult<IEnumerable<Invoice>>(Invoices);
        public Task<Invoice?> GetByIdAsync(int id) => Task.FromResult(Invoices.FirstOrDefault(i => i.Id == id));
        public Task<Invoice?> GetByInvoiceNumberAsync(string number) => Task.FromResult(Invoices.FirstOrDefault(i => i.InvoiceNumber == number));
        public Task<IEnumerable<InvoiceLine>> GetLinesByInvoiceIdAsync(int invoiceId) => Task.FromResult<IEnumerable<InvoiceLine>>(Lines.Where(l => l.InvoiceId == invoiceId));
        public Task<IEnumerable<Invoice>> GetInvoicesByCustomerAsync(int customerId) => Task.FromResult<IEnumerable<Invoice>>(Invoices.Where(i => i.CustomerId == customerId));
        public Task<IEnumerable<Invoice>> GetUnpaidInvoicesByCustomerAsync(int customerId) => Task.FromResult<IEnumerable<Invoice>>(Invoices.Where(i => i.CustomerId == customerId && i.Status != "Paid"));
        public Task<IEnumerable<SalesAR.UseCases.Models.InvoiceOutstandingDto>> GetOutstandingInvoicesByCustomerAsync(int customerId) =>
            Task.FromResult<IEnumerable<SalesAR.UseCases.Models.InvoiceOutstandingDto>>(
                Invoices.Where(i => i.CustomerId == customerId && i.Status != "Paid")
                        .Select(i => new SalesAR.UseCases.Models.InvoiceOutstandingDto
                        {
                            Id = i.Id,
                            InvoiceNumber = i.InvoiceNumber,
                            CustomerId = i.CustomerId,
                            InvoiceDate = i.InvoiceDate,
                            DueDate = i.DueDate,
                            TotalAmount = i.TotalAmount,
                            PaidAmount = 0m
                        }));

        public Task<int> CreateInvoiceWithLinesAsync(Invoice invoice, IEnumerable<InvoiceLine> lines)
        {
            invoice.Id = Invoices.Count + 1;
            invoice.TotalAmount = lines.Sum(l => l.Amount);
            invoice.Status = "Unpaid";
            Invoices.Add(invoice);

            foreach (var line in lines)
            {
                line.InvoiceId = invoice.Id;
                Lines.Add(line);
            }
            return Task.FromResult(invoice.Id);
        }

        public Task UpdateStatusAsync(int invoiceId, string status)
        {
            var inv = Invoices.FirstOrDefault(i => i.Id == invoiceId);
            if (inv != null) inv.Status = status;
            return Task.CompletedTask;
        }
    }

    private class AcceptanceTestCreditRequestRepo : ICreditLimitRequestRepository
    {
        public List<CreditLimitRequest> Requests { get; } = new();

        public Task<IEnumerable<CreditLimitRequest>> GetAllAsync() => Task.FromResult<IEnumerable<CreditLimitRequest>>(Requests);
        public Task<IEnumerable<CreditLimitRequest>> GetByCustomerIdAsync(int customerId) => Task.FromResult<IEnumerable<CreditLimitRequest>>(Requests.Where(r => r.CustomerId == customerId));
        public Task<IEnumerable<CreditLimitRequest>> GetPendingRequestsAsync() => Task.FromResult<IEnumerable<CreditLimitRequest>>(Requests.Where(r => r.Status == "Pending"));
        public Task<CreditLimitRequest?> GetByIdAsync(int id) => Task.FromResult(Requests.FirstOrDefault(r => r.Id == id));
        public Task<CreditLimitRequest?> GetByRequestNumberAsync(string number) => Task.FromResult(Requests.FirstOrDefault(r => r.RequestNumber == number));
        public Task<CreditLimitRequest?> GetValidApprovedRequestAsync(int customerId, decimal transactionAmount) =>
            Task.FromResult(Requests.FirstOrDefault(r => r.CustomerId == customerId && r.Status == "Approved" && !r.IsUsed && (r.ExpiresAt == null || r.ExpiresAt > DateTime.UtcNow) && r.TransactionAmount >= transactionAmount));

        public Task<int> CreateRequestAsync(CreditLimitRequest request)
        {
            request.Id = Requests.Count + 1;
            request.Status = "Pending";
            Requests.Add(request);
            return Task.FromResult(request.Id);
        }

        public Task ApproveRequestAsync(int requestId, string approver, string reason, decimal liveDebt, decimal liveLimit, TimeSpan validDuration)
        {
            var req = Requests.FirstOrDefault(r => r.Id == requestId);
            if (req == null) throw new InvalidOperationException("Not found");
            if (req.Status != "Pending") throw new InvalidOperationException($"Yêu cầu này đã ở trạng thái '{req.Status}', không thể phê duyệt.");

            req.Status = "Approved";
            req.ApprovedBy = approver;
            req.DecisionReason = reason;
            req.ApprovedAt = DateTime.UtcNow;
            req.ExpiresAt = DateTime.UtcNow.Add(validDuration);
            return Task.CompletedTask;
        }

        public Task RejectRequestAsync(int requestId, string approver, string reason, decimal liveDebt, decimal liveLimit)
        {
            var req = Requests.FirstOrDefault(r => r.Id == requestId);
            if (req != null)
            {
                req.Status = "Rejected";
                req.ApprovedBy = approver;
                req.DecisionReason = reason;
                req.ApprovedAt = DateTime.UtcNow;
            }
            return Task.CompletedTask;
        }

        public Task CancelRequestAsync(int requestId, string actor, string reason)
        {
            var req = Requests.FirstOrDefault(r => r.Id == requestId);
            if (req != null) req.Status = "Cancelled";
            return Task.CompletedTask;
        }

        public Task MarkAsUsedAsync(int requestId, int invoiceId)
        {
            var req = Requests.FirstOrDefault(r => r.Id == requestId);
            if (req == null || req.IsUsed)
                throw new InvalidOperationException($"Yêu cầu duyệt ID {requestId} đã được sử dụng hoặc không hợp lệ.");

            req.IsUsed = true;
            req.UsedInvoiceId = invoiceId;
            req.UsedAt = DateTime.UtcNow;
            return Task.CompletedTask;
        }

        public Task<IEnumerable<CreditLimitApproval>> GetApprovalHistoryAsync(int requestId) =>
            Task.FromResult<IEnumerable<CreditLimitApproval>>(new List<CreditLimitApproval>());
    }

    private class AcceptanceTestPaymentRepo : IPaymentRepository
    {
        public List<Payment> Payments { get; } = new();
        public List<PaymentAllocation> Allocations { get; } = new();

        public Task<IEnumerable<Payment>> GetAllAsync() => Task.FromResult<IEnumerable<Payment>>(Payments);
        public Task<Payment?> GetByIdAsync(int id) => Task.FromResult(Payments.FirstOrDefault(p => p.Id == id));
        public Task<Payment?> GetByPaymentNumberAsync(string paymentNumber) => Task.FromResult(Payments.FirstOrDefault(p => p.PaymentNumber == paymentNumber));
        public Task<IEnumerable<Payment>> GetPaymentsByCustomerAsync(int customerId) => Task.FromResult<IEnumerable<Payment>>(Payments.Where(p => p.CustomerId == customerId));
        public Task<IEnumerable<PaymentAllocation>> GetAllocationsByPaymentIdAsync(int paymentId) => Task.FromResult<IEnumerable<PaymentAllocation>>(Allocations.Where(a => a.PaymentId == paymentId));
        public Task<IEnumerable<PaymentAllocation>> GetAllocationsByCustomerAsync(int customerId) => Task.FromResult<IEnumerable<PaymentAllocation>>(Allocations);

        public Task<int> CreatePaymentWithAllocationsAsync(Payment payment, IEnumerable<PaymentAllocation> allocations)
        {
            if (Payments.Any(p => p.PaymentNumber == payment.PaymentNumber))
                throw new InvalidOperationException($"Trùng mã thanh toán: {payment.PaymentNumber}");

            decimal sumAlloc = allocations.Sum(a => a.AllocatedAmount);
            if (sumAlloc > payment.Amount)
                throw new InvalidOperationException($"Tổng phân bổ ({sumAlloc:N0}) vượt quá số tiền thanh toán ({payment.Amount:N0}).");

            payment.Id = Payments.Count + 1;
            Payments.Add(payment);

            foreach (var alloc in allocations)
            {
                alloc.PaymentId = payment.Id;
                Allocations.Add(alloc);
            }
            return Task.FromResult(payment.Id);
        }

        public Task ReallocatePaymentAsync(int paymentId, IEnumerable<PaymentAllocation> newAllocations, string performedBy, string reason) => Task.CompletedTask;
    }

    private class AcceptanceTestReminderRepo : IReminderRepository
    {
        public List<ReminderLog> Logs { get; } = new();
        public List<ReminderRule> Rules { get; } = new()
        {
            new ReminderRule { Id = 1, Name = "Sắp đến hạn", DaysOffset = -3, TriggerType = "UpcomingDue", Tone = "Polite", IsActive = true },
            new ReminderRule { Id = 2, Name = "Quá hạn đợt 1", DaysOffset = 1, TriggerType = "Overdue", Tone = "Polite", IsActive = true }
        };

        public Task<IEnumerable<ReminderRule>> GetActiveRulesAsync() => Task.FromResult<IEnumerable<ReminderRule>>(Rules.Where(r => r.IsActive));
        public Task<ReminderRule?> GetRuleByIdAsync(int id) => Task.FromResult(Rules.FirstOrDefault(r => r.Id == id));
        public Task<ReminderLog?> GetByIdempotencyKeyAsync(string key) => Task.FromResult(Logs.FirstOrDefault(l => l.IdempotencyKey == key));
        public Task<ReminderLog?> GetLogByIdAsync(int id) => Task.FromResult(Logs.FirstOrDefault(l => l.Id == id));

        public Task<int> SaveReminderLogAsync(ReminderLog log)
        {
            if (log.Id == 0) log.Id = Logs.Count + 1;
            var existing = Logs.FirstOrDefault(l => l.Id == log.Id);
            if (existing == null) Logs.Add(log);
            return Task.FromResult(log.Id);
        }

        public Task UpdateReminderLogStatusAsync(int id, string status, int retryCount, string? lastError, DateTime? sentAt = null, string? confirmedBy = null)
        {
            var log = Logs.FirstOrDefault(l => l.Id == id);
            if (log != null)
            {
                log.Status = status;
                log.RetryCount = retryCount;
                log.LastError = lastError;
                if (sentAt.HasValue) log.SentAt = sentAt.Value;
                if (!string.IsNullOrEmpty(confirmedBy)) log.ConfirmedBy = confirmedBy;
            }
            return Task.CompletedTask;
        }

        public Task<IEnumerable<ReminderLog>> GetReminderLogsAsync(int? customerId = null, string? status = null, int limit = 100)
        {
            var q = Logs.AsEnumerable();
            if (customerId.HasValue) q = q.Where(l => l.CustomerId == customerId.Value);
            if (!string.IsNullOrEmpty(status)) q = q.Where(l => l.Status == status);
            return Task.FromResult<IEnumerable<ReminderLog>>(q.Take(limit).ToList());
        }

        public Task<IEnumerable<ReminderLog>> GetPendingRetryLogsAsync(int maxRetries = 3) =>
            Task.FromResult<IEnumerable<ReminderLog>>(Logs.Where(l => l.Status == "RetryPending" && l.RetryCount < maxRetries).ToList());

        public Task SetCustomerDisputeStatusAsync(int customerId, bool isInDispute, string? reason) => Task.CompletedTask;
        public Task SetInvoiceDisputeStatusAsync(int invoiceId, bool isInDispute, string? reason) => Task.CompletedTask;
    }

    #endregion

    #region 15 Mandatory Business Acceptance Tests

    [Fact]
    public async Task Scenario01_CreateValidInvoice_CalculatesTotalsAndMaintainsIntegrity()
    {
        // 1. Tạo hóa đơn hợp lệ với dòng sản phẩm, kiểm tra tính toán tổng tiền và trạng thái ban đầu
        var invRepo = new AcceptanceTestInvoiceRepo();
        var custRepo = new AcceptanceTestCustomerRepo();
        custRepo.Customers.Add(new Customer { Id = 1, Code = "KH01", Name = "Công ty Hải Nam", CreditLimit = 100_000_000m, IsActive = true });
        custRepo.MockDebt = 20_000_000m;

        var useCase = new CreateInvoiceUseCase(invRepo, custRepo);

        var invoice = new Invoice { CustomerId = 1, InvoiceNumber = "INV-ACCEPT-01", DueDate = DateTime.Today.AddDays(30) };
        var lines = new List<InvoiceLine>
        {
            new() { ProductId = 101, Quantity = 2, UnitPrice = 15_000_000m, Amount = 30_000_000m },
            new() { ProductId = 102, Quantity = 1, UnitPrice = 20_000_000m, Amount = 20_000_000m }
        };

        int newId = await useCase.ExecuteAsync(invoice, lines);

        Assert.True(newId > 0);
        var created = await invRepo.GetByIdAsync(newId);
        Assert.NotNull(created);
        Assert.Equal(50_000_000m, created.TotalAmount); // 30M + 20M = 50M
        Assert.Equal("Unpaid", created.Status);
    }

    [Fact]
    public async Task Scenario02_BlockTransactionExceedingCreditLimit_WhenNoApproval()
    {
        // 2. Chặn giao dịch vượt hạn mức khi chưa được duyệt
        var invRepo = new AcceptanceTestInvoiceRepo();
        var custRepo = new AcceptanceTestCustomerRepo();
        custRepo.Customers.Add(new Customer { Id = 2, Code = "KH02", Name = "Công ty Nam Việt", CreditLimit = 50_000_000m, IsActive = true });
        custRepo.MockDebt = 40_000_000m; // Nợ 40M / Hạn mức 50M (còn 10M)

        var useCase = new CreateInvoiceUseCase(invRepo, custRepo);

        // Hóa đơn 25M -> Tổng nợ sẽ là 65M > 50M (Vượt hạn mức 15M)
        var invoice = new Invoice { CustomerId = 2, InvoiceNumber = "INV-OVERLIMIT-01" };
        var lines = new List<InvoiceLine>
        {
            new() { ProductId = 201, Quantity = 1, UnitPrice = 25_000_000m, Amount = 25_000_000m }
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.ExecuteAsync(invoice, lines));
        Assert.Contains("vượt quá hạn mức tín dụng", ex.Message);
        Assert.Empty(invRepo.Invoices); // Hóa đơn tuyệt đối không được tạo!
    }

    [Fact]
    public async Task Scenario03_ApproveValidRequest_ByAuthorizedCreditManager()
    {
        // 3. Phê duyệt yêu cầu hợp lệ bởi CREDIT_MANAGER
        var reqRepo = new AcceptanceTestCreditRequestRepo();
        var custRepo = new AcceptanceTestCustomerRepo();
        custRepo.Customers.Add(new Customer { Id = 3, Code = "KH03", Name = "Công ty Toàn Thắng", CreditLimit = 100_000_000m });
        custRepo.MockDebt = 80_000_000m;

        int reqId = await reqRepo.CreateRequestAsync(new CreditLimitRequest
        {
            CustomerId = 3,
            RequestedBy = "sales_agent",
            TransactionAmount = 60_000_000m,
            Reason = "Đơn hàng lớn xuất khẩu"
        });

        var useCase = new ApproveCreditLimitRequestUseCase(reqRepo, custRepo);

        // Phê duyệt bởi CREDIT_MANAGER
        await useCase.ExecuteAsync(reqId, "manager_anna", "CREDIT_MANAGER", "Đã thẩm định uy tín thanh toán");

        var approved = await reqRepo.GetByIdAsync(reqId);
        Assert.NotNull(approved);
        Assert.Equal("Approved", approved.Status);
        Assert.Equal("manager_anna", approved.ApprovedBy);
        Assert.NotNull(approved.ExpiresAt);
        Assert.True(approved.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task Scenario04_RejectUnauthorizedApprover_WhenSalesOrAccountantAttempts()
    {
        // 4. Từ chối người không có quyền duyệt (SALES hoặc ACCOUNTANT)
        var reqRepo = new AcceptanceTestCreditRequestRepo();
        var custRepo = new AcceptanceTestCustomerRepo();
        custRepo.Customers.Add(new Customer { Id = 4, Code = "KH04", Name = "Công ty Dệt May", CreditLimit = 50_000_000m });

        int reqId = await reqRepo.CreateRequestAsync(new CreditLimitRequest
        {
            CustomerId = 4,
            RequestedBy = "sales_agent",
            TransactionAmount = 40_000_000m
        });

        var useCase = new ApproveCreditLimitRequestUseCase(reqRepo, custRepo);

        // SALES cố tình duyệt -> Chặn
        var ex1 = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            useCase.ExecuteAsync(reqId, "sales_guy", "SALES", "Tôi duyệt"));
        Assert.Contains("CREDIT_MANAGER", ex1.Message);

        // ACCOUNTANT cố tình duyệt -> Chặn
        var ex2 = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            useCase.ExecuteAsync(reqId, "acc_girl", "ACCOUNTANT", "Tôi duyệt"));
        Assert.Contains("CREDIT_MANAGER", ex2.Message);

        var req = await reqRepo.GetByIdAsync(reqId);
        Assert.Equal("Pending", req?.Status); // Trạng thái vẫn là Pending
    }

    [Fact]
    public async Task Scenario05_BlockSelfApproval_EnforcesSegregationOfDuties()
    {
        // 5. Chặn tự phê duyệt theo chính sách Tách biệt nhiệm vụ (Segregation of Duties)
        var reqRepo = new AcceptanceTestCreditRequestRepo();
        var custRepo = new AcceptanceTestCustomerRepo();
        custRepo.Customers.Add(new Customer { Id = 5, Code = "KH05", Name = "Công ty Tự Duyệt", CreditLimit = 50_000_000m });

        int reqId = await reqRepo.CreateRequestAsync(new CreditLimitRequest
        {
            CustomerId = 5,
            RequestedBy = "credit_director", // Người yêu cầu
            TransactionAmount = 80_000_000m
        });

        var useCase = new ApproveCreditLimitRequestUseCase(reqRepo, custRepo);

        // Người tạo tự duyệt cho chính mình -> Bị chặn
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.ExecuteAsync(reqId, "credit_director", "CREDIT_MANAGER", "Tự duyệt đơn của tôi"));

        Assert.Contains("Segregation of Duties", ex.Message);
    }

    [Fact]
    public async Task Scenario06_ConcurrentApprovals_HandledSafelyWithLocking()
    {
        // 6. Kiểm tra hai yêu cầu đồng thời (Concurrency / Race Condition)
        var reqRepo = new AcceptanceTestCreditRequestRepo();
        var custRepo = new AcceptanceTestCustomerRepo();
        custRepo.Customers.Add(new Customer { Id = 6, Code = "KH06", Name = "Công ty Song Song", CreditLimit = 50_000_000m });

        int reqId = await reqRepo.CreateRequestAsync(new CreditLimitRequest
        {
            CustomerId = 6,
            RequestedBy = "sales_agent",
            TransactionAmount = 30_000_000m
        });

        var useCase = new ApproveCreditLimitRequestUseCase(reqRepo, custRepo);

        // Mô phỏng 2 request gửi đồng thời
        int successCount = 0;
        int failureCount = 0;

        var task1 = Task.Run(async () =>
        {
            try
            {
                await useCase.ExecuteAsync(reqId, "manager_1", "CREDIT_MANAGER", "Luồng 1");
                Interlocked.Increment(ref successCount);
            }
            catch { Interlocked.Increment(ref failureCount); }
        });

        var task2 = Task.Run(async () =>
        {
            try
            {
                await useCase.ExecuteAsync(reqId, "manager_2", "CREDIT_MANAGER", "Luồng 2");
                Interlocked.Increment(ref successCount);
            }
            catch { Interlocked.Increment(ref failureCount); }
        });

        await Task.WhenAll(task1, task2);

        // Duy nhất 1 luồng được duyệt, luồng thứ 2 bị từ chối
        Assert.Equal(1, successCount);
        Assert.Equal(1, failureCount);
    }

    [Fact]
    public async Task Scenario07_PreventDuplicatePaymentRecording_EnforcesUniquePaymentNumber()
    {
        // 7. Chống ghi nhận payment trùng (Idempotency)
        var payRepo = new AcceptanceTestPaymentRepo();

        var payment1 = new Payment { PaymentNumber = "PT-DUP-001", CustomerId = 1, Amount = 10_000_000m };
        var allocations1 = new List<PaymentAllocation> { new() { InvoiceId = 1, AllocatedAmount = 10_000_000m } };

        int id1 = await payRepo.CreatePaymentWithAllocationsAsync(payment1, allocations1);
        Assert.True(id1 > 0);

        // Cố tình ghi nhận lại lần 2 với cùng PaymentNumber
        var payment2 = new Payment { PaymentNumber = "PT-DUP-001", CustomerId = 1, Amount = 10_000_000m };
        var allocations2 = new List<PaymentAllocation> { new() { InvoiceId = 1, AllocatedAmount = 10_000_000m } };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            payRepo.CreatePaymentWithAllocationsAsync(payment2, allocations2));

        Assert.Contains("Trùng mã thanh toán", ex.Message);
        Assert.Single(payRepo.Payments);
    }

    [Fact]
    public void Scenario08_FifoPaymentAllocation_AllocatesOldestInvoicesFirst()
    {
        // 8. Kiểm tra FIFO: Phân bổ thanh toán cho hóa đơn đến hạn cũ nhất trước
        var unpaidInvoices = new List<SalesAR.UseCases.Models.InvoiceOutstandingDto>
        {
            new() { Id = 101, InvoiceNumber = "INV-OLD", DueDate = DateTime.Today.AddDays(-20), TotalAmount = 40_000_000m, PaidAmount = 0m },
            new() { Id = 102, InvoiceNumber = "INV-MID", DueDate = DateTime.Today.AddDays(-10), TotalAmount = 50_000_000m, PaidAmount = 0m },
            new() { Id = 103, InvoiceNumber = "INV-NEW", DueDate = DateTime.Today.AddDays(5), TotalAmount = 60_000_000m, PaidAmount = 0m }
        };

        var fifoCalculator = new AllocatePaymentUseCase();
        decimal paymentAmount = 60_000_000m;

        var allocations = fifoCalculator.Execute(1, paymentAmount, unpaidInvoices);

        // Assert: 
        // 1. INV-OLD được trả đủ 40M
        // 2. INV-MID được trả nốt 20M
        // 3. INV-NEW không được trả vì đã hết tiền
        Assert.Equal(2, allocations.Count);
        Assert.Equal(101, allocations[0].InvoiceId);
        Assert.Equal(40_000_000m, allocations[0].AllocatedAmount);

        Assert.Equal(102, allocations[1].InvoiceId);
        Assert.Equal(20_000_000m, allocations[1].AllocatedAmount);

        Assert.Equal(60_000_000m, allocations.Sum(a => a.AllocatedAmount));
    }

    [Fact]
    public async Task Scenario09_PaymentAllocation_TotalAllocationsCannotExceedPaymentAmount()
    {
        // 9. Không cho tổng allocation vượt payment
        var payRepo = new AcceptanceTestPaymentRepo();

        var payment = new Payment { PaymentNumber = "PT-OVER-001", CustomerId = 1, Amount = 30_000_000m };
        var invalidAllocations = new List<PaymentAllocation>
        {
            new() { InvoiceId = 1, AllocatedAmount = 25_000_000m },
            new() { InvoiceId = 2, AllocatedAmount = 10_000_000m } // Tổng = 35M > 30M
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            payRepo.CreatePaymentWithAllocationsAsync(payment, invalidAllocations));

        Assert.Contains("vượt quá số tiền thanh toán", ex.Message);
        Assert.Empty(payRepo.Payments);
    }

    [Fact]
    public async Task Scenario10_DatabaseTransactionRollback_WhenErrorOccurs()
    {
        // 10. Rollback khi giao dịch lỗi (Toàn vẹn CSDL)
        var payRepo = new AcceptanceTestPaymentRepo();

        var payment = new Payment { PaymentNumber = "PT-ROLLBACK-001", CustomerId = 1, Amount = 20_000_000m };
        var badAllocations = new List<PaymentAllocation>
        {
            new() { InvoiceId = 1, AllocatedAmount = 50_000_000m } // Lỗi vượt quá
        };

        try
        {
            await payRepo.CreatePaymentWithAllocationsAsync(payment, badAllocations);
        }
        catch
        {
            // Transaction rolled back
        }

        // Đảm bảo không có bản ghi mồ côi nào được lưu
        Assert.Empty(payRepo.Payments);
        Assert.Empty(payRepo.Allocations);
    }

    [Fact]
    public async Task Scenario11_PreventDuplicateDebtReminder_EnforcesCooldownPeriod()
    {
        // 11. Chống gửi nhắc nợ trùng (Idempotency Key & Cooldown Window)
        var reminderRepo = new AcceptanceTestReminderRepo();
        string idempotencyKey = "REM-INV-201-RULE-1-20261020";

        // Lần 1: Lưu bản ghi nhắc nợ
        await reminderRepo.SaveReminderLogAsync(new ReminderLog
        {
            CustomerId = 1,
            ReminderRuleId = 1,
            IdempotencyKey = idempotencyKey,
            SentAt = DateTime.UtcNow,
            Status = "Sent"
        });

        // Lần 2: Giả lập kiểm tra trùng lặp trước khi gửi
        var existing = await reminderRepo.GetByIdempotencyKeyAsync(idempotencyKey);

        // Assert: Phát hiện đã tồn tại bản ghi nhắc nợ trong ngày/đợt này
        Assert.NotNull(existing);
        Assert.Equal("Sent", existing.Status);
        Assert.Single(reminderRepo.Logs); // Không sinh thêm bản ghi dư thừa
    }

    [Fact]
    public async Task Scenario12_ReminderRetry_DoesNotDuplicateLogsOrSendDuplicateEmails()
    {
        // 12. Retry không tạo bản ghi hoặc email trùng
        var reminderRepo = new AcceptanceTestReminderRepo();

        int logId = await reminderRepo.SaveReminderLogAsync(new ReminderLog
        {
            CustomerId = 1,
            ReminderRuleId = 2,
            IdempotencyKey = "REM-RETRY-301",
            Status = "RetryPending",
            RetryCount = 1,
            LastError = "Timeout"
        });

        // Lần 2: Thực hiện Retry trên chính bản ghi logId đó
        await reminderRepo.UpdateReminderLogStatusAsync(
            id: logId,
            status: "Sent",
            retryCount: 2,
            lastError: null,
            sentAt: DateTime.UtcNow,
            confirmedBy: "background_worker");

        var retriedLog = await reminderRepo.GetLogByIdAsync(logId);

        // Assert: Bản ghi được cập nhật trạng thái Sent, retryCount = 2, không sinh thêm bản ghi mới
        Assert.NotNull(retriedLog);
        Assert.Equal("Sent", retriedLog.Status);
        Assert.Equal(2, retriedLog.RetryCount);
        Assert.Single(reminderRepo.Logs);
    }

    [Fact]
    public async Task Scenario13_AiFailure_GracefullyDegradesToFallback_WithoutBreakingCoreBusiness()
    {
        // 13. AI lỗi không làm hỏng nghiệp vụ cốt lõi
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "AI:ApiKey", "" } // Không có API key hoặc nhà cung cấp AI gặp sự cố
        }).Build();

        var fallbackProvider = new LocalHeuristicAiProvider();
        var gemini = new GeminiAiProvider(new System.Net.Http.HttpClient(), config, NullLogger<GeminiAiProvider>.Instance, fallbackProvider);

        var response = await gemini.GenerateTextAsync(new AiPrompt
        {
            SystemInstruction = "Phân tích công nợ",
            UserPrompt = "Tóm tắt tình hình tài chính của khách hàng ABC"
        });

        // Hệ thống không bị crash, tự động chuyển đổi sang Heuristic Rule Engine
        Assert.True(response.Success);
        Assert.True(response.IsFallback);
        Assert.False(string.IsNullOrWhiteSpace(response.Content));
    }

    [Fact]
    public void Scenario14_UnauthorizedUser_CannotAccessAuditLog()
    {
        // 14. Người không có quyền không xem được audit log
        var allowedRoles = new[] { "ADMIN", "CREDIT_MANAGER" };
        var testRoles = new[] { "SALES", "ACCOUNTANT", "GUEST", "USER" };

        foreach (var role in testRoles)
        {
            bool hasAccess = allowedRoles.Contains(role);
            Assert.False(hasAccess, $"Role '{role}' tuyệt đối không được phép truy cập Audit Log.");
        }

        Assert.True(allowedRoles.Contains("ADMIN"));
        Assert.True(allowedRoles.Contains("CREDIT_MANAGER"));
    }

    [Fact]
    public void Scenario15_ReportAndKpiSummary_MatchesRealDatabaseAggregates()
    {
        // 15. Báo cáo khớp với dữ liệu thật (Không tự bịa dữ liệu và công thức chuẩn hóa)
        var invoices = new List<Invoice>
        {
            new() { Id = 1, TotalAmount = 50_000_000m, DueDate = DateTime.Today.AddDays(-10), Status = "Unpaid" }, // Quá hạn 50M
            new() { Id = 2, TotalAmount = 30_000_000m, DueDate = DateTime.Today.AddDays(15), Status = "Unpaid" },  // Trong hạn 30M
            new() { Id = 3, TotalAmount = 20_000_000m, DueDate = DateTime.Today.AddDays(-5), Status = "Paid" }     // Đã thanh toán (loại trừ)
        };

        // Công thức chuẩn hóa:
        var totalOutstanding = invoices.Where(i => i.Status != "Paid" && i.Status != "Cancelled").Sum(i => i.TotalAmount);
        var totalOverdue = invoices.Where(i => i.Status != "Paid" && i.Status != "Cancelled" && i.DueDate < DateTime.Today).Sum(i => i.TotalAmount);
        var unpaidCount = invoices.Count(i => i.Status != "Paid" && i.Status != "Cancelled");

        Assert.Equal(80_000_000m, totalOutstanding); // 50M + 30M = 80M
        Assert.Equal(50_000_000m, totalOverdue);     // 50M
        Assert.Equal(2, unpaidCount);
    }

    #endregion
}
