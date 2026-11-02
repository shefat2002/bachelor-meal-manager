using FluentValidation;
using MealManager.Application.Abstractions;
using MealManager.Application.Common;
using MealManager.Application.Domain;
using MealManager.Application.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MealManager.Application.Features.Auth;

public record VerifyOtpCommand(string Phone, string Code) : IRequest<AuthResult>;

public sealed class VerifyOtpCommandValidator : AbstractValidator<VerifyOtpCommand>
{
    public VerifyOtpCommandValidator()
    {
        RuleFor(x => x.Phone).NotEmpty().Must(p => Phone.IsValid(Phone.Normalize(p)))
            .WithMessage("Phone must be 8-15 digits, optionally starting with '+'.");
        RuleFor(x => x.Code).NotEmpty().Matches("^[0-9]{6}$").WithMessage("Code must be 6 digits.");
    }
}

public sealed class VerifyOtpCommandHandler(
    IApplicationContext db,
    UserManager<Member> userManager,
    ITokenService tokens) : IRequestHandler<VerifyOtpCommand, AuthResult>
{
    private const int MaxAttempts = 5;

    public async Task<AuthResult> Handle(VerifyOtpCommand request, CancellationToken cancellationToken)
    {
        var phone = Phone.Normalize(request.Phone);
        var user = await userManager.FindByNameAsync(phone);
        if (user is null)
        {
            throw new UnauthorizedException("Invalid code.");
        }

        var otp = await db.OtpCodes
            .Where(o => o.PhoneNumber == phone && o.ConsumedAt == null)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (otp is null || otp.Attempts >= MaxAttempts || otp.ExpiresAt < DateTime.UtcNow)
        {
            throw new UnauthorizedException("Invalid or expired code.");
        }

        if (!tokens.HashToken(request.Code.Trim()).Equals(otp.CodeHash, StringComparison.OrdinalIgnoreCase))
        {
            otp.Attempts++;
            await db.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedException("Invalid or expired code.");
        }

        otp.ConsumedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return await AuthTokenIssuer.IssueAsync(user, db, userManager, tokens);
    }
}
