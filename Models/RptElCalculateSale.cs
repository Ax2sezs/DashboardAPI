namespace DashboardAPI.Models
{
    public class RptElCalculateSale
    {
        public Guid S_Branch_Id { get; set; }
        public string Branch_Code { get; set; }
        public DateTime OrderDate { get; set; }
        public int? OrderHour { get; set; }
        public int? Branch_Staff { get; set; }
        public string? Branch_Area { get; set; }
        public string? CashierSpeedSec { get; set; }
        public decimal? NetSale { get; set; }
        public decimal? NetSaleEatIn { get; set; }
        public decimal? NetSaleOther { get; set; }
        public decimal? EatInPercent { get; set; }
        public decimal? OtherPercent { get; set; }
        public int? TotalBill { get; set; }
        public int? EatingBill { get; set; }
        public int? OtherBill { get; set; }
        public int? Branch_Bill { get; set; }
        public decimal? Saleprebill { get; set; }
        public decimal? LostSalePerBill { get; set; }

    }
}
