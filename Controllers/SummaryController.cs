using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DashboardAPI.Models;
using DashboardAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
        [Authorize]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<LossSellSummaryResponseDto>>> GetSummary(
            string? outletNameFilter,
            DateTime? startDate,
            DateTime? endDate,
            bool isSum
        )
        {
            var result = await _summaryService.GetSummaryAsync(
                User,
                outletNameFilter,
                startDate,
                endDate,
                isSum
            );
            return Ok(result);
        }

        [Authorize]
        [HttpGet("summary/hourly")]
        public async Task<IActionResult> GetHourlySummary(
            string? outletName,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate
        )
        {
            var result = await _summaryService.GetHourlySummaryAsync(
                User,
                outletName,
                startDate,
                endDate
            );
            return Ok(new { hourlyTrend = result });
        }

        [Authorize]
        [HttpGet("{outletNameFilter}")]
        public async Task<ActionResult<LossSellDetailDto>> GetDetail(
            string outletNameFilter,
            DateTime? startDate,
            DateTime? endDate
        )
        {
            var result = await _summaryService.GetDetailByOutletAsync(
                outletNameFilter,
                startDate,
                endDate
            );
            return Ok(result);
        }

        [Authorize]
        [HttpGet("get-bill-details")]
        public async Task<IActionResult> GetBillDetails(
            [FromQuery] string outletName,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] string? hour
        )
        {
            if (string.IsNullOrEmpty(outletName))
                return BadRequest("outletName is required");

            var result = await _summaryService.GetBillsByOutletAndHourAsync(
                outletName,
                startDate,
                endDate,
                hour
            );

            if (result == null || !result.Any())
                return NotFound("No bill data found for the given parameters");

            return Ok(result);
        }

        [Authorize]
        [HttpGet("get-seatinglost-details")]
        public async Task<IActionResult> GetSeatingLostDetails(
            [FromQuery] string outletName,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] string? hour
        )
        {
            if (string.IsNullOrEmpty(outletName))
                return BadRequest("outletName is required");

            var result = await _summaryService.GetSeatingLostByOutletAndHourAsync(
                outletName,
                startDate,
                endDate,
                hour
            );

            return Ok(result);
        }

        [Authorize]
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

        [Authorize]
        [HttpGet("notes-feed")]
        public async Task<IActionResult> GetNotesFeed(string outletNameFilter, DateTime? calDate)
        {
            var feed = await _summaryService.GetNotesFeedAsync(outletNameFilter, calDate);
            return Ok(feed);
        }

        [Authorize]
        [HttpGet("notes-id/{runId}")]
        public async Task<IActionResult> GetNotesByRunId([FromRoute] Guid runId)
        {
            var notes = await _summaryService.GetNotesByRunIdAsync(runId);
            return Ok(notes);
        }
    }
}
