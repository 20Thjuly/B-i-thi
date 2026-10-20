namespace SalesAR.UseCases.Dashboard.Models;

public class ArDashboardFilterDto
{
    /// <summary>
    /// Ngày mốc đối soát công nợ & tuổi nợ (Mặc định: ngày hiện tại)
    /// </summary>
    public DateTime AsOfDate { get; set; } = DateTime.Today;

    /// <summary>
    /// Từ ngày phân tích dòng tiền & doanh thu (Mặc định: 30 ngày trước)
    /// </summary>
    public DateTime FromDate { get; set; } = DateTime.Today.AddDays(-30);

    /// <summary>
    /// Đến ngày phân tích dòng tiền & doanh thu (Mặc định: ngày hiện tại)
    /// </summary>
    public DateTime ToDate { get; set; } = DateTime.Today;

    /// <summary>
    /// Lọc theo khách hàng cụ thể (null = tất cả khách hàng)
    /// </summary>
    public int? CustomerId { get; set; }

    /// <summary>
    /// Lọc theo trạng thái hóa đơn: "All", "Unpaid", "PartiallyPaid", "Overdue", "InDispute"
    /// </summary>
    public string? InvoiceStatus { get; set; } = "All";

    /// <summary>
    /// Lọc theo nhóm tuổi nợ: "All", "Current", "1To30", "31To60", "61To90", "Over90"
    /// </summary>
    public string? AgingBucket { get; set; } = "All";

    /// <summary>
    /// Trang hiện tại (1-based index)
    /// </summary>
    public int PageIndex { get; set; } = 1;

    /// <summary>
    /// Số lượng bản ghi trên một trang (Mặc định: 10)
    /// </summary>
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// Vị trí offset an toàn cho SQL OFFSET ... FETCH NEXT
    /// </summary>
    public int Offset => Math.Max(0, (PageIndex - 1) * Math.Max(1, PageSize));
}
