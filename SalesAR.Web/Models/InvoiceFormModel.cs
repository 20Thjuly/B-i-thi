namespace SalesAR.Web.Models;

using System.ComponentModel.DataAnnotations;

public class InvoiceFormModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn khách hàng.")]
    public int CustomerId { get; set; }

    [Required(ErrorMessage = "Số hóa đơn là bắt buộc.")]
    [StringLength(50, ErrorMessage = "Số hóa đơn không quá 50 ký tự.")]
    public string InvoiceNumber { get; set; } = $"HD-{DateTime.Now:yyyyMMdd-HHmmss}";

    [Required(ErrorMessage = "Ngày lập hóa đơn là bắt buộc.")]
    public DateTime InvoiceDate { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Ngày hết hạn thanh toán là bắt buộc.")]
    public DateTime DueDate { get; set; } = DateTime.Today.AddDays(30);

    public List<InvoiceLineFormModel> Lines { get; set; } = new();

    public decimal TotalAmount => Lines.Sum(l => l.Amount);
}

public class InvoiceLineFormModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn sản phẩm.")]
    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    [Range(1, 1000000, ErrorMessage = "Số lượng phải lớn hơn 0.")]
    public int Quantity { get; set; } = 1;

    [Range(0, 1000000000, ErrorMessage = "Đơn giá không được âm.")]
    public decimal UnitPrice { get; set; }

    public decimal Amount => Quantity * UnitPrice;
}
