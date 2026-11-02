namespace MealManager.Application.Domain;

public enum DepositStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
}

/// <summary>A member's cash/digital contribution toward the mess balance. Member-created deposits start Pending; manager-created deposits are auto-approved (P2 decision).</summary>
public class Deposit
{
    public Guid Id { get; set; }

    public Guid MessId { get; set; }

    public Guid MemberId { get; set; }

    public decimal Amount { get; set; }

    public DateOnly Date { get; set; }

    public DepositStatus Status { get; set; } = DepositStatus.Pending;

    public Guid? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }
}
