namespace SalesAR.UnitTests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SalesAR.CoreBusiness.Models;
using SalesAR.Plugins.Reminders;
using SalesAR.UseCases.Models;
using SalesAR.UseCases.PluginInterfaces;
using SalesAR.UseCases.Reminders;
using SalesAR.UseCases.Reminders.Models;
using Xunit;

public class AutomatedDebtReminderTests
{
    private class InMemoryReminderRepository : IReminderRepository
    {
        public List<ReminderRule> Rules { get; } = new()
        {
            new ReminderRule { Id = 1, Name = "Sắp đến hạn", DaysOffset = -3, TriggerType = "UpcomingDue", Tone = "Polite", TemplateSubject = "Sắp đến hạn {CustomerName}", TemplateBody = "Kính gửi {CustomerName}, tổng nợ {TotalAmount}", AutoSendApproved = true, IsActive = true },
            new ReminderRule { Id = 2, Name = "Quá hạn đợt 1", DaysOffset = 1, TriggerType = "Overdue", Tone = "Polite", TemplateSubject = "Quá hạn lần 1 {CustomerName}", TemplateBody = "Kính gửi {CustomerName}, tổng nợ {TotalAmount}", AutoSendApproved = true, IsActive = true },
            new ReminderRule { Id = 3, Name = "Quá hạn đợt 2", DaysOffset = 8, TriggerType = "Overdue", Tone = "Firm", TemplateSubject = "Quá hạn lần 2 {CustomerName}", TemplateBody = "Kính gửi {CustomerName}, tổng nợ {TotalAmount}", AutoSendApproved = false, IsActive = true },
            new ReminderRule { Id = 4, Name = "Nợ khó đòi", DaysOffset = 31, TriggerType = "Overdue", Tone = "Urgent", TemplateSubject = "Nợ khó đòi {CustomerName}", TemplateBody = "Cảnh báo {CustomerName}, tổng nợ {TotalAmount}", AutoSendApproved = false, IsActive = true }
        };

        public List<ReminderLog> Logs { get; } = new();

        public Task<IEnumerable<ReminderRule>> GetActiveRulesAsync() => Task.FromResult<IEnumerable<ReminderRule>>(Rules.Where(r => r.IsActive));
        public Task<ReminderRule?> GetRuleByIdAsync(int id) => Task.FromResult(Rules.FirstOrDefault(r => r.Id == id));
        public Task<ReminderLog?> GetByIdempotencyKeyAsync(string key) => Task.FromResult(Logs.FirstOrDefault(l => l.IdempotencyKey == key));
        public Task<ReminderLog?> GetLogByIdAsync(int id) => Task.FromResult(Logs.FirstOrDefault(l => l.Id == id));

        public Task<int> SaveReminderLogAsync(ReminderLog log)
        {
            if (log.Id == 0)
            {
                log.Id = Logs.Count + 1;
            }
            var existing = Logs.FirstOrDefault(l => l.Id == log.Id);
            if (existing == null)
            {
                Logs.Add(log);
            }
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

        public Task<IEnumerable<ReminderLog>> GetPendingRetryLogsAsync(int maxRetries = 3)
        {
            var q = Logs.Where(l => (l.Status == "RetryPending" || l.Status == "Queued") && l.RetryCount < maxRetries).ToList();
            return Task.FromResult<IEnumerable<ReminderLog>>(q);
        }

        public Task SetCustomerDisputeStatusAsync(int customerId, bool isInDispute, string? reason) => Task.CompletedTask;
        public Task SetInvoiceDisputeStatusAsync(int invoiceId, bool isInDispute, string? reason) => Task.CompletedTask;
    }

    private class InMemoryCustomerRepository : ICustomerRepository
    {
        public List<Customer> Customers { get; } = new();

