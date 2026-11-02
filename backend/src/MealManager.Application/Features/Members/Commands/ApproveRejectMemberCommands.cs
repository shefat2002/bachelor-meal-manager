using MealManager.Application.Abstractions;
using MealManager.Application.Domain;
using MealManager.Application.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MealManager.Application.Features.Members;

public record ApproveMemberCommand(Guid MemberId) : ITenantRequest, IRequest<Unit>;

public record RejectMemberCommand(Guid MemberId) : ITenantRequest, IRequest<Unit>;

public sealed class ApproveMemberCommandHandler(IApplicationContext db, ITenantContext tenant)
    : IRequestHandler<ApproveMemberCommand, Unit>
{
    public async Task<Unit> Handle(ApproveMemberCommand request, CancellationToken cancellationToken)
    {
        await SetStatusAsync(db, tenant, request.MemberId, MemberStatus.Active, "approve", cancellationToken);
        return Unit.Value;
    }

    internal static async Task<MemberMess> SetStatusAsync(
        IApplicationContext db, ITenantContext tenant, Guid memberId, MemberStatus target, string action, CancellationToken ct)
    {
        RequireManager(tenant);

        var membership = await db.MembersMesses.IgnoreQueryFilters()
            .FirstOrDefaultAsync(mm => mm.MessId == tenant.MessId!.Value && mm.MemberId == memberId, ct)
            ?? throw new NotFoundException("Membership not found.");

        if (membership.Status != MemberStatus.Pending)
        {
            throw new ConflictException($"Only pending join requests can be {action}d.");
        }

        membership.Status = target;
        if (target == MemberStatus.Active)
        {
            membership.JoinedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);
        return membership;
    }

    internal static void RequireManager(ITenantContext tenant)
    {
        if (tenant.Role != MessRole.MealManager)
        {
            throw new ForbiddenException("Only mess managers can manage join requests.");
        }
    }
}

public sealed class RejectMemberCommandHandler(IApplicationContext db, ITenantContext tenant)
    : IRequestHandler<RejectMemberCommand, Unit>
{
    public async Task<Unit> Handle(RejectMemberCommand request, CancellationToken cancellationToken)
    {
        await ApproveMemberCommandHandler.SetStatusAsync(db, tenant, request.MemberId, MemberStatus.Rejected, "reject", cancellationToken);
        return Unit.Value;
    }
}
