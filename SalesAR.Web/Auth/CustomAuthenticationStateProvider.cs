namespace SalesAR.Web.Auth;

using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using SalesAR.CoreBusiness.Models;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly ProtectedSessionStorage _sessionStorage;
    private ClaimsPrincipal _anonymous = new(new ClaimsIdentity());
    private ClaimsPrincipal _currentUser;

    public CustomAuthenticationStateProvider(ProtectedSessionStorage sessionStorage)
    {
        _sessionStorage = sessionStorage;
        _currentUser = _anonymous;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var sessionResult = await _sessionStorage.GetAsync<UserSession>("UserSession");
            var userSession = sessionResult.Success ? sessionResult.Value : null;

            if (userSession == null)
            {
                _currentUser = _anonymous;
                return new AuthenticationState(_anonymous);
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userSession.Id.ToString()),
                new(ClaimTypes.Name, userSession.Username),
                new(ClaimTypes.GivenName, userSession.FullName),
                new(ClaimTypes.Role, userSession.RoleName)
            };

            var identity = new ClaimsIdentity(claims, "CustomAuth");
            _currentUser = new ClaimsPrincipal(identity);
            return new AuthenticationState(_currentUser);
        }
        catch
        {
            // Trong quá trình prerender hoặc nếu JSInterop chưa sẵn sàng
            return new AuthenticationState(_currentUser);
        }
    }

    public async Task MarkUserAsAuthenticatedAsync(User user)
    {
        var userSession = new UserSession
        {
            Id = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            RoleName = user.RoleName
        };

        try
        {
            await _sessionStorage.SetAsync("UserSession", userSession);
        }
        catch
        {
            // Bỏ qua nếu chưa sẵn sàng
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.GivenName, user.FullName),
            new(ClaimTypes.Role, user.RoleName)
        };

        var identity = new ClaimsIdentity(claims, "CustomAuth");
        _currentUser = new ClaimsPrincipal(identity);

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
    }

    public async Task MarkUserAsLoggedOutAsync()
    {
        try
        {
            await _sessionStorage.DeleteAsync("UserSession");
        }
        catch
        {
            // Bỏ qua
        }

        _currentUser = _anonymous;
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_anonymous)));
    }
}
