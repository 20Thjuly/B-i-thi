namespace SalesAR.CoreBusiness.Models;

public class AuditLog
{
    public int Id { get; set; }
    public string TableName { get; set; } = string.Empty;
    public int RecordId { get; set; }
    public string Action { get; set; } = string.Empty; // 'INSERT', 'UPDATE', 'DELETE', 'STATUS_CHANGE', 'BALANCE_ADJUST'
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string ChangedBy { get; set; } = "System";
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public string? Reason { get; set; }
}
