using System.ComponentModel.DataAnnotations;

namespace DashboardAPI.Models
{
    public class ElCalculation
    {
        [Key]
        public Guid RunId { get; set; }
        public string OutletId { get; set; }
        public string OutletName { get; set; }
        public DateTime? CalDate { get; set; }
        public string CalHour { get; set; }
        public decimal? Avg_BillTotal { get; set; }
        public decimal? Avg_Price { get; set; }
        public decimal? Production_Time { get; set; }
        public decimal? Optimal_Minute { get; set; }
        public decimal? Lost_Time { get; set; }
        public decimal? Lost_Item { get; set; }
        public decimal? Lost_Productivity { get; set; }
        public decimal? Avg_SeatingTime { get; set; }
        public decimal? Gap_Between_Queue { get; set; }
        public decimal? Lost_Table { get; set; }
        public decimal? Lost_Seating { get; set; }
        public decimal? Bill_Total { get; set; }
        public decimal? Total_Optimal_Bill_Price { get; set; }
        public decimal? Lost_Per_Bill { get; set; }
        public decimal? ProdTimeAVG { get; set; }
        public DateTime? Createdon { get; set; }
    }
}