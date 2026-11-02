using MealManager.Application.Abstractions;
using MealManager.Application.Domain;
using MealManager.Application.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MealManager.Application.Features.Messes;

/// <summary>Single mess for the MessProvider. Non-members get 404 (never a cross-tenant leak).</summary>
public record GetMessQuery(Guid Id) : IRequest<MessDto>;

public sealed class GetMessQueryHandler(IApplicationContext db, ICurrentUser currentUser)
    : IRequestHandler<GetMessQuery, MessDto>
{
    public async Task<MessDto> Handle(GetMessQuery request, CancellationToken cancellationToken)
    {
        var mess = await db.Messes.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Mess not found.");

        var userId = currentUser.UserId!.Value;
        var membership = await db.MembersMesses.AsNoTracking().IgnoreQueryFilters()
            .FirstOrDefaultAsync(mm => mm.MessId == mess.Id && mm.MemberId == userId, cancellationToken);

        if (membership is null && !currentUser.IsSuperAdmin)
        {
            throw new NotFoundException("Mess not found.");
        }

        var activeMembers = await db.MembersMesses.AsNoTracking().IgnoreQueryFilters()
            .CountAsync(mm => mm.MessId == mess.Id && mm.Status == MemberStatus.Active, cancellationToken);

        var role = membership?.Role.ToString() ?? "superAdmin";
        return new MessDto(mess.Id, mess.Name, mess.CreatedAt, role, activeMembers);
    }
}
