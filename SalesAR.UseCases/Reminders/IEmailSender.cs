namespace SalesAR.UseCases.Reminders;

using SalesAR.UseCases.Reminders.Models;

public interface IEmailSender
{
    Task<EmailSendResult> SendEmailAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default);
}
