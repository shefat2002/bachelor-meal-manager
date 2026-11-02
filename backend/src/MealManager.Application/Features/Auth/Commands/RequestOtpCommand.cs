using System.Security.Cryptography;
using FluentValidation;
using MealManager.Application.Abstractions;
using MealManager.Application.Common;
using MealManager.Application.Domain;
using MealManager.Application.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace MealManager.Application.Features.Auth;

public record RequestOtpCommand(string Phone) : IRequest<OtpRequestResult>;

public sealed class RequestOtpCommandValidator : AbstractValidator<RequestOtpCommand>
{
    public RequestOtpCommandValidator()
    {
        RuleFor(x => x.Phone).NotEmpty().Must(p => Phone.IsValid(Phone.Normalize(p)))
            .WithMessage("Phone must be 8-15 digits, optionally starting with '+'.");
    }
}

public sealed class RequestOtpCommandHandler(
    IApplicationContext db,
    ITokenService tokens,
    IHostEnvironment environment) : IRequestHandler<RequestOtpCommand, OtpRequestResult>
{
    private static readonly TimeSpan Expiry = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan RateWindow = TimeSpan.FromMinutes(15);
    private const int MaxRequestsPerWindow = 3;

    public async Task<OtpRequestResult> Handle(RequestOtpCommand request, CancellationToken cancellationToken)
    {
        var phone = Phone.Normalize(request.Phone);
        var now = DateTime.UtcNow;

        var recentCount = await db.OtpCodes.AsNoTracking()
            .CountAsync(o => o.PhoneNumber == phone && o.ConsumedAt == null && o.CreatedAt > now.Subtract(RateWindow), cancellationToken);
        if (recentCount >= MaxRequestsPerWindow)
        {
            throw new AppException(429, "RATE_LIMITED", "Too many OTP requests. Try again later.");
        }

        var expiresAt = now.Add(Expiry);
        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

        db.OtpCodes.Add(new OtpCode
        {
            Id = Guid.NewGuid(),
            PhoneNumber = phone,
            CodeHash = tokens.HashToken(code),
            CreatedAt = now,
            ExpiresAt = expiresAt,
        });
        await db.SaveChangesAsync(cancellationToken);

        // No SMS provider until Phase 4 (RabbitMQ notification worker); development returns the code for testing.
        return new OtpRequestResult(true, environment.IsDevelopment() ? code : null, expiresAt);
    }
}
