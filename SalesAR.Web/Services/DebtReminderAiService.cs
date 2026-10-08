namespace SalesAR.Web.Services;

using System.Text;
using SalesAR.UseCases.AI;
using SalesAR.UseCases.Models;

public class DebtReminderAiService : IDebtReminderAiService
{
    public Task<string> DraftReminderEmailAsync(CustomerDebtDto customerDebt, IEnumerable<InvoiceOutstandingDto> unpaidInvoices, string tone = "polite")
    {
        if (customerDebt == null)
            throw new ArgumentNullException(nameof(customerDebt));

        var sb = new StringBuilder();
        var invoices = (unpaidInvoices ?? Enumerable.Empty<InvoiceOutstandingDto>()).ToList();

        // 1. Tiêu đề Email
        sb.AppendLine($"[THƯ NHẮC NỢ {(tone == "urgent" ? "KHẨN CẤP" : "ĐỊNH KỲ")}] - ĐỐI SOÁT CÔNG NỢ KHÁCH HÀNG: {customerDebt.CustomerName.ToUpper()}");
        sb.AppendLine("--------------------------------------------------------------------------------");
        sb.AppendLine();

        // 2. Lời chào
        sb.AppendLine($"Kính gửi: Ban Giám đốc và Bộ phận Kế toán - {customerDebt.CustomerName},");
        sb.AppendLine();
        sb.AppendLine("Công ty chúng tôi xin gửi lời chào trân trọng và lời chúc sức khỏe, thành công đến Quý công ty.");
        sb.AppendLine();

        // 3. Nội dung nhắc nợ tùy theo mức độ / văn phong
        if (tone == "urgent")
        {
            sb.AppendLine("Căn cứ vào hợp đồng mua bán và biên bản giao hàng đã ký kết giữa hai bên, hệ thống ghi nhận một số khoản công nợ của Quý khách đã quá hạn thanh toán.");
            sb.AppendLine("Để không ảnh hưởng đến hạn mức tín dụng và tiến độ các đơn hàng tiếp theo, chúng tôi đề nghị Quý khách ưu tiên thanh toán dứt điểm các hóa đơn quá hạn sau:");
        }
        else
        {
            sb.AppendLine("Theo định kỳ đối soát công nợ khách hàng, Phòng Tài chính - Kế toán xin gửi đến Quý khách thông tin chi tiết về số dư công nợ phải thu tính đến thời điểm hiện tại như sau:");
        }
        sb.AppendLine();

        // 4. Bảng kê hóa đơn chi tiết
        sb.AppendLine($"• Mã khách hàng: {customerDebt.CustomerCode}");
        sb.AppendLine($"• Hạn mức tín dụng được cấp: {customerDebt.CreditLimit:N0} VNĐ");
        sb.AppendLine($"• Tổng công nợ hiện tại: {customerDebt.CurrentDebt:N0} VNĐ");
        sb.AppendLine();
        sb.AppendLine("DANH SÁCH HÓA ĐƠN CẦN THANH TOÁN (THEO NGUYÊN TẮC FIFO):");

        if (invoices.Any())
        {
            int stt = 1;
            foreach (var inv in invoices)
            {
                int overdueDays = (int)(DateTime.Today - inv.InvoiceDate).TotalDays;
                string statusText = overdueDays > 30 ? $"(Quá hạn {overdueDays - 30} ngày)" : "(Trong hạn thanh toán)";
                sb.AppendLine($"  {stt++}. Số HĐ: {inv.InvoiceNumber} | Ngày lập: {inv.InvoiceDate:dd/MM/yyyy} | Còn nợ: {inv.RemainingAmount:N0} VNĐ {statusText}");
            }
        }
        else
        {
            sb.AppendLine("  (Tất cả hóa đơn đã được cấn trừ hoàn tất)");
        }
        sb.AppendLine();

        // 5. Thông tin thanh toán & Call to Action
        sb.AppendLine("THÔNG TIN TÀI KHOẢN THỤ HƯỞNG:");
        sb.AppendLine("• Chủ tài khoản: CÔNG TY CỔ PHẦN THƯƠNG MẠI SALES & AR");
        sb.AppendLine("• Số tài khoản: 19036888888888 tại Techcombank - CN Hà Nội");
        sb.AppendLine($"• Nội dung chuyển khoản: [{customerDebt.CustomerCode}] Thanh toan cong no thang {DateTime.Now.Month}");
        sb.AppendLine();
        sb.AppendLine("Nếu Quý công ty đã thực hiện lệnh chuyển tiền gần đây, xin vui lòng bỏ qua thư nhắc nợ này hoặc gửi bản sao Ủy nhiệm chi qua email để chúng tôi cập nhật vào hệ thống.");
        sb.AppendLine();
        sb.AppendLine("Trân trọng cảm ơn sự hợp tác của Quý khách hàng!");
        sb.AppendLine("Phòng Tài chính - Kế toán Công ty");
        sb.AppendLine("Hotline: 1900 6868 | Email: ketoan@salesar.vn");

        return Task.FromResult(sb.ToString());
    }
}
