namespace SalesAR.UnitTests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using SalesAR.CoreBusiness.Models;
using SalesAR.Plugins.AI;
using SalesAR.UseCases.AI;
using SalesAR.UseCases.AI.Models;
using SalesAR.UseCases.AI.Providers;
using SalesAR.UseCases.Models;
using SalesAR.UseCases.PluginInterfaces;
using Xunit;

public class AiAssistanceTests
{
    #region In-Memory Test Fakes

    private class FakeCustomerRepository : ICustomerRepository
    {
        public List<Customer> Customers { get; } = new();

        public Task<IEnumerable<Customer>> GetAllAsync() => Task.FromResult<IEnumerable<Customer>>(Customers);
        public Task<Customer?> GetByIdAsync(int id) => Task.FromResult(Customers.FirstOrDefault(c => c.Id == id));
        public Task<Customer?> GetByCodeAsync(string code) => Task.FromResult(Customers.FirstOrDefault(c => c.Code == code));
        public Task<int> AddAsync(Customer customer) { Customers.Add(customer); return Task.FromResult(customer.Id); }
        public Task UpdateAsync(Customer customer) => Task.CompletedTask;
        public Task DeleteAsync(int id) => Task.CompletedTask;
        public Task<decimal> GetCurrentDebtAsync(int customerId) => Task.FromResult(50_000_000m);
        public Task<CustomerDebtDto?> GetCustomerDebtDetailsAsync(int customerId)
        {
            var cust = Customers.FirstOrDefault(c => c.Id == customerId);
            if (cust == null) return Task.FromResult<CustomerDebtDto?>(null);

            return Task.FromResult<CustomerDebtDto?>(new CustomerDebtDto
            {
                CustomerId = cust.Id,
                CustomerCode = cust.Code,
                CustomerName = cust.Name,
                CreditLimit = cust.CreditLimit,
                TotalInvoiced = 100_000_000m,
                TotalPaid = 50_000_000m,
                TotalAdjusted = 0m,
                CurrentDebt = 50_000_000m
            });
        }
        public Task UpdateCreditLimitAsync(int customerId, decimal newCreditLimit, string changedBy, string reason)
        {
            var cust = Customers.FirstOrDefault(c => c.Id == customerId);
            if (cust != null) cust.CreditLimit = newCreditLimit;
            return Task.CompletedTask;
        }
    }

    private class FakeInvoiceRepository : IInvoiceRepository
    {
        public List<InvoiceOutstandingDto> OutstandingInvoices { get; } = new();

        public Task<IEnumerable<Invoice>> GetAllAsync() => Task.FromResult<IEnumerable<Invoice>>(new List<Invoice>());
        public Task<Invoice?> GetByIdAsync(int id) => Task.FromResult<Invoice?>(null);
        public Task<Invoice?> GetByInvoiceNumberAsync(string invoiceNumber) => Task.FromResult<Invoice?>(null);
        public Task<IEnumerable<InvoiceLine>> GetLinesByInvoiceIdAsync(int invoiceId) => Task.FromResult<IEnumerable<InvoiceLine>>(new List<InvoiceLine>());
        public Task<IEnumerable<Invoice>> GetInvoicesByCustomerAsync(int customerId) => Task.FromResult<IEnumerable<Invoice>>(new List<Invoice>());
        public Task<IEnumerable<Invoice>> GetUnpaidInvoicesByCustomerAsync(int customerId) => Task.FromResult<IEnumerable<Invoice>>(new List<Invoice>());
        public Task<IEnumerable<InvoiceOutstandingDto>> GetOutstandingInvoicesByCustomerAsync(int customerId) =>
            Task.FromResult<IEnumerable<InvoiceOutstandingDto>>(OutstandingInvoices.Where(i => i.CustomerId == customerId));
        public Task<int> CreateInvoiceWithLinesAsync(Invoice invoice, IEnumerable<InvoiceLine> lines) => Task.FromResult(1);
        public Task UpdateStatusAsync(int invoiceId, string status) => Task.CompletedTask;
    }

