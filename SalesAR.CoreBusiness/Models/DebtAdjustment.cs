namespace SalesAR.CoreBusiness.Models;

public class DebtAdjustment
{
    public int Id { get; set; }
    public string AdjustmentNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public int? InvoiceId { get; set; }
    public DateTime AdjustmentDate { get; set; } = DateTime.UtcNow;
    public string AdjustmentType { get; set; } = "ManualCorrection"; // 'CreditLimitChange', 'BadDebtWriteOff', 'Discount', 'Return', 'ManualCorrection'
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = "System";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
