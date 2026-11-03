namespace MealManager.Application.Domain;

/// <summary>
/// In-app notification inbox entry — user-scoped (UserId), NOT tenant-filtered: a user reads all
/// their notifications regardless of which mess they're browsing. DataJson carries type-specific
/// payloads (e.g. { "jobId": "…", "year": 2026, "month": 11 } for reportReady).
/// </summary>
public class Notification
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid? MessId { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public string? DataJson { get; set; }

    public DateTime? ReadAt { get; set; }

    public DateTime CreatedAt { get; set; }
}

public enum ReportJobStatus
{
    Pending = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
}

/// <summary>Monthly settlement PDF job — Pending → Running → Completed/Failed, polled by the frontend.</summary>
public class ReportJob
{
    public Guid Id { get; set; }

    public Guid MessId { get; set; }

    public int Year { get; set; }

    public int Month { get; set; }

    public ReportJobStatus Status { get; set; } = ReportJobStatus.Pending;

    public Guid RequestedBy { get; set; }

    public string? Error { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }
}

/// <summary>The rendered PDF artifact — stored as bytea; small monthly artifacts, DB is fine.</summary>
public class ReportFile
{
    public Guid Id { get; set; }

    public Guid ReportJobId { get; set; }

    public string ContentType { get; set; } = "application/pdf";

    public byte[] Content { get; set; } = [];

    public DateTime CreatedAt { get; set; }

    public ReportJob ReportJob { get; set; } = null!;
}

/// <summary>
/// Consumer idempotency (skill bmm-rabbitmq-workers): row existence means the message was handled;
/// redeliveries are skipped. A re-sent invitation email is annoying; a double report is a bug.
/// </summary>
public class ProcessedMessage
{
    public Guid MessageId { get; set; }

    public string Type { get; set; } = string.Empty;

    public DateTime ProcessedAt { get; set; }
}
