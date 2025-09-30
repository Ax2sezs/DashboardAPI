
using DashboardAPI.Models;
using DashboardAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DashboardAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SummaryController : ControllerBase
    {
        private readonly ISummaryService _summaryService;

        public SummaryController(ISummaryService summaryService)
        {
            _summaryService = summaryService;
        }

        // GET: api/Summary?startDate=2025-06-01&endDate=2025-06-30
        [HttpGet]
        public async Task<ActionResult<IEnumerable<LossSellSummaryDto>>> GetSummary(string? outletNameFilter, DateTime? startDate, DateTime? endDate)
        {
            var result = await _summaryService.GetSummaryAsync(User, outletNameFilter, startDate, endDate);
            return Ok(result);
        }
        [HttpGet("summary/hourly")]
        public async Task<IActionResult> GetHourlySummary(
            string? outletName,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            var result = await _summaryService.GetHourlySummaryAsync(User, outletName, startDate, endDate);
            return Ok(new { hourlyTrend = result });
        }

        // GET: api/Summary/{outletId}?startDate=2025-06-01&endDate=2025-06-30
        [HttpGet("{outletNameFilter}")]
        public async Task<ActionResult<LossSellDetailDto>> GetDetail(string outletNameFilter, DateTime? startDate, DateTime? endDate)
        {
            var result = await _summaryService.GetDetailByOutletAsync(outletNameFilter, startDate, endDate);
            return Ok(result);
        }

        [HttpPost("add-note")]
        public async Task<IActionResult> AddNote([FromBody] AddNoteDto dto)
        {
            if (dto == null || dto.RunId == Guid.Empty || string.IsNullOrWhiteSpace(dto.Note))
                return BadRequest("RunId และ Note ต้องระบุ");

            var success = await _summaryService.AddNoteAsync(dto);

            if (!success)
                return NotFound("ไม่พบ RunId สำหรับบันทึก Note");

            return Ok(new { message = "บันทึก Note สำเร็จ" });
        }
        [HttpGet("notes-feed")]
        public async Task<IActionResult> GetNotesFeed(string outletNameFilter, DateTime? calDate)
        {
            var feed = await _summaryService.GetNotesFeedAsync(outletNameFilter, calDate);
            return Ok(feed);
        }
        [HttpGet("notes-id/{runId}")]
        public async Task<IActionResult> GetNotesByRunId([FromRoute] Guid runId)
        {
            var notes = await _summaryService.GetNotesByRunIdAsync(runId);
            return Ok(notes);
        }


    }
}
