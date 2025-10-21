using DashboardAPI.Models;

public class LossSellSummaryResponseDto
{
    public bool IsSum { get; set; }
    public List<LossSellSummaryDto>? Sum { get; set; } // ถ้า isSum = false จะ null
    public List<LossSellSummaryDto> Details { get; set; } = new();
}