        public Task<IEnumerable<Customer>> GetAllAsync() => Task.FromResult<IEnumerable<Customer>>(Customers);
        public Task<Customer?> GetByIdAsync(int id) => Task.FromResult(Customers.FirstOrDefault(c => c.Id == id));
        public Task<Customer?> GetByCodeAsync(string code) => Task.FromResult(Customers.FirstOrDefault(c => c.Code == code));
        public Task<int> AddAsync(Customer customer) { Customers.Add(customer); return Task.FromResult(customer.Id); }
        public Task UpdateAsync(Customer customer) => Task.CompletedTask;
        public Task DeleteAsync(int id) => Task.CompletedTask;
        public Task<decimal> GetCurrentDebtAsync(int customerId) => Task.FromResult(0m);
        public Task<CustomerDebtDto?> GetCustomerDebtDetailsAsync(int customerId) => Task.FromResult<CustomerDebtDto?>(null);
        public Task UpdateCreditLimitAsync(int customerId, decimal newCreditLimit, string changedBy, string reason) => Task.CompletedTask;
    }

    private class InMemoryInvoiceRepository : IInvoiceRepository
    {
        public List<InvoiceOutstandingDto> OutstandingInvoices { get; } = new();

        public Task<IEnumerable<InvoiceOutstandingDto>> GetOutstandingInvoicesByCustomerAsync(int customerId)
        {
            return Task.FromResult<IEnumerable<InvoiceOutstandingDto>>(OutstandingInvoices.Where(i => i.CustomerId == customerId));
        }

        public Task<IEnumerable<Invoice>> GetAllAsync() => Task.FromResult<IEnumerable<Invoice>>(Enumerable.Empty<Invoice>());
        public Task<Invoice?> GetByIdAsync(int id) => Task.FromResult<Invoice?>(null);
        public Task<Invoice?> GetByInvoiceNumberAsync(string number) => Task.FromResult<Invoice?>(null);
        public Task<IEnumerable<InvoiceLine>> GetLinesByInvoiceIdAsync(int invoiceId) => Task.FromResult<IEnumerable<InvoiceLine>>(Enumerable.Empty<InvoiceLine>());
        public Task<IEnumerable<Invoice>> GetInvoicesByCustomerAsync(int customerId) => Task.FromResult<IEnumerable<Invoice>>(Enumerable.Empty<Invoice>());
        public Task<IEnumerable<Invoice>> GetUnpaidInvoicesByCustomerAsync(int customerId) => Task.FromResult<IEnumerable<Invoice>>(Enumerable.Empty<Invoice>());
        public Task<int> CreateInvoiceWithLinesAsync(Invoice invoice, IEnumerable<InvoiceLine> lines) => Task.FromResult(1);
        public Task UpdateStatusAsync(int id, string status) => Task.CompletedTask;
    }

    [Fact]
    public async Task IdentifyReminderCandidates_DetectsUpcomingDueAndOverdueInvoices()
    {
        // Arrange
        var reminderRepo = new InMemoryReminderRepository();
        var customerRepo = new InMemoryCustomerRepository();
        var invoiceRepo = new InMemoryInvoiceRepository();
        var emailSender = new FakeEmailSender();
        var service = new ReminderService(reminderRepo, customerRepo, invoiceRepo, emailSender);

        var asOfDate = new DateTime(2026, 10, 20);

        customerRepo.Customers.Add(new Customer { Id = 1, Code = "KH001", Name = "Công ty A", Email = "a@test.vn", IsActive = true });
        customerRepo.Customers.Add(new Customer { Id = 2, Code = "KH002", Name = "Công ty B", Email = "b@test.vn", IsActive = true });

        // Khách A: có hóa đơn sắp đến hạn trong 2 ngày (22/10/2026)
        invoiceRepo.OutstandingInvoices.Add(new InvoiceOutstandingDto
        {
            Id = 101, CustomerId = 1, InvoiceNumber = "HD-101",
            InvoiceDate = asOfDate.AddDays(-28), DueDate = asOfDate.AddDays(2),
            TotalAmount = 5000000
        });

        // Khách B: có hóa đơn quá hạn 10 ngày (10/10/2026)
        invoiceRepo.OutstandingInvoices.Add(new InvoiceOutstandingDto
        {
            Id = 102, CustomerId = 2, InvoiceNumber = "HD-102",
            InvoiceDate = asOfDate.AddDays(-40), DueDate = asOfDate.AddDays(-10),
            TotalAmount = 12000000
        });

        // Act
        var candidates = (await service.IdentifyReminderCandidatesAsync(asOfDate)).ToList();

        // Assert
        Assert.Equal(2, candidates.Count);

        var candA = candidates.First(c => c.CustomerId == 1);
        Assert.Equal("UpcomingDue", candA.TriggerType);
        Assert.Equal(1, candA.ReminderRuleId); // Rule 1: Sắp đến hạn

        var candB = candidates.First(c => c.CustomerId == 2);
        Assert.Equal("Overdue", candB.TriggerType);
        Assert.Equal(3, candB.ReminderRuleId); // Rule 3: Quá hạn 8-30 ngày
        Assert.Equal(10, candB.MaxDaysOverdue);
    }

