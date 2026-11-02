using System.Text.Json;
using FluentValidation;
using MealManager.Application.Abstractions;
using MealManager.Application.Domain;
using MealManager.Application.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MealManager.Application.Features.Summary;

/// <summary>
/// Manager-only month lock/unlock (P3-BE-5). Locking freezes every write dated in that month (409),
/// captures the current summary as a durable snapshot, and evicts grain state; unlocking deletes the
/// snapshot so the month recomputes live.
/// </summary>
public record SetMonthLockCommand(Guid MessId, int Year, int Month, bool Locked) : ITenantRequest, IRequest<Unit>;

public sealed class SetMonthLockCommandValidator : AbstractValidator<SetMonthLockCommand>
{
    public SetMonthLockCommandValidator()
    {
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
    }
}

public sealed class SetMonthLockCommandHandler(
    IApplicationContext db,
    ITenantContext tenant,
    ICurrentUser currentUser,
    ISummaryProvider summaryProvider,
    ITotalsProjection projection)
    : IRequestHandler<SetMonthLockCommand, Unit>
{
    private static readonly JsonSerializerOptions SnapshotJsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<Unit> Handle(SetMonthLockCommand request, CancellationToken cancellationToken)
    {
        if (request.MessId != tenant.MessId!.Value)
        {
            throw new NotFoundException("Mess not found.");
        }

        TenantGuards.RequireManager(tenant, "lock months");

        var existing = await db.MonthLocks.IgnoreQueryFilters()
            .FirstOrDefaultAsync(l => l.MessId == request.MessId && l.Year == request.Year && l.Month == request.Month, cancellationToken);

        if (request.Locked)
        {
            // Fresh computation (cache dropped first), then frozen as the month's durable snapshot.
            await summaryProvider.InvalidateAsync(request.MessId, request.Year, request.Month, cancellationToken);
            var summary = await summaryProvider.GetAsync(request.MessId, request.Year, request.Month, cancellationToken);

            var snapshotJson = JsonSerializer.Serialize(summary, SnapshotJsonOptions);
            if (existing is null)
            {
                db.MonthLocks.Add(new MonthLock
                {
                    Id = Guid.NewGuid(),
                    MessId = request.MessId,
                    Year = request.Year,
                    Month = request.Month,
                    LockedBy = currentUser.UserId!.Value,
                    LockedAt = DateTime.UtcNow,
                    SnapshotJson = snapshotJson,
                });
            }
            else
            {
                existing.SnapshotJson = snapshotJson;
            }
        }
        else if (existing is not null)
        {
            db.MonthLocks.Remove(existing);
        }

        await db.SaveChangesAsync(cancellationToken);
        await projection.MonthLockChangedAsync(request.MessId, request.Year, request.Month, request.Locked, cancellationToken);
        return Unit.Value;
    }
}
