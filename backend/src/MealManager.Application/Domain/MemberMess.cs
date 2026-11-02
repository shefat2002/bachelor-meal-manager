namespace MealManager.Application.Domain;

public enum MessRole
{
    Member = 0,
    MealManager = 1,
}

public enum MemberStatus
{
    Pending = 0,
    Active = 1,
    Left = 2,
    Rejected = 3,
}

/// <summary>A user's membership in a mess — the mess-scoped role lives here, not in JWT claims.</summary>
public class MemberMess
{
    public Guid MessId { get; set; }

    public Guid MemberId { get; set; }

    public MessRole Role { get; set; } = MessRole.Member;

    public MemberStatus Status { get; set; } = MemberStatus.Pending;

    public DateTime JoinedAt { get; set; }

    public Guid? InvitedBy { get; set; }

    public Mess Mess { get; set; } = null!;

    public Member Member { get; set; } = null!;
}
