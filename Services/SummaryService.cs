using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using DashboardAPI.Data;
using DashboardAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace DashboardAPI.Services
{
    public class SummaryService : ISummaryService
    {
        private readonly AppDbContext _context;

        public SummaryService(AppDbContext context)
        {
            _context = context;
        }

        private List<string> GetUserBranches(ClaimsPrincipal user)
        {
            return user
                .Claims.Where(c => c.Type == "Branch")
                .Select(c => c.Value.ToUpper()) // normalize uppercase เลย
                .ToList();
        }

        // ============================
        // Summary (Loss + Sale)
        // ============================
        public async Task<IEnumerable<LossSellSummaryDto>> GetSummaryAsync(
            ClaimsPrincipal user,
            string? outletNameFilter,
            DateTime? startDate,
            DateTime? endDate
        )
        {
            _context.Database.SetCommandTimeout(180);

            var allowedBranches = GetUserBranches(user).Select(b => b.ToUpper()).ToList();

            if (!allowedBranches.Any())
                return new List<LossSellSummaryDto>();

            // =========================
            // Loss Group
            // =========================
            var lossGrouped = await _context
                .uvw_El_Calculation.Where(x => allowedBranches.Contains(x.OutletName.ToUpper()))
                .Where(x =>
                    string.IsNullOrEmpty(outletNameFilter)
                    || x.OutletName.ToUpper() == outletNameFilter.ToUpper()
                )
                .Where(x =>
                    !startDate.HasValue
                    || (x.CalDate.HasValue && x.CalDate.Value.Date >= startDate.Value.Date)
                )
                .Where(x =>
                    !endDate.HasValue
                    || (x.CalDate.HasValue && x.CalDate.Value.Date <= endDate.Value.Date)
                )
                .GroupBy(x => new { x.OutletId, x.OutletName })
                .Select(g => new
                {
                    g.Key.OutletId,
                    g.Key.OutletName,
                    TotalLostItem = g.Sum(x => (decimal?)x.Lost_Item) ?? 0,
                    TotalLostTime = g.Sum(x => (decimal?)x.Lost_Time) ?? 0,
                    TotalLostSeating = g.Sum(x => (decimal?)x.Lost_Seating) ?? 0,
                    TotalLostTable = g.Sum(x => (decimal?)x.Lost_Table) ?? 0,
                    TotalLostPerBill = g.Sum(x => (decimal?)x.Lost_Per_Bill) ?? 0,
                    AvgPrice = g.Average(x => (decimal?)x.Avg_Price) ?? 0,
                    AvgPerBill = g.Average(x => (decimal?)x.Bill_Total) ?? 0,
                })
                .ToListAsync();

            // =========================
            // Sale Group
            // =========================
            var saleGrouped = await _context
                .Rpt_EL_CalculateSale.Where(s =>
                    string.IsNullOrEmpty(outletNameFilter)
                    || s.Branch_Code.ToUpper() == outletNameFilter.ToUpper()
                )
                .Where(s => !startDate.HasValue || s.OrderDate.Date >= startDate.Value.Date)
                .Where(s => !endDate.HasValue || s.OrderDate.Date <= endDate.Value.Date)
                .GroupBy(s => s.Branch_Code)
                .Select(g => new
                {
                    BranchCode = g.Key,
                    NetSale = g.Sum(x => (decimal?)x.NetSale) ?? 0,
                    NetSaleEatIn = g.Sum(x => (decimal?)x.NetSaleEatIn) ?? 0,
                    NetSaleOther = g.Sum(x => (decimal?)x.NetSaleOther) ?? 0,
                    Branch_Area = g.Max(x => x.Branch_Area) ?? string.Empty,
                    Branch_Staff = g.Max(x => (int?)x.Branch_Staff) ?? 0,
                    Lossell = g.Sum(x => (decimal?)x.LostSalePerBill * (decimal?)(x.TotalBill ?? 0))
                        ?? 0,
                })
                .ToListAsync();

            // =========================
            // Join
            // =========================
            var result =
                from l in lossGrouped
                join s in saleGrouped on l.OutletName equals s.BranchCode into ls
                from s in ls.DefaultIfEmpty()
                select new LossSellSummaryDto
                {
                    OutletId = l.OutletId,
                    OutletName = l.OutletName,
                    NetSale = s?.NetSale ?? 0,
                    NetSaleEatIn = s?.NetSaleEatIn ?? 0,
                    //  NetSaleOther = s?.NetSaleOther ?? 0,
                    BranchArea = s?.Branch_Area ?? string.Empty,
                    BranchStaff = s?.Branch_Staff ?? 0,
                    //  LSOpp = (l.TotalLostItem * l.AvgPrice) + (l.TotalLostTable * l.AvgPerBill) + l.TotalLostPerBill,
                    LSProd = l.TotalLostItem * l.AvgPrice,
                    LSSeating = l.TotalLostTable * l.AvgPerBill,
                    LSPerBill = s?.Lossell ?? 0,
                    LSOpp =
                        (l.TotalLostItem * l.AvgPrice)
                        + (l.TotalLostTable * l.AvgPerBill)
                        + (s?.Lossell ?? 0),
                    //  LostTime = l.TotalLostTime,
                    //  LostSeating = l.TotalLostSeating
                };

            return result.ToList();
        }

        // ============================
        // Detail per Outlet
        // ============================
        public async Task<LossSellDetailDto?> GetDetailByOutletAsync(
            string outletNameFilter,
            DateTime? startDate,
            DateTime? endDate
        )
        {
            if (string.IsNullOrEmpty(outletNameFilter))
                return null;

            var query = _context
                .uvw_El_Calculation.AsNoTracking()
                .Where(x => x.OutletName == outletNameFilter)
                .Where(x =>
                    !startDate.HasValue
                    || (x.CalDate.HasValue && x.CalDate.Value.Date >= startDate.Value.Date)
                )
                .Where(x =>
                    !endDate.HasValue
                    || (x.CalDate.HasValue && x.CalDate.Value.Date <= endDate.Value.Date)
                );

            var data = await query.ToListAsync();
            if (!data.Any())
                return null;

            var runIds = data.Select(d => d.RunId).Distinct().ToList();
            var notes = await _context
                .El_Calculation_Notes.AsNoTracking()
                .Where(n => runIds.Contains(n.RunId))
                .ToListAsync();

            var noteCountMap = notes.GroupBy(n => n.RunId).ToDictionary(g => g.Key, g => g.Count());

            DateTime? rangeStart = startDate?.Date;
            DateTime? rangeEndExclusive = endDate?.Date.AddDays(1);

            var rptSummary = await _context
                .Rpt_EL_CalculateSale.AsNoTracking()
                .Where(r => r.Branch_Code == outletNameFilter)
                .Where(r => !rangeStart.HasValue || r.OrderDate >= rangeStart.Value)
                .Where(r => !rangeEndExclusive.HasValue || r.OrderDate < rangeEndExclusive.Value)
                .ToListAsync();

            var isSingleDay =
                startDate.HasValue
                && endDate.HasValue
                && startDate.Value.Date == endDate.Value.Date;

            var hourlyDetail = data.GroupBy(x =>
                    string.IsNullOrWhiteSpace(x.CalHour) ? "0" : x.CalHour.Trim()
                )
                .Select(g =>
                {
                    var calHourStr = g.Key;
                    var digitPart = new string(calHourStr.TakeWhile(char.IsDigit).ToArray());
                    int calHourInt = -1;
                    if (!string.IsNullOrEmpty(digitPart))
                        int.TryParse(digitPart, out calHourInt);

                    var summaryForHour =
                        calHourInt >= 0
                            ? rptSummary.FirstOrDefault(r => r.OrderHour == calHourInt)
                            : null;

                    var totalLostItem = g.Sum(x => x.Lost_Item ?? 0m);
                    var totalLostTable = g.Sum(x => x.Lost_Table ?? 0m);
                    var avgPrice = g.Average(x => x.Avg_Price ?? 0m);
                    var avgPerBill = g.Average(x => x.Bill_Total ?? 0m);

                    return new HourlyTrendDto
                    {
                        Hour = calHourStr,
                        RunId = isSingleDay ? g.First().RunId : Guid.Empty,
                        LostItem = totalLostItem,
                        LostProductivity = Math.Round(g.Average(x => x.Lost_Productivity ?? 0m), 2),
                        LostTime = g.Sum(x => x.Lost_Time ?? 0m),
                        LostSeating = g.Sum(x => x.Lost_Seating ?? 0m),
                        LostTable = totalLostTable,
                        BillTotal = g.Sum(x => x.Bill_Total ?? 0m),
                        TotalOptimalBillPrice = g.Sum(x => x.Total_Optimal_Bill_Price ?? 0m),
                        LostPerBill =
                            summaryForHour != null
                                ? (summaryForHour.LostSalePerBill ?? 0)
                                    * (summaryForHour.TotalBill ?? 0)
                                : 0,
                        AvgBillTotal = g.Average(x => x.Avg_BillTotal ?? 0m),
                        AvgPrice = avgPrice,
                        ProductionTime = g.Sum(x => x.Production_Time ?? 0m),
                        OptimalMinute = g.Sum(x => x.Optimal_Minute ?? 0m),
                        AvgSeatingTime = g.Average(x => x.Avg_SeatingTime ?? 0m),
                        GapBetweenQueue = g.Average(x => x.Gap_Between_Queue ?? 0m),
                        NoteCount =
                            isSingleDay && g.Any() && noteCountMap.ContainsKey(g.First().RunId)
                                ? noteCountMap[g.First().RunId]
                                : 0,
                        LSProd = Math.Round(totalLostItem * avgPrice, 2),
                        LSSeating = Math.Round(totalLostTable * avgPerBill, 2),
                        LSPerBill =
                            summaryForHour != null
                                ? (summaryForHour.LostSalePerBill ?? 0)
                                    * (summaryForHour.TotalBill ?? 0)
                                : 0,
                        Median = summaryForHour?.Median,
                        AVGPerBill = summaryForHour?.Saleprebill,
                        ProdTimeAVG = g.Sum(x => x.ProdTimeAVG ?? 0),
                        Branch_Staff = summaryForHour?.Branch_Staff ?? 0,
                        DineInPercent = summaryForHour?.EatInPercent,
                        OtherPercent = summaryForHour?.OtherPercent,
                        BranchBill = summaryForHour?.Branch_Bill ?? 0,
                        LossSell =
                            summaryForHour != null
                                ? (summaryForHour.LostSalePerBill ?? 0)
                                    * (summaryForHour.TotalBill ?? 0)
                                : 0,
                        rptElCalculateSale = summaryForHour,
                    };
                })
                .OrderBy(x =>
                {
                    if (
                        int.TryParse(
                            new string(x.Hour.TakeWhile(char.IsDigit).ToArray()),
                            out var h
                        )
                    )
                        return h;
                    return int.MaxValue;
                })
                .ToList();

            return new LossSellDetailDto
            {
                OutletName = outletNameFilter,
                HourlyTrend = hourlyDetail,
            };
        }

        // ============================
        // Add Note
        // ============================
        public async Task<bool> AddNoteAsync(AddNoteDto dto)
        {
            var exists = await _context.uvw_El_Calculation.AnyAsync(x => x.RunId == dto.RunId);
            if (!exists)
                return false;

            var note = new ElCalculationNotes
            {
                RunId = dto.RunId,
                Note = dto.Note,
                CreatedBy = dto.CreatedBy,
                CreatedAt = DateTime.Now,
            };

            _context.El_Calculation_Notes.Add(note);
            await _context.SaveChangesAsync();
            return true;
        }

        // ============================
        // Note Feed
        // ============================
        public async Task<List<NoteFeedDto>> GetNotesFeedAsync(
            string outletNameFilter,
            DateTime? calDate
        )
        {
            var query = _context
                .El_Calculation_Notes.Join(
                    _context.uvw_El_Calculation,
                    note => note.RunId,
                    calc => calc.RunId,
                    (note, calc) => new { note, calc }
                )
                .Where(x => x.calc.OutletName == outletNameFilter);

            if (calDate.HasValue)
                query = query.Where(x => x.calc.CalDate.Value.Date == calDate.Value.Date);

            return await query
                .OrderByDescending(x => x.note.CreatedAt)
                .Select(x => new NoteFeedDto
                {
                    RunId = x.note.RunId,
                    OutletName = x.calc.OutletName,
                    Note = x.note.Note,
                    CreatedBy = x.note.CreatedBy,
                    CreatedAt = x.note.CreatedAt,
                    Hour = x.calc.CalHour,
                    CalDate = x.calc.CalDate.Value,
                })
                .ToListAsync();
        }

        public async Task<List<NoteFeedDto>> GetNotesByRunIdAsync(Guid runId)
        {
            return await _context
                .El_Calculation_Notes.Join(
                    _context.uvw_El_Calculation,
                    note => note.RunId,
                    calc => calc.RunId,
                    (note, calc) => new { note, calc }
                )
                .Where(x => x.note.RunId == runId)
                .OrderByDescending(x => x.note.CreatedAt)
                .Select(x => new NoteFeedDto
                {
                    RunId = x.note.RunId,
                    OutletName = x.calc.OutletName,
                    Note = x.note.Note,
                    CreatedBy = x.note.CreatedBy,
                    CreatedAt = x.note.CreatedAt,
                    Hour = x.calc.CalHour,
                    CalDate = x.calc.CalDate.Value,
                })
                .ToListAsync();
        }

        // ============================
        // Hourly Summary (aggregate only)
        // ============================
        public async Task<IEnumerable<HourlyChartDto>> GetHourlySummaryAsync(
            ClaimsPrincipal user,
            string? outletNameFilter,
            DateTime? startDate,
            DateTime? endDate
        )
        {
            var allowedBranches = GetUserBranches(user);
            if (!allowedBranches.Any())
                return new List<HourlyChartDto>();

            string? outletFilterUpper = outletNameFilter?.ToUpper();

            var query = _context
                .uvw_El_Calculation.AsQueryable()
                .Where(x => allowedBranches.Contains(x.OutletName.ToUpper()))
                .Where(x =>
                    string.IsNullOrEmpty(outletFilterUpper)
                    || x.OutletName.ToUpper() == outletFilterUpper
                )
                .Where(x =>
                    !startDate.HasValue
                    || (x.CalDate.HasValue && x.CalDate.Value.Date >= startDate.Value.Date)
                )
                .Where(x =>
                    !endDate.HasValue
                    || (x.CalDate.HasValue && x.CalDate.Value.Date <= endDate.Value.Date)
                );

            return await query
                .GroupBy(x => x.CalHour)
                .Select(g => new HourlyChartDto
                {
                    Hour = g.Key,
                    LostItem = g.Sum(x => x.Lost_Item ?? 0),
                    LostProductivity = Math.Round(g.Average(x => x.Lost_Productivity ?? 0m), 2),
                    LostTime = g.Sum(x => x.Lost_Time ?? 0),
                    LostSeating = g.Sum(x => x.Lost_Seating ?? 0),
                    LostTable = g.Sum(x => x.Lost_Table ?? 0),
                    BillTotal = g.Sum(x => x.Bill_Total ?? 0),
                    TotalOptimalBillPrice = g.Sum(x => x.Total_Optimal_Bill_Price ?? 0),
                    LostPerBill = g.Sum(x => x.Lost_Per_Bill ?? 0),
                    AvgBillTotal = g.Average(x => x.Avg_BillTotal ?? 0),
                    AvgPrice = g.Average(x => x.Avg_Price ?? 0),
                    ProductionTime = g.Sum(x => x.Production_Time ?? 0),
                    OptimalMinute = g.Sum(x => x.Optimal_Minute ?? 0),
                    AvgSeatingTime = g.Average(x => x.Avg_SeatingTime ?? 0),
                    GapBetweenQueue = g.Average(x => x.Gap_Between_Queue ?? 0),
                })
                .OrderBy(x => x.Hour)
                .ToListAsync();
        }
    }
}