    private class FakePaymentRepository : IPaymentRepository
    {
        public List<Payment> Payments { get; } = new();

        public Task<IEnumerable<Payment>> GetAllAsync() => Task.FromResult<IEnumerable<Payment>>(Payments);
        public Task<Payment?> GetByIdAsync(int id) => Task.FromResult(Payments.FirstOrDefault(p => p.Id == id));
        public Task<Payment?> GetByPaymentNumberAsync(string paymentNumber) => Task.FromResult<Payment?>(null);
        public Task<IEnumerable<Payment>> GetPaymentsByCustomerAsync(int customerId) =>
            Task.FromResult<IEnumerable<Payment>>(Payments.Where(p => p.CustomerId == customerId));
        public Task<IEnumerable<PaymentAllocation>> GetAllocationsByPaymentIdAsync(int paymentId) => Task.FromResult<IEnumerable<PaymentAllocation>>(new List<PaymentAllocation>());
        public Task<IEnumerable<PaymentAllocation>> GetAllocationsByCustomerAsync(int customerId) => Task.FromResult<IEnumerable<PaymentAllocation>>(new List<PaymentAllocation>());
        public Task<int> CreatePaymentWithAllocationsAsync(Payment payment, IEnumerable<PaymentAllocation> allocations) => Task.FromResult(1);
        public Task ReallocatePaymentAsync(int paymentId, IEnumerable<PaymentAllocation> newAllocations, string performedBy, string reason) => Task.CompletedTask;
    }

    private class FakeCreditRiskAssessmentRepository : ICreditRiskAssessmentRepository
    {
        public List<CreditRiskAssessment> Assessments { get; } = new();

        public Task<int> SaveAssessmentAsync(CreditRiskAssessment assessment)
        {
            assessment.Id = Assessments.Count + 1;
            Assessments.Add(assessment);
            return Task.FromResult(assessment.Id);
        }

        public Task<IEnumerable<CreditRiskAssessment>> GetHistoryByCustomerAsync(int customerId, int limit = 10)
        {
            return Task.FromResult<IEnumerable<CreditRiskAssessment>>(Assessments.Where(a => a.CustomerId == customerId).OrderByDescending(a => a.AssessedAt).Take(limit));
        }

        public Task<CreditRiskAssessment?> GetByIdAsync(int id)
        {
            return Task.FromResult(Assessments.FirstOrDefault(a => a.Id == id));
        }

        public Task UpdateReviewAsync(int id, string reviewedBy, string reviewNotes, DateTime reviewedAt)
        {
            var item = Assessments.FirstOrDefault(a => a.Id == id);
            if (item != null)
            {
                item.HumanReviewedBy = reviewedBy;
                item.HumanReviewNotes = reviewNotes;
                item.HumanReviewedAt = reviewedAt;
                item.RequiresHumanReview = false;
            }
            return Task.CompletedTask;
        }
    }

    #endregion

    [Fact]
    public async Task CustomerArSummary_SeparatesGroundTruthFromAiNarrative()
    {
        // Arrange
        var custRepo = new FakeCustomerRepository();
        custRepo.Customers.Add(new Customer { Id = 1, Code = "KH01", Name = "Công ty Hải Nam", CreditLimit = 100_000_000m });

        var invRepo = new FakeInvoiceRepository();
        invRepo.OutstandingInvoices.Add(new InvoiceOutstandingDto
        {
            Id = 10,
            InvoiceNumber = "HD-001",
            CustomerId = 1,
            TotalAmount = 50_000_000m,
            PaidAmount = 0m,
            DueDate = DateTime.Today.AddDays(-10) // Quá hạn 10 ngày
        });

        var payRepo = new FakePaymentRepository();
        var aiProvider = new LocalHeuristicAiProvider();
        var useCase = new CustomerArSummaryUseCase(custRepo, invRepo, payRepo, aiProvider);

        // Act
        var result = await useCase.SummarizeCustomerAsync(1);

        // Assert: GroundTruth mang số liệu thật chính xác từ database
        Assert.NotNull(result);
        Assert.NotNull(result.GroundTruth);
        Assert.Equal(50_000_000m, result.GroundTruth.CurrentDebt);
        Assert.Equal(100_000_000m, result.GroundTruth.CreditLimit);
        Assert.Equal(50.0m, result.GroundTruth.CreditLimitUtilizationRate);
        Assert.Equal(1, result.GroundTruth.OverdueInvoicesCount);
        Assert.Equal(10, result.GroundTruth.MaxDaysOverdue);

        // Assert: AiNarrative có phần diễn giải độc lập
        Assert.NotNull(result.AiNarrative);
        Assert.False(string.IsNullOrWhiteSpace(result.AiNarrative.ExecutiveSummary));
        Assert.False(string.IsNullOrWhiteSpace(result.AiNarrative.PaymentHealthAssessment));
        Assert.True(result.IsAiGenerated);
    }

