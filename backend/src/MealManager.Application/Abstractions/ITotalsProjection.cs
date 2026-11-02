namespace MealManager.Application.Abstractions;

/// <summary>
/// One meal row's old → new absolute units (handler computes both around the DB write).
/// </summary>
public sealed record MealChange(Guid MemberId, decimal OldUnits, decimal NewUnits);

/// <summary>
/// Write-side port: handlers call this AFTER SaveChanges succeeds to update grain totals and
/// invalidate the summary cache (skill bmm-orleans-grains: DB is the record, grains accumulate).
/// Implemented in MealManager.Grains via IGrainFactory; Application stays Orleans-free.
/// </summary>
public interface ITotalsProjection
{
    Task MealsChangedAsync(Guid messId, DateOnly date, IReadOnlyList<MealChange> changes, CancellationToken cancellationToken = default);

    Task BazarAddedAsync(Guid messId, DateOnly date, decimal lineTotal, CancellationToken cancellationToken = default);

    Task UtilityPaymentAddedAsync(Guid messId, DateOnly paymentDate, decimal amount, CancellationToken cancellationToken = default);

    /// <summary>Only approved deposits move money (member-created pending deposits contribute nothing).</summary>
    Task DepositApprovedAsync(Guid messId, DateOnly depositDate, decimal amount, CancellationToken cancellationToken = default);

    Task MonthLockChangedAsync(Guid messId, int year, int month, bool locked, CancellationToken cancellationToken = default);
}
