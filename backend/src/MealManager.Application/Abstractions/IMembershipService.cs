using MealManager.Application.Domain;

namespace MealManager.Application.Abstractions;

public interface IMembershipService
{
    /// <summary>Active membership role of a member in a mess, or null. Used by the tenant pipeline; ignores query filters.</summary>
    Task<MessRole?> ResolveAsync(Guid memberId, Guid messId, CancellationToken cancellationToken = default);
}
