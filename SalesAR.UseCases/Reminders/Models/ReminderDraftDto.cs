namespace SalesAR.UseCases.Reminders.Models;

public class ReminderDraftDto
{
    public int CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string RecipientEmail { get; set; } = string.Empty;
    public int? ReminderRuleId { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public string Tone { get; set; } = "Polite";
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public List<int> InvoiceIds { get; set; } = new();
    public decimal TotalDebtAmount { get; set; }
    public bool AutoSendApproved { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
}
