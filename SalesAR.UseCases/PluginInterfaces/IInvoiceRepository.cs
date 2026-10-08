namespace SalesAR.UseCases.PluginInterfaces;

using SalesAR.CoreBusiness.Models;
using SalesAR.UseCases.Models;

public interface IInvoiceRepository
{
    Task<IEnumerable<Invoice>> GetAllAsync();
    Task<Invoice?> GetByIdAsync(int id);
    Task<Invoice?> GetByInvoiceNumberAsync(string invoiceNumber);
    Task<IEnumerable<InvoiceLine>> GetLinesByInvoiceIdAsync(int invoiceId);
    Task<IEnumerable<Invoice>> GetUnpaidInvoicesByCustomerAsync(int customerId);
    Task<IEnumerable<InvoiceOutstandingDto>> GetOutstandingInvoicesByCustomerAsync(int customerId);
    Task<int> CreateInvoiceWithLinesAsync(Invoice invoice, IEnumerable<InvoiceLine> lines);
    Task UpdateStatusAsync(int invoiceId, string status);
}
