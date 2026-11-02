using MealManager.Application.Features.Summary;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MealManager.Api.Controllers;

/// <summary>Financial summary (§5 equations) + month lock (P3-BE-3/5).</summary>
[ApiController]
[Authorize]
[Route("api/summary/messes")]
public sealed class SummaryController(ISender sender) : ControllerBase
{
    [HttpGet("{messId:guid}")]
    public async Task<ActionResult<MessSummaryDto>> Get(
        Guid messId,
        [FromQuery] int? year,
        [FromQuery] int? month)
    {
        var now = DateTime.UtcNow;
        return Ok(await sender.Send(new GetMessSummaryQuery(
            messId, year ?? now.Year, month ?? now.Month)));
    }

    [HttpPut("{messId:guid}/month-lock")]
    public async Task<IActionResult> SetMonthLock(Guid messId, [FromBody] MonthLockRequest request)
    {
        await sender.Send(new SetMonthLockCommand(messId, request.Year, request.Month, request.Locked));
        return NoContent();
    }
}
