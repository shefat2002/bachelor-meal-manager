using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace MealManager.Application.Abstractions;

public interface ICurrentUser
{
    Guid? UserId { get; }

    bool IsAuthenticated { get; }

    bool IsSuperAdmin { get; }
}

public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public Guid? UserId =>
        Guid.TryParse(
            Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Principal?.FindFirst("sub")?.Value,
            out var id)
            ? id
            : null;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public bool IsSuperAdmin => Principal?.IsInRole("SuperAdmin") == true;
}
