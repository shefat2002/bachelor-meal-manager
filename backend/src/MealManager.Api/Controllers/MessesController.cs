using MealManager.Application.Common;
using MealManager.Application.Features.Messes;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MealManager.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/messes")]
public sealed class MessesController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<MessDto>> Create([FromBody] CreateMessRequest request)
    {
        var result = await sender.Send(new CreateMessCommand(request.Name));
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResult<MessDto>>> List([FromQuery] int? page, [FromQuery] int? pageSize) =>
        Ok(await sender.Send(new GetMessesQuery(page, pageSize)));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MessDto>> GetById(Guid id) =>
        Ok(await sender.Send(new GetMessQuery(id)));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<MessDto>> Update(Guid id, [FromBody] UpdateMessRequest request) =>
        Ok(await sender.Send(new UpdateMessCommand(id, request.Name)));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await sender.Send(new DeleteMessCommand(id));
        return NoContent();
    }
}
