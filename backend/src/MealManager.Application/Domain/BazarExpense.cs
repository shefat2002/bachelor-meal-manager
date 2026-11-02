namespace MealManager.Application.Domain;

/// <summary>One purchased bazar item line; a shopping trip is a batch of rows sharing date + buyer.</summary>
public class BazarExpense
{
    public Guid Id { get; set; }

    public Guid MessId { get; set; }

    public Guid BuyerMemberId { get; set; }

    public string ItemName { get; set; } = string.Empty;

    /// <summary>Units bought; the line total (Quantity × Price) is computed, never stored.</summary>
    public decimal Quantity { get; set; } = 1m;

    /// <summary>Unit price (decimal(18,2)).</summary>
    public decimal Price { get; set; }

    public DateOnly Date { get; set; }

    public DateTime CreatedAt { get; set; }
}
