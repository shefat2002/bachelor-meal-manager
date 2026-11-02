using MealManager.Application.Abstractions;
using MealManager.Application.Domain;
using MealManager.Grains.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Orleans.Runtime;

namespace MealManager.Grains;

internal sealed class MemberMonthState
{
    public decimal Units { get; set; }

    public decimal Deposited { get; set; }
}

public sealed class MemberGrain(IServiceScopeFactory scopeFactory) : Grain, IMemberGrain
{
    private const int MaxResidentMonths = 3;

    private readonly Dictionary<(int Year, int Month), MemberMonthState> _months = [];

    public async Task<MemberComputedRow> GetComputedRowAsync(int year, int month, decimal mealRate, decimal utilityTotal, int activeMembers)
    {
        var state = await EnsureStateAsync(year, month, CancellationToken.None);
        var mealCost = SettlementMath.MealCost(state.Units, mealRate);
        var utilityShare = SettlementMath.UtilityShare(utilityTotal, activeMembers);
        var totalCost = SettlementMath.TotalCost(mealCost, utilityShare);
        return new MemberComputedRow(
            this.GetPrimaryKey(), state.Units, state.Deposited, mealCost, utilityShare, totalCost,
            SettlementMath.Balance(state.Deposited, totalCost));
    }

    public Task RecordMealDeltaAsync(int year, int month, decimal deltaUnits)
    {
        // Same rule as MessGrain: deltas touch resident state only — evicted state rebuilds from the DB,
        // which already includes the committed write.
        if (_months.TryGetValue((year, month), out var state))
        {
            state.Units += deltaUnits;
        }

        return Task.CompletedTask;
    }

    public Task RecordDepositApprovedAsync(int year, int month, decimal deltaAmount)
    {
        if (_months.TryGetValue((year, month), out var state))
        {
            state.Deposited += deltaAmount;
        }

        return Task.CompletedTask;
    }

    public Task SetMonthLockedAsync(int year, int month, bool locked)
    {
        _months.Remove((year, month));
        return Task.CompletedTask;
    }

    private async Task<MemberMonthState> EnsureStateAsync(int year, int month, CancellationToken ct)
    {
        if (_months.TryGetValue((year, month), out var state))
        {
            return state;
        }

        var memberId = this.GetPrimaryKey(out var extendedKey);
        var messId = Guid.Parse(extendedKey);
        var from = new DateOnly(year, month, 1);
        var to = new DateOnly(year, month, DateTime.DaysInMonth(year, month));

        var rebuilt = new MemberMonthState
        {
            Units = (await QueryAsync(db => db.DailyMeals.AsNoTracking().IgnoreQueryFilters()
                .Where(m => m.MessId == messId && m.MemberId == memberId && m.Date >= from && m.Date <= to)
                .SumAsync(m => (decimal?)m.Breakfast + m.Lunch + m.Dinner, ct))) ?? 0m,
            Deposited = (await QueryAsync(db => db.Deposits.AsNoTracking().IgnoreQueryFilters()
                .Where(d => d.MessId == messId && d.MemberId == memberId
                    && d.Status == DepositStatus.Approved && d.Date >= from && d.Date <= to)
                .SumAsync(d => (decimal?)d.Amount, ct))) ?? 0m,
        };

        _months[(year, month)] = rebuilt;
        if (_months.Count > MaxResidentMonths)
        {
            foreach (var key in _months.Keys.OrderDescending().Skip(MaxResidentMonths).ToList())
            {
                _months.Remove(key);
            }
        }

        return rebuilt;
    }

    private async Task<T> QueryAsync<T>(Func<IApplicationContext, Task<T>> query)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationContext>();
        return await query(db);
    }
}
