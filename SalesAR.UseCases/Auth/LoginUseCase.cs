namespace SalesAR.UseCases.Auth;

using SalesAR.CoreBusiness.Models;
using SalesAR.CoreBusiness.Security;
using SalesAR.UseCases.PluginInterfaces;

public interface ILoginUseCase
{
    Task<User> ExecuteAsync(string username, string password);
}

public class LoginUseCase : ILoginUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public LoginUseCase(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<User> ExecuteAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new InvalidOperationException("Vui lòng nhập tên đăng nhập.");

        if (string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Vui lòng nhập mật khẩu.");

        var user = await _userRepository.GetByUsernameAsync(username.Trim());
        if (user == null)
            throw new InvalidOperationException("Tên đăng nhập hoặc mật khẩu không chính xác.");

        if (!user.IsActive)
            throw new InvalidOperationException("Tài khoản của bạn đã bị vô hiệu hóa. Vui lòng liên hệ quản trị viên.");

        bool isPasswordValid = _passwordHasher.VerifyPassword(password, user.PasswordHash);
        if (!isPasswordValid)
            throw new InvalidOperationException("Tên đăng nhập hoặc mật khẩu không chính xác.");

        return user;
    }
}
