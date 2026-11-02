using FluentValidation;
using MealManager.Application.Abstractions;
using MealManager.Application.Common;
using MealManager.Application.Domain;
using MealManager.Application.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MealManager.Application.Features.Deposits;

/// <summary>
/// Record a deposit. Members deposit for themselves (starts Pending, needs manager approval);
/// manager-created deposits are auto-approved (P2 decision — ApprovedBy = creator).
/// </summary>
public record CreateDepositCommand(Guid? MemberId, decimal Amount, DateOnly Date, string? Note)
    : ITenantRequest, IRequest<DepositDto>;

public sealed class CreateDepositCommandValidator : AbstractValidator<CreateDepositCommand>
{
    public CreateDepositCommandValidator()
    {
        RuleFor(x => x.Amount).InclusiveBetween(0.01m, 999_999_999m);
        RuleFor(x => x.Date).NotEmpty();
        RuleFor(x => x.Note).MaximumLength(300);
    }
}

public sealed class CreateDepositCommandHandler(
    IApplicationContext db,
    ITenantContext tenant,
    ICurrentUser currentUser,
    ITotalsProjection projection)
    : IRequestHandler<CreateDepositCommand, DepositDto>
{
    public async Task<DepositDto> Handle(CreateDepositCommand request, CancellationToken cancellationToken)
    {
        var messId = tenant.MessId!.Value;
        var userId = currentUser.UserId!.Value;
        var isManager = tenant.Role == MessRole.MealManager;
        var memberId = request.MemberId ?? userId;

        await MonthLockGuard.RequireOpenAsync(db, messId, request.Date, cancellationToken);

        if (!isManager && memberId != userId)
        {
            throw new ForbiddenException("Members can only record their own deposits.");
        }

        if (memberId != userId)
        {
            var targetActive = await db.MembersMesses.AsNoTracking().IgnoreQueryFilters()
                .AnyAsync(mm => mm.MessId == messId && mm.MemberId == memberId
                    && mm.Status == MemberStatus.Active, cancellationToken);
            if (!targetActive)
            {
                throw new NotFoundException("The selected member is not an active member of this mess.");
            }
        }

        var now = DateTime.UtcNow;
        var deposit = new Deposit
        {
            Id = Guid.NewGuid(),
            MessId = messId,
            MemberId = memberId,
            Amount = request.Amount,
            Date = request.Date,
            Note = request.Note?.Trim(),
            CreatedAt = now,
        };
        if (isManager)
        {
            deposit.Status = DepositStatus.Approved;
            deposit.ApprovedBy = userId;
            deposit.ApprovedAt = now;
        }

        db.Deposits.Add(deposit);
        await db.SaveChangesAsync(cancellationToken);

        // Only approved money moves the totals (pending deposits contribute nothing).
        if (isManager)
        {
            await projection.DepositApprovedAsync(messId, request.Date, request.Amount, cancellationToken);
        }

        var memberName = await db.Members.AsNoTracking()
            .Where(u => u.Id == memberId)
            .Select(u => u.Name)
            .FirstAsync(cancellationToken);

        return new DepositDto(deposit.Id, deposit.MemberId, memberName, deposit.Amount, deposit.Date,
            deposit.Status.ToString(), deposit.ApprovedBy, deposit.ApprovedAt, deposit.Note, deposit.CreatedAt);
    }
}
