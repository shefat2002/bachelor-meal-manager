using FluentValidation;
using MealManager.Application.Abstractions;
using MealManager.Application.Domain;
using MealManager.Application.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MealManager.Application.Features.Messes;

public record UpdateMessCommand(Guid Id, string Name) : IRequest<MessDto>;

public sealed class UpdateMessCommandValidator : AbstractValidator<UpdateMessCommand>
{
    public UpdateMessCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}

public sealed class UpdateMessCommandHandler(IApplicationContext db, ICurrentUser currentUser)
    : IRequestHandler<UpdateMessCommand, MessDto>
{
    public async Task<MessDto> Handle(UpdateMessCommand request, CancellationToken cancellationToken)
    {
        var mess = await db.Messes.FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Mess not found.");

        var userId = currentUser.UserId!.Value;
        var membership = await db.MembersMesses.IgnoreQueryFilters()
            .FirstOrDefaultAsync(mm => mm.MessId == mess.Id && mm.MemberId == userId, cancellationToken);

        var isManager = membership is { Role: MessRole.MealManager, Status: MemberStatus.Active };
        if (!isManager && !currentUser.IsSuperAdmin)
        {
            throw new ForbiddenException("Only the mess manager can update the mess.");
        }

        mess.Name = request.Name.Trim();
        await db.SaveChangesAsync(cancellationToken);

        var activeMembers = await db.MembersMesses.AsNoTracking().IgnoreQueryFilters()
            .CountAsync(mm => mm.MessId == mess.Id && mm.Status == MemberStatus.Active, cancellationToken);

        return new MessDto(mess.Id, mess.Name, mess.CreatedAt, membership?.Role.ToString() ?? "superAdmin", activeMembers);
    }
}
