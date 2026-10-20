namespace SalesAR.UseCases.AI.Models;

using System;
using System.Collections.Generic;

/// <summary>
/// Kết quả tổng hợp: Phân định rạch ròi giữa Dữ liệu sự thật (GroundTruth) và Lời diễn giải từ AI (AiNarrative).
/// </summary>
public class CustomerArSummaryResultDto
{
    /// <summary>
    /// DỮ LIỆU SỰ THẬT TỪ HỆ THỐNG (GROUND TRUTH) - Tuyệt đối không thay đổi.
    /// </summary>
    public CustomerArGroundTruthDto GroundTruth { get; set; } = new();

    /// <summary>
    /// LỜI DIỄN GIẢI DO AI TỔNG HỢP (AI NARRATIVE)
    /// </summary>
    public CustomerAiNarrativeDto AiNarrative { get; set; } = new();

    /// <summary>
    /// Cờ xác nhận nội dung có phần do AI sinh ra
    /// </summary>
    public bool IsAiGenerated { get; set; } = true;

    /// <summary>
    /// Nhà cung cấp AI đã sử dụng (Gemini, Local Heuristic, OpenAI,...)
    /// </summary>
    public string AiProvider { get; set; } = string.Empty;
    public string AiModel { get; set; } = string.Empty;
    public long ProcessingTimeMs { get; set; }
    public bool IsFallbackMode { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

public class CustomerAiNarrativeDto
{
    /// <summary>
    /// Tóm tắt điều hành (Executive Summary) về tình trạng công nợ khách hàng
    /// </summary>
    public string ExecutiveSummary { get; set; } = string.Empty;

    /// <summary>
    /// Đánh giá sức khỏe thanh toán (Payment Health Assessment)
    /// </summary>
    public string PaymentHealthAssessment { get; set; } = string.Empty;

    /// <summary>
    /// Phân tích các dấu hiệu rủi ro chính (Key Risk Highlights)
    /// </summary>
    public List<string> KeyRiskHighlights { get; set; } = new();

    /// <summary>
    /// Hành động khuyến nghị cho nhân viên tín dụng / kế toán (Recommended Action)
    /// </summary>
    public string RecommendedAction { get; set; } = string.Empty;
}
