namespace SalesAR.UseCases.Customers;

using SalesAR.CoreBusiness.Models;
using SalesAR.UseCases.PluginInterfaces;

public interface IViewCustomersUseCase
{
    Task<IEnumerable<Customer>> ExecuteAsync();
}

public class ViewCustomersUseCase : IViewCustomersUseCase
{
    private readonly ICustomerRepository _customerRepository;

    public ViewCustomersUseCase(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<IEnumerable<Customer>> ExecuteAsync()
    {
        return await _customerRepository.GetAllAsync();
    }
}
