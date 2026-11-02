using Orleans;

namespace MealManager.Grains.Contracts;

// Transport types crossing grain boundaries — [GenerateSerializer] is required by Orleans.
// Application-layer DTOs never cross grain boundaries (ISummaryProvider/ITotalsProjection map at the edge),
// so the Application project stays Orleans-free.

[GenerateSerializer]
public sealed record MealDelta(
    [Id(0)] Guid MemberId,
    [Id(1)] decimal OldUnits,
    [Id(2)] decimal NewUnits);

[GenerateSerializer]
public sealed record DailyTrendPoint(
    [Id(0)] DateOnly Date,
    [Id(1)] decimal BazarSpend,
    [Id(2)] decimal MealUnits);

/// <summary>A member's computed settlement row (name filled by MessGrain, which owns the member list).</summary>
[GenerateSerializer]
public sealed record MemberComputedRow(
    [Id(0)] Guid MemberId,
    [Id(1)] decimal MealUnits,
    [Id(2)] decimal Deposited,
    [Id(3)] decimal MealCost,
    [Id(4)] decimal UtilityShare,
    [Id(5)] decimal TotalCost,
    [Id(6)] decimal Balance);

[GenerateSerializer]
public sealed record MemberRow(
    [Id(0)] Guid MemberId,
    [Id(1)] string Name,
    [Id(2)] decimal MealUnits,
    [Id(3)] decimal Deposited,
    [Id(4)] decimal MealCost,
    [Id(5)] decimal UtilityShare,
    [Id(6)] decimal TotalCost,
    [Id(7)] decimal Balance);

/// <summary>The §5 equations' output for one mess-month (BACKEND_PROJECT_PLAN §5).</summary>
[GenerateSerializer]
public sealed record MessMonthSummary(
    [Id(0)] int Year,
    [Id(1)] int Month,
    [Id(2)] decimal MealRate,
    [Id(3)] decimal TotalMealUnits,
    [Id(4)] decimal TotalMealCosts,
    [Id(5)] decimal TotalUtilityCosts,
    [Id(6)] decimal TotalExpenditure,
    [Id(7)] decimal TotalDeposits,
    [Id(8)] decimal OnHandBalance,
    [Id(9)] List<MemberRow> Members,
    [Id(10)] List<DailyTrendPoint> Daily);
