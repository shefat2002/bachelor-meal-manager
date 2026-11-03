using System.Text.Json;
using MealManager.Application.Abstractions;
using MealManager.Application.Domain;
using MealManager.Application.Events;
using MealManager.Application.Features.Summary;
using MealManager.Workers.Consuming;
using MealManager.Workers.Reports;
using Microsoft.Extensions.Hosting;
using MealManager.Workers.Transport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MealManager.Workers;

/// <summary>
/// Consumes report.monthlySettlement.requested (skill bmm-rabbitmq-workers): renders the §5 summary
/// to PDF via QuestPDF, stores the artifact, marks the job Completed and publishes report.completed
/// (which the NotificationWorker turns into the requester's in-app "ready to download" alert).
/// </summary>
public sealed class ReportWorker(
    InProcessEventBus bus,
    IServiceScopeFactory scopeFactory,
    IEventPublisher publisher,
    ILogger<ReportWorker> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions SnapshotJsonOptions = new(JsonSerializerDefaults.Web);
    private readonly MessageProcessor _processor = new(scopeFactory, RabbitMqTopology.ReportsQueue, logger);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var envelope in bus.DequeueAllAsync(RabbitMqTopology.ReportsQueue, stoppingToken))
        {
            await _processor.ProcessAsync(
                envelope,
                (db, ct) => GenerateAsync(db, envelope.Unwrap<ReportRequestedEvent>(), ct),
                stoppingToken);
        }
    }

    private async Task GenerateAsync(IApplicationContext db, ReportRequestedEvent @event, CancellationToken ct)
    {
        var job = await db.ReportJobs.IgnoreQueryFilters()
            .FirstOrDefaultAsync(j => j.Id == @event.JobId, ct);
        if (job is null)
        {
            logger.LogWarning("Report job {JobId} vanished — skipping", @event.JobId);
            return;
        }

        job.Status = ReportJobStatus.Running;
        await db.SaveChangesAsync(ct);

        try
        {
            var summary = await LoadSummaryAsync(db, @event, ct);
            var messName = await db.Messes.AsNoTracking().IgnoreQueryFilters()
                .Where(m => m.Id == @event.MessId)
                .Select(m => m.Name)
                .FirstAsync(ct);

            var pdf = SettlementPdfRenderer.Render(summary, messName);
            db.ReportFiles.Add(new ReportFile
            {
                Id = Guid.NewGuid(),
                ReportJobId = job.Id,
                ContentType = "application/pdf",
                Content = pdf,
                CreatedAt = DateTime.UtcNow,
            });
            job.Status = ReportJobStatus.Completed;
            job.CompletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            await publisher.PublishAsync(new ReportCompletedEvent(
                job.Id, @event.MessId, messName, @event.Year, @event.Month, @event.RequestedBy), ct);
        }
        catch (Exception ex)
        {
            job.Status = ReportJobStatus.Failed;
            job.Error = ex.Message.Length > 1000 ? ex.Message[..1000] : ex.Message;
            job.CompletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            throw; // let the processor retry/metric/log — a later attempt may succeed transiently
        }
    }

    /// <summary>Locked months render their frozen snapshot; live months read through the grain/cache.</summary>
    private async Task<MessSummaryDto> LoadSummaryAsync(
        IApplicationContext db, ReportRequestedEvent @event, CancellationToken ct)
    {
        var lockRow = await db.MonthLocks.AsNoTracking().IgnoreQueryFilters()
            .FirstOrDefaultAsync(l => l.MessId == @event.MessId && l.Year == @event.Year && l.Month == @event.Month, ct);
        if (lockRow?.SnapshotJson is { Length: > 0 } snapshot
            && JsonSerializer.Deserialize<MessSummaryDto>(snapshot, SnapshotJsonOptions) is { } frozen)
        {
            return frozen;
        }

        // Read-side port is scoped — resolve per job, outside the processor's db scope.
        using var scope = scopeFactory.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<ISummaryProvider>();
        return await provider.GetAsync(@event.MessId, @event.Year, @event.Month, ct);
    }
}
