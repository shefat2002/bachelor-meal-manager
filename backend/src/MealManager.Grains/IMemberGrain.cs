using MealManager.Grains.Contracts;
using Orleans;

namespace MealManager.Grains;

/// <summary>
/// One member's standing inside one mess (compound key = memberId extended key messId).
/// Owns the member's monthly meal units + approved deposits; performs the member-level §5 math.
/// Call graph is strictly MessGrain → MemberGrain (never back).
/// </summary>
public interface IMemberGrain : IGrainWithGuidCompoundKey
{
    Task<MemberComputedRow> GetComputedRowAsync(int year, int month, decimal mealRate, decimal utilityTotal, int activeMembers);

    Task RecordMealDeltaAsync(int year, int month, decimal deltaUnits);

    Task RecordDepositApprovedAsync(int year, int month, decimal deltaAmount);

    /// <summary>Month lock/unlock: evicts the in-memory month standing (rebuilt from DB on demand).</summary>
    Task SetMonthLockedAsync(int year, int month, bool locked);
}
