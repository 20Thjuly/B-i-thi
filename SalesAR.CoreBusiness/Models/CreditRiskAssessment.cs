namespace SalesAR.CoreBusiness.Models;

using System;

public class CreditRiskAssessment
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public DateTime AssessedAt { get; set; } = DateTime.UtcNow;
    public string ModelVersion { get; set; } = "v1.0-RuleBasedScoring";
    public decimal RiskScore { get; set; }
    public string RiskLevel { get; set; } = "Low"; // Low, Medium, High, Critical
    public bool RequiresHumanReview { get; set; }
    public string ExplanationFactors { get; set; } = string.Empty;
    public string? RecommendedAction { get; set; }
    public string AssessedBy { get; set; } = "AI Assistant";
    public string? HumanReviewedBy { get; set; }
    public DateTime? HumanReviewedAt { get; set; }
    public string? HumanReviewNotes { get; set; }

    // Navigation / Display
    public string? CustomerCode { get; set; }
    public string? CustomerName { get; set; }
}
