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
        // public async Task<IEnumerable<LossSellSummaryDto>> GetSummaryAsync(
        //     ClaimsPrincipal user,
        //     string? outletNameFilter,
        //     DateTime? startDate,
        //     DateTime? endDate
        // )
        // {
        //     _context.Database.SetCommandTimeout(180);

        //     var allowedBranches = GetUserBranches(user).Select(b => b.ToUpper()).ToList();

        //     if (!allowedBranches.Any())
        //         return new List<LossSellSummaryDto>();

        //     // =========================
        //     // Loss Group
        //     // =========================
        //     var lossGrouped = await _context
        //         .uvw_El_Calculation.Where(x => allowedBranches.Contains(x.OutletName.ToUpper()))
        //         .Where(x =>
        //             string.IsNullOrEmpty(outletNameFilter)
        //             || x.OutletName.ToUpper() == outletNameFilter.ToUpper()
        //         )
        //         .Where(x =>
        //             !startDate.HasValue
        //             || (x.CalDate.HasValue && x.CalDate.Value.Date >= startDate.Value.Date)
        //         )
        //         .Where(x =>
        //             !endDate.HasValue
        //             || (x.CalDate.HasValue && x.CalDate.Value.Date <= endDate.Value.Date)
        //         )
        //         .GroupBy(x => new { x.OutletId, x.OutletName })
        //         .Select(g => new
        //         {
        //             g.Key.OutletId,
        //             g.Key.OutletName,
        //             TotalLostItem = g.Sum(x => (decimal?)x.Lost_Item) ?? 0,
        //             TotalLostTime = g.Sum(x => (decimal?)x.Lost_Time) ?? 0,
        //             TotalLostSeating = g.Sum(x => (decimal?)x.Lost_Seating) ?? 0,
        //             TotalLostTable = g.Sum(x => (decimal?)x.Lost_Table) ?? 0,
        //             TotalLostPerBill = g.Sum(x => (decimal?)x.Lost_Per_Bill) ?? 0,
        //             AvgPrice = g.Average(x => (decimal?)x.Avg_Price) ?? 0,
        //             AvgPerBill = g.Average(x => (decimal?)x.Bill_Total) ?? 0,
        //         })
        //         .ToListAsync();

        //     // =========================
        //     // Sale Group
        //     // =========================
        //     var saleGrouped = await _context
        //         .Rpt_EL_CalculateSale.Where(s =>
        //             string.IsNullOrEmpty(outletNameFilter)
        //             || s.Branch_Code.ToUpper() == outletNameFilter.ToUpper()
        //         )
        //         .Where(s => !startDate.HasValue || s.OrderDate.Date >= startDate.Value.Date)
        //         .Where(s => !endDate.HasValue || s.OrderDate.Date <= endDate.Value.Date)
        //         .GroupBy(s => s.Branch_Code)
        //         .Select(g => new
        //         {
        //             BranchCode = g.Key,
        //             NetSale = g.Sum(x => (decimal?)x.NetSale) ?? 0,
        //             NetSaleEatIn = g.Sum(x => (decimal?)x.NetSaleEatIn) ?? 0,
        //             NetSaleOther = g.Sum(x => (decimal?)x.NetSaleOther) ?? 0,
        //             Branch_Area = g.Max(x => x.Branch_Area) ?? string.Empty,
        //             Branch_Staff = g.Max(x => (int?)x.Branch_Staff) ?? 0,
        //             Lossell = g.Sum(x => (decimal?)x.LostSalePerBill * (decimal?)(x.TotalBill ?? 0))
        //                 ?? 0,
        //         })
        //         .ToListAsync();

        //     // =========================
        //     // Join
        //     // =========================
        //     var result =
        //         from l in lossGrouped
        //         join s in saleGrouped on l.OutletName equals s.BranchCode into ls
        //         from s in ls.DefaultIfEmpty()
        //         select new LossSellSummaryDto
        //         {
        //             OutletId = l.OutletId,
        //             OutletName = l.OutletName,
        //             NetSale = s?.NetSale ?? 0,
        //             NetSaleEatIn = s?.NetSaleEatIn ?? 0,
        //             //  NetSaleOther = s?.NetSaleOther ?? 0,
        //             BranchArea = s?.Branch_Area ?? string.Empty,
        //             BranchStaff = s?.Branch_Staff ?? 0,
        //             //  LSOpp = (l.TotalLostItem * l.AvgPrice) + (l.TotalLostTable * l.AvgPerBill) + l.TotalLostPerBill,
        //             LSProd = l.TotalLostItem * l.AvgPrice,
        //             LSSeating = l.TotalLostTable * l.AvgPerBill,
        //             LSPerBill = s?.Lossell ?? 0,
        //             LSOpp =
        //                 (l.TotalLostItem * l.AvgPrice)
        //                 + (l.TotalLostTable * l.AvgPerBill)
        //                 + (s?.Lossell ?? 0),
        //             //  LostTime = l.TotalLostTime,
        //             //  LostSeating = l.TotalLostSeating
        //         };

        //     return result.ToList();
        // }
        // public async Task<IEnumerable<LossSellSummaryDto>> GetSummaryAsync(
        //     ClaimsPrincipal user,
        //     string? outletNameFilter,
        //     DateTime? startDate,
        //     DateTime? endDate
        // )
        // {
        //     _context.Database.SetCommandTimeout(180);

        //     var allowedBranches = GetUserBranches(user).Select(b => b.ToUpper()).ToList();
        //     if (!allowedBranches.Any())
        //         return new List<LossSellSummaryDto>();

        //     // =========================
        //     // Loss Group (Daily)
        //     // =========================
        //     var lossGrouped = await _context
        //         .uvw_El_Calculation.Where(x => allowedBranches.Contains(x.OutletName.ToUpper()))
        //         .Where(x =>
        //             string.IsNullOrEmpty(outletNameFilter)
        //             || x.OutletName.ToUpper() == outletNameFilter.ToUpper()
        //         )
        //         .Where(x =>
        //             !startDate.HasValue
        //             || (x.CalDate.HasValue && x.CalDate.Value.Date >= startDate.Value.Date)
        //         )
        //         .Where(x =>
        //             !endDate.HasValue
        //             || (x.CalDate.HasValue && x.CalDate.Value.Date <= endDate.Value.Date)
        //         )
        //         .GroupBy(x => new
        //         {
        //             x.OutletId,
        //             x.OutletName,
        //             x.CalDate,
        //         })
        //         .Select(g => new
        //         {
        //             g.Key.OutletId,
        //             g.Key.OutletName,
        //             Date = g.Key.CalDate.Value.Date,
        //             TotalLostItem = g.Sum(x => (decimal?)x.Lost_Item) ?? 0,
        //             TotalLostTime = g.Sum(x => (decimal?)x.Lost_Time) ?? 0,
        //             TotalLostSeating = g.Sum(x => (decimal?)x.Lost_Seating) ?? 0,
        //             TotalLostTable = g.Sum(x => (decimal?)x.Lost_Table) ?? 0,
        //             TotalLostPerBill = g.Sum(x => (decimal?)x.Lost_Per_Bill) ?? 0,
        //             AvgPrice = g.Average(x => (decimal?)x.Avg_Price) ?? 0,
        //             AvgPerBill = g.Average(x => (decimal?)x.Bill_Total) ?? 0,
        //             LostProd = g.Sum(x => (decimal?)x.Lost_Productivity) ?? 0,
        //             LostSeat = g.Sum(x => (decimal?)x.Lost_Seating) ?? 0,
        //         })
        //         .ToListAsync();

        //     // =========================
        //     // Sale Group (Daily)
        //     // =========================
        //     var saleGrouped = await _context
        //         .Rpt_EL_CalculateSale.Where(s =>
        //             string.IsNullOrEmpty(outletNameFilter)
        //             || s.Branch_Code.ToUpper() == outletNameFilter.ToUpper()
        //         )
        //         .Where(s => !startDate.HasValue || s.OrderDate.Date >= startDate.Value.Date)
        //         .Where(s => !endDate.HasValue || s.OrderDate.Date <= endDate.Value.Date)
        //         .GroupBy(s => new { s.Branch_Code, s.OrderDate.Date })
        //         .Select(g => new
        //         {
        //             BranchCode = g.Key.Branch_Code,
        //             Date = g.Key.Date,
        //             NetSale = g.Sum(x => (decimal?)x.NetSale) ?? 0,
        //             NetSaleEatIn = g.Sum(x => (decimal?)x.NetSaleEatIn) ?? 0,
        //             NetSaleOther = g.Sum(x => (decimal?)x.NetSaleOther) ?? 0,
        //             Branch_Area = g.Max(x => x.Branch_Area) ?? string.Empty,
        //             Branch_Staff = g.Max(x => (int?)x.Branch_Staff) ?? 0,
        //             Lossell = g.Sum(x => (decimal?)x.LostSalePerBill * (decimal?)(x.TotalBill ?? 0))
        //                 ?? 0,
        //         })
        //         .ToListAsync();

        //     // =========================
        //     // Join
        //     // =========================
        //     var result =
        //         from l in lossGrouped
        //         join s in saleGrouped
        //             on new { l.OutletName, l.Date } equals new { OutletName = s.BranchCode, s.Date }
        //             into ls
        //         from s in ls.DefaultIfEmpty()
        //         select new LossSellSummaryDto
        //         {
        //             OutletId = l.OutletId,
        //             OutletName = l.OutletName,
        //             Date = l.Date,
        //             NetSale = s?.NetSale ?? 0,
        //             NetSaleEatIn = s?.NetSaleEatIn ?? 0,
        //             BranchArea = s?.Branch_Area ?? string.Empty,
        //             BranchStaff = s?.Branch_Staff ?? 0,
        //             // LSProd = l.TotalLostItem * l.AvgPrice,
        //             // LSSeating = l.TotalLostTable * l.AvgPerBill,
        //             LSProd = l.LostProd,
        //             LSSeating = l.LostSeat,
        //             LSPerBill = s?.Lossell ?? 0,
        //             LSOpp =
        //                 (l.TotalLostItem * l.AvgPrice)
        //                 + (l.TotalLostTable * l.AvgPerBill)
        //                 + (s?.Lossell ?? 0),
        //         };

        //     return result.ToList();
        // }

        public async Task<IEnumerable<LossSellSummaryResponseDto>> GetSummaryAsync(
            ClaimsPrincipal user,
            string? outletNameFilter,
            DateTime? startDate,
            DateTime? endDate,
            bool isSum
        )
        {
            _context.Database.SetCommandTimeout(180);

            var allowedBranches = GetUserBranches(user).Select(b => b.ToUpper()).ToList();
            if (!allowedBranches.Any())
                return new List<LossSellSummaryResponseDto>
                {
                    new LossSellSummaryResponseDto
                    {
                        IsSum = false,
                        Sum = null,
                        Details = new List<LossSellSummaryDto>(),
                    },
                };

            // =========================
            // 1. Loss Group (Daily)
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
                .GroupBy(x => new
                {
                    x.OutletId,
                    x.OutletName,
                    x.CalDate,
                })
                .Select(g => new
                {
                    g.Key.OutletId,
                    g.Key.OutletName,
                    Date = g.Key.CalDate.Value.Date,
                    LostProd = g.Sum(x => (decimal?)x.Lost_Productivity) ?? 0,
                    LostSeat = g.Sum(x => (decimal?)x.Lost_Seating) ?? 0,
                    TotalLostItem = g.Sum(x => (decimal?)x.Lost_Item) ?? 0,
                    TotalLostTable = g.Sum(x => (decimal?)x.Lost_Table) ?? 0,
                    AvgPrice = g.Average(x => (decimal?)x.Avg_Price) ?? 0,
                    AvgPerBill = g.Average(x => (decimal?)x.Bill_Total) ?? 0,
                })
                .ToListAsync();

            // =========================
            // 2. Sale Group (Daily)
            // =========================
            var saleGrouped = await _context
                .Rpt_EL_CalculateSale.Where(s =>
                    string.IsNullOrEmpty(outletNameFilter)
                    || s.Branch_Code.ToUpper() == outletNameFilter.ToUpper()
                )
                .Where(s => !startDate.HasValue || s.OrderDate.Date >= startDate.Value.Date)
                .Where(s => !endDate.HasValue || s.OrderDate.Date <= endDate.Value.Date)
                .GroupBy(s => new { s.Branch_Code, s.OrderDate.Date })
                .Select(g => new
                {
                    BranchCode = g.Key.Branch_Code,
                    Date = g.Key.Date,
                    NetSale = g.Sum(x => (decimal?)x.NetSale) ?? 0,
                    NetSaleEatIn = g.Sum(x => (decimal?)x.NetSaleEatIn) ?? 0,
                    BranchArea = g.Max(x => x.Branch_Area) ?? string.Empty,
                    BranchStaff = g.Max(x => (int?)x.Branch_Staff) ?? 0,
                    LossSell = g.Sum(x =>
                        (decimal?)x.LostSalePerBill * (decimal?)(x.TotalBill ?? 0)
                    ) ?? 0,
                })
                .ToListAsync();

            // =========================
            // 3. Join and Calculate รายวัน
            // =========================
            var details = lossGrouped
                .GroupJoin(
                    saleGrouped,
                    l => new { l.OutletName, l.Date },
                    s => new { OutletName = s.BranchCode, s.Date },
                    (l, sGroup) => new { l, s = sGroup.FirstOrDefault() }
                )
                .Select(x =>
                {
                    decimal lsProd = x.l.LostProd;
                    decimal lsSeating = x.l.LostSeat;
                    decimal lsPerBill = x.s?.LossSell ?? 0;
                    decimal lsOpp = lsProd + lsSeating + lsPerBill;

                    return new LossSellSummaryDto
                    {
                        OutletId = x.l.OutletId,
                        OutletName = x.l.OutletName,
                        Date = x.l.Date,
                        NetSaleEatIn = x.s?.NetSaleEatIn ?? 0,
                        BranchArea = x.s?.BranchArea ?? string.Empty,
                        BranchStaff = x.s?.BranchStaff ?? 0,
                        LSProd = lsProd,
                        LSSeating = lsSeating,
                        LSPerBill = lsPerBill,
                        LSOpp = lsOpp,
                    };
                })
                .ToList();

            // =========================
            // 4. สร้าง sum ถ้า isSum = true
            // =========================
            List<LossSellSummaryDto>? sum = null;
            if (isSum)
            {
                sum = details
                    .GroupBy(x => new { x.OutletId, x.OutletName })
                    .Select(g => new LossSellSummaryDto
                    {
                        OutletId = g.Key.OutletId,
                        OutletName = g.Key.OutletName,
                        NetSaleEatIn = g.Sum(x => x.NetSaleEatIn),
                        BranchArea = g.Max(x => x.BranchArea),
                        BranchStaff = g.Max(x => x.BranchStaff),
                        LSProd = g.Sum(x => x.LSProd),
                        LSSeating = g.Sum(x => x.LSSeating),
                        LSPerBill = g.Sum(x => x.LSPerBill),
                        LSOpp = g.Sum(x => x.LSOpp),
                        Date = default, // ไม่แสดงวันที่สำหรับ sum
                    })
                    .ToList();
            }

            // =========================
            // 5. Return เป็น IEnumerable<LossSellSummaryResponseDto>
            // =========================
            var response = new LossSellSummaryResponseDto
            {
                IsSum = isSum,
                Sum = sum,
                Details = details,
            };

            return new List<LossSellSummaryResponseDto> { response };
        }

        // ============================
        // Detail per Outlet
        // ============================
        // public async Task<LossSellDetailDto?> GetDetailByOutletAsync(
        //     string outletNameFilter,
        //     DateTime? startDate,
        //     DateTime? endDate
        // )
        // {
        //     if (string.IsNullOrEmpty(outletNameFilter))
        //         return null;

        //     var query = _context
        //         .uvw_El_Calculation.AsNoTracking()
        //         .Where(x => x.OutletName == outletNameFilter)
        //         .Where(x =>
        //             !startDate.HasValue
        //             || (x.CalDate.HasValue && x.CalDate.Value.Date >= startDate.Value.Date)
        //         )
        //         .Where(x =>
        //             !endDate.HasValue
        //             || (x.CalDate.HasValue && x.CalDate.Value.Date <= endDate.Value.Date)
        //         );

        //     var data = await query.ToListAsync();
        //     if (!data.Any())
        //         return null;

        //     var runIds = data.Select(d => d.RunId).Distinct().ToList();
        //     var notes = await _context
        //         .El_Calculation_Notes.AsNoTracking()
        //         .Where(n => runIds.Contains(n.RunId))
        //         .ToListAsync();

        //     var noteCountMap = notes.GroupBy(n => n.RunId).ToDictionary(g => g.Key, g => g.Count());

        //     DateTime? rangeStart = startDate?.Date;
        //     DateTime? rangeEndExclusive = endDate?.Date.AddDays(1);

        //     var rptSummary = await _context
        //         .Rpt_EL_CalculateSale.AsNoTracking()
        //         .Where(r => r.Branch_Code == outletNameFilter)
        //         .Where(r => !rangeStart.HasValue || r.OrderDate >= rangeStart.Value)
        //         .Where(r => !rangeEndExclusive.HasValue || r.OrderDate < rangeEndExclusive.Value)
        //         .ToListAsync();

        //     var isSingleDay =
        //         startDate.HasValue
        //         && endDate.HasValue
        //         && startDate.Value.Date == endDate.Value.Date;

        //     var hourlyDetail = data.GroupBy(x =>
        //             string.IsNullOrWhiteSpace(x.CalHour) ? "0" : x.CalHour.Trim()
        //         )
        //         .Select(g =>
        //         {
        //             var calHourStr = g.Key;
        //             var digitPart = new string(calHourStr.TakeWhile(char.IsDigit).ToArray());
        //             int calHourInt = -1;
        //             if (!string.IsNullOrEmpty(digitPart))
        //                 int.TryParse(digitPart, out calHourInt);

        //             var summaryForHour =
        //                 calHourInt >= 0
        //                     ? rptSummary.FirstOrDefault(r => r.OrderHour == calHourInt)
        //                     : null;

        //             var totalLostItem = g.Sum(x => x.Lost_Item ?? 0m);
        //             var totalLostTable = g.Sum(x => x.Lost_Table ?? 0m);
        //             var avgPrice = g.Average(x => x.Avg_Price ?? 0m);
        //             var avgPerBill = g.Average(x => x.Bill_Total ?? 0m);

        //             return new HourlyTrendDto
        //             {
        //                 Hour = calHourStr,
        //                 RunId = isSingleDay ? g.First().RunId : Guid.Empty,
        //                 LostItem = totalLostItem,
        //                 LostProductivity = Math.Round(g.Average(x => x.Lost_Productivity ?? 0m), 2),
        //                 LostTime = g.Sum(x => x.Lost_Time ?? 0m),
        //                 LostSeating = g.Sum(x => x.Lost_Seating ?? 0m),
        //                 LostTable = totalLostTable,
        //                 BillTotal = g.Sum(x => x.Bill_Total ?? 0m),
        //                 // TotalOptimalBillPrice = g.Sum(x => x.Total_Optimal_Bill_Price ?? 0m),
        //                 TotalOptimalBillPrice =
        //                     summaryForHour != null
        //                         ? (summaryForHour.Saleprebill ?? 0m)
        //                             * (summaryForHour.EatingBill ?? 0m)
        //                         : 0,
        //                 LostPerBill =
        //                     summaryForHour != null
        //                         ? (summaryForHour.LostSalePerBill ?? 0)
        //                             * (summaryForHour.TotalBill ?? 0)
        //                         : 0,
        //                 AvgBillTotal = g.Average(x => x.Avg_BillTotal ?? 0m),
        //                 AvgPrice = avgPrice,
        //                 ProductionTime = g.Sum(x => x.Production_Time ?? 0m),
        //                 OptimalMinute = g.Sum(x => x.Optimal_Minute ?? 0m),
        //                 AvgSeatingTime = g.Average(x => x.Avg_SeatingTime ?? 0m),
        //                 GapBetweenQueue = g.Average(x => x.Gap_Between_Queue ?? 0m),
        //                 NoteCount =
        //                     isSingleDay && g.Any() && noteCountMap.ContainsKey(g.First().RunId)
        //                         ? noteCountMap[g.First().RunId]
        //                         : 0,
        //                 LSProd = Math.Round(totalLostItem * avgPrice, 2),
        //                 // LSSeating = Math.Round(totalLostTable * avgPerBill, 2),
        //                 LSSeating = Math.Round(g.Sum(x => x.Lost_Seating ?? 0m), 2),

        //                 // LSPerBill =
        //                 //     summaryForHour != null
        //                 //         ? (summaryForHour.LostSalePerBill ?? 0)
        //                 //             * (summaryForHour.TotalBill ?? 0)
        //                 //         : 0,
        //                 LSPerBill =
        //                     summaryForHour != null
        //                         ? Math.Max(
        //                             0,
        //                             (
        //                                 (summaryForHour.Branch_Bill ?? 0m)
        //                                 - (summaryForHour.Saleprebill ?? 0m)
        //                             ) * (summaryForHour.EatingBill ?? 0m)
        //                         )
        //                         : 0m,

        //                 LSMedian =
        //                     summaryForHour != null
        //                         ? Math.Max(
        //                             0,
        //                             (
        //                                 (decimal)(summaryForHour.Median ?? 0d)
        //                                 - (summaryForHour.Saleprebill ?? 0m)
        //                             ) * (summaryForHour.EatingBill ?? 0m)
        //                         )
        //                         : 0m,

        //                 Median = summaryForHour?.Median,
        //                 AVGPerBill = summaryForHour?.Saleprebill,
        //                 ProdTimeAVG = g.Sum(x => x.ProdTimeAVG ?? 0),
        //                 Branch_Staff = summaryForHour?.Branch_Staff ?? 0,
        //                 DineInPercent = summaryForHour?.EatInPercent,
        //                 OtherPercent = summaryForHour?.OtherPercent,
        //                 BranchBill = summaryForHour?.Branch_Bill ?? 0,
        //                 LossSell =
        //                     summaryForHour != null
        //                         ? Math.Max(
        //                             0,
        //                             (
        //                                 (decimal)(summaryForHour.Branch_Bill ?? 0m)
        //                                 - (decimal)(summaryForHour.Saleprebill ?? 0m)
        //                             ) * (summaryForHour.EatingBill ?? 0m)
        //                         )
        //                         : 0m,

        //                 rptElCalculateSale = summaryForHour,
        //             };
        //         })
        //         .OrderBy(x =>
        //         {
        //             if (
        //                 int.TryParse(
        //                     new string(x.Hour.TakeWhile(char.IsDigit).ToArray()),
        //                     out var h
        //                 )
        //             )
        //                 return h;
        //             return int.MaxValue;
        //         })
        //         .ToList();

        //     return new LossSellDetailDto
        //     {
        //         OutletName = outletNameFilter,
        //         HourlyTrend = hourlyDetail,
        //     };
        // }
        public async Task<LossSellDetailDto?> GetDetailByOutletAsync(
            string outletName,
            DateTime? startDate,
            DateTime? endDate
        )
        {
            if (string.IsNullOrEmpty(outletName))
                return null;

            // 1. ดึงข้อมูล Loss
            var lossData = await _context
                .uvw_El_Calculation.AsNoTracking()
                .Where(x => x.OutletName == outletName)
                .Where(x => !startDate.HasValue || x.CalDate >= startDate.Value.Date)
                .Where(x => !endDate.HasValue || x.CalDate <= endDate.Value.Date)
                .ToListAsync();

            if (!lossData.Any())
                return null;

            var runIds = lossData.Select(d => d.RunId).Distinct().ToList();

            // 2. ดึง Note Count
            var noteCountMap = await _context
                .El_Calculation_Notes.AsNoTracking()
                .Where(n => runIds.Contains(n.RunId))
                .GroupBy(n => n.RunId)
                .ToDictionaryAsync(g => g.Key, g => g.Count());

            // 3. ดึงข้อมูลสรุปขาย
            var rangeEndExclusive = endDate?.Date.AddDays(1);
            var salesSummary = await _context
                .Rpt_EL_CalculateSale.AsNoTracking()
                .Where(r => r.Branch_Code == outletName)
                .Where(r => !startDate.HasValue || r.OrderDate >= startDate.Value)
                .Where(r => !rangeEndExclusive.HasValue || r.OrderDate < rangeEndExclusive.Value)
                .ToListAsync();

            var isSingleDay =
                startDate.HasValue
                && endDate.HasValue
                && startDate.Value.Date == endDate.Value.Date;

            // 4. คำนวณ Hourly Trend แบบ Flat
            var hourlyTrend = lossData
                .GroupBy(x => string.IsNullOrWhiteSpace(x.CalHour) ? "0" : x.CalHour.Trim())
                .Select(g =>
                {
                    int.TryParse(
                        new string(g.Key.TakeWhile(char.IsDigit).ToArray()),
                        out int hourInt
                    );

                    var sale = salesSummary.FirstOrDefault(s => s.OrderHour == hourInt);

                    // ✅ Median เป็น double → แปลงเป็น decimal ก่อนใช้
                    decimal medianValue = (decimal)(sale?.Median ?? 0.0);

                    // ✅ LSMedian = (Median - AvgPerBill) * TotalBill
                    decimal lsMedian =
                        sale != null
                            ? Math.Round(
                                ((medianValue) - (sale.Saleprebill ?? 0m)) * (sale.TotalBill ?? 0m),
                                2
                            )
                            : 0m;

                    // ✅ Loss Components
                    decimal lsProd = Math.Round(
                        g.Sum(x => x.Lost_Item ?? 0m) * g.Average(x => x.Avg_Price ?? 0m),
                        2
                    );
                    decimal lsSeating = Math.Round(g.Sum(x => x.Lost_Seating ?? 0m), 2);
                    decimal lsPerBill =
                        sale != null
                            ? Math.Max(
                                0,
                                ((sale.Branch_Bill ?? 0m) - (sale.Saleprebill ?? 0m))
                                    * (sale.TotalBill ?? 0m)
                            )
                            : 0m;

                    decimal lsOpp = lsProd + lsSeating + lsPerBill;

                    // ✅ ProdTimeAVG จาก uvw_El_Calculation (เฉลี่ยต่อชั่วโมง)
                    decimal prodTimeAVG = Math.Round(g.Average(x => x.ProdTimeAVG ?? 0m), 2);
                    decimal Avg_SeatingTime = Math.Round(
                        g.Average(x => x.Avg_SeatingTime ?? 0m),
                        2
                    );

                    // ✅ Note Count
                    int noteCount =
                        isSingleDay && noteCountMap.ContainsKey(g.First().RunId)
                            ? noteCountMap[g.First().RunId]
                            : 0;

                    return new HourlyTrendDto
                    {
                        Hour = g.Key,
                        RunId = isSingleDay ? g.First().RunId : Guid.Empty,

                        // Loss
                        LSProd = lsProd,
                        LSSeating = lsSeating,
                        LSPerBill = lsPerBill,
                        LSMedian = lsMedian,
                        LSOPP = lsOpp,
                        TotalBill = sale.TotalBill,
                        // Summary & Extra
                        NoteCount = noteCount,
                        ProdTimeAVG = prodTimeAVG,
                        Avg_SeatingTime = Avg_SeatingTime,
                        Branch_Bill = sale?.Branch_Bill ?? 0,
                        AVGPerBill = sale?.Saleprebill,
                        NetSaleEatIn = (decimal)(sale?.NetSaleEatIn ?? 0),
                        Median = (double?)medianValue,
                        CashierSpeedSec = sale?.CashierSpeedSec ?? "0",
                        Branch_Area = sale?.Branch_Area ?? "0",
                        Branch_Staff = sale?.Branch_Staff ?? 0,
                        DineInPercent = (decimal?)(sale?.EatInPercent ?? 0),
                        OtherPercent = (decimal?)(sale?.OtherPercent ?? 0),
                    };
                })
                .OrderBy(x =>
                    int.TryParse(new string(x.Hour.TakeWhile(char.IsDigit).ToArray()), out var h)
                        ? h
                        : 0
                )
                .ToList();

            // 5. รวมสรุปผล
            return new LossSellDetailDto
            {
                OutletName = outletName,
                NetDineIn = (decimal)salesSummary.Sum(s => s.NetSaleEatIn ?? 0),
                LossOpp = hourlyTrend.Sum(h => h.LSOPP),
                HourlyTrend = hourlyTrend,
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
                Status = dto.Status,
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
                    Status = x.note.Status,
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
                    Status = x.note.Status,
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

        public async Task<List<BillDetailDto>> GetBillsByOutletAndHourAsync(
            string outletName,
            DateTime? startDate,
            DateTime? endDate,
            string? hour
        )
        {
            if (string.IsNullOrEmpty(outletName))
                return new List<BillDetailDto>();

            var query = _context
                .rpt_El_ProductionTime.AsNoTracking()
                .Where(x => x.Branch_Code == outletName);

            if (startDate.HasValue)
                query = query.Where(x => x.BillDate >= startDate.Value.Date);

            if (endDate.HasValue)
                query = query.Where(x => x.BillDate <= endDate.Value.Date);

            if (!string.IsNullOrEmpty(hour))
                query = query.Where(x => x.BillHour == hour);

            var result = await query
                .OrderBy(x => x.BillDate)
                .ThenBy(x => x.BillNo)
                .Select(x => new BillDetailDto
                {
                    BillDate = x.BillDate,
                    Branch_Code = x.Branch_Code,
                    BillHour = x.BillHour,
                    BillNo = x.BillNo,
                    BillTotal = x.BillTotal,
                    Category = x.Category,
                    ProductName = x.ProductName,
                    Price = x.Price,
                    ProductionTime = x.ProductionTime,
                    OptimalTime = x.OptimalTime,
                    AICategory = x.AICatogory,
                    AIMatchTime = x.AIMatchTime,
                })
                .ToListAsync();

            return result;
        }

        public async Task<List<SeatingLostDto>> GetSeatingLostByOutletAndHourAsync(
            string outletName,
            DateTime? startDate,
            DateTime? endDate,
            string? hour
        )
        {
            if (string.IsNullOrEmpty(outletName))
                return new List<SeatingLostDto>();

            var query = _context
                .Rpt_EL_SeatingLost.AsNoTracking()
                .Where(x => x.BranchCode == outletName);

            if (startDate.HasValue)
                query = query.Where(x => x.SeatDate >= startDate.Value.Date);

            if (endDate.HasValue)
                query = query.Where(x => x.SeatDate <= endDate.Value.Date);

            if (!string.IsNullOrEmpty(hour))
                query = query.Where(x => x.SeatHour == hour);

            var result = await query
                .OrderBy(x => x.SeatDate)
                .ThenBy(x => x.Table_Name)
                .Select(x => new SeatingLostDto
                {
                    BranchCode = x.BranchCode,
                    Table_Name = x.Table_Name,
                    SeatDate = x.SeatDate,
                    SeatHour = x.SeatHour,
                    SeatingTime = x.SeatingTime,
                    Minute_Lost = x.Minute_Lost,
                    Start_Time = x.Start_Time,
                    End_Time = x.End_Time,
                    Matched_Queue_Time = x.Matched_Queue_Time,
                    Table_Image = x.Table_Image,
                })
                .ToListAsync();

            return result;
        }
    }
}
