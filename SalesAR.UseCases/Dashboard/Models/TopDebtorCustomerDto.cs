namespace SalesAR.UseCases.Dashboard.Models;

public class TopDebtorCustomerDto
{
    public int CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal CreditLimit { get; set; }
    public decimal TotalDebt { get; set; }
    public decimal OverdueDebt { get; set; }
    public int UnpaidInvoiceCount { get; set; }
    public int OverdueInvoiceCount { get; set; }

    /// <summary>
    /// Tỷ lệ sử dụng hạn mức tín dụng (%): TotalDebt / CreditLimit * 100
    /// </summary>
    public decimal CreditUtilizationRate => CreditLimit > 0 
        ? Math.Round(TotalDebt / CreditLimit * 100m, 1) 
        : (TotalDebt > 0 ? 100m : 0m);

    /// <summary>
    /// Tỷ trọng công nợ trên toàn hệ thống (%): TotalDebt / SystemTotalDebt * 100
    /// </summary>
    public decimal DebtSharePercentage { get; set; }
}
