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
            List<string>? outletNameFilter,
            DateTime? startDate,
            DateTime? endDate,
            bool isSum
        );

        Task<LossSellDetailDto?> GetDetailByOutletAsync(
            string? outletNameFilter,
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

        // B7: page/pageSize added — defaults maintain backward compatibility
        Task<List<BillDetailDto>> GetBillsByOutletAndHourAsync(
            string outletName,
            DateTime? startDate,
            DateTime? endDate,
            string? hour,
            int page = 1,
            int pageSize = 200
        );

        Task<List<ElCalculateSaleDetail>> GetLossPerBillByOutletAndHourAsync(
            string outletName,
            DateTime? startDate,
            DateTime? endDate,
            string? hour,
            int page = 1,
            int pageSize = 200
        );

        Task<List<SeatingLostDto>> GetSeatingLostByOutletAndHourAsync(
            string outletName,
            DateTime? startDate,
            DateTime? endDate,
            string? hour,
            int page = 1,
            int pageSize = 200
        );

        Task<DashboardInsightDto> GetDashboardInsightAsync(
            ClaimsPrincipal user,
            List<string>? outletNameFilter,
            DateTime? startDate,
            DateTime? endDate
        );
    }
}
