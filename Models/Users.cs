namespace DashboardAPI.Models
{
    public class Users
    {
        public Guid A_USER_ID { get; set; }
        public string LOGON_NAME { get; set; }
        public string User_Name { get; set; }
        public int IsAdmin { get; set; }
        public string Branch { get; set; }

    }
}