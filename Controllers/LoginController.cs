// Controllers/AuthController.cs
using DashboardAPI.Models;
using DashboardAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace DashboardAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await _authService.AuthenticateAsync(request);
            if (result == null)
                return Unauthorized("Invalid username or password");

            return Ok(result);
        }

        [HttpGet("test-admin")]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public IActionResult TestAdmin()
        {
            return Ok(new { Message = "Only Admin can see this" });
        }
    }
}
