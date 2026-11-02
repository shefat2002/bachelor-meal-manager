namespace MealManager.Grains;

/// <summary>
/// The §5 financial equations as pure decimal math (BACKEND_PROJECT_PLAN.md §5) — unit-tested without Orleans.
/// Rounding: meal rate 4dp, money rows 2dp (banker's-free MidpointRounding.ToEven default is fine for display aggregates).
/// </summary>
public static class SettlementMath
{
    /// <summary>Meal Rate = Total Meal Costs (bazar) ÷ Total Meal Units. 0 when no meals were eaten.</summary>
    public static decimal MealRate(decimal totalMealCosts, decimal totalUnits) =>
        totalUnits == 0 ? 0m : Math.Round(totalMealCosts / totalUnits, 4);

    /// <summary>Equal utility split (P3 user decision): TotalUtility ÷ activeMemberCount. 0 when nobody is active.</summary>
    public static decimal UtilityShare(decimal utilityTotal, int activeMembers) =>
        activeMembers <= 0 ? 0m : Math.Round(utilityTotal / activeMembers, 2);

    public static decimal Round2(decimal value) => Math.Round(value, 2);

    /// <summary>Individual Meal Cost = units × rate.</summary>
    public static decimal MealCost(decimal units, decimal mealRate) => Round2(units * mealRate);

    /// <summary>Total Individual Cost = mealCost + utilityShare.</summary>
    public static decimal TotalCost(decimal mealCost, decimal utilityShare) => Round2(mealCost + utilityShare);

    /// <summary>Surplus/Arrear = deposited − totalCost (positive = surplus).</summary>
    public static decimal Balance(decimal deposited, decimal totalCost) => Round2(deposited - totalCost);

    /// <summary>On-Hand = collected (approved deposits) − expenses (bazar + utility).</summary>
    public static decimal OnHand(decimal totalDeposits, decimal totalBazar, decimal totalUtility) =>
        Round2(totalDeposits - totalBazar - totalUtility);
}
