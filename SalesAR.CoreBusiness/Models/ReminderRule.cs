namespace SalesAR.CoreBusiness.Models;

public class ReminderRule
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DaysOffset { get; set; }
    public string TriggerType { get; set; } = "Overdue"; // UpcomingDue, Overdue
    public string Tone { get; set; } = "Polite"; // Polite, Firm, Urgent, Legal
    public string TemplateSubject { get; set; } = string.Empty;
    public string TemplateBody { get; set; } = string.Empty;
    public bool AutoSendApproved { get; set; } = false;
    public bool IsActive { get; set; } = true;
}
