using FluentValidation.TestHelper;
using MealManager.Application.Features.Bazar;
using MealManager.Application.Features.Deposits;
using MealManager.Application.Features.Meals;
using MealManager.Application.Features.Utilities;

namespace MealManager.UnitTests;

/// <summary>P2 validator rules — the frontend Zod schemas mirror these 1:1 (skill bmm-forms-validation).</summary>
public class Phase2ValidatorTests
{
    private readonly SubmitDailyMealsCommandValidator _meals = new();
    private readonly CreateBazarExpensesCommandValidator _bazar = new();
    private readonly CreateUtilityTypeCommandValidator _utilityType = new();
    private readonly CreateUtilityPaymentCommandValidator _utilityPayment = new();
    private readonly CreateDepositCommandValidator _deposit = new();

    private static MealEntryRequest Entry(decimal breakfast = 1m, decimal lunch = 1m, decimal dinner = 1m) =>
        new(Guid.NewGuid(), breakfast, lunch, dinner);

    [Fact]
    public void Meals_HalfSteps_Pass()
    {
        var command = new SubmitDailyMealsCommand(new DateOnly(2026, 11, 1),
            [new MealEntryRequest(Guid.NewGuid(), 0m, 0.5m, 2.5m)]);
        _meals.TestValidate(command).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0.3)]   // not a 0.5 step
    [InlineData(-0.5)]  // below range
    [InlineData(10.5)]  // above range
    public void Meals_InvalidCounts_Fail(decimal breakfast)
    {
        var command = new SubmitDailyMealsCommand(new DateOnly(2026, 11, 1), [Entry(breakfast: breakfast)]);
        _meals.TestValidate(command).ShouldHaveValidationErrorFor("Entries[0].Breakfast");
    }

    [Fact]
    public void Meals_Empty_Entries_Fail()
    {
        var command = new SubmitDailyMealsCommand(new DateOnly(2026, 11, 1), []);
        _meals.TestValidate(command).ShouldHaveValidationErrorFor(x => x.Entries);
    }

    [Fact]
    public void Meals_Duplicate_Member_Fails()
    {
        var memberId = Guid.NewGuid();
        var command = new SubmitDailyMealsCommand(new DateOnly(2026, 11, 1),
            [new MealEntryRequest(memberId, 1m, 1m, 1m), new MealEntryRequest(memberId, 0m, 0m, 0m)]);
        _meals.TestValidate(command).ShouldHaveValidationErrorFor(x => x.Entries);
    }

    [Fact]
    public void Bazar_Valid_Items_Pass()
    {
        var command = new CreateBazarExpensesCommand(new DateOnly(2026, 11, 1), null,
            [new BazarItemRequest("Rice", 10m, 65m)]);
        _bazar.TestValidate(command).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(100000)]
    public void Bazar_Quantity_Out_Of_Range_Fails(decimal quantity)
    {
        var command = new CreateBazarExpensesCommand(new DateOnly(2026, 11, 1), null,
            [new BazarItemRequest("Rice", quantity, 65m)]);
        _bazar.TestValidate(command).ShouldHaveValidationErrorFor("Items[0].Quantity");
    }

    [Fact]
    public void Bazar_Empty_Name_And_Items_Fail()
    {
        var emptyItems = new CreateBazarExpensesCommand(new DateOnly(2026, 11, 1), null, []);
        _bazar.TestValidate(emptyItems).ShouldHaveValidationErrorFor(x => x.Items);

        var noName = new CreateBazarExpensesCommand(new DateOnly(2026, 11, 1), null,
            [new BazarItemRequest("", 1m, 65m)]);
        _bazar.TestValidate(noName).ShouldHaveValidationErrorFor("Items[0].Name");
    }

    [Fact]
    public void UtilityType_Name_Rules()
    {
        _utilityType.TestValidate(new CreateUtilityTypeCommand("", null))
            .ShouldHaveValidationErrorFor(x => x.Name);
        _utilityType.TestValidate(new CreateUtilityTypeCommand(new string('x', 101), null))
            .ShouldHaveValidationErrorFor(x => x.Name);
        _utilityType.TestValidate(new CreateUtilityTypeCommand("Electricity", "Monthly bill"))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UtilityPayment_Amount_Must_Be_Positive()
    {
        var date = new DateOnly(2026, 11, 1);
        _utilityPayment.TestValidate(new CreateUtilityPaymentCommand(Guid.NewGuid(), 0m, date, null))
            .ShouldHaveValidationErrorFor(x => x.Amount);
        _utilityPayment.TestValidate(new CreateUtilityPaymentCommand(Guid.NewGuid(), -5m, date, null))
            .ShouldHaveValidationErrorFor(x => x.Amount);
        _utilityPayment.TestValidate(new CreateUtilityPaymentCommand(Guid.NewGuid(), 5000m, date, null))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Deposit_Amount_Must_Be_Positive()
    {
        var date = new DateOnly(2026, 11, 1);
        _deposit.TestValidate(new CreateDepositCommand(null, 0m, date, null))
            .ShouldHaveValidationErrorFor(x => x.Amount);
        _deposit.TestValidate(new CreateDepositCommand(null, -1m, date, null))
            .ShouldHaveValidationErrorFor(x => x.Amount);
        _deposit.TestValidate(new CreateDepositCommand(null, 2000m, date, "Cash"))
            .ShouldNotHaveAnyValidationErrors();
    }
}