    [Fact]
    public async Task CustomerArSummary_DetectsPriorityWarningSignals()
    {
        // Arrange: Khách hàng vượt hạn mức và có nợ quá hạn > 30 ngày kèm tranh chấp
        var custRepo = new FakeCustomerRepository();
        custRepo.Customers.Add(new Customer 
        { 
            Id = 2, 
            Code = "KH02", 
            Name = "Công ty Ánh Dương", 
            CreditLimit = 30_000_000m,
            IsInDispute = true,
            DisputeReason = "Sai sót đơn giá theo phụ lục hợp đồng"
        });

        var invRepo = new FakeInvoiceRepository();
        invRepo.OutstandingInvoices.Add(new InvoiceOutstandingDto
        {
            Id = 20,
            InvoiceNumber = "HD-020",
            CustomerId = 2,
            TotalAmount = 50_000_000m,
            PaidAmount = 0m,
            DueDate = DateTime.Today.AddDays(-45) // Quá hạn 45 ngày (>30 ngày)
        });

        var payRepo = new FakePaymentRepository();
        var aiProvider = new LocalHeuristicAiProvider();
        var useCase = new CustomerArSummaryUseCase(custRepo, invRepo, payRepo, aiProvider);

        // Act
        var result = await useCase.SummarizeCustomerAsync(2);

        // Assert: Phát hiện 3 tín hiệu cảnh báo ưu tiên
        Assert.NotNull(result.GroundTruth.PriorityWarningSignals);
        Assert.True(result.GroundTruth.PriorityWarningSignals.Count >= 3);
        Assert.Contains(result.GroundTruth.PriorityWarningSignals, s => s.Contains("VƯỢT HẠN MỨC"));
        Assert.Contains(result.GroundTruth.PriorityWarningSignals, s => s.Contains("QUÁ HẠN DÀI NGÀY"));
        Assert.Contains(result.GroundTruth.PriorityWarningSignals, s => s.Contains("ĐANG KHIẾU NẠI"));
    }

    [Fact]
    public async Task CreditRiskAssessment_CalculatesRiskScore_LowRiskCustomer()
    {
        // Arrange: Khách hàng uy tín, nợ thấp (10%), không quá hạn, không tranh chấp
        var custRepo = new FakeCustomerRepository();
        custRepo.Customers.Add(new Customer { Id = 3, Code = "KH03", Name = "Công ty Sao Mai", CreditLimit = 500_000_000m });

        var invRepo = new FakeInvoiceRepository();
        invRepo.OutstandingInvoices.Add(new InvoiceOutstandingDto
        {
            Id = 30,
            InvoiceNumber = "HD-030",
            CustomerId = 3,
            TotalAmount = 50_000_000m,
            PaidAmount = 0m,
            DueDate = DateTime.Today.AddDays(15) // Trong hạn
        });

        var payRepo = new FakePaymentRepository();
        payRepo.Payments.Add(new Payment { Id = 1, CustomerId = 3, Amount = 50_000_000m, PaymentDate = DateTime.Today.AddDays(-10) });

        var riskRepo = new FakeCreditRiskAssessmentRepository();
        var service = new CreditRiskAssessmentService(custRepo, invRepo, payRepo, riskRepo);

        // Act
        var result = await service.AssessCustomerRiskAsync(3);

        // Assert: 100 điểm, Rủi ro Thấp, Không cần Kế toán trưởng kiểm tra
        Assert.NotNull(result);
        Assert.Equal(100m, result.RiskScore);
        Assert.Equal("Thấp", result.RiskLevel);
        Assert.False(result.RequiresHumanReview);
        Assert.Contains("v1.0-RuleBasedScoring", result.ModelVersion);
    }

