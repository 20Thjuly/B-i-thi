namespace SalesAR.UseCases.AI;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SalesAR.UseCases.AI.Models;
using SalesAR.UseCases.AI.Providers;
using SalesAR.UseCases.PluginInterfaces;

public class ReminderDraftGenerator : IReminderDraftGenerator
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IAIProvider _aiProvider;

    public ReminderDraftGenerator(
        ICustomerRepository customerRepository,
        IInvoiceRepository invoiceRepository,
        IAIProvider aiProvider)
    {
        _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
        _invoiceRepository = invoiceRepository ?? throw new ArgumentNullException(nameof(invoiceRepository));
        _aiProvider = aiProvider ?? throw new ArgumentNullException(nameof(aiProvider));
    }

    public async Task<AiReminderDraftResultDto> GenerateDraftAsync(ReminderDraftRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var customer = await _customerRepository.GetByIdAsync(request.CustomerId);
        if (customer == null)
            throw new KeyNotFoundException($"Không tìm thấy khách hàng ID = {request.CustomerId}");

        var allInvoices = (await _invoiceRepository.GetOutstandingInvoicesByCustomerAsync(request.CustomerId)).ToList();
        
        // Lọc danh sách hóa đơn theo yêu cầu hoặc lấy các hóa đơn còn nợ
        var selectedInvoices = request.InvoiceIds != null && request.InvoiceIds.Any()
            ? allInvoices.Where(i => request.InvoiceIds.Contains(i.Id) && i.RemainingAmount > 0).ToList()
            : allInvoices.Where(i => i.RemainingAmount > 0).ToList();

        decimal totalDebt = selectedInvoices.Sum(i => i.RemainingAmount);
        DateTime today = DateTime.Today;

        var invoiceSummaries = selectedInvoices.Select(inv => new CustomerArInvoiceSummaryDto
        {
            InvoiceId = inv.Id,
            InvoiceNumber = inv.InvoiceNumber,
            InvoiceDate = inv.InvoiceDate,
            DueDate = inv.DueDate,
            TotalAmount = inv.TotalAmount,
            RemainingAmount = inv.RemainingAmount,
            DaysOverdue = (today - inv.DueDate.Date).Days,
            Status = inv.Status,
            IsInDispute = customer.IsInDispute
        }).ToList();

        // 1. CHUẨN BỊ THÔNG TIN SỰ THẬT (GROUNDED DATA)
        var sbInvoiceList = new StringBuilder();
        foreach (var inv in invoiceSummaries)
        {
            string overdueText = inv.DaysOverdue > 0 
                ? $"Quá hạn {inv.DaysOverdue} ngày" 
                : $"Đến hạn {inv.DueDate:dd/MM/yyyy}";
            sbInvoiceList.AppendLine($"- HĐ số {inv.InvoiceNumber} (Ngày: {inv.InvoiceDate:dd/MM/yyyy}): Số dư còn nợ {inv.RemainingAmount:N0} đ [{overdueText}]");
        }

        string tone = string.IsNullOrWhiteSpace(request.Tone) ? "Polite" : request.Tone;
        string toneDescription = tone switch
        {
            "Formal" => "Trang trọng, kiên quyết, nhấn mạnh trách nhiệm hợp đồng và chế tài tạm ngừng cung cấp hàng hóa/dịch vụ nếu tiếp tục trễ hạn.",
            "Neutral" => "Trung lập, khách quan, mang tính đối soát công nợ chuyên nghiệp và nhắc hạn thanh toán định kỳ.",
            _ => "Lịch sự, hòa nhã, cảm ơn sự hợp tác của khách hàng và trân trọng nhắc nhở kế hoạch thanh toán đến hạn."
        };

        string prompt = $@"
Thông tin dữ liệu thực tế:
- Khách hàng: {customer.Name} ({customer.Code})
- Email nhận: {customer.Email ?? "ke-toan@khachhang.vn"}
- Tổng dư nợ cần thanh toán: {totalDebt:N0} VNĐ
- Danh sách hóa đơn:
{sbInvoiceList}
- Tài khoản thụ hưởng công ty: Ngân hàng TMCP Ngoại Thương Việt Nam (Vietcombank) - Chi nhánh TP.HCM, Số TK: 0071000888999, Tên TK: CÔNG TY TNHH PHÂN PHỐI BÁN HÀNG VÀ CÔNG NỢ SALESAR
- Yêu cầu giọng điệu: {toneDescription}
- Ghi chú thêm của nhân viên: {(string.IsNullOrWhiteSpace(request.CustomInstructions) ? "Không có" : request.CustomInstructions)}

Yêu cầu xuất ra 2 phần rõ ràng:
TIÊU ĐỀ: [Tiêu đề thư email]
NỘI DUNG:
[Nội dung thư hoàn chỉnh bằng tiếng Việt chuẩn văn phòng, tích hợp chính xác danh sách hóa đơn và số tài khoản ngân hàng trên]";

        var aiPrompt = new AiPrompt
        {
            SystemInstruction = 
                "Bạn là chuyên viên giao tiếp công nợ doanh nghiệp. " +
                "Nhiệm vụ: Soạn thảo email nhắc nợ chuyên nghiệp bằng tiếng Việt. " +
                "QUY TẮC BẮT BUỘC: Giữ nguyên số tiền, số hóa đơn, ngày tháng và số tài khoản thụ hưởng. Không bịa số liệu.",
            UserPrompt = prompt,
            Temperature = 0.3
        };

        var response = await _aiProvider.GenerateTextAsync(aiPrompt, cancellationToken);

        string subject = string.Empty;
        string body = string.Empty;

        if (response.Success && !response.IsFallback && !string.IsNullOrWhiteSpace(response.Content))
        {
            ParseSubjectAndBody(response.Content, tone, customer.Name, totalDebt, out subject, out body);
        }
        else
        {
            // Dự phòng nội dung chuẩn chỉnh chất lượng cao nếu AI provider chưa có API key hoặc ở chế độ Fallback
            GenerateFallbackDraft(tone, customer.Name, totalDebt, sbInvoiceList.ToString(), out subject, out body);
        }

        return new AiReminderDraftResultDto
        {
            CustomerId = customer.Id,
            CustomerCode = customer.Code,
            CustomerName = customer.Name,
            RecipientEmail = customer.Email ?? "ke-toan@doitac.com",
            Tone = tone,
            Subject = subject,
            Body = body,
            TotalDebtAmount = totalDebt,
            Invoices = invoiceSummaries,
            IsAiGenerated = true,
            GeneratorModel = _aiProvider.IsConfigured ? "Gemini-1.5-Flash" : "LocalHeuristic-Template",
            RequiresStaffConfirmation = true
        };
    }

    private static void ParseSubjectAndBody(string raw, string tone, string customerName, decimal totalDebt, out string subject, out string body)
    {
        string[] lines = raw.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        subject = string.Empty;
        var bodyBuilder = new StringBuilder();
        bool inBody = false;

        foreach (var line in lines)
        {
            if (line.StartsWith("TIÊU ĐỀ:", StringComparison.OrdinalIgnoreCase) || line.StartsWith("SUBJECT:", StringComparison.OrdinalIgnoreCase))
            {
                subject = line.Substring(line.IndexOf(':') + 1).Trim();
            }
            else if (line.StartsWith("NỘI DUNG:", StringComparison.OrdinalIgnoreCase) || line.StartsWith("BODY:", StringComparison.OrdinalIgnoreCase))
            {
                inBody = true;
            }
            else if (inBody)
            {
                bodyBuilder.AppendLine(line);
            }
            else if (string.IsNullOrEmpty(subject) && line.Contains("nhắc nợ", StringComparison.OrdinalIgnoreCase))
            {
                subject = line.Trim();
            }
        }

        body = bodyBuilder.ToString().Trim();

        if (string.IsNullOrWhiteSpace(subject))
        {
            subject = tone switch
            {
                "Formal" => $"[THÔNG BÁO KHẨN] Đề nghị tất toán công nợ quá hạn - {customerName}",
                "Neutral" => $"Đối soát và nhắc thanh toán công nợ kỳ này - {customerName}",
                _ => $"Thư nhắc lịch thanh toán công nợ đến hạn - {customerName}"
            };
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            body = raw;
        }
    }

    private static void GenerateFallbackDraft(string tone, string customerName, decimal totalDebt, string invoiceListText, out string subject, out string body)
    {
        switch (tone)
        {
            case "Formal":
                subject = $"[THÔNG BÁO ĐÔN ĐỐC] Đề nghị thanh toán dứt điểm công nợ quá hạn - {customerName}";
                body = 
$@"Kính gửi: Ban Giám đốc & Phòng Kế toán Quý Công ty {customerName},

Công ty chúng tôi xin trân trọng gửi lời chào và thông báo về tình hình công nợ thương mại giữa hai đơn vị.

Theo biên bản đối soát và dữ liệu kế toán đến thời điểm hiện tại, Quý Công ty hiện có các khoản công nợ đã quá hạn thanh toán với tổng giá trị là: {totalDebt:N0} VNĐ.

Chi tiết các hóa đơn quá hạn:
{invoiceListText}
Để đảm bảo quyền lợi hợp đồng và không làm gián đoạn kế hoạch giao nhận các lô hàng tiếp theo, kính đề nghị Quý Công ty khẩn trương thực hiện thanh toán chuyển khoản trước ngày làm việc tiếp theo:

Thông tin thanh toán thụ hưởng:
- Đơn vị thụ hưởng: CÔNG TY TNHH PHÂN PHỐI BÁN HÀNG VÀ CÔNG NỢ SALESAR
- Số tài khoản: 0071000888999 tại Ngân hàng TMCP Ngoại Thương Việt Nam (Vietcombank) - Chi nhánh TP.HCM
- Nội dung: Thanh toan cong no {customerName}

Rất mong nhận được sự hợp tác giải quyết dứt điểm từ Quý Công ty.

Trân trọng kính thư,
Phòng Quản lý Công nợ & Thu hồi vốn";
                break;

            case "Neutral":
                subject = $"Bảng kê đối soát và nhắc thanh toán công nợ định kỳ - {customerName}";
                body = 
$@"Kính gửi: Bộ phận Kế toán - Quý Công ty {customerName},

Phòng Kế toán xin gửi đến Quý Đơn vị bảng kê đối soát các hóa đơn đang phát sinh đến hạn và quá hạn với tổng số tiền {totalDebt:N0} VNĐ.

Danh sách hóa đơn chi tiết:
{invoiceListText}
Kính đề nghị Quý Công ty kiểm tra, đối soát và sắp xếp lịch chuyển khoản thanh toán vào tài khoản công ty chúng tôi:
- Tên tài khoản: CÔNG TY TNHH PHÂN PHỐI BÁN HÀNG VÀ CÔNG NỢ SALESAR
- STK: 0071000888999 - Ngân hàng Vietcombank - CN TP.HCM

Nếu đã hoàn tất lệnh chuyển tiền hoặc có thắc mắc về chứng từ, xin vui lòng phản hồi lại email này kèm ủy nhiệm chi.

Trân trọng cảm ơn sự phối hợp của Quý Anh/Chị!
Phòng Tài chính Kế toán";
                break;

            default: // Polite
                subject = $"Nhắc lịch thanh toán hóa đơn đến hạn - {customerName}";
                body = 
$@"Kính gửi: Quý Khách hàng {customerName},

Công ty SalesAR xin trân trọng cảm ơn sự tin tưởng và hợp tác của Quý Khách hàng trong suốt thời gian qua.

Hệ thống quản lý công nợ xin gửi lời nhắc thân thiện về các khoản thanh toán sắp đến hạn và đến hạn của Quý Đơn vị với tổng số tiền là: {totalDebt:N0} VNĐ.

Chi tiết danh sách hóa đơn:
{invoiceListText}
Quý Khách vui lòng thu xếp thanh toán theo thông tin chuyển khoản:
- Số tài khoản: 0071000888999
- Ngân hàng: Vietcombank - Chi nhánh TP.HCM
- Tên thụ hưởng: CÔNG TY TNHH PHÂN PHỐI BÁN HÀNG VÀ CÔNG NỢ SALESAR

Chúc Quý Doanh nghiệp ngày càng phát triển và thành công!

Trân trọng cảm ơn,
Bộ phận Chăm sóc Khách hàng & Công nợ";
                break;
        }
    }
}
