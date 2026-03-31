// Services/AuthService.cs
using DashboardAPI.Data;
using DashboardAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace DashboardAPI.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }
        private static string EncryptStringMD5(string strString)
        {
            if (string.IsNullOrEmpty(strString)) return string.Empty;

            using (var md5 = MD5.Create())
            {
                byte[] byteSourceText = Encoding.UTF8.GetBytes(strString);
                byte[] byteHash = md5.ComputeHash(byteSourceText);

                StringBuilder sb = new StringBuilder();
                foreach (byte b in byteHash)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }

        public async Task<LoginResponse?> AuthenticateAsync(LoginRequest request)
        {
            // ดึง user
            var user = await _context.Users
                .FromSqlRaw("SELECT * FROM AUDB.dbo.A_User WHERE LOGON_NAME = {0}", request.LogonName)
                .FirstOrDefaultAsync();

            if (user == null)
                return null;
            string hashedInputPassword = EncryptStringMD5(request.Password);

            // ตรวจสอบ password default
            if (user.USER_PASSWORD == null || !user.USER_PASSWORD.Equals(hashedInputPassword, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            // ดึง Branch ทั้งหมด
            var branches = await _context.UserBranches
                .FromSqlRaw("SELECT * FROM AUDB.dbo.A_UserBranch WHERE A_User_Id = {0}", user.A_USER_ID)
                .Select(b => b.Branch)
                .ToListAsync();

            // กำหนด role ตาม IsAdmin
            string role = user.IsAdmin == 1 ? "Admin" : "User";

            // สร้าง JWT
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"] ?? "YourSuperSecretKeyHere123!");
            var tokenHandler = new JwtSecurityTokenHandler();
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, user.User_Name),
        new Claim(ClaimTypes.Role, role),
        new Claim("LogonName", user.LOGON_NAME),
        new Claim("UserId", user.A_USER_ID.ToString())
    };

            // เพิ่ม branch ลงใน JWT ทีละค่า
            foreach (var branch in branches)
            {
                claims.Add(new Claim("Branch", branch));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(4),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var jwtToken = tokenHandler.WriteToken(token);

            return new LoginResponse
            {
                Token = jwtToken,
                UserName = user.User_Name,
                Role = user.IsAdmin,
                Branches = branches // เปลี่ยนเป็น List<string>
            };
        }
    }
}
