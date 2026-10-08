namespace SalesAR.Web.Models;

using System.ComponentModel.DataAnnotations;

public class CustomerModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Mã khách hàng là bắt buộc.")]
    [StringLength(50, ErrorMessage = "Mã khách hàng không quá 50 ký tự.")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tên khách hàng là bắt buộc.")]
    [StringLength(200, ErrorMessage = "Tên khách hàng không quá 200 ký tự.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Số điện thoại là bắt buộc.")]
    [RegularExpression(@"^(0[3|5|7|8|9])+([0-9]{8})$", ErrorMessage = "Số điện thoại không hợp lệ (10 chữ số, ví dụ 0901234567).")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Địa chỉ là bắt buộc.")]
    [StringLength(500, ErrorMessage = "Địa chỉ không quá 500 ký tự.")]
    public string Address { get; set; } = string.Empty;

    [Range(0, 100000000000, ErrorMessage = "Hạn mức tín dụng không được âm.")]
    public decimal CreditLimit { get; set; } = 10000000m;

    [Range(0, 365, ErrorMessage = "Thời hạn thanh toán phải từ 0 đến 365 ngày.")]
    public int PaymentTermDays { get; set; } = 30;

    public bool IsActive { get; set; } = true;
}
