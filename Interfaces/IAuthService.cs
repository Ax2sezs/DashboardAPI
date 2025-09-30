// Services/IAuthService.cs
using DashboardAPI.Models;

namespace DashboardAPI.Services
{
    public interface IAuthService
    {
        Task<LoginResponse?> AuthenticateAsync(LoginRequest request);
    }
}
