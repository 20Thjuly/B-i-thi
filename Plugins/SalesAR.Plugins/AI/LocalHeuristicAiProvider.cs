namespace SalesAR.Plugins.AI;

using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using SalesAR.UseCases.AI.Providers;

public class LocalHeuristicAiProvider : IAIProvider
{
    public string ProviderName => "LocalHeuristicEngine";
    public bool IsConfigured => true;

    public Task<AiResponse> GenerateTextAsync(AiPrompt prompt, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        string userPrompt = prompt.UserPrompt ?? string.Empty;
        string resultText;

        if (userPrompt.Contains("NHẮC NỢ") || userPrompt.Contains("REMINDER_DRAFT") || prompt.SystemInstruction.Contains("thư nhắc nợ"))
        {
            resultText = GenerateReminderDraftNarrative(userPrompt);
        }
        else if (userPrompt.Contains("RỦI RO") || userPrompt.Contains("RISK_ASSESSMENT") || prompt.SystemInstruction.Contains("rủi ro"))
        {
            resultText = GenerateRiskAssessmentNarrative(userPrompt);
        }
        else
        {
            resultText = GenerateCustomerSummaryNarrative(userPrompt);
        }

        sw.Stop();
        return Task.FromResult(AiResponse.Successful(resultText, ProviderName, "RuleBased-NLG-v1.0", sw.ElapsedMilliseconds, isFallback: true));
    }

    private string GenerateCustomerSummaryNarrative(string prompt)
    {
        return 
            "Khách hàng duy trì quan hệ thương mại định kỳ với doanh nghiệp. " +
            "Các chỉ số công nợ phản ánh tình hình tài chính cần được giám sát theo quy trình kiểm soát rủi ro tiêu chuẩn. " +
            "Đề xuất bộ phận kinh doanh và kế toán phối hợp theo dõi kỳ hạn các hóa đơn chuẩn bị đáo hạn và chủ động đối soát định kỳ.";
    }

    private string GenerateRiskAssessmentNarrative(string prompt)
    {
        return 
            "Hệ thống ghi nhận các yếu tố rủi ro tập trung vào tỷ lệ sử dụng hạn mức và số ngày trễ hạn của các khoản phải thu. " +
            "Khuyến nghị áp dụng chính sách cấp tín dụng thận trọng, yêu cầu bảo lãnh hoặc thanh toán từng phần trước khi giao các lô hàng giá trị lớn tiếp theo.";
    }

    private string GenerateReminderDraftNarrative(string prompt)
    {
        return 
            "Kính gửi Quý Khách hàng,\n\n" +
            "Phòng Tài chính - Kế toán xin trân trọng cảm ơn sự hợp tác của Quý Đơn vị trong suốt thời gian qua.\n\n" +
            "Theo dữ liệu đối soát công nợ tính đến thời điểm hiện tại, chúng tôi xin gửi đến Quý Khách hàng danh sách các khoản thanh toán đến hạn và quá hạn cần được tất toán.\n\n" +
            "Kính đề nghị Quý Công ty sắp xếp kế hoạch tài chính và chuyển khoản thanh toán để đảm bảo tiến độ duy trì hạn mức thương mại cho các đơn hàng tiếp theo.\n\n" +
            "Trân trọng cảm ơn!";
    }
}
