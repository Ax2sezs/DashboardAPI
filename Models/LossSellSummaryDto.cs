namespace DashboardAPI.Models
{
    public class LossSellSummaryDto
    {
        public string? OutletId { get; set; }
        public string? OutletName { get; set; }
        // public decimal? TotalLostItem { get; set; }
        // public decimal? TotalLostProductivity { get; set; }
        // public decimal? TotalLostTime { get; set; }
        // public decimal? TotalLostSeating { get; set; }
        // public decimal? TotalLostTable { get; set; }
        public DateTime Date { get; set; }
        // public decimal? NetSale { get; set; }
        public decimal? NetSaleEatIn { get; set; }
        // public decimal? NetSaleOther { get; set; }
        public string? BranchArea { get; set; }
        public int? BranchStaff { get; set; }
        public decimal? LSOpp { get; set; }
        public decimal? LSProd { get; set; }
        public decimal? LSSeating { get; set; }
        public decimal? LSPerBill { get; set; }
    }
}