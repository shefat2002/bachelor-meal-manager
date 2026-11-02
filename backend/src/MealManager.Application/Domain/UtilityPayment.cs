namespace MealManager.Application.Domain;

/// <summary>A periodic utility payment (electricity bill, cook salary…) tied to a mess-defined type.</summary>
public class UtilityPayment
{
    public Guid Id { get; set; }

    public Guid MessId { get; set; }

    public Guid UtilityTypeId { get; set; }

    public decimal Amount { get; set; }

    public DateOnly PaymentDate { get; set; }

    public string? Note { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }
}
