using MealManager.Application.Common;
using MealManager.Application.Features.Deposits;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MealManager.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/deposits")]
public sealed class DepositsController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<DepositDto>> Create([FromBody] CreateDepositRequest request) =>
        CreatedAtAction(nameof(List), new { }, await sender.Send(
            new CreateDepositCommand(request.MemberId, request.Amount, request.Date, request.Note)));

    [HttpGet]
    public async Task<ActionResult<PaginatedResult<DepositDto>>> List(
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] Guid? memberId,
        [FromQuery] string? status,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] string? sortBy,
        [FromQuery] string? sortDir) =>
        Ok(await sender.Send(new GetDepositsQuery(page, pageSize, memberId, status, from, to, sortBy, sortDir)));

    [HttpPut("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id)
    {
        await sender.Send(new ApproveDepositCommand(id));
        return NoContent();
    }

    [HttpPut("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id)
    {
        await sender.Send(new RejectDepositCommand(id));
        return NoContent();
    }
}
