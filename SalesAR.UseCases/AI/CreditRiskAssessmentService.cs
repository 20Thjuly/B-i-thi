namespace SalesAR.UseCases.AI;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SalesAR.CoreBusiness.Models;
using SalesAR.UseCases.AI.Models;
using SalesAR.UseCases.PluginInterfaces;

public class CreditRiskAssessmentService : ICreditRiskAssessmentService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly ICreditRiskAssessmentRepository _assessmentRepository;

    public const string CurrentModelVersion = "v1.0-RuleBasedScoring";

    public CreditRiskAssessmentService(
        ICustomerRepository customerRepository,
        IInvoiceRepository invoiceRepository,
        IPaymentRepository paymentRepository,
        ICreditRiskAssessmentRepository assessmentRepository)
    {
        _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
        _invoiceRepository = invoiceRepository ?? throw new ArgumentNullException(nameof(invoiceRepository));
        _paymentRepository = paymentRepository ?? throw new ArgumentNullException(nameof(paymentRepository));
        _assessmentRepository = assessmentRepository ?? throw new ArgumentNullException(nameof(assessmentRepository));
    }

    public async Task<CreditRiskAssessmentResultDto> AssessCustomerRiskAsync(int customerId, string assessedBy = "AI Assistant", CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId);
        if (customer == null)
            throw new KeyNotFoundException($"Không tìm thấy khách hàng ID = {customerId}");

        var debtDetails = await _customerRepository.GetCustomerDebtDetailsAsync(customerId);
        var invoices = (await _invoiceRepository.GetOutstandingInvoicesByCustomerAsync(customerId)).ToList();
        var payments = (await _paymentRepository.GetPaymentsByCustomerAsync(customerId)).ToList();

        decimal currentDebt = debtDetails?.CurrentDebt ?? 0m;
        decimal creditLimit = customer.CreditLimit;
        DateTime today = DateTime.Today;

        // BẮT ĐẦU VỚI ĐIỂM CHUẨN 100 ĐIỂM (TỐT NHẤT)
        decimal score = 100m;
        var factors = new List<RiskFactorExplanationDto>();

        // 1. ĐÁNH GIÁ TỶ LỆ SỬ DỤNG HẠN MỨC TÍN DỤNG (Credit Limit Utilization)
        if (creditLimit > 0)
        {
            decimal utilization = Math.Round(currentDebt / creditLimit * 100m, 1);
            if (currentDebt > creditLimit)
            {
                score -= 35m;
                factors.Add(new RiskFactorExplanationDto
                {
                    Category = "Hạn mức tín dụng",
                    Description = $"Dư nợ vượt quá hạn mức được cấp ({utilization}% > 100%). Vượt {currentDebt - creditLimit:N0} đ.",
                    ScoreImpact = -35m,
                    Severity = "Danger"
                });
            }
            else if (utilization >= 80m)
            {
                score -= 20m;
                factors.Add(new RiskFactorExplanationDto
                {
                    Category = "Hạn mức tín dụng",
                    Description = $"Sử dụng hạn mức ở mức cao ({utilization}% >= 80%). Dư địa tín dụng còn ít.",
                    ScoreImpact = -20m,
                    Severity = "Warning"
                });
            }
            else if (utilization >= 50m)
            {
                score -= 10m;
                factors.Add(new RiskFactorExplanationDto
                {
                    Category = "Hạn mức tín dụng",
                    Description = $"Sử dụng hạn mức ở mức trung bình ({utilization}% >= 50%).",
                    ScoreImpact = -10m,
                    Severity = "Normal"
                });
            }
            else
            {
                factors.Add(new RiskFactorExplanationDto
                {
                    Category = "Hạn mức tín dụng",
                    Description = $"Hạn mức tín dụng an toàn ({utilization}% < 50%). Còn khả dụng {Math.Max(0, creditLimit - currentDebt):N0} đ.",
                    ScoreImpact = 0m,
                    Severity = "Good"
                });
            }
        }
        else if (currentDebt > 0)
        {
            score -= 20m;
            factors.Add(new RiskFactorExplanationDto
            {
                Category = "Hạn mức tín dụng",
                Description = "Khách hàng không được thiết lập hạn mức tín dụng nhưng đang phát sinh nợ tồn đọng.",
                ScoreImpact = -20m,
                Severity = "Warning"
            });
        }

        // 2. ĐÁNH GIÁ ĐỘ TRỄ KỲ HẠN & TUỔI NỢ (Aging Analysis)
        int maxOverdueDays = 0;
        decimal totalOverdue = 0;
        foreach (var inv in invoices.Where(i => i.RemainingAmount > 0))
        {
            int days = (today - inv.DueDate.Date).Days;
            if (days > 0)
            {
                totalOverdue += inv.RemainingAmount;
                maxOverdueDays = Math.Max(maxOverdueDays, days);
            }
        }

        if (maxOverdueDays > 60)
        {
            score -= 30m;
            factors.Add(new RiskFactorExplanationDto
            {
                Category = "Tuổi nợ",
                Description = $"Phát sinh nợ quá hạn nghiêm trọng trên 60 ngày (Trễ tối đa: {maxOverdueDays} ngày, Tổng nợ trễ: {totalOverdue:N0} đ).",
                ScoreImpact = -30m,
                Severity = "Danger"
            });
        }
        else if (maxOverdueDays > 30)
        {
            score -= 20m;
            factors.Add(new RiskFactorExplanationDto
            {
                Category = "Tuổi nợ",
                Description = $"Phát sinh nợ quá hạn từ 31 đến 60 ngày (Trễ tối đa: {maxOverdueDays} ngày, Tổng nợ trễ: {totalOverdue:N0} đ).",
                ScoreImpact = -20m,
                Severity = "Warning"
            });
        }
        else if (maxOverdueDays > 0)
        {
            score -= 10m;
            factors.Add(new RiskFactorExplanationDto
            {
                Category = "Tuổi nợ",
                Description = $"Phát sinh nợ quá hạn ngắn hạn dưới 30 ngày (Trễ tối đa: {maxOverdueDays} ngày).",
                ScoreImpact = -10m,
                Severity = "Normal"
            });
        }
        else
        {
            factors.Add(new RiskFactorExplanationDto
            {
                Category = "Tuổi nợ",
                Description = "Tất cả các khoản nợ đều trong hạn thanh toán, không phát sinh quá hạn.",
                ScoreImpact = 0m,
                Severity = "Good"
            });
        }

        // 3. ĐÁNH GIÁ TRANH CHẤP / KHIẾU NẠI (Dispute Impact)
        if (customer.IsInDispute)
        {
            score -= 20m;
            factors.Add(new RiskFactorExplanationDto
            {
                Category = "Tranh chấp",
                Description = $"Khách hàng đang có khiếu nại hoặc tranh chấp công nợ mở ({customer.DisputeReason ?? "Chưa rõ lý do"}).",
                ScoreImpact = -20m,
                Severity = "Danger"
            });
        }

        // 4. ĐÁNH GIÁ LỊCH SỬ THANH TOÁN (Payment Track Record)
        if (!payments.Any() && currentDebt > 0)
        {
            score -= 15m;
            factors.Add(new RiskFactorExplanationDto
            {
                Category = "Lịch sử thanh toán",
                Description = "Chưa ghi nhận bất kỳ chứng từ thanh toán nào từ khách hàng này kể từ khi tạo tài khoản.",
                ScoreImpact = -15m,
                Severity = "Warning"
            });
        }
        else if (payments.Any())
        {
            var lastPayment = payments.OrderByDescending(p => p.PaymentDate).First();
            int daysSinceLastPayment = (today - lastPayment.PaymentDate.Date).Days;
            if (daysSinceLastPayment > 90 && currentDebt > 0)
            {
                score -= 10m;
                factors.Add(new RiskFactorExplanationDto
                {
                    Category = "Lịch sử thanh toán",
                    Description = $"Đã {daysSinceLastPayment} ngày chưa có dòng tiền thanh toán mới từ khách hàng.",
                    ScoreImpact = -10m,
                    Severity = "Warning"
                });
            }
        }

        // Giới hạn điểm trong thang 0 - 100
        score = Math.Clamp(score, 0m, 100m);

        // PHÂN CẤP RỦI RO & ĐIỀU KIỆN CẦN NGƯỜI DUYỆT (RequiresHumanReview)
        string riskLevel;
        bool requiresHumanReview;
        string recommendedAction;

        if (score >= 80m && !customer.IsInDispute)
        {
            riskLevel = "Thấp";
            requiresHumanReview = false;
            recommendedAction = "Duy trì hạn mức tín dụng và chính sách bán hàng bình thường. Tiếp tục đối soát định kỳ.";
        }
        else if (score >= 60m && !customer.IsInDispute)
        {
            riskLevel = "Trung bình";
            requiresHumanReview = false;
            recommendedAction = "Theo dõi sát sao tiến độ thanh toán của các hóa đơn sắp đến hạn. Không cấp vượt hạn mức.";
        }
        else if (score >= 40m)
        {
            riskLevel = "Cao";
            requiresHumanReview = true;
            recommendedAction = "Yêu cầu thanh toán các khoản quá hạn trước khi giao đơn hàng mới. Đề xuất Kế toán trưởng xem xét hạ hạn mức.";
        }
        else
        {
            riskLevel = "Nghiêm trọng";
            requiresHumanReview = true;
            recommendedAction = "Đình chỉ cấp tín dụng bán chịu; chuyển hồ sơ sang bộ phận pháp chế / chế tài thu nợ đặc biệt. Cần Giám đốc Tài chính phê duyệt xử lý.";
        }

        // Nếu có khiếu nại tranh chấp thì luôn bắt buộc người kiểm tra
        if (customer.IsInDispute)
        {
            requiresHumanReview = true;
        }

        // LƯU KẾT QUẢ VÀO BẢNG CreditRiskAssessments ĐỂ AUDIT
        var entity = new CreditRiskAssessment
        {
            CustomerId = customerId,
            AssessedAt = DateTime.UtcNow,
            ModelVersion = CurrentModelVersion,
            RiskScore = score,
            RiskLevel = riskLevel,
            RequiresHumanReview = requiresHumanReview,
            ExplanationFactors = JsonSerializer.Serialize(factors),
            RecommendedAction = recommendedAction,
            AssessedBy = assessedBy
        };

        int assessmentId = await _assessmentRepository.SaveAssessmentAsync(entity);

        return new CreditRiskAssessmentResultDto
        {
            AssessmentId = assessmentId,
            CustomerId = customerId,
            CustomerCode = customer.Code,
            CustomerName = customer.Name,
            AssessedAt = entity.AssessedAt,
            ModelVersion = CurrentModelVersion,
            RiskScore = score,
            RiskLevel = riskLevel,
            RequiresHumanReview = requiresHumanReview,
            Factors = factors,
            RecommendedAction = recommendedAction
        };
    }

    public async Task<IEnumerable<CreditRiskAssessment>> GetAssessmentHistoryAsync(int customerId)
    {
        return await _assessmentRepository.GetHistoryByCustomerAsync(customerId, 10);
    }

    public async Task<bool> ReviewAssessmentAsync(int assessmentId, string reviewedBy, string reviewNotes)
    {
        var existing = await _assessmentRepository.GetByIdAsync(assessmentId);
        if (existing == null) return false;

        await _assessmentRepository.UpdateReviewAsync(assessmentId, reviewedBy, reviewNotes, DateTime.UtcNow);
        return true;
    }
}
