using System.Text.Json;
using MealManager.Application.Events;

namespace MealManager.Workers.Transport;

/// <summary>
/// Versioned wire envelope (skill bmm-rabbitmq-workers): { messageId, occurredAt, type, payload }.
/// Payload is raw JSON — consumers deserialize to the shared event records in Application/Events.
/// </summary>
public sealed record EventEnvelope(Guid MessageId, DateTime OccurredAt, string Type, string PayloadJson)
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.General);

    public static EventEnvelope Wrap<TEvent>(TEvent @event) where TEvent : notnull => new(
        Guid.NewGuid(),
        DateTime.UtcNow,
        EventNames.NameOf(@event),
        JsonSerializer.Serialize(@event, Options));

    public TEvent Unwrap<TEvent>() => JsonSerializer.Deserialize<TEvent>(PayloadJson, Options)!;
}

/// <summary>Compile-time-checked event-type mapping (no reflection guessing on the wire).</summary>
public static class EventNames
{
    private static readonly Dictionary<Type, string> Map = new()
    {
        [typeof(InvitationCreatedEvent)] = EventTypes.InvitationCreated,
        [typeof(JoinRequestedEvent)] = EventTypes.JoinRequested,
        [typeof(MemberApprovedEvent)] = EventTypes.MemberApproved,
        [typeof(MemberRejectedEvent)] = EventTypes.MemberRejected,
        [typeof(MemberRoleChangedEvent)] = EventTypes.MemberRoleChanged,
        [typeof(ReportRequestedEvent)] = EventTypes.ReportRequested,
        [typeof(ReportCompletedEvent)] = EventTypes.ReportCompleted,
    };

    public static string NameOf<TEvent>(TEvent @event) where TEvent : notnull =>
        Map.TryGetValue(@event.GetType(), out var type)
            ? type
            : throw new InvalidOperationException(
                $"Event type {@event.GetType().Name} is not mapped in {nameof(EventNames)}.");
}
