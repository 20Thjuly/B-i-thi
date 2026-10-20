namespace SalesAR.UseCases.AI;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SalesAR.UseCases.AI.Models;
using SalesAR.UseCases.AI.Providers;
using SalesAR.UseCases.PluginInterfaces;

public class CustomerArSummaryUseCase : ICustomerArSummaryUseCase
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IAIProvider _aiProvider;

    public CustomerArSummaryUseCase(
        ICustomerRepository customerRepository,
        IInvoiceRepository invoiceRepository,
        IPaymentRepository paymentRepository,
        IAIProvider aiProvider)
    {
        _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
        _invoiceRepository = invoiceRepository ?? throw new ArgumentNullException(nameof(invoiceRepository));
        _paymentRepository = paymentRepository ?? throw new ArgumentNullException(nameof(paymentRepository));
        _aiProvider = aiProvider ?? throw new ArgumentNullException(nameof(aiProvider));
    }

    public async Task<CustomerArSummaryResultDto> SummarizeCustomerAsync(int customerId, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        DateTime asOfDate = DateTime.Today;

        // 1. TRÍCH XUẤT DỮ LIỆU SỰ THẬT TỪ HỆ THỐNG (GROUND TRUTH EXTRACTION)
        var customer = await _customerRepository.GetByIdAsync(customerId);
        if (customer == null)
            throw new KeyNotFoundException($"Không tìm thấy khách hàng có ID = {customerId}");

        var debtDetails = await _customerRepository.GetCustomerDebtDetailsAsync(customerId);
        var invoices = (await _invoiceRepository.GetOutstandingInvoicesByCustomerAsync(customerId)).ToList();
        var payments = (await _paymentRepository.GetPaymentsByCustomerAsync(customerId)).OrderByDescending(p => p.PaymentDate).ToList();

        var groundTruth = new CustomerArGroundTruthDto
        {
            CustomerId = customer.Id,
            CustomerCode = customer.Code,
            CustomerName = customer.Name,
            Email = customer.Email ?? string.Empty,
            Phone = customer.Phone ?? string.Empty,
            CreditLimit = customer.CreditLimit,
            IsInDispute = customer.IsInDispute,
            DisputeReason = customer.DisputeReason
        };

        if (debtDetails != null)
        {
            groundTruth.TotalInvoicedAmount = debtDetails.TotalInvoiced;
            groundTruth.TotalPaidAmount = debtDetails.TotalPaid;
            groundTruth.TotalAdjustedAmount = debtDetails.TotalAdjusted;
            groundTruth.CurrentDebt = debtDetails.CurrentDebt;
        }

        // Lịch sử thanh toán
        groundTruth.TotalPaymentsCount = payments.Count;
        if (payments.Any())
        {
            var lastPayment = payments.First();
            groundTruth.LastPaymentDate = lastPayment.PaymentDate;
            groundTruth.LastPaymentAmount = lastPayment.Amount;
        }

        // Phân loại hóa đơn
        var openInvoices = new List<CustomerArInvoiceSummaryDto>();
        int overdueCount = 0;
        decimal overdueAmount = 0;
        int upcomingCount = 0;
        decimal upcomingAmount = 0;
        int maxOverdue = 0;
        var overdueDaysList = new List<int>();

        foreach (var inv in invoices.Where(i => i.RemainingAmount > 0))
        {
            int daysOverdue = (asOfDate.Date - inv.DueDate.Date).Days;
            if (daysOverdue > 0)
            {
                overdueCount++;
                overdueAmount += inv.RemainingAmount;
                maxOverdue = Math.Max(maxOverdue, daysOverdue);
                overdueDaysList.Add(daysOverdue);
            }
            else if (daysOverdue >= -7) // Sắp đến hạn trong vòng 7 ngày
            {
                upcomingCount++;
                upcomingAmount += inv.RemainingAmount;
            }

            openInvoices.Add(new CustomerArInvoiceSummaryDto
            {
                InvoiceId = inv.Id,
                InvoiceNumber = inv.InvoiceNumber,
                InvoiceDate = inv.InvoiceDate,
                DueDate = inv.DueDate,
                TotalAmount = inv.TotalAmount,
                RemainingAmount = inv.RemainingAmount,
                DaysOverdue = daysOverdue,
                Status = inv.Status,
                IsInDispute = customer.IsInDispute
            });
        }

        groundTruth.UpcomingInvoicesCount = upcomingCount;
        groundTruth.UpcomingInvoicesAmount = upcomingAmount;
        groundTruth.OverdueInvoicesCount = overdueCount;
        groundTruth.OverdueInvoicesAmount = overdueAmount;
        groundTruth.MaxDaysOverdue = maxOverdue;
        groundTruth.AverageDaysLate = overdueDaysList.Any() ? Math.Round(overdueDaysList.Average(), 1) : 0;
        groundTruth.OpenInvoices = openInvoices;

        // XÁC ĐỊNH DẤU HIỆU CẢNH BÁO ƯU TIÊN (PRIORITY WARNING SIGNALS)
        var warnings = new List<string>();
        if (groundTruth.CreditLimit > 0 && groundTruth.CurrentDebt > groundTruth.CreditLimit)
        {
            warnings.Add($"VƯỢT HẠN MỨC: Dư nợ ({groundTruth.CurrentDebt:N0} đ) vượt quá hạn mức tín dụng ({groundTruth.CreditLimit:N0} đ).");
        }
        else if (groundTruth.CreditLimitUtilizationRate >= 80m)
        {
            warnings.Add($"CẢNH BÁO HẠN MỨC: Đã sử dụng {groundTruth.CreditLimitUtilizationRate}% hạn mức tín dụng được cấp.");
        }

        if (groundTruth.MaxDaysOverdue > 30)
        {
            warnings.Add($"QUÁ HẠN DÀI NGÀY: Có hóa đơn quá hạn nghiêm trọng tới {groundTruth.MaxDaysOverdue} ngày (nhóm nợ rủi ro).");
        }
        else if (groundTruth.OverdueInvoicesCount > 0)
        {
            warnings.Add($"PHÁT SINH NỢ QUÁ HẠN: Có {groundTruth.OverdueInvoicesCount} hóa đơn quá hạn với tổng số tiền {groundTruth.OverdueInvoicesAmount:N0} đ.");
        }

        if (groundTruth.IsInDispute)
        {
            warnings.Add($"ĐANG KHIẾU NẠI / TRANH CHẤP: Khách hàng đang có trạng thái tranh chấp (Lý do: {groundTruth.DisputeReason ?? "Chưa rõ"}).");
        }

        if (!payments.Any() && groundTruth.CurrentDebt > 0)
        {
            warnings.Add("CHƯA CÓ LỊCH SỬ THANH TOÁN: Khách hàng chưa phát sinh giao dịch thu tiền nào trên hệ thống.");
        }

        groundTruth.PriorityWarningSignals = warnings;

        // 2. SINH PHẦN DIỄN GIẢI TỔNG HỢP QUA AI (AI NARRATIVE GENERATION)
        var narrative = await GenerateAiNarrativeAsync(groundTruth, cancellationToken);
        sw.Stop();

        return new CustomerArSummaryResultDto
        {
            GroundTruth = groundTruth,
            AiNarrative = narrative,
            IsAiGenerated = true,
            AiProvider = _aiProvider.ProviderName,
            AiModel = _aiProvider.IsConfigured ? "gemini-1.5-flash" : "LocalHeuristic-v1.0",
            ProcessingTimeMs = sw.ElapsedMilliseconds,
            IsFallbackMode = !_aiProvider.IsConfigured,
            GeneratedAt = DateTime.UtcNow
        };
    }

    private async Task<CustomerAiNarrativeDto> GenerateAiNarrativeAsync(CustomerArGroundTruthDto truth, CancellationToken cancellationToken)
    {
        string prompt = $@"
Dữ liệu công nợ thực tế của khách hàng:
- Tên khách hàng: {truth.CustomerName} ({truth.CustomerCode})
- Hạn mức tín dụng: {truth.CreditLimit:N0} đ
- Tổng dư nợ hiện tại: {truth.CurrentDebt:N0} đ
- Tỷ lệ sử dụng hạn mức: {truth.CreditLimitUtilizationRate}%
- Hóa đơn sắp đến hạn (7 ngày tới): {truth.UpcomingInvoicesCount} hóa đơn, trị giá {truth.UpcomingInvoicesAmount:N0} đ
- Hóa đơn quá hạn: {truth.OverdueInvoicesCount} hóa đơn, trị giá {truth.OverdueInvoicesAmount:N0} đ
- Quá hạn lâu nhất: {truth.MaxDaysOverdue} ngày (Trung bình trễ: {truth.AverageDaysLate} ngày)
- Lần trả tiền gần nhất: {(truth.LastPaymentDate.HasValue ? truth.LastPaymentDate.Value.ToString("dd/MM/yyyy") : "Chưa có")} ({truth.LastPaymentAmount:N0} đ)
- Trạng thái khiếu nại: {(truth.IsInDispute ? $"Có khiếu nại: {truth.DisputeReason}" : "Không có")}

Hãy tổng hợp bản tóm tắt tình hình tài chính ngắn gọn, chuyên nghiệp cho giám đốc tài chính.";

        var aiPrompt = new AiPrompt
        {
            SystemInstruction = 
                "Bạn là trợ lý AI chuyên gia phân tích công nợ khách hàng (Accounts Receivable Specialist). " +
                "QUY TẮC CỐT LÕI: Chỉ dựa vào các con số thực tế được cung cấp, tuyệt đối không bịa đặt số liệu. " +
                "Đưa ra đánh giá khách quan, nêu rõ rủi ro và khuyến nghị hành động cho bộ phận công nợ.",
            UserPrompt = prompt,
            Temperature = 0.2
        };

        var response = await _aiProvider.GenerateTextAsync(aiPrompt, cancellationToken);

        var narrative = new CustomerAiNarrativeDto();

        if (response.Success && !string.IsNullOrWhiteSpace(response.Content))
        {
            narrative.ExecutiveSummary = response.Content;
        }
        else
        {
            narrative.ExecutiveSummary = 
                $"Khách hàng {truth.CustomerName} hiện có tổng dư nợ {truth.CurrentDebt:N0} đ, " +
                $"sử dụng {truth.CreditLimitUtilizationRate}% hạn mức ({truth.CreditLimit:N0} đ). " +
                $"Có {truth.OverdueInvoicesCount} hóa đơn quá hạn ({truth.OverdueInvoicesAmount:N0} đ).";
        }

        // Tạo cấu trúc phân tích
        if (truth.OverdueInvoicesCount > 0 || truth.CreditLimitUtilizationRate > 80m || truth.IsInDispute)
        {
            narrative.PaymentHealthAssessment = "Tình trạng thanh toán ở mức CẦN THEO DÕI ĐẶC BIỆT. Khách hàng có xu hướng kéo dài kỳ hạn nợ hoặc sử dụng gần hết hạn mức thương mại.";
            narrative.RecommendedAction = "Tạm ngừng cấp thêm hạn mức tín dụng mới cho các đơn hàng bổ sung; liên hệ kế toán đối soát và đôn đốc thanh toán các khoản quá hạn.";
        }
        else
        {
            narrative.PaymentHealthAssessment = "Tình trạng thanh toán ở mức LÀNH MẠNH. Dư nợ trong tầm kiểm soát an toàn và tuân thủ kỳ hạn thanh toán cam kết.";
            narrative.RecommendedAction = "Tiếp tục duy trì chính sách bán hàng và hạn mức tín dụng hiện hành; theo dõi các hóa đơn sắp đến hạn trong tuần.";
        }

        narrative.KeyRiskHighlights = truth.PriorityWarningSignals.ToList();

        return narrative;
    }
}
