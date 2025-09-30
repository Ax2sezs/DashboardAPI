namespace DashboardAPI.Models
{
    public class LoginRequest
    {
        public string LogonName { get; set; }
        public string Password { get; set; } // ถ้ามี password column
    }

    public class LoginResponse
    {
        public string Token { get; set; }
        public string UserName { get; set; }
        public int Role { get; set; }
        public List<string> Branches { get; set; } = new List<string>();
    }
}
