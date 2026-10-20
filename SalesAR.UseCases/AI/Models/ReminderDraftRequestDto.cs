namespace SalesAR.UseCases.AI.Models;

using System.Collections.Generic;

public class ReminderDraftRequestDto
{
    public int CustomerId { get; set; }
    
    /// <summary>
    /// Danh sách hóa đơn được chọn để nhắc nợ (nếu rỗng, tự động lấy các hóa đơn quá hạn/sắp đến hạn)
    /// </summary>
    public List<int> InvoiceIds { get; set; } = new();

    /// <summary>
    /// Giọng điệu mong muốn: "Polite" (Lịch sự, hòa nhã), "Neutral" (Trung lập, chuyên nghiệp), "Formal" (Trang trọng, kiên quyết)
    /// </summary>
    public string Tone { get; set; } = "Polite";

    /// <summary>
    /// Chỉ dẫn hoặc ghi chú bổ sung của nhân viên khi tạo thư
    /// </summary>
    public string? CustomInstructions { get; set; }
}

public class AiReminderDraftResultDto
{
    public int CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string RecipientEmail { get; set; } = string.Empty;
    public string Tone { get; set; } = "Polite";

    /// <summary>
    /// Tiêu đề thư do AI đề xuất
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Nội dung thư đầy đủ số liệu thật do AI đề xuất
    /// </summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Tổng dư nợ cần thanh toán
    /// </summary>
    public decimal TotalDebtAmount { get; set; }

    /// <summary>
    /// Danh sách hóa đơn bao gồm trong thư
    /// </summary>
    public List<CustomerArInvoiceSummaryDto> Invoices { get; set; } = new();

    /// <summary>
    /// Cờ nguồn gốc AI
    /// </summary>
    public bool IsAiGenerated { get; set; } = true;
    public string GeneratorModel { get; set; } = string.Empty;

    /// <summary>
    /// QUY TẮC: Bắt buộc nhân viên phải xác nhận đã kiểm tra và phê duyệt trước khi gửi
    /// </summary>
    public bool RequiresStaffConfirmation { get; set; } = true;
}
