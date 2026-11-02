using FluentAssertions;
using FluentValidation.TestHelper;
using MealManager.Application.Features.Summary;
using MealManager.Grains;

namespace MealManager.UnitTests;

/// <summary>P3: the §5 equations as pure decimal math (hand-checked against the e2e dataset).</summary>
public class SettlementMathTests
{
    [Fact]
    public void MealRate_Is_Bazar_Over_Units_Rounded_4dp()
    {
        SettlementMath.MealRate(930m, 13.5m).Should().Be(68.8889m);
        SettlementMath.MealRate(500m, 50m).Should().Be(10m);
        SettlementMath.MealRate(100m, 0m).Should().Be(0m, "no meals eaten → rate 0, never divide-by-zero");
    }

    [Fact]
    public void UtilityShare_Is_Equal_Split_Rounded_2dp()
    {
        SettlementMath.UtilityShare(800m, 3).Should().Be(266.67m);
        SettlementMath.UtilityShare(500m, 2).Should().Be(250m);
        SettlementMath.UtilityShare(800m, 0).Should().Be(0m, "no active members → share 0");
    }

    [Fact]
    public void Member_Row_Computes_The_Equation_Chain()
    {
        // A: units 5.5, rate 68.8889 → 378.89 + 266.67 = 645.56; deposited 400 → −245.56
        var mealCost = SettlementMath.MealCost(5.5m, 68.8889m);
        mealCost.Should().Be(378.89m);
        var totalCost = SettlementMath.TotalCost(mealCost, 266.67m);
        totalCost.Should().Be(645.56m);
        SettlementMath.Balance(400m, totalCost).Should().Be(-245.56m);
    }

    [Fact]
    public void OnHand_Is_Collected_Minus_Expenses()
    {
        SettlementMath.OnHand(1100m, 930m, 800m).Should().Be(-630m);
        SettlementMath.OnHand(1000m, 500m, 0m).Should().Be(500m);
    }

    [Fact]
    public void Summary_And_MonthLock_Validators_Reject_Bad_Months()
    {
        new GetMessSummaryQueryValidator().TestValidate(new GetMessSummaryQuery(Guid.NewGuid(), 2026, 13))
            .ShouldHaveValidationErrorFor(x => x.Month);
        new GetMessSummaryQueryValidator().TestValidate(new GetMessSummaryQuery(Guid.NewGuid(), 2026, 11))
            .ShouldNotHaveAnyValidationErrors();
        new SetMonthLockCommandValidator().TestValidate(new SetMonthLockCommand(Guid.NewGuid(), 1999, 11, true))
            .ShouldHaveValidationErrorFor(x => x.Year);
    }
}
