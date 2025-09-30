using System;

namespace DashboardAPI.Models
{
    public class EL_LossSummary
    {
        public int Id { get; set; }                 // Identity PK
        public Guid RunId { get; set; }            // จาก EL_Calculation
        public string OutletName { get; set; }
        public DateTime CalDate { get; set; }
        public string CalHour { get; set; }

        public decimal? NetSale { get; set; }
        public decimal? NetSaleEatIn { get; set; }
        public string? BranchArea { get; set; }
        public int? BranchStaff { get; set; }

        public decimal? LSProd { get; set; }       // TotalLostItem * AvgPrice
        public decimal? LSSeating { get; set; }    // TotalLostTable * AvgPerBill
        public decimal? LSPerBill { get; set; }    // TotalLostPerBill

        public string? CashierSpeed { get; set; }
        public decimal? EatInPercent { get; set; }

        public DateTime CreatedAt { get; set; }    // DEFAULT GETDATE()
    }
}