    [Fact]
    public async Task CreditRiskAssessment_OverdueAndUtilization_TriggersHighRiskAndRequiresHumanReview()
    {
        // Arrange: Nợ vượt hạn mức và trễ hạn > 60 ngày
        var custRepo = new FakeCustomerRepository();
        custRepo.Customers.Add(new Customer { Id = 4, Code = "KH04", Name = "Công ty Rủi Ro", CreditLimit = 40_000_000m });

        var invRepo = new FakeInvoiceRepository();
        invRepo.OutstandingInvoices.Add(new InvoiceOutstandingDto
        {
            Id = 40,
            InvoiceNumber = "HD-040",
            CustomerId = 4,
            TotalAmount = 50_000_000m, // Dư nợ 50M > Limit 40M (-35đ)
            PaidAmount = 0m,
            DueDate = DateTime.Today.AddDays(-65) // Quá hạn 65 ngày (-30đ)
        });

        var payRepo = new FakePaymentRepository();
        var riskRepo = new FakeCreditRiskAssessmentRepository();
        var service = new CreditRiskAssessmentService(custRepo, invRepo, payRepo, riskRepo);

        // Act
        var result = await service.AssessCustomerRiskAsync(4);

        // Assert: Điểm bị trừ = 100 - 35 (vượt hạn mức) - 30 (quá hạn > 60d) - 15 (chưa có lịch sử thanh toán) = 20 điểm
        Assert.True(result.RiskScore < 40m);
        Assert.Equal("Nghiêm trọng", result.RiskLevel);
        Assert.True(result.RequiresHumanReview); // Bắt buộc Kế toán trưởng duyệt
        Assert.Single(riskRepo.Assessments);
    }

    [Fact]
    public async Task CreditRiskAssessment_DisputeTriggersMandatoryHumanReviewRegardlessOfScore()
    {
        // Arrange: Khách hàng có tranh chấp khiếu nại
        var custRepo = new FakeCustomerRepository();
        custRepo.Customers.Add(new Customer 
        { 
            Id = 5, 
            Code = "KH05", 
            Name = "Công ty Khiếu Nại", 
            CreditLimit = 500_000_000m,
            IsInDispute = true,
            DisputeReason = "Hàng bị lỗi bao bì đang đối soát"
        });

        var invRepo = new FakeInvoiceRepository();
        var payRepo = new FakePaymentRepository();
        payRepo.Payments.Add(new Payment { Id = 2, CustomerId = 5, Amount = 10_000_000m, PaymentDate = DateTime.Today });

        var riskRepo = new FakeCreditRiskAssessmentRepository();
        var service = new CreditRiskAssessmentService(custRepo, invRepo, payRepo, riskRepo);

        // Act
        var result = await service.AssessCustomerRiskAsync(5);

        // Assert: Có tranh chấp bắt buộc RequiresHumanReview = true
        Assert.True(result.RequiresHumanReview);
        Assert.Contains(result.Factors, f => f.Category == "Tranh chấp");
    }

    [Fact]
    public async Task CreditRiskAssessment_EnforcesGuardrails_DoesNotAlterCreditLimitOrPostPayments()
    {
        // Arrange
        var custRepo = new FakeCustomerRepository();
        var cust = new Customer { Id = 6, Code = "KH06", Name = "Công ty Thẩm Quyền", CreditLimit = 100_000_000m };
        custRepo.Customers.Add(cust);

        var invRepo = new FakeInvoiceRepository();
        var payRepo = new FakePaymentRepository();
        var riskRepo = new FakeCreditRiskAssessmentRepository();
        var service = new CreditRiskAssessmentService(custRepo, invRepo, payRepo, riskRepo);

        // Act: Đánh giá rủi ro
        var result = await service.AssessCustomerRiskAsync(6);

        // Assert: Hạn mức tín dụng của khách hàng giữ nguyên 100M, không bị AI sửa đổi
        Assert.Equal(100_000_000m, cust.CreditLimit);
        Assert.Empty(payRepo.Payments);
        Assert.Contains("AI TUYỆT ĐỐI KHÔNG", result.AuthorityDisclaimer);
    }

