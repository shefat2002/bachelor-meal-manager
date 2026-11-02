using MealManager.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MealManager.Application.Features.Auth;

/// <summary>Revokes the refresh token from the cookie (best effort — idempotent).</summary>
public record LogoutCommand(string? RefreshToken) : IRequest<Unit>;

public sealed class LogoutCommandHandler(IApplicationContext db, ITokenService tokens)
    : IRequestHandler<LogoutCommand, Unit>
{
    public async Task<Unit> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            var hash = tokens.HashToken(request.RefreshToken);
            var stored = await db.RefreshTokens.IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Hash == hash && t.RevokedAt == null, cancellationToken);
            if (stored is not null)
            {
                stored.RevokedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(cancellationToken);
            }
        }

        return Unit.Value;
    }
}
