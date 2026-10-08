namespace SalesAR.UseCases.PluginInterfaces;

using SalesAR.CoreBusiness.Models;
using SalesAR.UseCases.Models;

public interface ICustomerRepository
{
    Task<IEnumerable<Customer>> GetAllAsync();
    Task<Customer?> GetByIdAsync(int id);
    Task<Customer?> GetByCodeAsync(string code);
    Task<int> AddAsync(Customer customer);
    Task UpdateAsync(Customer customer);
    Task DeleteAsync(int id);
    Task<decimal> GetCurrentDebtAsync(int customerId);
    Task<CustomerDebtDto?> GetCustomerDebtDetailsAsync(int customerId);
}
