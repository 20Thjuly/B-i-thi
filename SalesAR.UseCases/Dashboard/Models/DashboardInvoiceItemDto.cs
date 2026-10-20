namespace SalesAR.UseCases.Dashboard.Models;

public class DashboardInvoiceItemDto
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal AdjustedAmount { get; set; }
    public decimal RemainingAmount => Math.Max(0, TotalAmount - PaidAmount - AdjustedAmount);
    public int DaysOverdue { get; set; }
    public string AgingCategory { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsInDispute { get; set; }
    public string? DisputeReason { get; set; }
}
