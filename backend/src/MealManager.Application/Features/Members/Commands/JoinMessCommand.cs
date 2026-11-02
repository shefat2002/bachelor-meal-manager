using FluentValidation;
using MealManager.Application.Abstractions;
using MealManager.Application.Domain;
using MealManager.Application.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MealManager.Application.Features.Members;

/// <summary>Join via invite token — deliberately NOT tenant-scoped (the joiner doesn't know the mess id yet).
/// Creates a Pending membership the mess manager must approve.</summary>
public record JoinMessCommand(string InviteToken) : IRequest<JoinedMessResult>;

public sealed class JoinMessCommandValidator : AbstractValidator<JoinMessCommand>
{
    public JoinMessCommandValidator()
    {
        RuleFor(x => x.InviteToken).NotEmpty().MaximumLength(128);
    }
}

public sealed class JoinMessCommandHandler(IApplicationContext db, ICurrentUser currentUser)
    : IRequestHandler<JoinMessCommand, JoinedMessResult>
{
    public async Task<JoinedMessResult> Handle(JoinMessCommand request, CancellationToken cancellationToken)
    {
        var invite = await db.InviteLinks.IgnoreQueryFilters()
            .Include(i => i.Mess)
            .FirstOrDefaultAsync(i => i.Token == request.InviteToken.Trim(), cancellationToken)
            ?? throw new NotFoundException("Invite not found.");

        if (invite.Used || invite.ExpiresAt < DateTime.UtcNow)
        {
            throw new ConflictException("This invite has expired or was already used.");
        }

        var userId = currentUser.UserId!.Value;
        var membership = await db.MembersMesses.IgnoreQueryFilters()
            .FirstOrDefaultAsync(mm => mm.MessId == invite.MessId && mm.MemberId == userId, cancellationToken);

        switch (membership?.Status)
        {
            case MemberStatus.Active:
                throw new ConflictException("You are already a member of this mess.");
            case MemberStatus.Pending:
                throw new ConflictException("Your join request is already pending approval.");
        }

        if (membership is null)
        {
            db.MembersMesses.Add(new MemberMess
            {
                MessId = invite.MessId,
                MemberId = userId,
                Role = MessRole.Member,
                Status = MemberStatus.Pending,
                JoinedAt = DateTime.UtcNow,
                InvitedBy = invite.CreatedBy,
            });
        }
        else
        {
            membership.Status = MemberStatus.Pending;
            membership.JoinedAt = DateTime.UtcNow;
        }

        invite.Used = true;
        await db.SaveChangesAsync(cancellationToken);

        return new JoinedMessResult(invite.MessId, invite.Mess.Name, MemberStatus.Pending.ToString());
    }
}
