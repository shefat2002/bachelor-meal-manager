using MealManager.Application.Abstractions;
using MealManager.Application.Domain;
using MealManager.Application.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace MealManager.Application.Features.Auth;

public record GetCurrentUserQuery : IRequest<AuthUserDto>;

public sealed class GetCurrentUserQueryHandler(
    ICurrentUser currentUser,
    UserManager<Member> userManager) : IRequestHandler<GetCurrentUserQuery, AuthUserDto>
{
    public async Task<AuthUserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            throw new UnauthorizedException();
        }

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            throw new UnauthorizedException();
        }

        var roles = await userManager.GetRolesAsync(user);
        return new AuthUserDto(
            user.Id,
            user.Name,
            user.PhoneNumber ?? string.Empty,
            roles.Contains(AuthRoles.SuperAdmin) ? "superAdmin" : "user");
    }
}
