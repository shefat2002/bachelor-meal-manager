using FluentValidation;
using MealManager.Application.Abstractions;
using MealManager.Application.Common;
using MealManager.Application.Domain;
using MealManager.Application.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace MealManager.Application.Features.Auth;

public record LoginCommand(string Phone, string Password) : IRequest<AuthResult>;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Phone).NotEmpty().Must(p => Phone.IsValid(Phone.Normalize(p)))
            .WithMessage("Phone must be 8-15 digits, optionally starting with '+'.");
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class LoginCommandHandler(
    IApplicationContext db,
    UserManager<Member> userManager,
    SignInManager<Member> signInManager,
    ITokenService tokens) : IRequestHandler<LoginCommand, AuthResult>
{
    public async Task<AuthResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var phone = Phone.Normalize(request.Phone);
        var user = await userManager.FindByNameAsync(phone);
        if (user is null)
        {
            // Same message for unknown phone and wrong password — no account enumeration.
            throw new UnauthorizedException("Invalid phone number or password.");
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            throw new UnauthorizedException(
                result.IsLockedOut
                    ? "Account is temporarily locked after too many failed attempts."
                    : "Invalid phone number or password.");
        }

        return await AuthTokenIssuer.IssueAsync(user, db, userManager, tokens);
    }
}
