namespace SalesAR.UseCases.Models;

public class AgingReportItem
{
    public int CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal CreditLimit { get; set; }
    public decimal TotalDebt { get; set; }
    public decimal CurrentDebt => TotalDebt;
    public decimal Bucket0To30 { get; set; }
    public decimal Bucket31To60 { get; set; }
    public decimal Bucket61To90 { get; set; }
    public decimal BucketOver90 { get; set; }
    public decimal AvailableCredit => Math.Max(0, CreditLimit - TotalDebt);
}

public class AgingReportSummary
{
    public IEnumerable<AgingReportItem> Items { get; set; } = Enumerable.Empty<AgingReportItem>();
    public decimal TotalCreditLimit => Items.Sum(i => i.CreditLimit);
    public decimal TotalDebt => Items.Sum(i => i.TotalDebt);
    public decimal Total0To30 => Items.Sum(i => i.Bucket0To30);
    public decimal Total31To60 => Items.Sum(i => i.Bucket31To60);
    public decimal Total61To90 => Items.Sum(i => i.Bucket61To90);
    public decimal TotalOver90 => Items.Sum(i => i.BucketOver90);
}
