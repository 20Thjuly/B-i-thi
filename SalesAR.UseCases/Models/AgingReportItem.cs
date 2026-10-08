namespace SalesAR.UseCases.Models;

public class AgingReportItem
{
    public int CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal CreditLimit { get; set; }
    public decimal CurrentDebt { get; set; }
    public decimal Bucket0To30 { get; set; }
    public decimal Bucket31To60 { get; set; }
    public decimal Bucket61To90 { get; set; }
    public decimal BucketOver90 { get; set; }
    public decimal TotalOutstanding => Bucket0To30 + Bucket31To60 + Bucket61To90 + BucketOver90;
    public decimal AvailableCredit => CreditLimit - CurrentDebt;
}
