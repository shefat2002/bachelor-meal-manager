using System.Security.Cryptography;
using MealManager.Application.Abstractions;
using MealManager.Application.Domain;
using MealManager.Application.Exceptions;
using MediatR;

namespace MealManager.Application.Features.Members;

/// <summary>Manager-only: mint a single-use invite token (7-day expiry). Tenant-scoped via X-Mess-Id.</summary>
public record CreateInviteCommand : ITenantRequest, IRequest<InviteResult>;

public sealed class CreateInviteCommandHandler(IApplicationContext db, ITenantContext tenant, ICurrentUser currentUser)
    : IRequestHandler<CreateInviteCommand, InviteResult>
{
    public async Task<InviteResult> Handle(CreateInviteCommand request, CancellationToken cancellationToken)
    {
        if (tenant.Role != MessRole.MealManager)
        {
            throw new ForbiddenException("Only mess managers can create invites.");
        }

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        var expiresAt = DateTime.UtcNow.AddDays(7);

        db.InviteLinks.Add(new InviteLink
        {
            Id = Guid.NewGuid(),
            MessId = tenant.MessId!.Value,
            Token = token,
            ExpiresAt = expiresAt,
            CreatedBy = currentUser.UserId!.Value,
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync(cancellationToken);

        return new InviteResult(token, expiresAt);
    }
}
