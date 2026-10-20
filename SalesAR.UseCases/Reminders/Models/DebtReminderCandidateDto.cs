namespace SalesAR.UseCases.Reminders.Models;

using SalesAR.UseCases.Models;

public class DebtReminderCandidateDto
{
    public int CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string RecipientEmail { get; set; } = string.Empty;
    public bool IsInDispute { get; set; }
    public string? DisputeReason { get; set; }
    public string? SpecialAgreementNotes { get; set; }
    
    public int ReminderRuleId { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public string TriggerType { get; set; } = "Overdue";
    public string Tone { get; set; } = "Polite";
    public bool AutoSendApproved { get; set; }

    public List<InvoiceOutstandingDto> Invoices { get; set; } = new();
    public decimal TotalDebtAmount { get; set; }
    public int MaxDaysOverdue { get; set; }
    public int MinDaysUntilDue { get; set; }
    
    public string SuggestedIdempotencyKey { get; set; } = string.Empty;
    public bool AlreadySentToday { get; set; }
    public string? LastLogStatus { get; set; }
}
