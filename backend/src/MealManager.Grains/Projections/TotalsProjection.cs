using MealManager.Application.Abstractions;
using MealManager.Grains.Contracts;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Orleans;

namespace MealManager.Grains.Projections;

/// <summary>
/// ITotalsProjection implementation: forwards write deltas to the MessGrain (which fans member-level
/// deltas to MemberGrains) and removes the cached summary. Failures are logged and swallowed — the
/// DB write has already committed and rebuild-on-activation self-heals, so a projection hiccup must
/// never fail a successful request.
/// </summary>
public sealed class TotalsProjection(
    IGrainFactory grainFactory,
    HybridCache cache,
    ILogger<TotalsProjection> logger) : ITotalsProjection
{
    public Task MealsChangedAsync(Guid messId, DateOnly date, IReadOnlyList<MealChange> changes, CancellationToken cancellationToken = default) =>
        SafeAsync(async () =>
        {
            var deltas = changes.Select(c => new MealDelta(c.MemberId, c.OldUnits, c.NewUnits)).ToList();
            await grainFactory.GetGrain<IMessGrain>(messId)
                .RecordMealsAsync(date.Year, date.Month, date.Day, deltas);
            await InvalidateAsync(messId, date.Year, date.Month, cancellationToken);
        }, messId, "meals");

    public Task BazarAddedAsync(Guid messId, DateOnly date, decimal lineTotal, CancellationToken cancellationToken = default) =>
        SafeAsync(async () =>
        {
            await grainFactory.GetGrain<IMessGrain>(messId).RecordBazarAsync(date.Year, date.Month, date.Day, lineTotal);
            await InvalidateAsync(messId, date.Year, date.Month, cancellationToken);
        }, messId, "bazar");

    public Task UtilityPaymentAddedAsync(Guid messId, DateOnly paymentDate, decimal amount, CancellationToken cancellationToken = default) =>
        SafeAsync(async () =>
        {
            await grainFactory.GetGrain<IMessGrain>(messId).RecordUtilityPaymentAsync(paymentDate.Year, paymentDate.Month, amount);
            await InvalidateAsync(messId, paymentDate.Year, paymentDate.Month, cancellationToken);
        }, messId, "utility");

    public Task DepositApprovedAsync(Guid messId, DateOnly depositDate, decimal amount, CancellationToken cancellationToken = default) =>
        SafeAsync(async () =>
        {
            await grainFactory.GetGrain<IMessGrain>(messId).RecordDepositApprovedAsync(depositDate.Year, depositDate.Month, amount);
            await InvalidateAsync(messId, depositDate.Year, depositDate.Month, cancellationToken);
        }, messId, "deposit");

    public Task MonthLockChangedAsync(Guid messId, int year, int month, bool locked, CancellationToken cancellationToken = default) =>
        SafeAsync(async () =>
        {
            // MessGrain evicts its month state; member grains need no eviction — writes were frozen
            // while locked, so any resident member state still equals the DB.
            await grainFactory.GetGrain<IMessGrain>(messId).SetMonthLockedAsync(year, month, locked);
            await InvalidateAsync(messId, year, month, cancellationToken);
        }, messId, "month-lock");

    private async Task InvalidateAsync(Guid messId, int year, int month, CancellationToken ct) =>
        await cache.RemoveAsync(SummaryCache.Key(messId, year, month), ct);

    private async Task SafeAsync(Func<Task> action, Guid messId, string operation)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Post-commit {Operation} projection failed for mess {MessId} — state will rebuild from the DB on next read",
                operation, messId);
        }
    }
}

internal static class SummaryCache
{
    public static string Key(Guid messId, int year, int month) => $"summary:{messId}:{year}-{month:D2}";
}
