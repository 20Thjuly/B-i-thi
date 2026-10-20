namespace SalesAR.UseCases.Dashboard.Models;

public class CashCollectionForecastDto
{
    public string PeriodKey { get; set; } = string.Empty;
    public string PeriodLabel { get; set; } = string.Empty;
    public string DateRangeText { get; set; } = string.Empty;
    public int InvoiceCount { get; set; }
    
    /// <summary>
    /// Giá trị danh nghĩa của các hóa đơn có kỳ hạn đáo hạn trong khoảng thời gian này
    /// </summary>
    public decimal NominalDueAmount { get; set; }

    /// <summary>
    /// Trọng số xác suất thu hồi dự kiến (0.00 -> 1.00) dựa trên nhóm kỳ hạn và rủi ro
    /// </summary>
    public decimal CollectionProbability { get; set; }

    /// <summary>
    /// Dòng tiền dự báo kỳ vọng (Expected Cash Inflow) = NominalDueAmount * CollectionProbability
    /// QUY TẮC: Tách biệt hoàn toàn khỏi tiền thực thu trong quá khứ.
    /// </summary>
    public decimal ExpectedCashInflow => Math.Round(NominalDueAmount * CollectionProbability, 0);

    /// <summary>
    /// Mức độ tin cậy của dự báo: "Cao" (High), "Trung bình" (Medium), "Thận trọng" (Conservative)
    /// </summary>
    public string ConfidenceLevel { get; set; } = "Trung bình";

    /// <summary>
    /// Phương pháp & Giả định tính toán
    /// </summary>
    public string BasisOfEstimation { get; set; } = string.Empty;
}
