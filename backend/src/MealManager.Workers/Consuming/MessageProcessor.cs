using MealManager.Application.Abstractions;
using MealManager.Application.Common;
using MealManager.Application.Domain;
using MealManager.Workers.Transport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MealManager.Workers.Consuming;

/// <summary>
/// One message: idempotency check (ProcessedMessages), then the handler with bounded retries and
/// backoff (skill bmm-rabbitmq-workers — never infinite retry). Metrics + structured logs carry the
/// message id/type at INFO on start/finish/failure. Handlers are DB-only writes — the email sender is
/// best-effort and never throws — so a retry cannot double-send side effects.
/// </summary>
public sealed class MessageProcessor(
    IServiceScopeFactory scopeFactory,
    string queueName,
    ILogger logger)
{
    private const int MaxAttempts = 3;

    private static readonly TimeSpan[] Backoff =
    [
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(4),
    ];

    public async Task ProcessAsync(EventEnvelope envelope, Func<IApplicationContext, CancellationToken, Task> handler, CancellationToken ct)
    {
        BmmMetrics.MessagesConsumed.Add(1, new KeyValuePair<string, object?>("queue", queueName));
        logger.LogInformation("Consuming {EventType} {MessageId} on {Queue}", envelope.Type, envelope.MessageId, queueName);

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationContext>();

        if (await db.ProcessedMessages.AsNoTracking().AnyAsync(pm => pm.MessageId == envelope.MessageId, ct))
        {
            logger.LogInformation("Skipping redelivered {EventType} {MessageId} — already processed", envelope.Type, envelope.MessageId);
            return;
        }

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await handler(db, ct);
                db.ProcessedMessages.Add(new ProcessedMessage
                {
                    MessageId = envelope.MessageId,
                    Type = envelope.Type,
                    ProcessedAt = DateTime.UtcNow,
                });
                await db.SaveChangesAsync(ct);
                logger.LogInformation("Processed {EventType} {MessageId} on {Queue}", envelope.Type, envelope.MessageId, queueName);
                return;
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                BmmMetrics.MessageFailures.Add(1, new KeyValuePair<string, object?>("queue", queueName));
                if (attempt >= MaxAttempts)
                {
                    // Poison exhausted: broker mode dead-letters malformed deliveries at the bridge;
                    // handler-level failures land here — recorded and dropped, never blocking the queue.
                    logger.LogError(ex, "Dropping poisoned {EventType} {MessageId} on {Queue} after {Max} attempts",
                        envelope.Type, envelope.MessageId, queueName, MaxAttempts);
                    return;
                }

                logger.LogWarning(ex, "Attempt {Attempt}/{Max} failed for {EventType} {MessageId}",
                    attempt, MaxAttempts, envelope.Type, envelope.MessageId);
                await Task.Delay(Backoff[attempt - 1], ct);
            }
        }
    }
}
