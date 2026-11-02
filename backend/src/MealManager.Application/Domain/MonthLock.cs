namespace MealManager.Application.Domain;

/// <summary>
/// Freezes a mess-month after settlement: one row per (MessId, Year, Month) means locked.
/// The summary captured at lock time is stored in SnapshotJson and served verbatim while locked.
/// Unlock deletes the row (live recompute resumes).
/// </summary>
public class MonthLock
{
    public Guid Id { get; set; }

    public Guid MessId { get; set; }

    public int Year { get; set; }

    public int Month { get; set; }

    public Guid LockedBy { get; set; }

    public DateTime LockedAt { get; set; }

    public string? SnapshotJson { get; set; }
}
