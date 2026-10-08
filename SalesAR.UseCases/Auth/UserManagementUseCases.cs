namespace SalesAR.UseCases.Auth;

using SalesAR.CoreBusiness.Models;
using SalesAR.CoreBusiness.Security;
using SalesAR.UseCases.PluginInterfaces;

public interface IViewUsersUseCase
{
    Task<IEnumerable<User>> ExecuteAsync();
}

public class ViewUsersUseCase : IViewUsersUseCase
{
    private readonly IUserRepository _userRepository;

    public ViewUsersUseCase(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<IEnumerable<User>> ExecuteAsync()
    {
        return await _userRepository.GetAllUsersAsync();
    }
}

public interface ICreateUserUseCase
{
    Task<int> ExecuteAsync(User user, string plainPassword);
}

public class CreateUserUseCase : ICreateUserUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public CreateUserUseCase(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<int> ExecuteAsync(User user, string plainPassword)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));
        if (string.IsNullOrWhiteSpace(user.Username)) throw new InvalidOperationException("Tên đăng nhập không được để trống.");
        if (string.IsNullOrWhiteSpace(plainPassword)) throw new InvalidOperationException("Mật khẩu không được để trống.");

        var existing = await _userRepository.GetByUsernameAsync(user.Username.Trim());
        if (existing != null)
            throw new InvalidOperationException($"Tên đăng nhập '{user.Username}' đã tồn tại trong hệ thống.");

        user.PasswordHash = _passwordHasher.HashPassword(plainPassword);
        return await _userRepository.CreateUserAsync(user);
    }
}

public interface IToggleUserStatusUseCase
{
    Task ExecuteAsync(int userId, bool isActive);
}

public class ToggleUserStatusUseCase : IToggleUserStatusUseCase
{
    private readonly IUserRepository _userRepository;

    public ToggleUserStatusUseCase(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task ExecuteAsync(int userId, bool isActive)
    {
        await _userRepository.ToggleActiveAsync(userId, isActive);
    }
}

public interface IViewRolesUseCase
{
    Task<IEnumerable<Role>> ExecuteAsync();
}

public class ViewRolesUseCase : IViewRolesUseCase
{
    private readonly IRoleRepository _roleRepository;

    public ViewRolesUseCase(IRoleRepository roleRepository)
    {
        _roleRepository = roleRepository;
    }

    public async Task<IEnumerable<Role>> ExecuteAsync()
    {
        return await _roleRepository.GetAllRolesAsync();
    }
}
