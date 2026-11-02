namespace MealManager.Application.Features.Summary;

/// <summary>The §5 equations' output for one mess-month. Locked months serve a serialized copy of this.</summary>
public record MessSummaryDto(
    Guid MessId,
    int Year,
    int Month,
    bool Locked,
    decimal MealRate,
    decimal TotalMealUnits,
    decimal TotalMealCosts,
    decimal TotalUtilityCosts,
    decimal TotalExpenditure,
    decimal TotalDeposits,
    decimal OnHandBalance,
    IReadOnlyList<MemberSettlementRowDto> Members,
    IReadOnlyList<DailyTrendDto> Daily);

public record MemberSettlementRowDto(
    Guid MemberId,
    string Name,
    decimal MealUnits,
    decimal MealCost,
    decimal UtilityShare,
    decimal TotalCost,
    decimal Deposited,
    decimal Balance);

public record DailyTrendDto(DateOnly Date, decimal BazarSpend, decimal MealUnits);

public record MonthLockRequest(int Year, int Month, bool Locked);
