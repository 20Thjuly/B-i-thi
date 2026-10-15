namespace SalesAR.CoreBusiness.Models;

public class CreditLimitApproval
{
    public int Id { get; set; }
    public int RequestId { get; set; }
    public string Action { get; set; } = string.Empty; // 'SUBMIT', 'APPROVE', 'REJECT', 'CANCEL', 'EXPIRE'
    public string Actor { get; set; } = string.Empty;
    public DateTime ActionAt { get; set; } = DateTime.UtcNow;
    public string? Comment { get; set; }
    public decimal CurrentDebtAtDecision { get; set; }
    public decimal CreditLimitAtDecision { get; set; }
}
