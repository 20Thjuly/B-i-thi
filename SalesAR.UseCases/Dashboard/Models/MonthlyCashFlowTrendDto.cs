namespace SalesAR.UseCases.Dashboard.Models;

public class MonthlyCashFlowTrendDto
{
    public string YearMonth { get; set; } = string.Empty; // "2026-05"
    public string Label { get; set; } = string.Empty; // "Tháng 05/2026"
    
    /// <summary>
    /// Doanh thu bán hàng xuất hóa đơn trong tháng
    /// </summary>
    public decimal InvoicedAmount { get; set; }

    /// <summary>
    /// Dòng tiền thực thu qua các phiếu thu Payments trong tháng
    /// </summary>
    public decimal CashCollectedAmount { get; set; }

    /// <summary>
    /// Chênh lệch dòng tiền ròng: Thực thu - Doanh thu xuất bán
    /// Dương: Thu tiền vượt doanh thu xuất bán mới trong tháng
    /// Âm: Đang cấp tín dụng / đọng vốn nhiều hơn dòng tiền thu về
    /// </summary>
    public decimal NetCashGap => CashCollectedAmount - InvoicedAmount;

    /// <summary>
    /// Tỷ lệ thu hồi dòng tiền trên doanh số xuất (%)
    /// </summary>
    public decimal CollectionEfficiencyRate => InvoicedAmount > 0 
        ? Math.Round(CashCollectedAmount / InvoicedAmount * 100m, 1) 
        : (CashCollectedAmount > 0 ? 100m : 0m);
}
