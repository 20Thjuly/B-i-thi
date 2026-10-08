namespace SalesAR.UseCases.PluginInterfaces;

using SalesAR.CoreBusiness.Models;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByIdAsync(int id);
    Task<IEnumerable<User>> GetAllUsersAsync();
    Task<int> CreateUserAsync(User user);
    Task UpdateUserAsync(User user);
    Task ToggleActiveAsync(int userId, bool isActive);
}
