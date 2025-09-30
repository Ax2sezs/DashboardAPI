using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using DashboardAPI.Models;

namespace DashboardAPI.Services
{
    public interface ISummaryService
    {
        Task<IEnumerable<LossSellSummaryDto>> GetSummaryAsync(ClaimsPrincipal user, string? outletNameFilter, DateTime? startDate, DateTime? endDate);
        Task<LossSellDetailDto?> GetDetailByOutletAsync(string outletNameFilter, DateTime? startDate, DateTime? endDate);
        Task<bool> AddNoteAsync(AddNoteDto dto);
        Task<List<NoteFeedDto>> GetNotesFeedAsync(string outletNameFilter, DateTime? calDate);
        Task<List<NoteFeedDto>> GetNotesByRunIdAsync(Guid runId);
        Task<IEnumerable<HourlyChartDto>> GetHourlySummaryAsync(
        ClaimsPrincipal user,
        string? outletNameFilter,
        DateTime? startDate,
        DateTime? endDate);

    }
}