    [Fact]
    public async Task IdentifyReminderCandidates_FlagsDisputedCustomers()
    {
        // Arrange
        var reminderRepo = new InMemoryReminderRepository();
        var customerRepo = new InMemoryCustomerRepository();
        var invoiceRepo = new InMemoryInvoiceRepository();
        var emailSender = new FakeEmailSender();
        var service = new ReminderService(reminderRepo, customerRepo, invoiceRepo, emailSender);

        var asOfDate = new DateTime(2026, 10, 20);

        customerRepo.Customers.Add(new Customer
        {
            Id = 1, Code = "KH005", Name = "Công ty Tranh Chấp",
            Email = "dispute@test.vn", IsActive = true,
            IsInDispute = true, DisputeReason = "Khiếu nại hư hỏng hàng"
        });

        invoiceRepo.OutstandingInvoices.Add(new InvoiceOutstandingDto
        {
            Id = 105, CustomerId = 1, InvoiceNumber = "HD-105",
            InvoiceDate = asOfDate.AddDays(-40), DueDate = asOfDate.AddDays(-10),
            TotalAmount = 8000000
        });

        // Act
        var candidates = (await service.IdentifyReminderCandidatesAsync(asOfDate)).ToList();

        // Assert
        Assert.Single(candidates);
        var cand = candidates.First();
        Assert.True(cand.IsInDispute);
        Assert.Equal("Khiếu nại hư hỏng hàng", cand.DisputeReason);
    }

    [Fact]
    public async Task QueueOrSendReminder_PausesSending_WhenCustomerInDispute()
    {
        // Arrange
        var reminderRepo = new InMemoryReminderRepository();
        var customerRepo = new InMemoryCustomerRepository();
        var invoiceRepo = new InMemoryInvoiceRepository();
        var emailSender = new FakeEmailSender();
        var service = new ReminderService(reminderRepo, customerRepo, invoiceRepo, emailSender);

        customerRepo.Customers.Add(new Customer
        {
            Id = 5, Code = "KH005", Name = "Công ty Tranh Chấp",
            Email = "dispute@test.vn", IsActive = true,
            IsInDispute = true, DisputeReason = "Khiếu nại sản phẩm"
        });

        var draft = new ReminderDraftDto
        {
            CustomerId = 5, CustomerCode = "KH005", CustomerName = "Công ty Tranh Chấp",
            RecipientEmail = "dispute@test.vn", ReminderRuleId = 2,
            Subject = "Thư nhắc nợ", Body = "Nội dung nhắc nợ...",
            TotalDebtAmount = 10000000, IdempotencyKey = "REM-CUST-5-RULE-2-20261020"
        };

        // Act
        var log = await service.QueueOrSendReminderAsync(draft, confirmedBy: "admin", forceSend: true);

        // Assert
        Assert.Equal("PausedDispute", log.Status);
        Assert.Contains("tranh chấp", log.LastError);
        Assert.Empty(emailSender.SentHistory); // Tuyệt đối không gửi email
    }

