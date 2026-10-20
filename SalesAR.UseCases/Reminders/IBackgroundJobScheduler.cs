namespace SalesAR.UseCases.Reminders;

public interface IBackgroundJobScheduler
{
    Task TriggerDueRemindersJobAsync(CancellationToken cancellationToken = default);
    bool IsJobRunning { get; }
    DateTime? LastRunTime { get; }
    string? LastRunResult { get; }
}
