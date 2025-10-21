using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using DashboardAPI.Models;

namespace DashboardAPI.Services
{
    public interface ISummaryService
    {
        Task<IEnumerable<LossSellSummaryResponseDto>> GetSummaryAsync(
            ClaimsPrincipal user,
            string? outletNameFilter,
            DateTime? startDate,
            DateTime? endDate,
            bool isSum
        );
        Task<LossSellDetailDto?> GetDetailByOutletAsync(
            string outletNameFilter,
            DateTime? startDate,
            DateTime? endDate
        );
        Task<bool> AddNoteAsync(AddNoteDto dto);
        Task<List<NoteFeedDto>> GetNotesFeedAsync(string outletNameFilter, DateTime? calDate);
        Task<List<NoteFeedDto>> GetNotesByRunIdAsync(Guid runId);
        Task<IEnumerable<HourlyChartDto>> GetHourlySummaryAsync(
            ClaimsPrincipal user,
            string? outletNameFilter,
            DateTime? startDate,
            DateTime? endDate
        );
        Task<List<BillDetailDto>> GetBillsByOutletAndHourAsync(
            string outletName,
            DateTime? startDate,
            DateTime? endDate,
            string? hour
        );
        Task<List<SeatingLostDto>> GetSeatingLostByOutletAndHourAsync(
            string outletName,
            DateTime? startDate,
            DateTime? endDate,
            string? hour
        );
    }
}
