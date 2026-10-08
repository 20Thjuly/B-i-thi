namespace SalesAR.UnitTests;

using Xunit;
using SalesAR.CoreBusiness.Models;
using SalesAR.CoreBusiness.Security;
using SalesAR.UseCases.Auth;
using SalesAR.UseCases.PluginInterfaces;

public class FakeUserRepository : IUserRepository
{
    private readonly List<User> _users = new();

    public FakeUserRepository(IEnumerable<User>? initialUsers = null)
    {
        if (initialUsers != null) _users.AddRange(initialUsers);
    }

    public Task<User?> GetByUsernameAsync(string username) =>
        Task.FromResult(_users.FirstOrDefault(u => string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase)));

    public Task<User?> GetByIdAsync(int id) =>
        Task.FromResult(_users.FirstOrDefault(u => u.Id == id));

    public Task<IEnumerable<User>> GetAllUsersAsync() =>
        Task.FromResult<IEnumerable<User>>(_users);

    public Task<int> CreateUserAsync(User user)
    {
        user.Id = _users.Count + 1;
        _users.Add(user);
        return Task.FromResult(user.Id);
    }

    public Task UpdateUserAsync(User user) => Task.CompletedTask;

    public Task ToggleActiveAsync(int userId, bool isActive)
    {
        var user = _users.FirstOrDefault(u => u.Id == userId);
        if (user != null) user.IsActive = isActive;
        return Task.CompletedTask;
    }
}

public class LoginUseCaseTests
{
    private readonly IPasswordHasher _hasher = new PasswordHasher();

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsUser()
    {
        var passwordHash = _hasher.HashPassword("Admin@123");
        var user = new User
        {
            Id = 1,
            Username = "admin",
            PasswordHash = passwordHash,
            FullName = "Administrator",
            RoleId = 1,
            RoleName = "ADMIN",
            IsActive = true
        };

        var repo = new FakeUserRepository(new[] { user });
        var useCase = new LoginUseCase(repo, _hasher);

        var result = await useCase.ExecuteAsync("admin", "Admin@123");

        Assert.NotNull(result);
        Assert.Equal("admin", result.Username);
        Assert.Equal("ADMIN", result.RoleName);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ThrowsExactException()
    {
        var passwordHash = _hasher.HashPassword("Admin@123");
        var user = new User
        {
            Id = 1,
            Username = "admin",
            PasswordHash = passwordHash,
            IsActive = true
        };

        var repo = new FakeUserRepository(new[] { user });
        var useCase = new LoginUseCase(repo, _hasher);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.ExecuteAsync("admin", "WrongPassword!"));
        Assert.Equal("Tên đăng nhập hoặc mật khẩu không chính xác.", ex.Message);
    }

    [Fact]
    public async Task Login_WithInactiveUser_ThrowsAccountDisabledException()
    {
        var passwordHash = _hasher.HashPassword("Sales@123");
        var user = new User
        {
            Id = 2,
            Username = "sales",
            PasswordHash = passwordHash,
            IsActive = false // Vô hiệu hóa
        };

        var repo = new FakeUserRepository(new[] { user });
        var useCase = new LoginUseCase(repo, _hasher);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.ExecuteAsync("sales", "Sales@123"));
        Assert.Equal("Tài khoản của bạn đã bị vô hiệu hóa. Vui lòng liên hệ quản trị viên.", ex.Message);
    }
}
