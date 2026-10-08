namespace SalesAR.UseCases.AI;

using SalesAR.UseCases.Models;

public interface IDebtReminderAiService
{
    Task<string> DraftReminderEmailAsync(CustomerDebtDto customerDebt, IEnumerable<InvoiceOutstandingDto> unpaidInvoices, string tone = "polite");
}
