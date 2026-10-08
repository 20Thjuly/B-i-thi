namespace SalesAR.UseCases.Payments;

using SalesAR.CoreBusiness.Models;
using SalesAR.UseCases.PluginInterfaces;

public interface IViewPaymentsUseCase
{
    Task<IEnumerable<Payment>> ExecuteAsync();
    Task<Payment?> GetByIdAsync(int id);
    Task<IEnumerable<PaymentAllocation>> GetAllocationsAsync(int paymentId);
}

public class ViewPaymentsUseCase : IViewPaymentsUseCase
{
    private readonly IPaymentRepository _paymentRepository;

    public ViewPaymentsUseCase(IPaymentRepository paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<IEnumerable<Payment>> ExecuteAsync()
    {
        return await _paymentRepository.GetAllAsync();
    }

    public async Task<Payment?> GetByIdAsync(int id)
    {
        return await _paymentRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<PaymentAllocation>> GetAllocationsAsync(int paymentId)
    {
        return await _paymentRepository.GetAllocationsByPaymentIdAsync(paymentId);
    }
}
