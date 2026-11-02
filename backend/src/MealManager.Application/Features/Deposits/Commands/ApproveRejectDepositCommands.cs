using MealManager.Application.Abstractions;
using MealManager.Application.Common;
using MealManager.Application.Domain;
using MealManager.Application.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MealManager.Application.Features.Deposits;

public record ApproveDepositCommand(Guid Id) : ITenantRequest, IRequest<Unit>;

public record RejectDepositCommand(Guid Id) : ITenantRequest, IRequest<Unit>;

public sealed class ApproveDepositCommandHandler(IApplicationContext db, ITenantContext tenant, ICurrentUser currentUser, ITotalsProjection projection)
    : IRequestHandler<ApproveDepositCommand, Unit>
{
    public async Task<Unit> Handle(ApproveDepositCommand request, CancellationToken cancellationToken)
    {
        await DepositReview.SetStatusAsync(db, tenant, currentUser, projection, request.Id, DepositStatus.Approved, "approve", cancellationToken);
        return Unit.Value;
    }
}

public sealed class RejectDepositCommandHandler(IApplicationContext db, ITenantContext tenant, ICurrentUser currentUser, ITotalsProjection projection)
    : IRequestHandler<RejectDepositCommand, Unit>
{
    public async Task<Unit> Handle(RejectDepositCommand request, CancellationToken cancellationToken)
    {
        await DepositReview.SetStatusAsync(db, tenant, currentUser, projection, request.Id, DepositStatus.Rejected, "reject", cancellationToken);
        return Unit.Value;
    }
}

internal static class DepositReview
{
    public static async Task SetStatusAsync(
        IApplicationContext db,
        ITenantContext tenant,
        ICurrentUser currentUser,
        ITotalsProjection projection,
        Guid depositId,
        DepositStatus target,
        string action,
        CancellationToken ct)
    {
        TenantGuards.RequireManager(tenant, $"{action} deposits");

        var deposit = await db.Deposits.IgnoreQueryFilters()
            .FirstOrDefaultAsync(d => d.MessId == tenant.MessId!.Value && d.Id == depositId, ct)
            ?? throw new NotFoundException("Deposit not found.");

        if (deposit.Status != DepositStatus.Pending)
        {
            throw new ConflictException($"Only pending deposits can be {action}d.");
        }

        // Approving moves money INTO the month's collected total — a frozen month stays frozen.
        if (target == DepositStatus.Approved)
        {
            await MonthLockGuard.RequireOpenAsync(db, tenant.MessId!.Value, deposit.Date, ct);
        }

        deposit.Status = target;
        if (target == DepositStatus.Approved)
        {
            deposit.ApprovedBy = currentUser.UserId!.Value;
            deposit.ApprovedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);

        if (target == DepositStatus.Approved)
        {
            await projection.DepositApprovedAsync(tenant.MessId!.Value, deposit.Date, deposit.Amount, ct);
        }
    }
}
