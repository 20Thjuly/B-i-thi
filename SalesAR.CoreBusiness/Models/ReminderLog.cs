namespace SalesAR.CoreBusiness.Models;

public class ReminderLog
{
    public int Id { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int? ReminderRuleId { get; set; }
    public string? RuleName { get; set; }
    public string RecipientEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Status { get; set; } = "Draft"; // Draft, PendingApproval, Queued, Sent, Failed, RetryPending, PausedDispute
    public int RetryCount { get; set; } = 0;
    public int MaxRetries { get; set; } = 3;
    public string? LastError { get; set; }
    public DateTime? SentAt { get; set; }
    public string? ConfirmedBy { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public string? InvoiceIdsCsv { get; set; }
    public decimal TotalDebtAmount { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
