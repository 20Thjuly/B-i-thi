namespace SalesAR.UseCases.Products;

using SalesAR.CoreBusiness.Models;
using SalesAR.UseCases.PluginInterfaces;

public interface IViewProductByIdUseCase
{
    Task<Product?> ExecuteAsync(int id);
}

public class ViewProductByIdUseCase : IViewProductByIdUseCase
{
    private readonly IProductRepository _productRepository;

    public ViewProductByIdUseCase(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<Product?> ExecuteAsync(int id)
    {
        return await _productRepository.GetByIdAsync(id);
    }
}

public interface IAddProductUseCase
{
    Task<int> ExecuteAsync(Product product);
}

public class AddProductUseCase : IAddProductUseCase
{
    private readonly IProductRepository _productRepository;

    public AddProductUseCase(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<int> ExecuteAsync(Product product)
    {
        if (product == null) throw new ArgumentNullException(nameof(product));
        if (string.IsNullOrWhiteSpace(product.Code)) throw new InvalidOperationException("Mã sản phẩm không được để trống.");
        if (string.IsNullOrWhiteSpace(product.Name)) throw new InvalidOperationException("Tên sản phẩm không được để trống.");
        if (product.Price < 0) throw new InvalidOperationException("Đơn giá sản phẩm không được âm.");
        if (product.StockQuantity < 0) throw new InvalidOperationException("Số lượng tồn kho không được âm.");

        var existing = await _productRepository.GetByCodeAsync(product.Code);
        if (existing != null)
            throw new InvalidOperationException($"Mã sản phẩm '{product.Code}' đã tồn tại trong hệ thống.");

        return await _productRepository.AddAsync(product);
    }
}

public interface IEditProductUseCase
{
    Task ExecuteAsync(Product product);
}

public class EditProductUseCase : IEditProductUseCase
{
    private readonly IProductRepository _productRepository;

    public EditProductUseCase(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task ExecuteAsync(Product product)
    {
        if (product == null) throw new ArgumentNullException(nameof(product));
        if (string.IsNullOrWhiteSpace(product.Code)) throw new InvalidOperationException("Mã sản phẩm không được để trống.");
        if (string.IsNullOrWhiteSpace(product.Name)) throw new InvalidOperationException("Tên sản phẩm không được để trống.");
        if (product.Price < 0) throw new InvalidOperationException("Đơn giá sản phẩm không được âm.");
        if (product.StockQuantity < 0) throw new InvalidOperationException("Số lượng tồn kho không được âm.");

        var existing = await _productRepository.GetByIdAsync(product.Id);
        if (existing == null)
            throw new InvalidOperationException($"Không tìm thấy sản phẩm với ID: {product.Id}.");

        await _productRepository.UpdateAsync(product);
    }
}

public interface IDeleteProductUseCase
{
    Task ExecuteAsync(int id);
}

public class DeleteProductUseCase : IDeleteProductUseCase
{
    private readonly IProductRepository _productRepository;

    public DeleteProductUseCase(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task ExecuteAsync(int id)
    {
        await _productRepository.DeleteAsync(id);
    }
}
