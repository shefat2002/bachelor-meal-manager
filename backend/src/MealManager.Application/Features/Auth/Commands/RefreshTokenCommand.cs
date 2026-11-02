using MealManager.Application.Abstractions;
using MealManager.Application.Domain;
using MealManager.Application.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MealManager.Application.Features.Auth;

/// <summary>Rotates the refresh token from the HttpOnly cookie. Reuse of a revoked token revokes all user sessions.</summary>
public record RefreshTokenCommand(string? RefreshToken) : IRequest<AuthResult>;

public sealed class RefreshTokenCommandHandler(
    IApplicationContext db,
    UserManager<Member> userManager,
    ITokenService tokens) : IRequestHandler<RefreshTokenCommand, AuthResult>
{
    public async Task<AuthResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            throw new UnauthorizedException("Missing refresh token.");
        }

        var hash = tokens.HashToken(request.RefreshToken);
        var stored = await db.RefreshTokens.IgnoreQueryFilters()
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Hash == hash, cancellationToken);

        if (stored is null)
        {
            throw new UnauthorizedException("Invalid refresh token.");
        }

        if (stored.RevokedAt is not null)
        {
            // Token reuse detected: revoke every session for this user.
            var all = await db.RefreshTokens.IgnoreQueryFilters()
                .Where(t => t.UserId == stored.UserId && t.RevokedAt == null)
                .ToListAsync(cancellationToken);
            foreach (var token in all)
            {
                token.RevokedAt = DateTime.UtcNow;
            }

            await db.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedException("Invalid refresh token.");
        }

        if (stored.ExpiresAt < DateTime.UtcNow)
        {
            throw new UnauthorizedException("Refresh token expired.");
        }

        var user = stored.User;
        var (refresh, newHash) = tokens.NewRefreshToken();
        stored.RevokedAt = DateTime.UtcNow;
        stored.ReplacedByHash = newHash;
        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Hash = newHash,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
        });
        await db.SaveChangesAsync(cancellationToken);

        var roles = await userManager.GetRolesAsync(user);
        var accessToken = tokens.CreateAccessToken(user, roles);
        return new AuthResult(
            accessToken,
            refresh,
            new AuthUserDto(user.Id, user.Name, user.PhoneNumber ?? string.Empty, roles.Contains(AuthRoles.SuperAdmin) ? "superAdmin" : "user"));
    }
}
