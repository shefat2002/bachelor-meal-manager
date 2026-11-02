using MealManager.Application.Common;
using MealManager.Application.Features.Members;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MealManager.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/members")]
public sealed class MembersController(ISender sender) : ControllerBase
{
    [HttpPost("invite")]
    public async Task<ActionResult<InviteResult>> Invite() =>
        Ok(await sender.Send(new CreateInviteCommand()));

    [HttpPost("join")]
    public async Task<ActionResult<JoinedMessResult>> Join([FromBody] JoinMessCommand command)
    {
        var result = await sender.Send(command);
        return CreatedAtAction(nameof(List), new { }, result);
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResult<MessMemberDto>>> List([FromQuery] int? page, [FromQuery] int? pageSize) =>
        Ok(await sender.Send(new GetMembersQuery(page, pageSize)));

    [HttpPut("{memberId:guid}/approve")]
    public async Task<IActionResult> Approve(Guid memberId)
    {
        await sender.Send(new ApproveMemberCommand(memberId));
        return NoContent();
    }

    [HttpPut("{memberId:guid}/reject")]
    public async Task<IActionResult> Reject(Guid memberId)
    {
        await sender.Send(new RejectMemberCommand(memberId));
        return NoContent();
    }

    [HttpPut("{memberId:guid}/role")]
    public async Task<IActionResult> UpdateRole(Guid memberId, [FromBody] UpdateMemberRoleRequest request)
    {
        await sender.Send(new UpdateMemberRoleCommand(memberId, request.Role));
        return NoContent();
    }

    [HttpDelete("{memberId:guid}")]
    public async Task<IActionResult> Remove(Guid memberId)
    {
        await sender.Send(new RemoveMemberCommand(memberId));
        return NoContent();
    }
}
