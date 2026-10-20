namespace SalesAR.UseCases.Reminders.Models;

public class EmailSendResult
{
    public bool Success { get; set; }
    public string? MessageId { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsTransientError { get; set; }

    public static EmailSendResult Ok(string? messageId = null) => new()
    {
        Success = true,
        MessageId = messageId ?? Guid.NewGuid().ToString("N")
    };

    public static EmailSendResult Failed(string error, bool isTransient = true) => new()
    {
        Success = false,
        ErrorMessage = error,
        IsTransientError = isTransient
    };
}
