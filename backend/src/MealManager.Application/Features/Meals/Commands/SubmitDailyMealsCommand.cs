using FluentValidation;
using MealManager.Application.Abstractions;
using MealManager.Application.Domain;
using MealManager.Application.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MealManager.Application.Features.Meals;

/// <summary>
/// Bulk upsert of one date's meal counts for one or more members. Members may only log their own
/// entries; managers may log for anyone. Re-submitting a (member, date) pair overwrites the counts.
/// </summary>
public record SubmitDailyMealsCommand(DateOnly Date, IReadOnlyList<MealEntryRequest> Entries)
    : ITenantRequest, IRequest<IReadOnlyList<DailyMealDto>>;

public sealed class SubmitDailyMealsCommandValidator : AbstractValidator<SubmitDailyMealsCommand>
{
    public const decimal MaxMealCount = 10m;

    public SubmitDailyMealsCommandValidator()
    {
        RuleFor(x => x.Date).NotEmpty();
        RuleFor(x => x.Entries).NotEmpty().WithMessage("At least one meal entry is required.")
            .Must(entries => entries.Select(e => e.MemberId).Distinct().Count() == entries.Count)
            .WithMessage("Each member may appear only once per submission.");
        RuleFor(x => x.Entries).Must(e => e.Count <= 100).WithMessage("At most 100 entries per submission.");
        RuleForEach(x => x.Entries).ChildRules(entry =>
        {
            entry.RuleFor(e => e.Breakfast).InclusiveBetween(0m, MaxMealCount).Must(BeHalfStep)
                .WithMessage("Meal counts must be in 0.5 steps.");
            entry.RuleFor(e => e.Lunch).InclusiveBetween(0m, MaxMealCount).Must(BeHalfStep)
                .WithMessage("Meal counts must be in 0.5 steps.");
            entry.RuleFor(e => e.Dinner).InclusiveBetween(0m, MaxMealCount).Must(BeHalfStep)
                .WithMessage("Meal counts must be in 0.5 steps.");
        });
    }

    private static bool BeHalfStep(decimal value) => decimal.Truncate(value * 2m) == value * 2m;
}

public sealed class SubmitDailyMealsCommandHandler(IApplicationContext db, ITenantContext tenant, ICurrentUser currentUser)
    : IRequestHandler<SubmitDailyMealsCommand, IReadOnlyList<DailyMealDto>>
{
    public async Task<IReadOnlyList<DailyMealDto>> Handle(SubmitDailyMealsCommand request, CancellationToken cancellationToken)
    {
        var messId = tenant.MessId!.Value;
        var userId = currentUser.UserId!.Value;

        if (tenant.Role != MessRole.MealManager && request.Entries.Any(e => e.MemberId != userId))
        {
            throw new ForbiddenException("Members can only log their own meals.");
        }

        var memberIds = request.Entries.Select(e => e.MemberId).Distinct().ToList();
        var activeMemberIds = await db.MembersMesses.AsNoTracking().IgnoreQueryFilters()
            .Where(mm => mm.MessId == messId && mm.Status == MemberStatus.Active && memberIds.Contains(mm.MemberId))
            .Select(mm => mm.MemberId)
            .ToListAsync(cancellationToken);
        if (activeMemberIds.Count != memberIds.Count)
        {
            throw new NotFoundException("Not all target members are active members of this mess.");
        }

        var existing = await db.DailyMeals.IgnoreQueryFilters()
            .Where(m => m.MessId == messId && m.Date == request.Date && memberIds.Contains(m.MemberId))
            .ToListAsync(cancellationToken);
        var byMember = existing.ToDictionary(m => m.MemberId);

        var now = DateTime.UtcNow;
        var touched = new List<DailyMeal>(request.Entries.Count);
        foreach (var entry in request.Entries)
        {
            if (byMember.TryGetValue(entry.MemberId, out var meal))
            {
                meal.Breakfast = entry.Breakfast;
                meal.Lunch = entry.Lunch;
                meal.Dinner = entry.Dinner;
                meal.UpdatedAt = now;
            }
            else
            {
                meal = new DailyMeal
                {
                    Id = Guid.NewGuid(),
                    MessId = messId,
                    MemberId = entry.MemberId,
                    Date = request.Date,
                    Breakfast = entry.Breakfast,
                    Lunch = entry.Lunch,
                    Dinner = entry.Dinner,
                    CreatedAt = now,
                };
                db.DailyMeals.Add(meal);
            }

            touched.Add(meal);
        }

        await db.SaveChangesAsync(cancellationToken);
        return touched.Select(MealsContractsMap.ToDto).ToList();
    }
}

internal static class MealsContractsMap
{
    public static DailyMealDto ToDto(DailyMeal m) =>
        new(m.Id, m.MemberId, m.Date, m.Breakfast, m.Lunch, m.Dinner,
            m.Breakfast + m.Lunch + m.Dinner, m.CreatedAt, m.UpdatedAt);
}
