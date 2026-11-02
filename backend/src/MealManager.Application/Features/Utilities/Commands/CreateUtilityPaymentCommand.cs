using FluentValidation;
using MealManager.Application.Abstractions;
using MealManager.Application.Common;
using MealManager.Application.Domain;
using MealManager.Application.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MealManager.Application.Features.Utilities;

/// <summary>Manager-only: record a utility payment against a mess-defined type.</summary>
public record CreateUtilityPaymentCommand(Guid UtilityTypeId, decimal Amount, DateOnly PaymentDate, string? Note)
    : ITenantRequest, IRequest<UtilityPaymentDto>;

public sealed class CreateUtilityPaymentCommandValidator : AbstractValidator<CreateUtilityPaymentCommand>
{
    public CreateUtilityPaymentCommandValidator()
    {
        RuleFor(x => x.UtilityTypeId).NotEmpty();
        RuleFor(x => x.PaymentDate).NotEmpty();
        RuleFor(x => x.Amount).InclusiveBetween(0.01m, 999_999_999m);
        RuleFor(x => x.Note).MaximumLength(300);
    }
}

public sealed class CreateUtilityPaymentCommandHandler(
    IApplicationContext db,
    ITenantContext tenant,
    ICurrentUser currentUser,
    ITotalsProjection projection)
    : IRequestHandler<CreateUtilityPaymentCommand, UtilityPaymentDto>
{
    public async Task<UtilityPaymentDto> Handle(CreateUtilityPaymentCommand request, CancellationToken cancellationToken)
    {
        TenantGuards.RequireManager(tenant, "record utility payments");
        await MonthLockGuard.RequireOpenAsync(db, tenant.MessId!.Value, request.PaymentDate, cancellationToken);

        // Type must belong to the active mess — cross-mess type ids resolve to 404 (no leakage).
        var type = await db.UtilityTypes.AsNoTracking().IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.MessId == tenant.MessId!.Value && t.Id == request.UtilityTypeId, cancellationToken)
            ?? throw new NotFoundException("Utility type not found.");

        var payment = new UtilityPayment
        {
            Id = Guid.NewGuid(),
            MessId = tenant.MessId!.Value,
            UtilityTypeId = type.Id,
            Amount = request.Amount,
            PaymentDate = request.PaymentDate,
            Note = request.Note?.Trim(),
            CreatedBy = currentUser.UserId!.Value,
            CreatedAt = DateTime.UtcNow,
        };
        db.UtilityPayments.Add(payment);
        await db.SaveChangesAsync(cancellationToken);

        await projection.UtilityPaymentAddedAsync(tenant.MessId!.Value, request.PaymentDate, request.Amount, cancellationToken);

        return new UtilityPaymentDto(payment.Id, payment.UtilityTypeId, type.Name, payment.Amount,
            payment.PaymentDate, payment.Note, payment.CreatedBy, payment.CreatedAt);
    }
}
