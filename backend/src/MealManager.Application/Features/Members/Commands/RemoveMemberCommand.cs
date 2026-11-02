using MealManager.Application.Abstractions;
using MealManager.Application.Domain;
using MealManager.Application.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MealManager.Application.Features.Members;

/// <summary>Leave the mess (self) or remove a member (manager). Last-manager guard applies.</summary>
public record RemoveMemberCommand(Guid MemberId) : ITenantRequest, IRequest<Unit>;

public sealed class RemoveMemberCommandHandler(IApplicationContext db, ITenantContext tenant, ICurrentUser currentUser)
    : IRequestHandler<RemoveMemberCommand, Unit>
{
    public async Task<Unit> Handle(RemoveMemberCommand request, CancellationToken cancellationToken)
    {
        var isSelf = currentUser.UserId == request.MemberId;
        if (!isSelf && tenant.Role != MessRole.MealManager)
        {
            throw new ForbiddenException("Only mess managers can remove members.");
        }

        var membership = await db.MembersMesses.IgnoreQueryFilters()
            .FirstOrDefaultAsync(mm => mm.MessId == tenant.MessId!.Value && mm.MemberId == request.MemberId, cancellationToken)
            ?? throw new NotFoundException("Membership not found.");

        await UpdateMemberRoleCommandHandler.GuardLastManagerAsync(
            db, tenant.MessId.Value, membership, MessRole.Member, cancellationToken);

        membership.Status = MemberStatus.Left;
        await db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