    [Fact]
    public async Task QueueOrSendReminder_PreventsDuplicateSending_UsingIdempotencyKey()
    {
        // Arrange
        var reminderRepo = new InMemoryReminderRepository();
        var customerRepo = new InMemoryCustomerRepository();
        var invoiceRepo = new InMemoryInvoiceRepository();
        var emailSender = new FakeEmailSender();
        var service = new ReminderService(reminderRepo, customerRepo, invoiceRepo, emailSender);

        customerRepo.Customers.Add(new Customer
        {
            Id = 1, Code = "KH001", Name = "Công ty A",
            Email = "a@test.vn", IsActive = true, IsInDispute = false
        });

        var draft = new ReminderDraftDto
        {
            CustomerId = 1, CustomerCode = "KH001", CustomerName = "Công ty A",
            RecipientEmail = "a@test.vn", ReminderRuleId = 1,
            Subject = "Nhắc nợ", Body = "Nội dung...", TotalDebtAmount = 5000000,
            AutoSendApproved = true, IdempotencyKey = "REM-CUST-1-RULE-1-20261020"
        };

        // Act - Lần 1: Gửi thành công
        var firstResult = await service.QueueOrSendReminderAsync(draft, confirmedBy: "admin", forceSend: true);
        Assert.Equal("Sent", firstResult.Status);
        Assert.Single(emailSender.SentHistory);

        // Act - Lần 2: Cùng IdempotencyKey gửi lại (Job chạy lại hoặc bấm nhầm 2 lần)
        var secondResult = await service.QueueOrSendReminderAsync(draft, confirmedBy: "admin", forceSend: true);

        // Assert - Chống gửi trùng: Trả về log cũ, không gửi thêm email nào
        Assert.Equal("Sent", secondResult.Status);
        Assert.Equal(firstResult.Id, secondResult.Id);
        Assert.Single(emailSender.SentHistory); // Vẫn chỉ là 1 email, không có email thứ 2
    }

    [Fact]
    public async Task QueueOrSendReminder_RequiresApproval_WhenPolicyAutoSendIsFalse()
    {
        // Arrange
        var reminderRepo = new InMemoryReminderRepository();
        var customerRepo = new InMemoryCustomerRepository();
        var invoiceRepo = new InMemoryInvoiceRepository();
        var emailSender = new FakeEmailSender();
        var service = new ReminderService(reminderRepo, customerRepo, invoiceRepo, emailSender);

        customerRepo.Customers.Add(new Customer
        {
            Id = 3, Code = "KH003", Name = "Công ty C",
            Email = "c@test.vn", IsActive = true
        });

        var draft = new ReminderDraftDto
        {
            CustomerId = 3, CustomerCode = "KH003", CustomerName = "Công ty C",
            RecipientEmail = "c@test.vn", ReminderRuleId = 3,
            Subject = "Nhắc nợ quá hạn đợt 2", Body = "Yêu cầu tất toán...",
            TotalDebtAmount = 25000000, AutoSendApproved = false, // Chính sách: Bắt buộc duyệt thủ công
            IdempotencyKey = "REM-CUST-3-RULE-3-20261020"
        };

        // Act - Chạy tự động (không có confirmedBy và không forceSend)
        var log = await service.QueueOrSendReminderAsync(draft, confirmedBy: null, forceSend: false);

        // Assert
        Assert.Equal("PendingApproval", log.Status);
        Assert.Empty(emailSender.SentHistory); // Không được tự động gửi khi chưa có duyệt
    }

    [Fact]
    public async Task ProcessReminder_RetriesTransientFailure_UpToMaxRetries()
    {
        // Arrange
        var reminderRepo = new InMemoryReminderRepository();
        var customerRepo = new InMemoryCustomerRepository();
        var invoiceRepo = new InMemoryInvoiceRepository();
        var emailSender = new FakeEmailSender
        {
            SimulateFailure = true,
            SimulateTransient = true,
            FailureMessage = "SMTP Connection Timeout"
        };
        var service = new ReminderService(reminderRepo, customerRepo, invoiceRepo, emailSender);

        customerRepo.Customers.Add(new Customer { Id = 1, Code = "KH001", Name = "Công ty A", Email = "a@test.vn", IsActive = true });

        var draft = new ReminderDraftDto
        {
            CustomerId = 1, CustomerCode = "KH001", CustomerName = "Công ty A",
            RecipientEmail = "a@test.vn", ReminderRuleId = 1,
            Subject = "Nhắc nợ", Body = "Nội dung...", TotalDebtAmount = 5000000,
            AutoSendApproved = true, IdempotencyKey = "REM-RETRY-TEST-001"
        };

        // Act - Lần 1 gửi lỗi tạm thời
        var log = await service.QueueOrSendReminderAsync(draft, confirmedBy: "admin", forceSend: true);

        // Assert
        Assert.Equal("RetryPending", log.Status);
        Assert.Equal(1, log.RetryCount);
        Assert.Equal("SMTP Connection Timeout", log.LastError);

        // Act - Retry qua hàng đợi (lần 2 vẫn lỗi)
        await service.ProcessPendingQueueAsync();
        var updatedLog = await reminderRepo.GetLogByIdAsync(log.Id);
        Assert.NotNull(updatedLog);
        Assert.Equal("RetryPending", updatedLog.Status);
        Assert.Equal(2, updatedLog.RetryCount);

        // Act - Lần 3 email provider hoạt động trở lại
        emailSender.SimulateFailure = false;
        await service.ProcessPendingQueueAsync();

        var finalLog = await reminderRepo.GetLogByIdAsync(log.Id);
        Assert.NotNull(finalLog);
        Assert.Equal("Sent", finalLog.Status);
        Assert.NotNull(finalLog.SentAt);
        Assert.Null(finalLog.LastError);
    }

