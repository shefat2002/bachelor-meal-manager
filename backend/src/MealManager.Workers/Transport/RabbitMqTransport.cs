using System.Text;
using MealManager.Application.Abstractions;
using MealManager.Application.Events;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace MealManager.Workers.Transport;

/// <summary>
/// RabbitMQ topology (skill bmm-rabbitmq-workers): topic exchange bmm.events; durable worker
/// queues with a dead-letter exchange bmm.dlx and one DLQ per queue. RabbitMQ.Client 7.x is
/// all-async (IChannel, CreateConnectionAsync).
/// </summary>
public static class RabbitMqTopology
{
    public const string Exchange = "bmm.events";
    public const string DeadLetterExchange = "bmm.dlx";
    public const string NotificationsQueue = "bmm.worker.notifications";
    public const string ReportsQueue = "bmm.worker.reports";

    /// <summary>All event routing keys each queue binds (topic patterns would also work).</summary>
    public static readonly IReadOnlyDictionary<string, string[]> QueueBindings = new Dictionary<string, string[]>
    {
        [NotificationsQueue] =
        [
            EventTypes.InvitationCreated,
            EventTypes.JoinRequested,
            EventTypes.MemberApproved,
            EventTypes.MemberRejected,
            EventTypes.MemberRoleChanged,
            EventTypes.ReportCompleted,
        ],
        [ReportsQueue] = [EventTypes.ReportRequested],
    };

    /// <summary>Which queue consumes an event type — shared by the broker bindings and the in-proc bus.</summary>
    public static string QueueFor(string eventType) =>
        QueueBindings.First(kv => kv.Value.Contains(eventType)).Key;

    public static async Task DeclareAsync(IChannel channel, CancellationToken ct)
    {
        await channel.ExchangeDeclareAsync(Exchange, ExchangeType.Topic, durable: true, cancellationToken: ct);
        await channel.ExchangeDeclareAsync(DeadLetterExchange, ExchangeType.Fanout, durable: true, cancellationToken: ct);

        foreach (var (queue, bindings) in QueueBindings)
        {
            // Dead-letter to the DLX with this queue as routing key → one DLQ per worker queue.
            await channel.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false,
                arguments: new Dictionary<string, object?>
                {
                    ["x-dead-letter-exchange"] = DeadLetterExchange,
                    ["x-dead-letter-routing-key"] = queue,
                },
                cancellationToken: ct);
            await channel.QueueDeclareAsync($"{queue}.dlq", durable: true, exclusive: false, autoDelete: false,
                cancellationToken: ct);
            await channel.QueueBindAsync($"{queue}.dlq", DeadLetterExchange, queue, cancellationToken: ct);

            foreach (var routingKey in bindings)
            {
                await channel.QueueBindAsync(queue, Exchange, routingKey, cancellationToken: ct);
            }
        }
    }
}

/// <summary>
/// Broker-backed IEventPublisher with publisher confirms. The connection/channel are recreated on
/// failure (a singleton channel can die with the broker); publishing retries once on a fresh channel.
/// </summary>
public sealed class RabbitMqEventPublisher(
    IConnectionFactory connectionFactory,
    ILogger<RabbitMqEventPublisher> logger) : IEventPublisher, IAsyncDisposable
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IConnection? _connection;
    private IChannel? _channel;

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : notnull
    {
        var envelope = EventEnvelope.Wrap(@event);
        var body = Encoding.UTF8.GetBytes(envelope.PayloadJson);
        try
        {
            var channel = await GetChannelAsync(cancellationToken);
            await channel.BasicPublishAsync(
                RabbitMqTopology.Exchange,
                envelope.Type,
                mandatory: true,
                new BasicProperties
                {
                    MessageId = envelope.MessageId.ToString(),
                    ContentType = "application/json",
                    DeliveryMode = DeliveryModes.Persistent,
                    Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                },
                body,
                cancellationToken);
        }
        catch (Exception ex)
        {
            // Post-commit rule: log, never fail the committed write. One reconnect+retry attempt.
            logger.LogWarning(ex, "RabbitMQ publish failed for {EventType} {MessageId}, retrying once",
                envelope.Type, envelope.MessageId);
            try
            {
                await ResetAsync(cancellationToken);
                var channel = await GetChannelAsync(cancellationToken);
                await channel.BasicPublishAsync(RabbitMqTopology.Exchange, envelope.Type, mandatory: true,
                    new BasicProperties { MessageId = envelope.MessageId.ToString(), ContentType = "application/json", DeliveryMode = DeliveryModes.Persistent },
                    body, cancellationToken);
            }
            catch (Exception retryEx)
            {
                logger.LogError(retryEx, "RabbitMQ publish retry failed for {EventType} {MessageId} — event dropped",
                    envelope.Type, envelope.MessageId);
            }
        }
    }

    private async Task<IChannel> GetChannelAsync(CancellationToken ct)
    {
        await _lock.WaitAsync(ct);
        try
        {
            if (_connection is { IsOpen: true } && _channel is { IsOpen: true })
            {
                return _channel;
            }

            _connection ??= await connectionFactory.CreateConnectionAsync(ct);
            _channel = await _connection.CreateChannelAsync(cancellationToken: ct);
            await RabbitMqTopology.DeclareAsync(_channel, ct);
            return _channel;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task ResetAsync(CancellationToken ct)
    {
        await _lock.WaitAsync(ct);
        try
        {
            if (_channel is not null)
            {
                await _channel.DisposeAsync();
            }

            _channel = null;
            if (_connection is { IsOpen: false })
            {
                await _connection.DisposeAsync();
                _connection = null;
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
        {
            await _channel.DisposeAsync();
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        _lock.Dispose();
    }
}
