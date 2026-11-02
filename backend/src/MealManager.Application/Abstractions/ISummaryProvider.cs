using MealManager.Application.Features.Summary;

namespace MealManager.Application.Abstractions;

/// <summary>
/// Read-side port: cached summary access around the Orleans MessGrain. Implementations provide
/// cache-aside semantics (HybridCache + grain) and invalidation.
/// </summary>
public interface ISummaryProvider
{
    Task<MessSummaryDto> GetAsync(Guid messId, int year, int month, CancellationToken cancellationToken = default);

    Task InvalidateAsync(Guid messId, int year, int month, CancellationToken cancellationToken = default);
}
