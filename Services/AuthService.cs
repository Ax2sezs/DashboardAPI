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

        // เปลี่ยนเป็น Private เพราะใช้แค่ใน Service นี้ หรือถ้ามี HashHelper แยกอยู่แล้วให้เรียกใช้จากที่นั่น
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
            // 1. ค้นหา User
            var user = await _context.Users
                .FromSqlRaw("SELECT * FROM AUDB.dbo.A_User WHERE LOGON_NAME = {0}", request.LogonName)
                .FirstOrDefaultAsync();

            if (user == null)
                return null;

            // 2. ตรวจสอบรหัสผ่าน 
            // แก้ไข: เรียกใช้ EncryptStringMD5 ที่อยู่ในคลาสเดียวกัน (หรือเปลี่ยนเป็น HashHelper ถ้าแยกคลาส)
            string hashedInputPassword = EncryptStringMD5(request.Password);

            // ใช้ .Equals พร้อม StringComparison เพื่อป้องกันปัญหาตัวพิมพ์เล็ก-ใหญ่ใน DB
            if (user.USER_PASSWORD == null || !user.USER_PASSWORD.Equals(hashedInputPassword, StringComparison.OrdinalIgnoreCase))
            {
                return null; 
            }

            // 3. ดึงสาขา
            var branches = await _context.UserBranches
                .FromSqlRaw("SELECT * FROM AUDB.dbo.A_UserBranch WHERE A_User_Id = {0}", user.A_USER_ID)
                .Select(b => b.Branch)
                .ToListAsync();

            string role = user.IsAdmin == 1 ? "Admin" : "User";

            // 4. สร้าง JWT
            var jwtKey = _configuration["Jwt:Key"] ?? "YourSuperSecretKeyHere123!";
            var key = Encoding.ASCII.GetBytes(jwtKey);
            var tokenHandler = new JwtSecurityTokenHandler();
            
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.User_Name ?? ""),
                new Claim(ClaimTypes.Role, role),
                new Claim("LogonName", user.LOGON_NAME),
                new Claim("UserId", user.A_USER_ID.ToString())
            };

            foreach (var branch in branches)
            {
                if (!string.IsNullOrEmpty(branch))
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

            // 5. ส่งคำตอบกลับ
            return new LoginResponse
            {
                Token = jwtToken,
                UserName = user.User_Name,
                Role = user.IsAdmin,
                Branches = branches
            };
        }
    }
}