    [Fact]
    public async Task ProcessReminder_MarksFailed_WhenMaxRetriesExceeded()
    {
        // Arrange
        var reminderRepo = new InMemoryReminderRepository();
        var customerRepo = new InMemoryCustomerRepository();
        var invoiceRepo = new InMemoryInvoiceRepository();
        var emailSender = new FakeEmailSender
        {
            SimulateFailure = true,
            SimulateTransient = true,
            FailureMessage = "SMTP Fatal Error"
        };
        var service = new ReminderService(reminderRepo, customerRepo, invoiceRepo, emailSender);

        customerRepo.Customers.Add(new Customer { Id = 1, Code = "KH001", Name = "Công ty A", Email = "a@test.vn", IsActive = true });

        var draft = new ReminderDraftDto
        {
            CustomerId = 1, CustomerCode = "KH001", CustomerName = "Công ty A",
            RecipientEmail = "a@test.vn", ReminderRuleId = 1,
            Subject = "Nhắc nợ", Body = "Nội dung...", TotalDebtAmount = 5000000,
            AutoSendApproved = true, IdempotencyKey = "REM-MAX-RETRY-001"
        };

        // Lần 1
        var log = await service.QueueOrSendReminderAsync(draft, confirmedBy: "admin", forceSend: true);
        Assert.Equal(1, log.RetryCount);

        // Lần 2
        await service.ProcessPendingQueueAsync();
        // Lần 3
        await service.ProcessPendingQueueAsync();

        var finalLog = await reminderRepo.GetLogByIdAsync(log.Id);
        Assert.NotNull(finalLog);
        Assert.Equal(3, finalLog.RetryCount);
        Assert.Equal("Failed", finalLog.Status); // Vượt quá max retries -> Chuyển thành Failed
    }

    [Fact]
    public async Task PauseAndResume_HandlesDisputeWorkflowCorrectly()
    {
        // Arrange
        var reminderRepo = new InMemoryReminderRepository();
        var customerRepo = new InMemoryCustomerRepository();
        var invoiceRepo = new InMemoryInvoiceRepository();
        var emailSender = new FakeEmailSender();
        var service = new ReminderService(reminderRepo, customerRepo, invoiceRepo, emailSender);

        var log = new ReminderLog
        {
            Id = 10, CustomerId = 1, RecipientEmail = "a@test.vn",
            Subject = "Thư nhắc nợ", Body = "...", Status = "PendingApproval"
        };
        await reminderRepo.SaveReminderLogAsync(log);

        // Act - Tạm dừng do tranh chấp
        var paused = await service.PauseReminderForDisputeAsync(10, "Khách hàng khiếu nại sai số lượng", "Kế toán trưởng");
        Assert.True(paused);
        var pausedLog = await reminderRepo.GetLogByIdAsync(10);
        Assert.NotNull(pausedLog);
        Assert.Equal("PausedDispute", pausedLog.Status);

        // Act - Khôi phục sau khi giải quyết xong
        var resumed = await service.ResumeReminderFromDisputeAsync(10, "Kế toán trưởng");
        Assert.True(resumed);
        var resumedLog = await reminderRepo.GetLogByIdAsync(10);
        Assert.NotNull(resumedLog);
        Assert.Equal("PendingApproval", resumedLog.Status);
    }
}
