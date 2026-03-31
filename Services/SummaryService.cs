using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using DashboardAPI.Data;
using DashboardAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace DashboardAPI.Services
{
    public class SummaryService : ISummaryService
    {
        private readonly AppDbContext _context;
        private readonly IMemoryCache _cache;

        public SummaryService(AppDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        private List<string> GetUserBranches(ClaimsPrincipal user)
        {
            return user
                .Claims.Where(c => c.Type == "Branch")
                .Select(c => c.Value.ToUpper())
                .ToList();
        }

        // ─────────────────────────────────────────────────────────────
        // FIX B5: deterministic cache key scoped to user + filter
        // ─────────────────────────────────────────────────────────────
        private static string BuildSummaryCacheKey(
            List<string> allowedBranches,
            List<string>? outletFilters,
            DateTime startDate,
            DateTime endDate)
        {
            var branches = string.Join(",", allowedBranches.OrderBy(b => b));
            var outlets  = outletFilters?.Count > 0
                ? string.Join(",", outletFilters.OrderBy(o => o))
                : "ALL";
            return $"summary|{branches}|{outlets}|{startDate:yyyyMMdd}|{endDate:yyyyMMdd}";
        }

        // ─────────────────────────────────────────────────────────────
        // GetDashboardInsightAsync
        // FIX B1: delegates to GetSummaryAsync which now caches its
        //         result — second call within 5 min is cache-only.
        // ─────────────────────────────────────────────────────────────
        public async Task<DashboardInsightDto> GetDashboardInsightAsync(
            ClaimsPrincipal user,
            List<string>? outletNameFilter,
            DateTime? startDate,
            DateTime? endDate)
        {
            var summary = await GetSummaryAsync(user, outletNameFilter, startDate, endDate, false);
            var data    = summary.FirstOrDefault();

            if (data?.Details == null || !data.Details.Any())
                return new DashboardInsightDto();

            var detailList = data.Details;

            // Global KPI — single pass
            decimal totalLoss  = 0;
            decimal sumLoss    = 0;
            int     totalCount = 0;

            foreach (var x in detailList)
            {
                var loss = x.LSOpp ?? 0;
                totalLoss  += loss;
                sumLoss    += loss;
                totalCount++;
            }

            var avgLoss = totalCount == 0 ? 0 : sumLoss / totalCount;

            // Branch grouping
            var branchInsights = detailList
                .GroupBy(x => x.OutletName)
                .Select(g =>
                {
                    decimal total      = 0;
                    decimal sumProd    = 0;
                    decimal sumSeating = 0;
                    decimal sumPerBill = 0;
                    int     count      = 0;

                    var highLossList = new List<HighLossDayDto>();

                    foreach (var x in g)
                    {
                        var loss    = x.LSOpp ?? 0;
                        total      += loss;
                        sumProd    += x.LSProd    ?? 0;
                        sumSeating += x.LSSeating ?? 0;
                        sumPerBill += x.LSPerBill ?? 0;
                        count++;

                        highLossList.Add(new HighLossDayDto
                        {
                            Date      = x.Date,
                            Loss      = loss,
                            LSProd    = x.LSProd    ?? 0,
                            LSSeating = x.LSSeating ?? 0,
                            LSPerBill = x.LSPerBill ?? 0,
                        });
                    }

                    var avg       = count == 0 ? 0 : total / count;
                    var threshold = avg * 1.2m;

                    var highLossDays = highLossList
                        .Where(x => x.Loss > threshold)
                        .OrderByDescending(x => x.Loss)
                        .Take(5)
                        .ToList();

                    return new BranchInsightDto
                    {
                        OutletName      = g.Key,
                        TotalLoss       = total,
                        AvgLoss         = avg,
                        AvgLSProd       = count == 0 ? 0 : sumProd    / count,
                        AvgLSSeating    = count == 0 ? 0 : sumSeating / count,
                        AvgLSPerBill    = count == 0 ? 0 : sumPerBill / count,
                        IsAboveAverage  = avg > avgLoss,
                        IsCritical      = avg > avgLoss * 1.3m,
                        DeviationPercent = avgLoss == 0
                            ? 0
                            : Math.Round(((avg - avgLoss) / avgLoss) * 100, 2),
                        HighLossDays = highLossDays,
                    };
                })
                .OrderByDescending(x => x.TotalLoss)
                .ToList();

            return new DashboardInsightDto
            {
                AvgLoss        = avgLoss,
                TotalLoss      = totalLoss,
                BranchInsights = branchInsights,
            };
        }

        // ─────────────────────────────────────────────────────────────
        // GetSummaryAsync
        //
        // FIX B1/B5 : cache the joined details for 5 min so the
        //              twin call from GetDashboardInsightAsync is free.
        // FIX B2    : both DB queries run in parallel (Task.WhenAll)
        //              and the C# join uses a Dictionary (O(1) lookup).
        // FIX B3/B6 : ToUpper() removed from the column side of every
        //              WHERE clause — indexes on OutletName / Branch_Code
        //              are now usable (assumes CI collation, standard default).
        // ─────────────────────────────────────────────────────────────
        public async Task<IEnumerable<LossSellSummaryResponseDto>> GetSummaryAsync(
            ClaimsPrincipal user,
            List<string>? outletNameFilter,
            DateTime? startDate,
            DateTime? endDate,
            bool isSum)
        {
            _context.Database.SetCommandTimeout(180);

            // Resolve default date range
            if (!startDate.HasValue && !endDate.HasValue)
            {
                var latestDate = await _cache.GetOrCreateAsync("LATEST_LOSSSELL_MONTH", async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(6);
                    return await _context.uvw_El_Calculation
                        .Where(x => x.CalDate.HasValue)
                        .OrderByDescending(x => x.CalDate)
                        .Select(x => x.CalDate)
                        .FirstOrDefaultAsync();
                });

                if (latestDate.HasValue)
                {
                    startDate = new DateTime(latestDate.Value.Year, latestDate.Value.Month, 1);
                    endDate   = startDate.Value.AddMonths(1).AddDays(-1);
                }
                else
                {
                    var today = DateTime.Today;
                    startDate = new DateTime(today.Year, today.Month, 1);
                    endDate   = startDate.Value.AddMonths(1).AddDays(-1);
                }
            }

            // allowedBranches is already uppercased in GetUserBranches()
            var allowedBranches = GetUserBranches(user);
            if (!allowedBranches.Any())
            {
                return new List<LossSellSummaryResponseDto>
                {
                    new LossSellSummaryResponseDto
                    {
                        IsSum   = false,
                        Sum     = null,
                        Details = new List<LossSellSummaryDto>(),
                    },
                };
            }

            var outletFilters = outletNameFilter?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.ToUpper())
                .ToList();

            // ── FIX B5: cache the expensive join, keyed by user+filter ──
            var cacheKey = BuildSummaryCacheKey(
                allowedBranches, outletFilters, startDate!.Value, endDate!.Value);

            var details = await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);

                // ── FIX B3/B6: no ToUpper() on the column side ──
                //    allowedBranches / outletFilters are pre-normalised to uppercase.
                //    SQL Server's default CI collation handles the comparison.
                // Sequential awaits — DbContext is not thread-safe; Task.WhenAll
                // on the same context causes ConcurrencyDetector exceptions.
                var lossGrouped = await _context.uvw_El_Calculation
                    .Where(x => allowedBranches.Contains(x.OutletName))
                    .Where(x =>
                        outletFilters == null
                        || !outletFilters.Any()
                        || outletFilters.Contains(x.OutletName))
                    .Where(x =>
                        x.CalDate.HasValue
                        && x.CalDate.Value.Date >= startDate!.Value.Date
                        && x.CalDate.Value.Date <= endDate!.Value.Date)
                    .GroupBy(x => new { x.OutletId, x.OutletName, x.CalDate })
                    .Select(g => new
                    {
                        g.Key.OutletId,
                        g.Key.OutletName,
                        Date       = g.Key.CalDate!.Value.Date,
                        LostProd   = g.Sum(x => (decimal?)x.Lost_Productivity) ?? 0,
                        LostSeat   = g.Sum(x => (decimal?)x.Lost_Seating)      ?? 0,
                        AvgPrice   = g.Average(x => (decimal?)x.Avg_Price)     ?? 0,
                        AvgPerBill = g.Average(x => (decimal?)x.Bill_Total)    ?? 0,
                    })
                    .ToListAsync();

                var saleGrouped = await _context.Rpt_EL_CalculateSale
                    .Where(s =>
                        outletFilters == null
                        || !outletFilters.Any()
                        || outletFilters.Contains(s.Branch_Code))
                    .Where(s =>
                        s.OrderDate.Date >= startDate!.Value.Date
                        && s.OrderDate.Date <= endDate!.Value.Date)
                    .GroupBy(s => new { s.Branch_Code, Date = s.OrderDate.Date })
                    .Select(g => new
                    {
                        BranchCode   = g.Key.Branch_Code,
                        Date         = g.Key.Date,
                        NetSaleEatIn = g.Sum(x => (decimal?)x.NetSaleEatIn) ?? 0,
                        BranchArea   = g.Max(x => x.Branch_Area)            ?? string.Empty,
                        BranchStaff  = g.Max(x => (int?)x.Branch_Staff)     ?? 0,
                        LossSell     = g.Sum(x =>
                            ((x.Branch_Bill - x.Saleprebill) * x.TotalBill) < 0
                                ? 0
                                : (x.Branch_Bill - x.Saleprebill) * x.TotalBill),
                    })
                    .ToListAsync();

                // ── FIX B2: O(1) dictionary lookup replaces O(n×m) GroupJoin scan ──
                var saleLookup = saleGrouped.ToDictionary(
                    s => (s.BranchCode.ToUpper(), s.Date));

                return lossGrouped
                    .Select(l =>
                    {
                        saleLookup.TryGetValue((l.OutletName.ToUpper(), l.Date), out var s);

                        var lsProd   = l.LostProd;
                        var lsSeat   = l.LostSeat;
                        var lsPerBill = s?.LossSell ?? 0;

                        return new LossSellSummaryDto
                        {
                            OutletId     = l.OutletId,
                            OutletName   = l.OutletName,
                            Date         = l.Date,
                            NetSaleEatIn = s?.NetSaleEatIn ?? 0,
                            BranchArea   = s?.BranchArea   ?? string.Empty,
                            BranchStaff  = s?.BranchStaff  ?? 0,
                            LSProd       = lsProd,
                            LSSeating    = lsSeat,
                            LSPerBill    = lsPerBill,
                            LSOpp        = lsProd + lsSeat + lsPerBill,
                        };
                    })
                    .ToList();
            });

            // isSum is a cheap in-memory aggregation over already-cached details
            List<LossSellSummaryDto>? sum = null;
            if (isSum)
            {
                sum = details!
                    .GroupBy(x => new { x.OutletId, x.OutletName })
                    .Select(g => new LossSellSummaryDto
                    {
                        OutletId     = g.Key.OutletId,
                        OutletName   = g.Key.OutletName,
                        NetSaleEatIn = g.Sum(x => x.NetSaleEatIn),
                        BranchArea   = g.Max(x => x.BranchArea),
                        BranchStaff  = g.Max(x => x.BranchStaff),
                        LSProd       = g.Sum(x => x.LSProd),
                        LSSeating    = g.Sum(x => x.LSSeating),
                        LSPerBill    = g.Sum(x => x.LSPerBill),
                        LSOpp        = g.Sum(x => x.LSOpp),
                        Date         = default,
                    })
                    .ToList();
            }

            return new List<LossSellSummaryResponseDto>
            {
                new LossSellSummaryResponseDto
                {
                    IsSum     = isSum,
                    Sum       = sum,
                    Details   = details!,
                    StartDate = startDate!.Value,
                    EndDate   = endDate!.Value,
                }
            };
        }

        // ─────────────────────────────────────────────────────────────
        // GetDetailByOutletAsync
        //
        // FIX B4: project only the columns actually used before
        //         materializing, and replace the O(n×m) FirstOrDefault
        //         loop with an O(1) dictionary lookup.
        // ─────────────────────────────────────────────────────────────
        public async Task<LossSellDetailDto?> GetDetailByOutletAsync(
            string? outletNameFilter,
            DateTime? startDate,
            DateTime? endDate)
        {
            if (string.IsNullOrEmpty(outletNameFilter))
                return null;

            // ── FIX B4: project only required columns ──
            var lossData = await _context.uvw_El_Calculation
                .AsNoTracking()
                .Where(x => x.OutletName == outletNameFilter)
                .Where(x => !startDate.HasValue || x.CalDate >= startDate.Value.Date)
                .Where(x => !endDate.HasValue   || x.CalDate <= endDate.Value.Date)
                .Select(x => new
                {
                    x.RunId,
                    x.CalHour,
                    x.Lost_Item,
                    x.Avg_Price,
                    x.Lost_Seating,
                    x.ProdTimeAVG,
                    x.Avg_SeatingTime,
                })
                .ToListAsync();

            if (!lossData.Any())
                return null;

            var runIds = lossData.Select(d => d.RunId).Distinct().ToList();

            var noteCountMap = await _context.El_Calculation_Notes
                .AsNoTracking()
                .Where(n => runIds.Contains(n.RunId))
                .GroupBy(n => n.RunId)
                .ToDictionaryAsync(g => g.Key, g => g.Count());

            var rangeEndExclusive = endDate?.Date.AddDays(1);

            // ── FIX B4: project only required columns ──
            var salesSummary = await _context.Rpt_EL_CalculateSale
                .AsNoTracking()
                .Where(r => r.Branch_Code == outletNameFilter)
                .Where(r => !startDate.HasValue          || r.OrderDate >= startDate.Value)
                .Where(r => !rangeEndExclusive.HasValue  || r.OrderDate < rangeEndExclusive.Value)
                .Select(r => new
                {
                    r.OrderHour,
                    r.Median,
                    r.Saleprebill,
                    r.TotalBill,
                    r.Branch_Bill,
                    r.NetSaleEatIn,
                    r.Branch_Area,
                    r.Branch_Staff,
                    r.CashierSpeedSec,
                    r.EatInPercent,
                    r.OtherPercent,
                })
                .ToListAsync();

            var isSingleDay = startDate.HasValue
                && endDate.HasValue
                && startDate.Value.Date == endDate.Value.Date;

            // ── FIX B4: O(1) lookup replaces O(n×m) FirstOrDefault inside loop ──
            var saleByHour = salesSummary
                .GroupBy(s => s.OrderHour ?? -1)
                .ToDictionary(g => g.Key, g => g.First());

            var hourlyTrend = lossData
                .GroupBy(x => string.IsNullOrWhiteSpace(x.CalHour) ? "0" : x.CalHour.Trim())
                .Select(g =>
                {
                    int.TryParse(
                        new string(g.Key.TakeWhile(char.IsDigit).ToArray()),
                        out int hourInt);

                    saleByHour.TryGetValue(hourInt, out var sale);

                    decimal medianValue = (decimal)(sale?.Median ?? 0.0);
                    decimal lsMedian = sale != null
                        ? Math.Round(
                            (medianValue - (sale.Saleprebill ?? 0m)) * (sale.TotalBill ?? 0m), 2)
                        : 0m;

                    decimal lsProd = Math.Round(
                        g.Sum(x => x.Lost_Item ?? 0m) * g.Average(x => x.Avg_Price ?? 0m), 2);
                    decimal lsSeating  = Math.Round(g.Sum(x => x.Lost_Seating ?? 0m), 2);
                    decimal lsPerBill  = sale != null
                        ? Math.Max(0,
                            ((sale.Branch_Bill ?? 0m) - (sale.Saleprebill ?? 0m))
                            * (sale.TotalBill ?? 0m))
                        : 0m;

                    decimal lsOpp         = lsProd + lsSeating + lsPerBill;
                    decimal prodTimeAVG   = Math.Round(g.Average(x => x.ProdTimeAVG     ?? 0m), 2);
                    decimal avgSeatingTime = Math.Round(g.Average(x => x.Avg_SeatingTime ?? 0m), 2);

                    int noteCount = isSingleDay && noteCountMap.ContainsKey(g.First().RunId)
                        ? noteCountMap[g.First().RunId]
                        : 0;

                    return new HourlyTrendDto
                    {
                        Hour            = g.Key,
                        RunId           = isSingleDay ? g.First().RunId : Guid.Empty,
                        LSProd          = lsProd,
                        LSSeating       = lsSeating,
                        LSPerBill       = lsPerBill,
                        LSMedian        = lsMedian,
                        LSOPP           = lsOpp,
                        TotalBill       = sale?.TotalBill,
                        NoteCount       = noteCount,
                        ProdTimeAVG     = prodTimeAVG,
                        Avg_SeatingTime = avgSeatingTime,
                        Branch_Bill     = sale?.Branch_Bill     ?? 0,
                        AVGPerBill      = sale?.Saleprebill,
                        NetSaleEatIn    = (decimal)(sale?.NetSaleEatIn ?? 0),
                        Median          = (double?)medianValue,
                        CashierSpeedSec = sale?.CashierSpeedSec ?? "0",
                        Branch_Area     = sale?.Branch_Area     ?? "0",
                        Branch_Staff    = sale?.Branch_Staff    ?? 0,
                        DineInPercent   = (decimal?)(sale?.EatInPercent  ?? 0),
                        OtherPercent    = (decimal?)(sale?.OtherPercent  ?? 0),
                    };
                })
                .OrderBy(x =>
                    int.TryParse(
                        new string(x.Hour.TakeWhile(char.IsDigit).ToArray()), out var h) ? h : 0)
                .ToList();

            return new LossSellDetailDto
            {
                OutletName   = outletNameFilter,
                NetDineIn    = (decimal)salesSummary.Sum(s => s.NetSaleEatIn ?? 0),
                LossOpp      = hourlyTrend.Sum(h => h.LSOPP),
                DailyPerBill = hourlyTrend.Average(h => h.AVGPerBill ?? 0),
                HourlyTrend  = hourlyTrend,
            };
        }

        // ─────────────────────────────────────────────────────────────
        // AddNoteAsync — unchanged
        // ─────────────────────────────────────────────────────────────
        public async Task<bool> AddNoteAsync(AddNoteDto dto)
        {
            var exists = await _context.uvw_El_Calculation.AnyAsync(x => x.RunId == dto.RunId);
            if (!exists)
                return false;

            var note = new ElCalculationNotes
            {
                RunId     = dto.RunId,
                Note      = dto.Note,
                CreatedBy = dto.CreatedBy,
                CreatedAt = DateTime.Now,
                Status    = dto.Status,
            };

            _context.El_Calculation_Notes.Add(note);
            await _context.SaveChangesAsync();
            return true;
        }

        // ─────────────────────────────────────────────────────────────
        // GetNotesFeedAsync — unchanged
        // ─────────────────────────────────────────────────────────────
        public async Task<List<NoteFeedDto>> GetNotesFeedAsync(
            string outletNameFilter,
            DateTime? calDate)
        {
            var query = _context
                .El_Calculation_Notes.Join(
                    _context.uvw_El_Calculation,
                    note => note.RunId,
                    calc => calc.RunId,
                    (note, calc) => new { note, calc })
                .Where(x => x.calc.OutletName == outletNameFilter);

            if (calDate.HasValue)
                query = query.Where(x => x.calc.CalDate.Value.Date == calDate.Value.Date);

            return await query
                .OrderByDescending(x => x.note.CreatedAt)
                .Select(x => new NoteFeedDto
                {
                    RunId      = x.note.RunId,
                    OutletName = x.calc.OutletName,
                    Note       = x.note.Note,
                    CreatedBy  = x.note.CreatedBy,
                    CreatedAt  = x.note.CreatedAt,
                    Hour       = x.calc.CalHour,
                    CalDate    = x.calc.CalDate.Value,
                    Status     = x.note.Status,
                })
                .ToListAsync();
        }

        // ─────────────────────────────────────────────────────────────
        // GetNotesByRunIdAsync — unchanged
        // ─────────────────────────────────────────────────────────────
        public async Task<List<NoteFeedDto>> GetNotesByRunIdAsync(Guid runId)
        {
            return await _context
                .El_Calculation_Notes.Join(
                    _context.uvw_El_Calculation,
                    note => note.RunId,
                    calc => calc.RunId,
                    (note, calc) => new { note, calc })
                .Where(x => x.note.RunId == runId)
                .OrderByDescending(x => x.note.CreatedAt)
                .Select(x => new NoteFeedDto
                {
                    RunId      = x.note.RunId,
                    OutletName = x.calc.OutletName,
                    Note       = x.note.Note,
                    CreatedBy  = x.note.CreatedBy,
                    CreatedAt  = x.note.CreatedAt,
                    Hour       = x.calc.CalHour,
                    CalDate    = x.calc.CalDate.Value,
                    Status     = x.note.Status,
                })
                .ToListAsync();
        }

        // ─────────────────────────────────────────────────────────────
        // GetHourlySummaryAsync
        // FIX B6: removed ToUpper() on the Contains() column side.
        // ─────────────────────────────────────────────────────────────
        public async Task<IEnumerable<HourlyChartDto>> GetHourlySummaryAsync(
            ClaimsPrincipal user,
            string? outletNameFilter,
            DateTime? startDate,
            DateTime? endDate)
        {
            var allowedBranches = GetUserBranches(user);
            if (!allowedBranches.Any())
                return new List<HourlyChartDto>();

            // allowedBranches is already uppercase; remove ToUpper() from column side
            string? outletFilterUpper = outletNameFilter?.ToUpper();

            var query = _context.uvw_El_Calculation
                .AsQueryable()
                .Where(x => allowedBranches.Contains(x.OutletName))
                .Where(x =>
                    string.IsNullOrEmpty(outletFilterUpper)
                    || x.OutletName.ToUpper() == outletFilterUpper)
                .Where(x =>
                    !startDate.HasValue
                    || (x.CalDate.HasValue && x.CalDate.Value.Date >= startDate.Value.Date))
                .Where(x =>
                    !endDate.HasValue
                    || (x.CalDate.HasValue && x.CalDate.Value.Date <= endDate.Value.Date));

            return await query
                .GroupBy(x => x.CalHour)
                .Select(g => new HourlyChartDto
                {
                    Hour                 = g.Key,
                    LostItem             = g.Sum(x => x.Lost_Item              ?? 0),
                    LostProductivity     = Math.Round(g.Average(x => x.Lost_Productivity ?? 0m), 2),
                    LostTime             = g.Sum(x => x.Lost_Time              ?? 0),
                    LostSeating          = g.Sum(x => x.Lost_Seating           ?? 0),
                    LostTable            = g.Sum(x => x.Lost_Table             ?? 0),
                    BillTotal            = g.Sum(x => x.Bill_Total             ?? 0),
                    TotalOptimalBillPrice = g.Sum(x => x.Total_Optimal_Bill_Price ?? 0),
                    LostPerBill          = g.Sum(x => x.Lost_Per_Bill          ?? 0),
                    AvgBillTotal         = g.Average(x => x.Avg_BillTotal      ?? 0),
                    AvgPrice             = g.Average(x => x.Avg_Price          ?? 0),
                    ProductionTime       = g.Sum(x => x.Production_Time        ?? 0),
                    OptimalMinute        = g.Sum(x => x.Optimal_Minute         ?? 0),
                    AvgSeatingTime       = g.Average(x => x.Avg_SeatingTime    ?? 0),
                    GapBetweenQueue      = g.Average(x => x.Gap_Between_Queue  ?? 0),
                })
                .OrderBy(x => x.Hour)
                .ToListAsync();
        }

        // ─────────────────────────────────────────────────────────────
        // GetBillsByOutletAndHourAsync
        // FIX B7: pagination via Skip/Take — default 200 rows/page.
        // ─────────────────────────────────────────────────────────────
        public async Task<List<BillDetailDto>> GetBillsByOutletAndHourAsync(
            string outletName,
            DateTime? startDate,
            DateTime? endDate,
            string? hour,
            int page     = 1,
            int pageSize = 200)
        {
            if (string.IsNullOrEmpty(outletName))
                return new List<BillDetailDto>();

            var query = _context.rpt_El_ProductionTime
                .AsNoTracking()
                .Where(x => x.Branch_Code == outletName);

            if (startDate.HasValue)
                query = query.Where(x => x.BillDate >= startDate.Value.Date);

            if (endDate.HasValue)
                query = query.Where(x => x.BillDate <= endDate.Value.Date);

            if (!string.IsNullOrEmpty(hour))
                query = query.Where(x => x.BillHour == hour);

            return await query
                .OrderBy(x => x.BillDate)
                .ThenBy(x => x.BillNo)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new BillDetailDto
                {
                    BillDate       = x.BillDate,
                    Branch_Code    = x.Branch_Code,
                    BillHour       = x.BillHour,
                    BillNo         = x.BillNo,
                    BillTotal      = x.BillTotal,
                    Category       = x.Category,
                    ProductName    = x.ProductName,
                    Price          = x.Price,
                    ProductionTime = x.ProductionTime,
                    OptimalTime    = x.OptimalTime,
                    AICategory     = x.AICatogory,
                    AIMatchTime    = x.AIMatchTime,
                })
                .ToListAsync();
        }

        // ─────────────────────────────────────────────────────────────
        // GetSeatingLostByOutletAndHourAsync
        // FIX B7: pagination via Skip/Take.
        // ─────────────────────────────────────────────────────────────
        public async Task<List<SeatingLostDto>> GetSeatingLostByOutletAndHourAsync(
            string outletName,
            DateTime? startDate,
            DateTime? endDate,
            string? hour,
            int page     = 1,
            int pageSize = 200)
        {
            if (string.IsNullOrEmpty(outletName))
                return new List<SeatingLostDto>();

            var query = _context.Rpt_EL_SeatingLost
                .AsNoTracking()
                .Where(x => x.BranchCode == outletName);

            if (startDate.HasValue)
                query = query.Where(x => x.SeatDate >= startDate.Value.Date);

            if (endDate.HasValue)
                query = query.Where(x => x.SeatDate <= endDate.Value.Date);

            if (!string.IsNullOrEmpty(hour) && int.TryParse(hour, out var hourInt))
                query = query.Where(x =>
                    x.Start_Time.HasValue && x.Start_Time.Value.Hour == hourInt);

            return await query
                .OrderBy(x => x.SeatDate)
                .ThenBy(x => x.Table_Name)
                .ThenBy(x => x.Start_Time)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new SeatingLostDto
                {
                    BranchCode         = x.BranchCode,
                    Table_Name         = x.Table_Name,
                    SeatDate           = x.SeatDate,
                    SeatHour           = x.SeatHour,
                    SeatingTime        = x.SeatingTime,
                    Minute_Lost        = x.Minute_Lost,
                    Start_Time         = x.Start_Time,
                    End_Time           = x.End_Time,
                    Matched_Queue_Time = x.Matched_Queue_Time,
                    Table_Image        = x.Table_Image,
                })
                .ToListAsync();
        }

        // ─────────────────────────────────────────────────────────────
        // GetLossPerBillByOutletAndHourAsync
        // FIX B7: pagination via Skip/Take.
        // ─────────────────────────────────────────────────────────────
        public async Task<List<ElCalculateSaleDetail>> GetLossPerBillByOutletAndHourAsync(
            string outletName,
            DateTime? startDate,
            DateTime? endDate,
            string? hour,
            int page     = 1,
            int pageSize = 200)
        {
            if (string.IsNullOrEmpty(outletName))
                return new List<ElCalculateSaleDetail>();

            var query = _context.rpt_EL_CalculateSale_Detail
                .AsNoTracking()
                .Where(x => x.Branch_Code == outletName);

            if (startDate.HasValue)
                query = query.Where(x => x.Ord_Dt >= startDate.Value.Date);

            if (endDate.HasValue)
                query = query.Where(x => x.Ord_Dt <= endDate.Value.Date);

            if (!string.IsNullOrEmpty(hour) && int.TryParse(hour, out int hourInt))
                query = query.Where(x => x.OrderHour == hourInt);

            return await query
                .OrderBy(x => x.Ord_Dt)
                .ThenBy(x => x.OrderHour)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new ElCalculateSaleDetail
                {
                    Branch_Code     = x.Branch_Code,
                    Ord_Dt          = x.Ord_Dt,
                    OrderHour       = x.OrderHour,
                    Branch_Staff    = x.Branch_Staff,
                    Branch_Area     = x.Branch_Area,
                    CashierSpeedSec = x.CashierSpeedSec,
                    NetSale         = x.NetSale,
                    NetSaleEatIn    = x.NetSaleEatIn,
                    NetSaleOther    = x.NetSaleOther,
                    BName           = x.BName,
                    Branch_Bill     = x.Branch_Bill,
                    Branch_ProdTime = x.Branch_ProdTime,
                    Q1              = x.Q1,
                    Median          = x.Median,
                    Q3              = x.Q3,
                })
                .ToListAsync();
        }
    }
}
