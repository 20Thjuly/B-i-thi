namespace SalesAR.UseCases.PluginInterfaces;

using SalesAR.CoreBusiness.Models;

public interface IReminderRepository
{
    Task<IEnumerable<ReminderRule>> GetActiveRulesAsync();
    Task<ReminderRule?> GetRuleByIdAsync(int id);
    Task<ReminderLog?> GetByIdempotencyKeyAsync(string idempotencyKey);
    Task<ReminderLog?> GetLogByIdAsync(int id);
    Task<int> SaveReminderLogAsync(ReminderLog log);
    Task UpdateReminderLogStatusAsync(int id, string status, int retryCount, string? lastError, DateTime? sentAt = null, string? confirmedBy = null);
    Task<IEnumerable<ReminderLog>> GetReminderLogsAsync(int? customerId = null, string? status = null, int limit = 100);
    Task<IEnumerable<ReminderLog>> GetPendingRetryLogsAsync(int maxRetries = 3);
    Task SetCustomerDisputeStatusAsync(int customerId, bool isInDispute, string? reason);
    Task SetInvoiceDisputeStatusAsync(int invoiceId, bool isInDispute, string? reason);
}
