using MealManager.Application.Abstractions;
using MealManager.Application.Features.Summary;
using MealManager.Grains.Projections;
using Microsoft.Extensions.Caching.Hybrid;
using Orleans;

namespace MealManager.Grains.Projections;

/// <summary>
/// ISummaryProvider implementation: HybridCache get-or-create around the MessGrain. Invalidation is
/// push-based (TotalsProjection removes the key on every write); the 15-minute expiration is only a
/// safety net.
/// </summary>
public sealed class SummaryProvider(
    IGrainFactory grainFactory,
    HybridCache cache) : ISummaryProvider
{
    private static readonly HybridCacheEntryOptions EntryOptions = new()
    {
        Expiration = TimeSpan.FromMinutes(15),
        LocalCacheExpiration = TimeSpan.FromMinutes(15),
    };

    public async Task<MessSummaryDto> GetAsync(Guid messId, int year, int month, CancellationToken cancellationToken = default)
    {
        var cached = await cache.GetOrCreateAsync(
            SummaryCache.Key(messId, year, month),
            async ct => Map(messId, await grainFactory.GetGrain<IMessGrain>(messId).GetSummaryAsync(year, month), ct),
            EntryOptions,
            tags: [$"summary:{messId}"],
            cancellationToken: cancellationToken);
        return cached;
    }

    public async Task InvalidateAsync(Guid messId, int year, int month, CancellationToken cancellationToken = default) =>
        await cache.RemoveAsync(SummaryCache.Key(messId, year, month), cancellationToken);

    private static MessSummaryDto Map(Guid messId, MealManager.Grains.Contracts.MessMonthSummary summary, CancellationToken _) => new(
        messId,
        summary.Year,
        summary.Month,
        Locked: false,
        summary.MealRate,
        summary.TotalMealUnits,
        summary.TotalMealCosts,
        summary.TotalUtilityCosts,
        summary.TotalExpenditure,
        summary.TotalDeposits,
        summary.OnHandBalance,
        summary.Members.Select(m => new MemberSettlementRowDto(
            m.MemberId, m.Name, m.MealUnits, m.MealCost, m.UtilityShare, m.TotalCost, m.Deposited, m.Balance)).ToList(),
        summary.Daily.Select(d => new DailyTrendDto(d.Date, d.BazarSpend, d.MealUnits)).ToList());
}
