namespace DashboardAPI.Models
{
    public class SeatingLostDto
    {
        public string BranchCode { get; set; } = string.Empty;
        public string Table_Name { get; set; } = string.Empty;
        public DateTime SeatDate { get; set; }
        public string? SeatHour { get; set; }
        public decimal? SeatingTime { get; set; }
        public decimal? Minute_Lost { get; set; }
        public DateTime? Start_Time { get; set; }
        public DateTime? End_Time { get; set; }
        public DateTime? Matched_Queue_Time { get; set; }
        public string? Table_Image { get; set; }
    }
}
