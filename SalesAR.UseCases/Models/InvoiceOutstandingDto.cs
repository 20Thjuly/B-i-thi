namespace SalesAR.UseCases.Models;

public class InvoiceOutstandingDto
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount => Math.Max(0, TotalAmount - PaidAmount);
    public string Status { get; set; } = "Unpaid";
}
