namespace MealManager.Application.Features.Bazar;

public record BazarItemRequest(string Name, decimal Quantity, decimal Price);

public record CreateBazarExpensesRequest(DateOnly Date, Guid? BuyerMemberId, IReadOnlyList<BazarItemRequest> Items);

public record BazarExpenseDto(
    Guid Id,
    Guid BuyerMemberId,
    string ItemName,
    decimal Quantity,
    decimal Price,
    decimal LineTotal,
    DateOnly Date,
    DateTime CreatedAt);
