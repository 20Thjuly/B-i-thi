namespace SalesAR.Plugins.Reminders;

using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SalesAR.UseCases.Reminders;
using SalesAR.UseCases.Reminders.Models;

public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailSender>? _logger;
    private readonly FakeEmailSender _fallbackSender;

    public SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender>? logger = null)
    {
        _configuration = configuration;
        _logger = logger;
        _fallbackSender = new FakeEmailSender();
    }

    public async Task<EmailSendResult> SendEmailAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
    {
        var smtpHost = _configuration["EmailSettings:SmtpServer"];
        bool isEnabled = bool.TryParse(_configuration["EmailSettings:Enabled"], out bool enabled) && enabled;

        // Nếu chưa bật hoặc chưa cấu hình SMTP thật, fallback an toàn sang FakeEmailSender
        if (!isEnabled || string.IsNullOrWhiteSpace(smtpHost))
        {
            _logger?.LogInformation("[SmtpEmailSender] Chưa bật cấu hình SMTP thật (EmailSettings:Enabled = false), chuyển tiếp sang FakeEmailSender.");
            return await _fallbackSender.SendEmailAsync(toEmail, subject, body, cancellationToken);
        }

        try
        {
            int port = int.TryParse(_configuration["EmailSettings:Port"], out int p) ? p : 587;
            string fromEmail = _configuration["EmailSettings:SenderEmail"] ?? "no-reply@salesar.vn";
            string fromName = _configuration["EmailSettings:SenderName"] ?? "Hệ thống Bán hàng & Quản lý Công nợ SalesAR";
            string username = _configuration["EmailSettings:Username"] ?? "";
            string password = _configuration["EmailSettings:Password"] ?? "";
            bool enableSsl = !bool.TryParse(_configuration["EmailSettings:EnableSsl"], out bool ssl) || ssl;

            using var client = new SmtpClient(smtpHost, port)
            {
                EnableSsl = enableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Timeout = 10000 // 10s
            };

            if (!string.IsNullOrWhiteSpace(username))
            {
                client.Credentials = new NetworkCredential(username, password);
            }

            using var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };
            message.To.Add(toEmail);

            await client.SendMailAsync(message, cancellationToken);

            var messageId = $"SMTP-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}";
            _logger?.LogInformation("[SmtpEmailSender] Gửi email THẬT thành công tới: {ToEmail} (MessageId: {MessageId})", toEmail, messageId);
            return EmailSendResult.Ok(messageId);
        }
        catch (SmtpException ex)
        {
            _logger?.LogError(ex, "[SmtpEmailSender] Lỗi máy chủ SMTP tạm thời khi gửi tới {ToEmail}: {Error}", toEmail, ex.Message);
            return EmailSendResult.Failed($"Lỗi kết nối SMTP ({ex.StatusCode}): {ex.Message}", isTransient: true);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[SmtpEmailSender] Lỗi gửi email tới {ToEmail}: {Error}", toEmail, ex.Message);
            return EmailSendResult.Failed($"Lỗi gửi thư: {ex.Message}", isTransient: false);
        }
    }
}
