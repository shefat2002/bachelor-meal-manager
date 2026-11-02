using MealManager.Grains.Contracts;
using Orleans;

namespace MealManager.Grains;

/// <summary>
/// The accounting brain of one mess (key = messId). All §5 stateful math runs here or in the
/// MemberGrains it orchestrates — never in controllers (root AGENTS.md §4). Sequential turn-based
/// concurrency serializes concurrent write projections per mess.
/// </summary>
public interface IMessGrain : IGrainWithGuidKey
{
    /// <summary>Full summary for the month: mess totals + per-member settlement rows + daily trend.</summary>
    Task<MessMonthSummary> GetSummaryAsync(int year, int month);

    /// <summary>Meal upsert deltas for one date (old → new absolute units per member).</summary>
    Task RecordMealsAsync(int year, int month, int day, List<MealDelta> changes);

    Task RecordBazarAsync(int year, int month, int day, decimal lineTotal);

    Task RecordUtilityPaymentAsync(int year, int month, decimal amount);

    /// <summary>Only approved deposits move money (pending contributes nothing; approval calls this too).</summary>
    Task RecordDepositApprovedAsync(int year, int month, decimal amount);

    /// <summary>Month lock/unlock: evicts the in-memory month state (locked months serve from DB snapshots).</summary>
    Task SetMonthLockedAsync(int year, int month, bool locked);
}
