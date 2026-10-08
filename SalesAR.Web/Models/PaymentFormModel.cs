namespace SalesAR.Web.Models;

using System.ComponentModel.DataAnnotations;

public class PaymentFormModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn khách hàng cần thu tiền.")]
    public int CustomerId { get; set; }

    [Required(ErrorMessage = "Số phiếu thu là bắt buộc.")]
    [StringLength(50, ErrorMessage = "Số phiếu thu không quá 50 ký tự.")]
    public string PaymentNumber { get; set; } = $"PT-{DateTime.Now:yyyyMMdd-HHmmss}";

    [Required(ErrorMessage = "Ngày thu tiền là bắt buộc.")]
    public DateTime PaymentDate { get; set; } = DateTime.Today;

    [Range(1, 100000000000, ErrorMessage = "Số tiền thanh toán phải lớn hơn 0.")]
    public decimal Amount { get; set; }

    [StringLength(500, ErrorMessage = "Ghi chú không quá 500 ký tự.")]
    public string? Note { get; set; }
}
