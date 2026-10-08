namespace SalesAR.UseCases.Customers;

using SalesAR.CoreBusiness.Models;
using SalesAR.UseCases.PluginInterfaces;

public interface IViewCustomerByIdUseCase
{
    Task<Customer?> ExecuteAsync(int id);
}

public class ViewCustomerByIdUseCase : IViewCustomerByIdUseCase
{
    private readonly ICustomerRepository _customerRepository;

    public ViewCustomerByIdUseCase(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<Customer?> ExecuteAsync(int id)
    {
        return await _customerRepository.GetByIdAsync(id);
    }
}

public interface IAddCustomerUseCase
{
    Task<int> ExecuteAsync(Customer customer);
}

public class AddCustomerUseCase : IAddCustomerUseCase
{
    private readonly ICustomerRepository _customerRepository;

    public AddCustomerUseCase(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<int> ExecuteAsync(Customer customer)
    {
        if (customer == null) throw new ArgumentNullException(nameof(customer));
        if (string.IsNullOrWhiteSpace(customer.Code)) throw new InvalidOperationException("Mã khách hàng không được để trống.");
        if (string.IsNullOrWhiteSpace(customer.Name)) throw new InvalidOperationException("Tên khách hàng không được để trống.");
        if (customer.CreditLimit < 0) throw new InvalidOperationException("Hạn mức tín dụng không được âm.");

        var existing = await _customerRepository.GetByCodeAsync(customer.Code);
        if (existing != null)
            throw new InvalidOperationException($"Mã khách hàng '{customer.Code}' đã tồn tại trong hệ thống.");

        return await _customerRepository.AddAsync(customer);
    }
}

public interface IEditCustomerUseCase
{
    Task ExecuteAsync(Customer customer);
}

public class EditCustomerUseCase : IEditCustomerUseCase
{
    private readonly ICustomerRepository _customerRepository;

    public EditCustomerUseCase(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task ExecuteAsync(Customer customer)
    {
        if (customer == null) throw new ArgumentNullException(nameof(customer));
        if (string.IsNullOrWhiteSpace(customer.Code)) throw new InvalidOperationException("Mã khách hàng không được để trống.");
        if (string.IsNullOrWhiteSpace(customer.Name)) throw new InvalidOperationException("Tên khách hàng không được để trống.");
        if (customer.CreditLimit < 0) throw new InvalidOperationException("Hạn mức tín dụng không được âm.");

        var existing = await _customerRepository.GetByIdAsync(customer.Id);
        if (existing == null)
            throw new InvalidOperationException($"Không tìm thấy khách hàng với ID: {customer.Id}.");

        await _customerRepository.UpdateAsync(customer);
    }
}

public interface IDeleteCustomerUseCase
{
    Task ExecuteAsync(int id);
}

public class DeleteCustomerUseCase : IDeleteCustomerUseCase
{
    private readonly ICustomerRepository _customerRepository;

    public DeleteCustomerUseCase(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task ExecuteAsync(int id)
    {
        await _customerRepository.DeleteAsync(id);
    }
}
