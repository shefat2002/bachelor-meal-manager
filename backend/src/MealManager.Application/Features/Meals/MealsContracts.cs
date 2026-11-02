namespace MealManager.Application.Features.Meals;

public record DailyMealDto(
    Guid Id,
    Guid MemberId,
    DateOnly Date,
    decimal Breakfast,
    decimal Lunch,
    decimal Dinner,
    decimal TotalUnits,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record MealEntryRequest(Guid MemberId, decimal Breakfast, decimal Lunch, decimal Dinner);

public record SubmitDailyMealsRequest(DateOnly Date, IReadOnlyList<MealEntryRequest> Entries);
