namespace DashboardAPI.Models
{
    public class ElCalculateSaleDetail
    {
        public string Branch_Code { get; set; } = string.Empty;
        public DateTime? Ord_Dt { get; set; }
        public int OrderHour { get; set; }
        public int Branch_Staff { get; set; }
        public string? Branch_Area { get; set; }
        public string CashierSpeedSec { get; set; }
        public decimal NetSale { get; set; }
        public decimal NetSaleEatIn { get; set; }
        public decimal NetSaleOther { get; set; }
        public string BName { get; set; }
        public int Branch_Bill { get; set; }
        public int Branch_ProdTime { get; set; }
        public double Q1 { get; set; }
        public double Median { get; set; }
        public double Q3 { get; set; }
    }
}
