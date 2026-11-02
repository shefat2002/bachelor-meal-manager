using MealManager.Application.Abstractions;
using MealManager.Application.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace MealManager.Application.Common;

/// <summary>
/// Write-path guard: a MonthLock row existing for (mess, year, month) freezes all writes dated in
/// that month (P3-BE-5). Called by meals/bazar/utility/deposit write handlers.
/// </summary>
public static class MonthLockGuard
{
    public static async Task RequireOpenAsync(IApplicationContext db, Guid messId, DateOnly date, CancellationToken ct)
    {
        var locked = await db.MonthLocks.AsNoTracking().IgnoreQueryFilters()
            .AnyAsync(l => l.MessId == messId && l.Year == date.Year && l.Month == date.Month, ct);
        if (locked)
        {
            throw new ConflictException("This month is locked; edits are frozen until it is unlocked.");
        }
    }
}
