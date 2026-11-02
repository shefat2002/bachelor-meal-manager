using MealManager.Application.Domain;
using MealManager.Application.Exceptions;

namespace MealManager.Application.Abstractions;

/// <summary>Handler-side role guards (backend is the enforcement authority; frontend gates are UX only).</summary>
public static class TenantGuards
{
    /// <summary>SuperAdmin already resolves to MealManager in TenantContext, so one check covers both.</summary>
    public static void RequireManager(ITenantContext tenant, string action)
    {
        if (tenant.Role != MessRole.MealManager)
        {
            throw new ForbiddenException($"Only mess managers can {action}.");
        }
    }
}
