using MealManager.Application.Abstractions;
using MealManager.Application.Domain;
using MealManager.Application.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace MealManager.Application.Behaviors;

/// <summary>
/// Resolves the tenant for requests marked with ITenantRequest: validates the X-Mess-Id header,
/// checks membership (SuperAdmin bypasses) and populates the scoped TenantContext before the handler runs.
/// Skill bmm-cqrs-mediatr: handlers must never trust client-supplied tenant ids.
/// </summary>
public sealed class TenantResolutionBehavior<TRequest, TResponse>(
    IHttpContextAccessor httpAccessor,
    ICurrentUser currentUser,
    TenantContext tenant,
    IMembershipService membershipService)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is not ITenantRequest)
        {
            return await next();
        }

        var header = httpAccessor.HttpContext?.Request.Headers["X-Mess-Id"].ToString();
        if (string.IsNullOrWhiteSpace(header))
        {
            throw new AppException(400, "TENANT_REQUIRED", "The X-Mess-Id header is required for this endpoint.");
        }

        if (!Guid.TryParse(header, out var messId))
        {
            throw new AppException(400, "TENANT_REQUIRED", "X-Mess-Id must be a valid GUID.");
        }

        if (currentUser.UserId is not { } memberId)
        {
            throw new UnauthorizedException();
        }

        if (!currentUser.IsSuperAdmin)
        {
            var role = await membershipService.ResolveAsync(memberId, messId, cancellationToken);
            if (role is null)
            {
                throw new ForbiddenException("You are not an active member of this mess.");
            }

            tenant.Set(messId, role.Value);
        }
        else
        {
            tenant.Set(messId, MessRole.MealManager);
        }

        return await next();
    }
}
