using MealManager.Application.Common;
using MealManager.Application.Features.Utilities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MealManager.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/utility-types")]
public sealed class UtilityTypesController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<UtilityTypeDto>> Create([FromBody] CreateUtilityTypeRequest request) =>
        CreatedAtAction(nameof(List), new { }, await sender.Send(
            new CreateUtilityTypeCommand(request.Name, request.Description)));

    [HttpGet]
    public async Task<ActionResult<PaginatedResult<UtilityTypeDto>>> List(
        [FromQuery] int? page, [FromQuery] int? pageSize) =>
        Ok(await sender.Send(new GetUtilityTypesQuery(page, pageSize)));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UtilityTypeDto>> Update(Guid id, [FromBody] UpdateUtilityTypeRequest request) =>
        Ok(await sender.Send(new UpdateUtilityTypeCommand(id, request.Name, request.Description, request.IsActive)));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await sender.Send(new DeleteUtilityTypeCommand(id));
        return NoContent();
    }
}
