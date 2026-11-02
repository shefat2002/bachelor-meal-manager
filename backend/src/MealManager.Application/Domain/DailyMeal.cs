namespace MealManager.Application.Domain;

/// <summary>
/// One member's meal counts for one date. Counts are 0–10 in 0.5 steps and include guest meals
/// (P2 decision: guests fold into the B/L/D counts rather than a separate column).
/// </summary>
public class DailyMeal
{
    public Guid Id { get; set; }

    public Guid MessId { get; set; }

    public Guid MemberId { get; set; }

    public DateOnly Date { get; set; }

    public decimal Breakfast { get; set; }

    public decimal Lunch { get; set; }

    public decimal Dinner { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
