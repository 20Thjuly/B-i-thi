namespace SalesAR.UseCases.Invoices;

using SalesAR.CoreBusiness.Models;
using SalesAR.UseCases.PluginInterfaces;

public interface IViewInvoicesUseCase
{
    Task<IEnumerable<Invoice>> ExecuteAsync();
    Task<Invoice?> GetByIdAsync(int id);
    Task<IEnumerable<InvoiceLine>> GetLinesAsync(int invoiceId);
}

public class ViewInvoicesUseCase : IViewInvoicesUseCase
{
    private readonly IInvoiceRepository _invoiceRepository;

    public ViewInvoicesUseCase(IInvoiceRepository invoiceRepository)
    {
        _invoiceRepository = invoiceRepository;
    }

    public async Task<IEnumerable<Invoice>> ExecuteAsync()
    {
        return await _invoiceRepository.GetAllAsync();
    }

    public async Task<Invoice?> GetByIdAsync(int id)
    {
        return await _invoiceRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<InvoiceLine>> GetLinesAsync(int invoiceId)
    {
        return await _invoiceRepository.GetLinesByInvoiceIdAsync(invoiceId);
    }
}
