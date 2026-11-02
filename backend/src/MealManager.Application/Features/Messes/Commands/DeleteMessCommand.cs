using MealManager.Application.Abstractions;
using MealManager.Application.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MealManager.Application.Features.Messes;

public record DeleteMessCommand(Guid Id) : IRequest<Unit>;

public sealed class DeleteMessCommandHandler(IApplicationContext db, ICurrentUser currentUser)
    : IRequestHandler<DeleteMessCommand, Unit>
{
    public async Task<Unit> Handle(DeleteMessCommand request, CancellationToken cancellationToken)
    {
        var mess = await db.Messes.FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Mess not found.");

        if (!currentUser.IsSuperAdmin && mess.CreatedBy != currentUser.UserId)
        {
            throw new ForbiddenException("Only the mess creator or a Super Admin can delete a mess.");
        }

        var memberships = await db.MembersMesses.IgnoreQueryFilters()
            .Where(mm => mm.MessId == mess.Id).ToListAsync(cancellationToken);
        var invites = await db.InviteLinks.IgnoreQueryFilters()
            .Where(i => i.MessId == mess.Id).ToListAsync(cancellationToken);

        db.MembersMesses.RemoveRange(memberships);
        db.InviteLinks.RemoveRange(invites);
        db.Messes.Remove(mess);
        await db.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
