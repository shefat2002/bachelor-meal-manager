using FluentValidation;
using MealManager.Application.Abstractions;
using MealManager.Application.Domain;
using MealManager.Application.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MealManager.Application.Features.Members;

public record UpdateMemberRoleCommand(Guid MemberId, string Role) : ITenantRequest, IRequest<Unit>;

public sealed class UpdateMemberRoleCommandValidator : AbstractValidator<UpdateMemberRoleCommand>
{
    public UpdateMemberRoleCommandValidator()
    {
        RuleFor(x => x.Role).NotEmpty().Must(r => Enum.TryParse<MessRole>(r, true, out _))
            .WithMessage("Role must be 'Member' or 'MealManager'.");
    }
}

public sealed class UpdateMemberRoleCommandHandler(IApplicationContext db, ITenantContext tenant)
    : IRequestHandler<UpdateMemberRoleCommand, Unit>
{
    public async Task<Unit> Handle(UpdateMemberRoleCommand request, CancellationToken cancellationToken)
    {
        ApproveMemberCommandHandler.RequireManager(tenant);
        var newRole = Enum.Parse<MessRole>(request.Role, ignoreCase: true);

        var membership = await db.MembersMesses.IgnoreQueryFilters()
            .FirstOrDefaultAsync(mm => mm.MessId == tenant.MessId!.Value && mm.MemberId == request.MemberId, cancellationToken)
            ?? throw new NotFoundException("Membership not found.");

        if (membership.Status != MemberStatus.Active)
        {
            throw new ConflictException("Roles can only be changed for active members.");
        }

        await GuardLastManagerAsync(db, tenant.MessId.Value, membership, newRole, cancellationToken);

        membership.Role = newRole;
        await db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }

    internal static async Task GuardLastManagerAsync(
        IApplicationContext db, Guid messId, MemberMess membership, MessRole newRole, CancellationToken ct)
    {
        if (membership.Role == MessRole.MealManager && newRole != MessRole.MealManager)
        {
            var managerCount = await db.MembersMesses.AsNoTracking().IgnoreQueryFilters()
                .CountAsync(mm => mm.MessId == messId && mm.Status == MemberStatus.Active && mm.Role == MessRole.MealManager, ct);
            if (managerCount <= 1)
            {
                throw new ConflictException("A mess must keep at least one Meal Manager.");
            }
        }
    }
}
