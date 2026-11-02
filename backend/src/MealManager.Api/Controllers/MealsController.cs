using MealManager.Application.Common;
using MealManager.Application.Features.Meals;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MealManager.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/meals")]
public sealed class MealsController(ISender sender) : ControllerBase
{
    /// <summary>Upsert (not a pure create) of a date's entries → 200 with the resulting rows.</summary>
    [HttpPost]
    public async Task<ActionResult<IReadOnlyList<DailyMealDto>>> Submit([FromBody] SubmitDailyMealsRequest request) =>
        Ok(await sender.Send(new SubmitDailyMealsCommand(request.Date, request.Entries)));

    [HttpGet]
    public async Task<ActionResult<PaginatedResult<DailyMealDto>>> List(
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] Guid? memberId,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] string? sortBy,
        [FromQuery] string? sortDir) =>
        Ok(await sender.Send(new GetMealsQuery(page, pageSize, memberId, from, to, sortBy, sortDir)));
}
