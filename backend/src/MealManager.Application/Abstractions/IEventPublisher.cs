namespace MealManager.Application.Abstractions;

/// <summary>
/// Publishes domain events post-commit (skill bmm-rabbitmq-workers): the DB transaction has already
/// committed when handlers call this — never publish for writes that may roll back. Implementations
/// wrap payloads in the versioned envelope { messageId, occurredAt, type, payload } and route to
/// RabbitMQ (bmm.events topic exchange) or the in-process channel fallback. Failures are logged and
/// swallowed: a committed write must not fail because the broker hiccuped.
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : notnull;
}
