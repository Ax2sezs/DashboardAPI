namespace DashboardAPI.Models
{
    public class DashboardInsightDto
    {
        public decimal AvgLoss { get; set; }
        public decimal TotalLoss { get; set; }

        public List<BranchInsightDto> BranchInsights { get; set; } = new();
        public List<TimeInsightDto> TimeInsights { get; set; } = new();
        public LossCompositionDto Composition { get; set; } = new();
    }

    public class BranchInsightDto
    {
        public string OutletName { get; set; }

        public decimal TotalLoss { get; set; }
        public decimal AvgLoss { get; set; }

        public decimal LSProd { get; set; }
        public decimal LSSeating { get; set; }
        public decimal LSPerBill { get; set; }

        public decimal AvgLSProd { get; set; }
        public decimal AvgLSSeating { get; set; }
        public decimal AvgLSPerBill { get; set; }

        public bool IsAboveAverage { get; set; }
        public bool IsCritical { get; set; }
        public decimal DeviationPercent { get; set; }

        public List<HighLossDayDto> HighLossDays { get; set; } = new();
    }

    public class HighLossDayDto
    {
        public DateTime Date { get; set; }
        public decimal Loss { get; set; }

        public decimal LSProd { get; set; }
        public decimal LSSeating { get; set; }
        public decimal LSPerBill { get; set; }
    }

    public class TimeInsightDto
    {
        public string Hour { get; set; }

        public decimal TotalLoss { get; set; }
        public decimal LSProd { get; set; }
        public decimal LSSeating { get; set; }
        public decimal LSPerBill { get; set; }
    }

    public class LossCompositionDto
    {
        public decimal LSProd { get; set; }
        public decimal LSSeating { get; set; }
        public decimal LSPerBill { get; set; }
    }

}