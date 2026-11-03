namespace MealManager.Application.Events;

/// <summary>
/// Routing keys for the topic exchange bmm.events (skill bmm-rabbitmq-workers):
/// feature.eventName. Never rename — old consumers must keep working.
/// </summary>
public static class EventTypes
{
    public const string InvitationCreated = "members.invitationCreated";
    public const string JoinRequested = "members.joinRequested";
    public const string MemberApproved = "members.memberApproved";
    public const string MemberRejected = "members.memberRejected";
    public const string MemberRoleChanged = "members.memberRoleChanged";
    public const string ReportRequested = "report.monthlySettlement.requested";
    public const string ReportCompleted = "report.monthlySettlement.completed";
}

// Event payloads — shared by producers (Application handlers, post-commit) and consumers
// (MealManager.Workers). Keep them serializable-friendly: primitives + Guids only.

public sealed record InvitationCreatedEvent(
    Guid MessId,
    string MessName,
    string RecipientEmail,
    string InviteUrl,
    DateTime ExpiresAt);

public sealed record JoinRequestedEvent(Guid MessId, string MessName, Guid MemberId, string MemberName);

public sealed record MemberApprovedEvent(Guid MessId, string MessName, Guid MemberId);

public sealed record MemberRejectedEvent(Guid MessId, string MessName, Guid MemberId);

public sealed record MemberRoleChangedEvent(Guid MessId, string MessName, Guid MemberId, string Role);

public sealed record ReportRequestedEvent(
    Guid JobId,
    Guid MessId,
    string MessName,
    int Year,
    int Month,
    Guid RequestedBy);

public sealed record ReportCompletedEvent(
    Guid JobId,
    Guid MessId,
    string MessName,
    int Year,
    int Month,
    Guid RequestedBy);
