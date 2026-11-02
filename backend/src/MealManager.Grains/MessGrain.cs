using MealManager.Application.Abstractions;
using MealManager.Application.Domain;
using MealManager.Grains.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Orleans.Runtime;

namespace MealManager.Grains;

/// <summary>
/// In-memory month state; the DB is the record and rebuild-on-miss is the load path (skill bmm-orleans-grains
/// "RecoverableState" pattern). Only the newest few months stay resident.
/// </summary>
internal sealed class MessMonthState
{
    public decimal TotalUnits { get; set; }

    public decimal BazarCost { get; set; }

    public decimal UtilityCost { get; set; }

    public decimal DepositsCollected { get; set; }

    public Dictionary<int, (decimal Units, decimal Bazar)> Daily { get; } = [];
}

public sealed class MessGrain(
    IGrainFactory grainFactory,
    IServiceScopeFactory scopeFactory) : Grain, IMessGrain
{
    private const int MaxResidentMonths = 3;

    private readonly Dictionary<(int Year, int Month), MessMonthState> _months = [];

    public async Task<MessMonthSummary> GetSummaryAsync(int year, int month)
    {
        var state = await EnsureStateAsync(year, month, CancellationToken.None);

        // Active members are queried fresh each call — an equal utility split must reflect the
        // CURRENT membership, not a snapshot taken when the month state was built.
        var actives = await QueryAsync(db => db.MembersMesses.AsNoTracking().IgnoreQueryFilters()
            .Where(mm => mm.MessId == this.GetPrimaryKey() && mm.Status == MemberStatus.Active)
            .Join(db.Members.AsNoTracking(), mm => mm.MemberId, u => u.Id, (mm, u) => new { u.Id, u.Name })
            .ToListAsync());

        var rate = SettlementMath.MealRate(state.BazarCost, state.TotalUnits);
        var computed = await Task.WhenAll(actives.Select(a =>
            grainFactory.GetGrain<IMemberGrain>(a.Id, this.GetPrimaryKey().ToString())
                .GetComputedRowAsync(year, month, rate, state.UtilityCost, actives.Count)));

        var byId = computed.ToDictionary(r => r.MemberId);
        var members = actives
            .Select(a =>
            {
                var row = byId.GetValueOrDefault(a.Id);
                return new MemberRow(
                    a.Id, a.Name, row?.MealUnits ?? 0m, row?.Deposited ?? 0m,
                    row?.MealCost ?? 0m, row?.UtilityShare ?? 0m, row?.TotalCost ?? 0m, row?.Balance ?? 0m);
            })
            .OrderBy(r => r.Name, StringComparer.Ordinal)
            .ToList();

        var daysInMonth = DateTime.DaysInMonth(year, month);
        var daily = Enumerable.Range(1, daysInMonth)
            .Where(day => state.Daily.TryGetValue(day, out var point) && (point.Units != 0m || point.Bazar != 0m))
            .Select(day =>
            {
                var point = state.Daily[day];
                return new DailyTrendPoint(new DateOnly(year, month, day), point.Bazar, point.Units);
            })
            .ToList();

        return new MessMonthSummary(
            year, month, rate, state.TotalUnits, state.BazarCost, state.UtilityCost,
            SettlementMath.Round2(state.BazarCost + state.UtilityCost),
            state.DepositsCollected,
            SettlementMath.OnHand(state.DepositsCollected, state.BazarCost, state.UtilityCost),
            members, daily);
    }

    public Task RecordMealsAsync(int year, int month, int day, List<MealDelta> changes)
    {
        // Deltas apply ONLY to resident state. If the month was evicted we intentionally do nothing:
        // the handler calls this AFTER SaveChanges, so a rebuild would already include the write and
        // applying the delta on top would double-count. Absent state simply rebuilds correctly on demand.
        if (!_months.TryGetValue((year, month), out var state))
        {
            return Task.CompletedTask;
        }

        var memberTasks = new List<Task>();
        foreach (var change in changes)
        {
            state.TotalUnits += change.NewUnits - change.OldUnits;
            var today = state.Daily.GetValueOrDefault(day);
            state.Daily[day] = (today.Units + change.NewUnits - change.OldUnits, today.Bazar);
            memberTasks.Add(grainFactory.GetGrain<IMemberGrain>(change.MemberId, this.GetPrimaryKey().ToString())
                .RecordMealDeltaAsync(year, month, change.NewUnits - change.OldUnits));
        }

        return Task.WhenAll(memberTasks);
    }

    public Task RecordBazarAsync(int year, int month, int day, decimal lineTotal)
    {
        if (_months.TryGetValue((year, month), out var state))
        {
            state.BazarCost += lineTotal;
            var today = state.Daily.GetValueOrDefault(day);
            state.Daily[day] = (today.Units, today.Bazar + lineTotal);
        }

        return Task.CompletedTask;
    }

    public Task RecordUtilityPaymentAsync(int year, int month, decimal amount)
    {
        if (_months.TryGetValue((year, month), out var state))
        {
            state.UtilityCost += amount;
        }

        return Task.CompletedTask;
    }

    public Task RecordDepositApprovedAsync(int year, int month, decimal amount)
    {
        if (_months.TryGetValue((year, month), out var state))
        {
            state.DepositsCollected += amount;
        }

        return Task.CompletedTask;
    }

    public Task SetMonthLockedAsync(int year, int month, bool locked)
    {
        // Lock or unlock: the resident state is dropped either way. Locked months serve from the DB
        // snapshot; an unlocked month rebuilds from the (authoritative) domain tables on next read.
        _months.Remove((year, month));
        return Task.CompletedTask;
    }

    private async Task<MessMonthState> EnsureStateAsync(int year, int month, CancellationToken ct)
    {
        if (_months.TryGetValue((year, month), out var state))
        {
            return state;
        }

        var messId = this.GetPrimaryKey();
        var from = new DateOnly(year, month, 1);
        var to = new DateOnly(year, month, DateTime.DaysInMonth(year, month));

        var rebuilt = new MessMonthState();

        var mealDays = await QueryAsync(db => db.DailyMeals.AsNoTracking().IgnoreQueryFilters()
            .Where(m => m.MessId == messId && m.Date >= from && m.Date <= to)
            .Select(m => new { Day = m.Date.Day, Units = m.Breakfast + m.Lunch + m.Dinner })
            .ToListAsync());

        var bazarDays = await QueryAsync(db => db.BazarExpenses.AsNoTracking().IgnoreQueryFilters()
            .Where(e => e.MessId == messId && e.Date >= from && e.Date <= to)
            .GroupBy(e => e.Date)
            .Select(g => new { Day = g.Key.Day, Total = g.Sum(e => e.Quantity * e.Price) })
            .ToListAsync());

        rebuilt.UtilityCost = (await QueryAsync(db => db.UtilityPayments.AsNoTracking().IgnoreQueryFilters()
            .Where(p => p.MessId == messId && p.PaymentDate >= from && p.PaymentDate <= to)
            .SumAsync(p => (decimal?)p.Amount, ct))) ?? 0m;

        rebuilt.DepositsCollected = (await QueryAsync(db => db.Deposits.AsNoTracking().IgnoreQueryFilters()
            .Where(d => d.MessId == messId && d.Status == DepositStatus.Approved && d.Date >= from && d.Date <= to)
            .SumAsync(d => (decimal?)d.Amount, ct))) ?? 0m;

        foreach (var meal in mealDays)
        {
            rebuilt.TotalUnits += meal.Units;
            var point = rebuilt.Daily.GetValueOrDefault(meal.Day);
            rebuilt.Daily[meal.Day] = (point.Units + meal.Units, point.Bazar);
        }

        foreach (var bazar in bazarDays)
        {
            rebuilt.BazarCost += bazar.Total;
            var point = rebuilt.Daily.GetValueOrDefault(bazar.Day);
            rebuilt.Daily[bazar.Day] = (point.Units, point.Bazar + bazar.Total);
        }

        _months[(year, month)] = rebuilt;
        EvictOldMonths();
        return rebuilt;
    }

    private void EvictOldMonths()
    {
        if (_months.Count <= MaxResidentMonths)
        {
            return;
        }

        foreach (var key in _months.Keys.OrderDescending().Skip(MaxResidentMonths).ToList())
        {
            _months.Remove(key);
        }
    }

    /// <summary>Grains resolve scoped services per call via the silo's service provider — never hold a DbContext across turns.</summary>
    private async Task<T> QueryAsync<T>(Func<IApplicationContext, Task<T>> query)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationContext>();
        return await query(db);
    }
}
