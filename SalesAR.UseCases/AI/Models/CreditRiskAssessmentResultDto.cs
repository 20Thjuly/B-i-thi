namespace SalesAR.UseCases.AI.Models;

using System;
using System.Collections.Generic;

public class CreditRiskAssessmentResultDto
{
    public int AssessmentId { get; set; }
    public int CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;

    public DateTime AssessedAt { get; set; } = DateTime.UtcNow;
    public string ModelVersion { get; set; } = "v1.0-RuleBasedScoring";
    
    /// <summary>
    /// Điểm rủi ro tín dụng (0 - 100). Điểm càng cao rủi ro càng thấp (tương tự thang điểm tín nhiệm).
    /// </summary>
    public decimal RiskScore { get; set; }

    /// <summary>
    /// Phân cấp rủi ro: "Thấp" (Low), "Trung bình" (Medium), "Cao" (High), "Nghiêm trọng" (Critical)
    /// </summary>
    public string RiskLevel { get; set; } = "Thấp";

    /// <summary>
    /// Bắt buộc người kiểm tra (Kế toán trưởng / Giám đốc duyệt) khi rủi ro Cao/Nghiêm trọng hoặc có khiếu nại
    /// </summary>
    public bool RequiresHumanReview { get; set; }

    /// <summary>
    /// Danh sách các yếu tố giải thích chi tiết vì sao có điểm số này (Explainability Factors)
    /// </summary>
    public List<RiskFactorExplanationDto> Factors { get; set; } = new();

    /// <summary>
    /// Khuyến nghị hành động
    /// </summary>
    public string RecommendedAction { get; set; } = string.Empty;

    /// <summary>
    /// Ranh giới thẩm quyền & Guardrail bắt buộc
    /// </summary>
    public string AuthorityDisclaimer { get; set; } = 
        "CẢNH BÁO THẨM QUYỀN: Đánh giá này do hệ thống phân tích hỗ trợ. AI TUYỆT ĐỐI KHÔNG tự động từ chối đơn hàng, " +
        "không tự động thay đổi hạn mức tín dụng, không sửa thanh toán và không tự phê duyệt giao dịch. Quyền quyết định thuộc về Giám đốc tài chính / Kế toán trưởng.";

    // Trạng thái kiểm tra bởi con người
    public string? HumanReviewedBy { get; set; }
    public DateTime? HumanReviewedAt { get; set; }
    public string? HumanReviewNotes { get; set; }
}

public class RiskFactorExplanationDto
{
    public string Category { get; set; } = string.Empty; // Tuổi nợ, Hạn mức, Lịch sử trả tiền, Tranh chấp
    public string Description { get; set; } = string.Empty;
    public decimal ScoreImpact { get; set; } // Điểm cộng/trừ
    public string Severity { get; set; } = "Normal"; // Good, Warning, Danger
}
