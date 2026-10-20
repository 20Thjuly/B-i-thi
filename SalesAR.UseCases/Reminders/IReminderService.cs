namespace SalesAR.UseCases.Reminders;

using SalesAR.CoreBusiness.Models;
using SalesAR.UseCases.Reminders.Models;

public interface IReminderService
{
    Task<IEnumerable<DebtReminderCandidateDto>> IdentifyReminderCandidatesAsync(DateTime asOfDate);
    Task<ReminderDraftDto> GenerateReminderDraftAsync(int customerId, List<int> invoiceIds, int? ruleId = null, DateTime? asOfDate = null);
    Task<ReminderLog> QueueOrSendReminderAsync(ReminderDraftDto draft, string? confirmedBy = null, bool forceSend = false, CancellationToken cancellationToken = default);
    Task<int> ProcessPendingQueueAsync(CancellationToken cancellationToken = default);
    Task<int> ProcessAutomaticRemindersJobAsync(DateTime asOfDate, CancellationToken cancellationToken = default);
    Task<bool> PauseReminderForDisputeAsync(int reminderLogId, string reason, string actor);
    Task<bool> ResumeReminderFromDisputeAsync(int reminderLogId, string actor);
    Task<IEnumerable<ReminderLog>> GetReminderLogsAsync(int? customerId = null, string? status = null);
    Task<IEnumerable<ReminderRule>> GetReminderRulesAsync();
}
