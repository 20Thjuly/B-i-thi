namespace SalesAR.UseCases.AI.Models;

using System;
using System.Collections.Generic;

/// <summary>
/// Dữ liệu sự thật (Ground Truth) 100% được truy vấn từ cơ sở dữ liệu SQL Server SalesARDB.
/// AI TUYỆT ĐỐI KHÔNG ĐƯỢC TỰ BỊA ĐẶT HOẶC THAY ĐỔI CÁC SỐ LIỆU NÀY.
/// </summary>
public class CustomerArGroundTruthDto
{
    public int CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public decimal CreditLimit { get; set; }
    
    // Công nợ hiện tại
    public decimal TotalInvoicedAmount { get; set; }
    public decimal TotalPaidAmount { get; set; }
    public decimal TotalAdjustedAmount { get; set; }
    public decimal CurrentDebt { get; set; }
    public decimal AvailableCredit => Math.Max(0, CreditLimit - CurrentDebt);
    public decimal CreditLimitUtilizationRate => CreditLimit > 0 
        ? Math.Round(CurrentDebt / CreditLimit * 100m, 1) 
        : 0m;

    // Hóa đơn sắp đến hạn (trong vòng 7 ngày tới)
    public int UpcomingInvoicesCount { get; set; }
    public decimal UpcomingInvoicesAmount { get; set; }

    // Hóa đơn quá hạn
    public int OverdueInvoicesCount { get; set; }
    public decimal OverdueInvoicesAmount { get; set; }
    public int MaxDaysOverdue { get; set; }
    public double AverageDaysLate { get; set; }

    // Lịch sử thanh toán gần nhất
    public int TotalPaymentsCount { get; set; }
    public DateTime? LastPaymentDate { get; set; }
    public decimal LastPaymentAmount { get; set; }

    // Dấu hiệu tranh chấp
    public bool IsInDispute { get; set; }
    public string? DisputeReason { get; set; }

    // Danh sách các dấu hiệu cảnh báo cần ưu tiên theo dõi (Warning Signals)
    public List<string> PriorityWarningSignals { get; set; } = new();

    // Danh sách tóm tắt hóa đơn mở
    public List<CustomerArInvoiceSummaryDto> OpenInvoices { get; set; } = new();
}

public class CustomerArInvoiceSummaryDto
{
    public int InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public int DaysOverdue { get; set; }
    public string Status { get; set; } = "Unpaid";
    public bool IsInDispute { get; set; }
}
