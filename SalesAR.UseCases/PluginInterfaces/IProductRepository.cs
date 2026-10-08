namespace SalesAR.UseCases.PluginInterfaces;

using SalesAR.CoreBusiness.Models;

public interface IProductRepository
{
    Task<IEnumerable<Product>> GetAllAsync();
    Task<Product?> GetByIdAsync(int id);
    Task<Product?> GetByCodeAsync(string code);
    Task<int> AddAsync(Product product);
    Task UpdateAsync(Product product);
    Task DeleteAsync(int id);
}
