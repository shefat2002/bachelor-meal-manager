using FluentValidation;
using MealManager.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MealManager.Application.Features.Auth;

/// <summary>
/// Sets/updates the current user's notification email (Identity's existing Email column — no
/// migration). User-scoped: no X-Mess-Id. Emails power Phase 4 alerts; optional by design.
/// </summary>
public record UpdateEmailCommand(string Email) : IRequest<Unit>;

public sealed class UpdateEmailCommandValidator : AbstractValidator<UpdateEmailCommand>
{
    public UpdateEmailCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
    }
}

public sealed class UpdateEmailCommandHandler(IApplicationContext db, ICurrentUser currentUser)
    : IRequestHandler<UpdateEmailCommand, Unit>
{
    public async Task<Unit> Handle(UpdateEmailCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var member = await db.Members.FirstAsync(u => u.Id == currentUser.UserId, cancellationToken);
        member.Email = email;
        member.NormalizedEmail = email.ToUpperInvariant();
        await db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
