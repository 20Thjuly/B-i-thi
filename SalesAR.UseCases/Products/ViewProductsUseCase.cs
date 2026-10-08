namespace SalesAR.UseCases.Products;

using SalesAR.CoreBusiness.Models;
using SalesAR.UseCases.PluginInterfaces;

public interface IViewProductsUseCase
{
    Task<IEnumerable<Product>> ExecuteAsync();
}

public class ViewProductsUseCase : IViewProductsUseCase
{
    private readonly IProductRepository _productRepository;

    public ViewProductsUseCase(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<IEnumerable<Product>> ExecuteAsync()
    {
        return await _productRepository.GetAllAsync();
    }
}
