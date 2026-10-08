namespace SalesAR.UseCases.Customers;

using SalesAR.UseCases.Models;
using SalesAR.UseCases.PluginInterfaces;

public interface IGetCustomerDebtUseCase
{
    Task<CustomerDebtDto?> ExecuteAsync(int customerId);
}

public class GetCustomerDebtUseCase : IGetCustomerDebtUseCase
{
    private readonly ICustomerRepository _customerRepository;

    public GetCustomerDebtUseCase(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<CustomerDebtDto?> ExecuteAsync(int customerId)
    {
        return await _customerRepository.GetCustomerDebtDetailsAsync(customerId);
    }
}
