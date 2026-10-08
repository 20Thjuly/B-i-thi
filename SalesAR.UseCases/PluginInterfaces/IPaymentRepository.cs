namespace SalesAR.UseCases.PluginInterfaces;

using SalesAR.CoreBusiness.Models;

public interface IPaymentRepository
{
    Task<IEnumerable<Payment>> GetAllAsync();
    Task<Payment?> GetByIdAsync(int id);
    Task<IEnumerable<Payment>> GetPaymentsByCustomerAsync(int customerId);
    Task<IEnumerable<PaymentAllocation>> GetAllocationsByPaymentIdAsync(int paymentId);
    Task<IEnumerable<PaymentAllocation>> GetAllocationsByCustomerAsync(int customerId);
    Task<int> CreatePaymentWithAllocationsAsync(Payment payment, IEnumerable<PaymentAllocation> allocations);
}