    [Theory]
    [InlineData("Polite")]
    [InlineData("Neutral")]
    [InlineData("Formal")]
    public async Task ReminderDraftGenerator_GroundedDataInAllTones(string tone)
    {
        // Arrange
        var custRepo = new FakeCustomerRepository();
        custRepo.Customers.Add(new Customer { Id = 7, Code = "KH07", Name = "Công ty Bách Khoa", Email = "ketoan@bachkhoa.vn" });

        var invRepo = new FakeInvoiceRepository();
        invRepo.OutstandingInvoices.Add(new InvoiceOutstandingDto
        {
            Id = 70,
            InvoiceNumber = "HD-BK-001",
            CustomerId = 7,
            TotalAmount = 80_000_000m,
            PaidAmount = 0m,
            DueDate = DateTime.Today.AddDays(-15)
        });

        var aiProvider = new LocalHeuristicAiProvider();
        var generator = new ReminderDraftGenerator(custRepo, invRepo, aiProvider);

        // Act
        var draft = await generator.GenerateDraftAsync(new ReminderDraftRequestDto
        {
            CustomerId = 7,
            Tone = tone
        });

        // Assert
        Assert.NotNull(draft);
        Assert.Equal(7, draft.CustomerId);
        Assert.Equal("Công ty Bách Khoa", draft.CustomerName);
        Assert.Equal(80_000_000m, draft.TotalDebtAmount);
        Assert.Equal(tone, draft.Tone);
        Assert.False(string.IsNullOrWhiteSpace(draft.Subject));
        Assert.False(string.IsNullOrWhiteSpace(draft.Body));
        Assert.True(draft.Body.Contains("HD-BK-001") || draft.Body.Contains("80,000,000") || draft.Body.Contains("80.000.000") || draft.Body.Contains("Bách Khoa"));
        Assert.True(draft.RequiresStaffConfirmation);
    }

    [Fact]
    public async Task AIProvider_Unconfigured_GracefullyDegradesToFallback()
    {
        // Arrange: Cấu hình rỗng hoặc không có API Key
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "AI:ApiKey", "" }
        }).Build();

        var fallbackProvider = new LocalHeuristicAiProvider();
        var gemini = new GeminiAiProvider(new HttpClient(), config, NullLogger<GeminiAiProvider>.Instance, fallbackProvider);

        // Act
        Assert.False(gemini.IsConfigured);
        var response = await gemini.GenerateTextAsync(new AiPrompt
        {
            SystemInstruction = "Soạn thảo thư nhắc nợ",
            UserPrompt = "REMINDER_DRAFT cho khách hàng A"
        });

        // Assert: Thành công qua cơ chế Fallback
        Assert.True(response.Success);
        Assert.True(response.IsFallback);
        Assert.False(string.IsNullOrWhiteSpace(response.Content));
    }

    [Fact]
    public void AIProvider_SanitizePrompt_MasksSensitivePII()
    {
        // Arrange
        string rawPrompt = "Khách hàng Nguyễn Văn A, SĐT: 0912345678, Email: nguyenvana@company.com, STK: 0071000888999 nợ 50.000.000 đ";

        // Act
        string sanitized = GeminiAiProvider.SanitizePrompt(rawPrompt);

        // Assert: Đã che số điện thoại, email và số tài khoản
        Assert.DoesNotContain("0912345678", sanitized);
        Assert.Contains("09***-xxx", sanitized);
        Assert.DoesNotContain("nguyenvana@company.com", sanitized);
        Assert.Contains("***@company.com", sanitized);
        Assert.Contains("***0071000888999", sanitized);
    }
}
