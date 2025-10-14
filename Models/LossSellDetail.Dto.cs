namespace DashboardAPI.Models
{
    public class LossSellDetailDto
    {
        // public string OutletId { get; set; }
        public string OutletName { get; set; }
        public List<HourlyTrendDto> HourlyTrend { get; set; }
    }

    public class HourlyTrendDto
    {
        public Guid RunId { get; set; }
        public string Hour { get; set; }                 // เช่น "10:00–10:59"
        public decimal LostItem { get; set; }                // จำนวนสินค้าหาย/เสีย
        public decimal LostProductivity { get; set; }    // สูญเสีย productivity
        public decimal LostTime { get; set; }           // Lost Time (นาที)
        public decimal LostSeating { get; set; }        // Lost Seating
        public decimal LostTable { get; set; }          // Lost Table
        public decimal BillTotal { get; set; }          // ยอดขายจริง
        public decimal TotalOptimalBillPrice { get; set; } // ยอดขาย optimal
        public decimal LostPerBill { get; set; }        // ความสูญเสียต่อบิล
        public decimal AvgBillTotal { get; set; }
        public decimal AvgPrice { get; set; }
        public decimal ProductionTime { get; set; }
        public decimal OptimalMinute { get; set; }
        public decimal AvgSeatingTime { get; set; }
        public decimal GapBetweenQueue { get; set; }
        public int NoteCount { get; set; }
        public int Branch_Staff { get; set; }
        public decimal? DineInPercent { get; set; }
        public decimal? OtherPercent { get; set; }
        public int BranchBill { get; set; }
        public decimal LSProd { get; set; }
        public decimal LSSeating { get; set; }
        public decimal LSPerBill { get; set; }
        public decimal LossSell { get; set; }
        public decimal? AVGPerBill { get; set; }
        public decimal ProdTimeAVG { get; set; }
        public double? Median { get; set; }
        // public string? CashierSpeed { get; set; }
        public RptElCalculateSale? rptElCalculateSale { get; set; }

    }

    public class HourlyChartDto
    {
        public string Hour { get; set; }                 // เช่น "10:00–10:59"
        public decimal LostItem { get; set; }                // จำนวนสินค้าหาย/เสีย
        public decimal LostProductivity { get; set; }    // สูญเสีย productivity
        public decimal LostTime { get; set; }           // Lost Time (นาที)
        public decimal LostSeating { get; set; }        // Lost Seating
        public decimal LostTable { get; set; }          // Lost Table
        public decimal BillTotal { get; set; }          // ยอดขายจริง
        public decimal TotalOptimalBillPrice { get; set; } // ยอดขาย optimal
        public decimal LostPerBill { get; set; }        // ความสูญเสียต่อบิล
        public decimal AvgBillTotal { get; set; }
        public decimal AvgPrice { get; set; }
        public decimal ProductionTime { get; set; }
        public decimal  OptimalMinute { get; set; }
        public double Median { get; set; }
        public decimal AvgSeatingTime { get; set; }
        public decimal GapBetweenQueue { get; set; }

    }
}