using System.Threading.Channels;
using MealManager.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace MealManager.Workers.Transport;

/// <summary>
/// In-process transport fallback (no Docker/RabbitMQ locally or in test hosts). Mirrors the broker
/// topology: one bounded channel per worker queue, envelopes routed by event type exactly like the
/// RabbitMQ queue bindings — so each worker consumes its own stream and broker mode swaps in without
/// touching producers or consumers.
/// </summary>
public sealed class InProcessEventBus
{
    private readonly Dictionary<string, Channel<EventEnvelope>> _channels =
        RabbitMqTopology.QueueBindings.ToDictionary(
            kv => kv.Key,
            _ => Channel.CreateBounded<EventEnvelope>(
                new BoundedChannelOptions(1024) { FullMode = BoundedChannelFullMode.Wait }));

    public ValueTask EnqueueAsync(EventEnvelope envelope, CancellationToken ct = default) =>
        _channels[RabbitMqTopology.QueueFor(envelope.Type)].Writer.WriteAsync(envelope, ct);

    public IAsyncEnumerable<EventEnvelope> DequeueAllAsync(string queueName, CancellationToken ct) =>
        _channels[queueName].Reader.ReadAllAsync(ct);
}

public sealed class InProcessEventPublisher(InProcessEventBus bus, ILogger<InProcessEventPublisher> logger)
    : IEventPublisher
{
    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : notnull
    {
        try
        {
            await bus.EnqueueAsync(EventEnvelope.Wrap(@event), cancellationToken);
        }
        catch (Exception ex)
        {
            // Post-commit only — never fail a committed write because of the transport.
            logger.LogWarning(ex, "In-process publish failed for {EventType}", @event.GetType().Name);
        }
    }
}
