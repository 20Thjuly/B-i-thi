namespace SalesAR.UseCases.Models;

public class CustomerDebtDto
{
    public int CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalInvoiced { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal CurrentDebt { get; set; }
    public decimal CreditLimit { get; set; }
    public decimal AvailableCredit => Math.Max(0, CreditLimit - CurrentDebt);
}
