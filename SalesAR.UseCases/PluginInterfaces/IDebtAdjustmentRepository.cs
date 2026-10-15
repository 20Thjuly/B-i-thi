using SalesAR.CoreBusiness.Models;

namespace SalesAR.UseCases.PluginInterfaces;

public interface IDebtAdjustmentRepository
{
    Task<IEnumerable<DebtAdjustment>> GetAllAsync();
    Task<IEnumerable<DebtAdjustment>> GetByCustomerIdAsync(int customerId);
    Task<DebtAdjustment?> GetByIdAsync(int id);
    Task<int> AddAsync(DebtAdjustment adjustment);
}
