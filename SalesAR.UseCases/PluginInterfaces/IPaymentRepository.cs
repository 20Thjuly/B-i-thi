namespace SalesAR.UseCases.PluginInterfaces;

using SalesAR.CoreBusiness.Models;

public interface IPaymentRepository
{
    Task<IEnumerable<Payment>> GetAllAsync();
    Task<Payment?> GetByIdAsync(int id);
    Task<Payment?> GetByPaymentNumberAsync(string paymentNumber);
    Task<IEnumerable<Payment>> GetPaymentsByCustomerAsync(int customerId);
    Task<IEnumerable<PaymentAllocation>> GetAllocationsByPaymentIdAsync(int paymentId);
    Task<IEnumerable<PaymentAllocation>> GetAllocationsByCustomerAsync(int customerId);
    Task<int> CreatePaymentWithAllocationsAsync(Payment payment, IEnumerable<PaymentAllocation> allocations);
    Task ReallocatePaymentAsync(int paymentId, IEnumerable<PaymentAllocation> newAllocations, string performedBy, string reason);
}
