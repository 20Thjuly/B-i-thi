namespace SalesAR.UseCases.Dashboard.Models;

public class AgingBucketSummaryDto
{
    // 1. Trong hạn (Current / Not Due: DaysOverdue <= 0)
    public decimal CurrentAmount { get; set; }
    public int CurrentCount { get; set; }

    // 2. Quá hạn 1 - 30 ngày (Overdue 1 - 30 days)
    public decimal Overdue1To30Amount { get; set; }
    public int Overdue1To30Count { get; set; }

    // 3. Quá hạn 31 - 60 ngày (Overdue 31 - 60 days)
    public decimal Overdue31To60Amount { get; set; }
    public int Overdue31To60Count { get; set; }

    // 4. Quá hạn 61 - 90 ngày (Overdue 61 - 90 days)
    public decimal Overdue61To90Amount { get; set; }
    public int Overdue61To90Count { get; set; }

    // 5. Quá hạn > 90 ngày (Overdue > 90 days - Nợ khó đòi)
    public decimal OverdueOver90Amount { get; set; }
    public int OverdueOver90Count { get; set; }

    // Tổng cộng
    public decimal TotalAmount => CurrentAmount + Overdue1To30Amount + Overdue31To60Amount + Overdue61To90Amount + OverdueOver90Amount;
    public int TotalCount => CurrentCount + Overdue1To30Count + Overdue31To60Count + Overdue61To90Count + OverdueOver90Count;

    // Tỷ lệ %
    public decimal CurrentPercentage => TotalAmount > 0 ? Math.Round(CurrentAmount / TotalAmount * 100m, 1) : 0m;
    public decimal Overdue1To30Percentage => TotalAmount > 0 ? Math.Round(Overdue1To30Amount / TotalAmount * 100m, 1) : 0m;
    public decimal Overdue31To60Percentage => TotalAmount > 0 ? Math.Round(Overdue31To60Amount / TotalAmount * 100m, 1) : 0m;
    public decimal Overdue61To90Percentage => TotalAmount > 0 ? Math.Round(Overdue61To90Amount / TotalAmount * 100m, 1) : 0m;
    public decimal OverdueOver90Percentage => TotalAmount > 0 ? Math.Round(OverdueOver90Amount / TotalAmount * 100m, 1) : 0m;
}
