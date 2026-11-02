using FluentValidation;
using MealManager.Application.Abstractions;
using MealManager.Application.Domain;
using MealManager.Application.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MealManager.Application.Features.Bazar;

/// <summary>Manager-only: record a bazar run as one row per item, sharing date + buyer.</summary>
public record CreateBazarExpensesCommand(DateOnly Date, Guid? BuyerMemberId, IReadOnlyList<BazarItemRequest> Items)
    : ITenantRequest, IRequest<IReadOnlyList<BazarExpenseDto>>;

public sealed class CreateBazarExpensesCommandValidator : AbstractValidator<CreateBazarExpensesCommand>
{
    public const int MaxItems = 50;

    public CreateBazarExpensesCommandValidator()
    {
        RuleFor(x => x.Date).NotEmpty();
        RuleFor(x => x.Items).NotEmpty().WithMessage("At least one item is required.")
            .Must(i => i.Count <= MaxItems).WithMessage($"At most {MaxItems} items per submission.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Name).NotEmpty().MaximumLength(200);
            item.RuleFor(i => i.Quantity).InclusiveBetween(0.01m, 99_999m);
            item.RuleFor(i => i.Price).InclusiveBetween(0.01m, 999_999_999m);
        });
    }
}

public sealed class CreateBazarExpensesCommandHandler(IApplicationContext db, ITenantContext tenant, ICurrentUser currentUser)
    : IRequestHandler<CreateBazarExpensesCommand, IReadOnlyList<BazarExpenseDto>>
{
    public async Task<IReadOnlyList<BazarExpenseDto>> Handle(CreateBazarExpensesCommand request, CancellationToken cancellationToken)
    {
        TenantGuards.RequireManager(tenant, "record bazar expenses");

        var messId = tenant.MessId!.Value;
        var buyerId = request.BuyerMemberId ?? currentUser.UserId!.Value;
        if (buyerId != currentUser.UserId)
        {
            var buyerActive = await db.MembersMesses.AsNoTracking().IgnoreQueryFilters()
                .AnyAsync(mm => mm.MessId == messId && mm.MemberId == buyerId
                    && mm.Status == MemberStatus.Active, cancellationToken);
            if (!buyerActive)
            {
                throw new NotFoundException("The selected buyer is not an active member of this mess.");
            }
        }

        var now = DateTime.UtcNow;
        var expenses = request.Items.Select(item => new BazarExpense
        {
            Id = Guid.NewGuid(),
            MessId = messId,
            BuyerMemberId = buyerId,
            ItemName = item.Name.Trim(),
            Quantity = item.Quantity,
            Price = item.Price,
            Date = request.Date,
            CreatedAt = now,
        }).ToList();
        db.BazarExpenses.AddRange(expenses);
        await db.SaveChangesAsync(cancellationToken);

        return expenses
            .Select(e => new BazarExpenseDto(e.Id, e.BuyerMemberId, e.ItemName, e.Quantity, e.Price,
                Math.Round(e.Quantity * e.Price, 2), e.Date, e.CreatedAt))
            .ToList();
    }
}
