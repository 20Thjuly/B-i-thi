namespace SalesAR.Plugins.Reminders;

using Microsoft.Extensions.Logging;
using SalesAR.UseCases.Reminders;
using SalesAR.UseCases.Reminders.Models;

public class FakeEmailSender : IEmailSender
{
    private readonly ILogger<FakeEmailSender>? _logger;

    public bool SimulateFailure { get; set; } = false;
    public bool SimulateTransient { get; set; } = true;
    public string FailureMessage { get; set; } = "SMTP Server timeout: Không thể kết nối tới máy chủ gửi thư tạm thời.";
    public List<(string To, string Subject, string Body, DateTime SentAt)> SentHistory { get; } = new();

    public FakeEmailSender(ILogger<FakeEmailSender>? logger = null)
    {
        _logger = logger;
    }

    public Task<EmailSendResult> SendEmailAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
        {
            return Task.FromResult(EmailSendResult.Failed("Địa chỉ email người nhận không được để trống.", isTransient: false));
        }

        if (SimulateFailure)
        {
            _logger?.LogWarning("[FakeEmailSender] Giả lập gửi email THẤT BẠI tới: {ToEmail} - Lỗi: {Error}", toEmail, FailureMessage);
            return Task.FromResult(EmailSendResult.Failed(FailureMessage, isTransient: SimulateTransient));
        }

        var messageId = $"SIM-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}";
        SentHistory.Add((toEmail, subject, body, DateTime.UtcNow));

        _logger?.LogInformation("[FakeEmailSender] Gửi email thành công tới {ToEmail} | Tiêu đề: {Subject} | MessageId: {MessageId}", 
            toEmail, subject, messageId);

        return Task.FromResult(EmailSendResult.Ok(messageId));
    }
}
