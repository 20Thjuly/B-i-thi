namespace SalesAR.UseCases.Dashboard.Models;

public class ArKpiSummaryDto
{
    /// <summary>
    /// 1. TỔNG CÔNG NỢ PHẢI THU (Total AR Outstanding):
    /// Tổng số dư còn phải thu trên tất cả hóa đơn chưa tất toán (RemainingAmount > 0).
    /// Công thức: SUM(TotalAmount - PaidAmount - AdjustedAmount).
    /// </summary>
    public decimal TotalArOutstanding { get; set; }

    /// <summary>
    /// 2. TỔNG CÔNG NỢ QUÁ HẠN (Total Overdue AR):
    /// Tổng số dư còn phải thu trên các hóa đơn có DueDate < AsOfDate.
    /// </summary>
    public decimal TotalOverdueAr { get; set; }

    /// <summary>
    /// 3. TỔNG TIỀN ĐÃ THU TRONG KỲ (Total Cash Collected in Period):
    /// Dòng tiền thực tế nhận vào từ phiếu thu Payments trong khoảng [FromDate, ToDate].
    /// QUY TẮC: Tuyệt đối không đánh đồng với Doanh thu xuất hóa đơn.
    /// </summary>
    public decimal TotalCashCollectedInPeriod { get; set; }

    /// <summary>
    /// 4. TỔNG DOANH THU XUẤT HÓA ĐƠN TRONG KỲ (Total Credit Sales in Period):
    /// Giá trị hàng hóa xuất bán ghi nhận theo hóa đơn trong khoảng [FromDate, ToDate].
    /// </summary>
    public decimal TotalCreditSalesInPeriod { get; set; }

    /// <summary>
    /// 5. SỐ LƯỢNG HÓA ĐƠN CHƯA THANH TOÁN (Unpaid Invoices Count):
    /// Đếm số hóa đơn có RemainingAmount > 0 (gồm cả Unpaid và PartiallyPaid).
    /// </summary>
    public int UnpaidInvoicesCount { get; set; }

    /// <summary>
    /// 6. GIÁ TRỊ HÓA ĐƠN CHƯA THANH TOÁN (Unpaid Invoices Amount):
    /// Tổng dư nợ còn lại của các hóa đơn chưa tất toán (= TotalArOutstanding).
    /// </summary>
    public decimal UnpaidInvoicesAmount { get; set; }

    /// <summary>
    /// 7. SỐ LƯỢNG HÓA ĐƠN QUÁ HẠN (Overdue Invoices Count):
    /// Số lượng hóa đơn có DueDate < AsOfDate và còn nợ.
    /// </summary>
    public int OverdueInvoicesCount { get; set; }

    /// <summary>
    /// 8. TỔNG SỐ KHÁCH HÀNG (Total Customers Count)
    /// </summary>
    public int TotalCustomersCount { get; set; }

    /// <summary>
    /// 9. SỐ KHÁCH HÀNG ĐANG CÓ DƯ NỢ (Indebted Customers Count)
    /// </summary>
    public int IndebtedCustomersCount { get; set; }

    /// <summary>
    /// 10. TỶ LỆ CÔNG NỢ QUÁ HẠN THEO GIÁ TRỊ (Overdue Value Ratio %):
    /// Công thức: (TotalOverdueAr / TotalArOutstanding) * 100
    /// </summary>
    public decimal OverdueValueRatio => TotalArOutstanding > 0 
        ? Math.Round(TotalOverdueAr / TotalArOutstanding * 100m, 2) 
        : 0m;

    /// <summary>
    /// 11. TỶ LỆ HÓA ĐƠN QUÁ HẠN THEO SỐ LƯỢNG (Overdue Count Ratio %):
    /// Công thức: (OverdueInvoicesCount / UnpaidInvoicesCount) * 100
    /// </summary>
    public decimal OverdueCountRatio => UnpaidInvoicesCount > 0 
        ? Math.Round((decimal)OverdueInvoicesCount / UnpaidInvoicesCount * 100m, 2) 
        : 0m;

    /// <summary>
    /// 12. SỐ NGÀY THU HỒI NỢ BÌNH QUÂN DSO (Days Sales Outstanding):
    /// Công thức chuẩn tài chính:
    /// DSO = (TotalArOutstanding / TotalCreditSalesInPeriod) * DaysInPeriod
    /// Trả về null nếu doanh thu trong kỳ = 0 hoặc khoảng ngày không hợp lệ.
    /// </summary>
    public double? DsoDays { get; set; }

    /// <summary>
    /// Thuyết minh chi tiết cách tính DSO và tình trạng dữ liệu
    /// </summary>
    public string DsoExplanation { get; set; } = string.Empty;
}
