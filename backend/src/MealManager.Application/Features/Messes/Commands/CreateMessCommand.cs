using FluentValidation;
using MealManager.Application.Abstractions;
using MealManager.Application.Domain;
using MediatR;

namespace MealManager.Application.Features.Messes;

public record CreateMessCommand(string Name) : IRequest<MessDto>;

public sealed class CreateMessCommandValidator : AbstractValidator<CreateMessCommand>
{
    public CreateMessCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}

public sealed class CreateMessCommandHandler(IApplicationContext db, ICurrentUser currentUser)
    : IRequestHandler<CreateMessCommand, MessDto>
{
    public async Task<MessDto> Handle(CreateMessCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId!.Value;
        var now = DateTime.UtcNow;
        var mess = new Mess { Id = Guid.NewGuid(), Name = request.Name.Trim(), CreatedBy = userId, CreatedAt = now };

        // Creator auto-joins as Meal Manager — docs/BACKEND_PROJECT_PLAN.md §4.
        mess.Members.Add(new MemberMess
        {
            MessId = mess.Id,
            MemberId = userId,
            Role = MessRole.MealManager,
            Status = MemberStatus.Active,
            JoinedAt = now,
        });

        db.Messes.Add(mess);
        await db.SaveChangesAsync(cancellationToken);

        return new MessDto(mess.Id, mess.Name, mess.CreatedAt, MessRole.MealManager.ToString(), 1);
    }
}
