using FluentValidation;
using MealManager.Application.Abstractions;
using MealManager.Application.Common;
using MealManager.Application.Domain;
using MealManager.Application.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace MealManager.Application.Features.Auth;

public record RegisterCommand(string Name, string Phone, string Password) : IRequest<AuthResult>;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Phone).NotEmpty().Must(p => Phone.IsValid(Phone.Normalize(p)))
            .WithMessage("Phone must be 8-15 digits, optionally starting with '+'.");
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(128);
    }
}

public sealed class RegisterCommandHandler(
    IApplicationContext db,
    UserManager<Member> userManager,
    ITokenService tokens) : IRequestHandler<RegisterCommand, AuthResult>
{
    public async Task<AuthResult> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var phone = Phone.Normalize(request.Phone);
        if (await userManager.FindByNameAsync(phone) is not null)
        {
            throw new ConflictException("This phone number is already registered.");
        }

        var user = new Member
        {
            Id = Guid.NewGuid(),
            UserName = phone,
            PhoneNumber = phone,
            Name = request.Name.Trim(),
            CreatedAt = DateTime.UtcNow,
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors
                .GroupBy(e => e.Code.Contains("Password", StringComparison.OrdinalIgnoreCase) ? "password" : "phone")
                .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());
            throw new AppException(400, "VALIDATION_ERROR", "Registration failed.", errors);
        }

        return await AuthTokenIssuer.IssueAsync(user, db, userManager, tokens);
    }
}

/// <summary>Shared by register/login/otp-verify: mints the access token and stores the rotating refresh token row.</summary>
public static class AuthTokenIssuer
{
    public static async Task<AuthResult> IssueAsync(
        Member user,
        IApplicationContext db,
        UserManager<Member> userManager,
        ITokenService tokens)
    {
        var roles = await userManager.GetRolesAsync(user);
        var accessToken = tokens.CreateAccessToken(user, roles);
        var (refresh, hash) = tokens.NewRefreshToken();

        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Hash = hash,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
        });
        await db.SaveChangesAsync();

        return new AuthResult(
            accessToken,
            refresh,
            new AuthUserDto(user.Id, user.Name, user.PhoneNumber ?? string.Empty, roles.Contains(AuthRoles.SuperAdmin) ? "superAdmin" : "user"));
    }
}
