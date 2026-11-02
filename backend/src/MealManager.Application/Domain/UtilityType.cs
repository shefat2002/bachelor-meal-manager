namespace MealManager.Application.Domain;

/// <summary>Manager-defined utility category per mess (Electricity, Wi-Fi, Cook Salary…).</summary>
public class UtilityType
{
    public Guid Id { get; set; }

    public Guid MessId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Soft-disable switch — payment history keeps referencing deactivated types.</summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
}
