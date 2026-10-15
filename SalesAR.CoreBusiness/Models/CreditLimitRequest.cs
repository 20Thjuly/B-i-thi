namespace SalesAR.CoreBusiness.Models;

public class CreditLimitRequest
{
    public int Id { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public decimal TransactionAmount { get; set; }
    public decimal CurrentDebtSnapshot { get; set; }
    public decimal CreditLimitSnapshot { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // 'Pending', 'Approved', 'Rejected', 'Cancelled', 'Expired'
    public string RequestedBy { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? DecisionReason { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public DateTime? UsedAt { get; set; }
    public int? UsedInvoiceId { get; set; }
}
