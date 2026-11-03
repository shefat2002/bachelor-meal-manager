using System.Text.Json;
using MealManager.Application.Abstractions;
using MealManager.Application.Domain;
using MealManager.Application.Events;
using MealManager.Workers.Consuming;
using MealManager.Workers.Emails;
using Microsoft.Extensions.Hosting;
using MealManager.Workers.Transport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MealManager.Workers;

/// <summary>
/// Consumer for human-facing side effects (skill bmm-rabbitmq-workers): invitation emails, manager
/// approval prompts, member approval/rejection/role-change alerts, and report-ready notifications.
/// Consumes the InProcessEventBus in both modes (the RabbitMqConsumerBridge feeds it in broker mode).
/// </summary>
public sealed class NotificationWorker(
    InProcessEventBus bus,
    IServiceScopeFactory scopeFactory,
    IEmailSender emailSender,
    ILogger<NotificationWorker> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions DataJsonOptions = new(JsonSerializerDefaults.Web);
    private readonly MessageProcessor _processor = new(scopeFactory, RabbitMqTopology.NotificationsQueue, logger);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var envelope in bus.DequeueAllAsync(RabbitMqTopology.NotificationsQueue, stoppingToken))
        {
            await _processor.ProcessAsync(envelope, (db, ct) => DispatchAsync(db, envelope, ct), stoppingToken);
        }
    }

    private async Task DispatchAsync(IApplicationContext db, EventEnvelope envelope, CancellationToken ct)
    {
        switch (envelope.Type)
        {
            case EventTypes.InvitationCreated:
                await HandleInviteAsync(db, envelope.Unwrap<InvitationCreatedEvent>(), ct);
                break;
            case EventTypes.JoinRequested:
                await HandleJoinAsync(db, envelope.Unwrap<JoinRequestedEvent>(), ct);
                break;
            case EventTypes.MemberApproved:
            {
                var e = envelope.Unwrap<MemberApprovedEvent>();
                await HandleDecisionAsync(db, e.MessId, e.MemberId, approved: true, ct);
                break;
            }
            case EventTypes.MemberRejected:
            {
                var e = envelope.Unwrap<MemberRejectedEvent>();
                await HandleDecisionAsync(db, e.MessId, e.MemberId, approved: false, ct);
                break;
            }
            case EventTypes.MemberRoleChanged:
                await HandleRoleAsync(db, envelope.Unwrap<MemberRoleChangedEvent>(), ct);
                break;
            case EventTypes.ReportCompleted:
                await HandleReportReadyAsync(db, envelope.Unwrap<ReportCompletedEvent>(), ct);
                break;
            // Unknown types (queue misbinding) are acked silently.
        }
    }

    private async Task HandleInviteAsync(IApplicationContext db, InvitationCreatedEvent @event, CancellationToken ct)
    {
        // Email-only side effect; the sender is best-effort (never throws).
        await emailSender.SendAsync(
            @event.RecipientEmail,
            $"You're invited to join {@event.MessName}",
            EmailTemplates.Invitation(@event.MessName, @event.InviteUrl, @event.ExpiresAt),
            ct);
    }

    private async Task HandleJoinAsync(IApplicationContext db, JoinRequestedEvent @event, CancellationToken ct)
    {
        var managerIds = await db.MembersMesses.AsNoTracking().IgnoreQueryFilters()
            .Where(mm => mm.MessId == @event.MessId && mm.Role == MessRole.MealManager && mm.Status == MemberStatus.Active)
            .Select(mm => mm.MemberId)
            .ToListAsync(ct);
        foreach (var managerId in managerIds)
        {
            db.Notifications.Add(Notification(
                managerId, @event.MessId, "joinRequested",
                $"{@event.MemberName} requested to join",
                $"Approve or reject {@event.MemberName}'s request to join {@event.MessName} on the members page."));
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task HandleDecisionAsync(IApplicationContext db, Guid messId, Guid memberId, bool approved, CancellationToken ct)
    {
        var member = await db.Members.AsNoTracking()
            .Where(u => u.Id == memberId)
            .Select(u => new { u.Name, u.Email })
            .FirstOrDefaultAsync(ct);
        if (member is null)
        {
            return;
        }

        var mess = await db.Messes.AsNoTracking().IgnoreQueryFilters()
            .Where(m => m.Id == messId)
            .Select(m => m.Name)
            .FirstOrDefaultAsync(ct) ?? "your mess";

        db.Notifications.Add(Notification(
            memberId, messId,
            approved ? "memberApproved" : "memberRejected",
            approved ? $"You're in — {mess}" : $"Request declined — {mess}",
            approved
                ? $"Your request to join {mess} was approved. Start logging your meals!"
                : $"The manager declined your request to join {mess}."));
        await db.SaveChangesAsync(ct);

        if (!string.IsNullOrEmpty(member.Email))
        {
            await emailSender.SendAsync(
                member.Email,
                approved ? $"Welcome to {mess}!" : $"Your request to join {mess} was declined",
                EmailTemplates.MembershipDecision(mess, approved),
                ct);
        }
    }

    private async Task HandleRoleAsync(IApplicationContext db, MemberRoleChangedEvent @event, CancellationToken ct)
    {
        var member = await db.Members.AsNoTracking()
            .Where(u => u.Id == @event.MemberId)
            .Select(u => new { u.Name, u.Email })
            .FirstOrDefaultAsync(ct);
        if (member is null)
        {
            return;
        }

        db.Notifications.Add(Notification(
            @event.MemberId, @event.MessId, "roleChanged",
            $"You are now a {@event.Role} of {@event.MessName}",
            $"Your role in {@event.MessName} changed to {@event.Role}."));
        await db.SaveChangesAsync(ct);

        if (!string.IsNullOrEmpty(member.Email))
        {
            await emailSender.SendAsync(
                member.Email,
                $"Your role in {@event.MessName} changed",
                EmailTemplates.RoleChanged(@event.MessName, @event.Role),
                ct);
        }
    }

    private async Task HandleReportReadyAsync(IApplicationContext db, ReportCompletedEvent @event, CancellationToken ct)
    {
        db.Notifications.Add(Notification(
            @event.RequestedBy, @event.MessId, "reportReady",
            $"{MonthName(@event.Year, @event.Month)} settlement ready",
            $"The {MonthName(@event.Year, @event.Month)} settlement report for {@event.MessName} is ready to download.",
            JsonSerializer.Serialize(new { jobId = @event.JobId, year = @event.Year, month = @event.Month }, DataJsonOptions)));
        await db.SaveChangesAsync(ct);
    }

    private static Notification Notification(
        Guid userId, Guid? messId, string type, string title, string body, string? dataJson = null) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        MessId = messId,
        Type = type,
        Title = title,
        Body = body,
        DataJson = dataJson,
        CreatedAt = DateTime.UtcNow,
    };

    private static string MonthName(int year, int month) =>
        new DateOnly(year, month, 1).ToString("MMMM yyyy");
}
