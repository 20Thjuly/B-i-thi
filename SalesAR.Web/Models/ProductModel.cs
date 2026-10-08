namespace SalesAR.Web.Models;

using System.ComponentModel.DataAnnotations;

public class ProductModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Mã sản phẩm là bắt buộc.")]
    [StringLength(50, ErrorMessage = "Mã sản phẩm không quá 50 ký tự.")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tên sản phẩm là bắt buộc.")]
    [StringLength(200, ErrorMessage = "Tên sản phẩm không quá 200 ký tự.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Đơn vị tính là bắt buộc.")]
    [StringLength(50, ErrorMessage = "Đơn vị tính không quá 50 ký tự.")]
    public string Unit { get; set; } = "Thùng";

    [Range(0.01, 1000000000, ErrorMessage = "Đơn giá phải lớn hơn 0.")]
    public decimal Price { get; set; } = 100000m;

    [Range(0, 1000000, ErrorMessage = "Số lượng tồn kho không được âm.")]
    public int StockQuantity { get; set; } = 100;

    public bool IsActive { get; set; } = true;
}
