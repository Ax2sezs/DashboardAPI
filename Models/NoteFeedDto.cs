public class NoteFeedDto
{
    public Guid RunId { get; set; }
    public string OutletName { get; set; }
    public string Note { get; set; }
    public string CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Hour { get; set; } // ชั่วโมงที่บันทึก note
    public string? Status { get; set; } 
    public DateTime CalDate { get; set; } // วันที่บันทึก note
}
