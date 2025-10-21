namespace DashboardAPI.Models
{
    public class RptELProductionTime
    {
        public DateTime BillDate { get; set; }
        public string Branch_Code { get; set; } = string.Empty;
        public string BillHour { get; set; } = string.Empty;
        public string BillNo { get; set; } = string.Empty;
        public decimal? BillTotal { get; set; }
        public string Category { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal ProductionTime { get; set; }
        public decimal? OptimalTime { get; set; }
        public string AICatogory { get; set; } = string.Empty;
        public DateTime? AIMatchTime { get; set; }
    }
}
