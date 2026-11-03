using System.Text;
using Microsoft.Extensions.Hosting;
using MealManager.Workers.Transport;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MealManager.Workers.Consuming;

/// <summary>
/// Broker mode bridge: drains a RabbitMQ queue into the InProcessEventBus so worker loops are
/// identical in both modes. Malformed deliveries are nacked without requeue → the queue's
/// dead-letter settings route them to its DLQ.
/// </summary>
public sealed class RabbitMqConsumerBridge(
    string queueName,
    InProcessEventBus bus,
    IConnectionFactory connectionFactory,
    ILogger<RabbitMqConsumerBridge> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var connection = await connectionFactory.CreateConnectionAsync(stoppingToken);
                await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
                await RabbitMqTopology.DeclareAsync(channel, stoppingToken);
                await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 4, global: false, stoppingToken);

                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.ReceivedAsync += async (_, eventArgs) =>
                {
                    try
                    {
                        var messageId = Guid.TryParse(eventArgs.BasicProperties.MessageId, out var parsed)
                            ? parsed
                            : Guid.NewGuid();
                        var envelope = new EventEnvelope(
                            messageId,
                            DateTimeOffset.FromUnixTimeSeconds(eventArgs.BasicProperties.Timestamp.UnixTime).UtcDateTime,
                            eventArgs.RoutingKey,
                            Encoding.UTF8.GetString(eventArgs.Body.ToArray()));
                        await bus.EnqueueAsync(envelope, stoppingToken);
                        await channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Malformed delivery on {Queue} → DLQ", queueName);
                        await channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: false);
                    }
                };

                await channel.BasicConsumeAsync(queue: queueName, autoAck: false, consumer: consumer, stoppingToken);
                logger.LogInformation("RabbitMQ consumer bridge started on {Queue}", queueName);
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "RabbitMQ bridge {Queue} disconnected — reconnecting in 5s", queueName);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
