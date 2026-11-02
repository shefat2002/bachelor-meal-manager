using FluentValidation;
using MealManager.Application.Abstractions;
using MealManager.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MealManager.Application.Features.Meals;

/// <summary>Historical meal counts for the active mess — filters, sorting (date), pagination.</summary>
public record GetMealsQuery(
    int? Page,
    int? PageSize,
    Guid? MemberId,
    DateOnly? From,
    DateOnly? To,
    string? SortBy,
    string? SortDir) : IListQuery, ITenantRequest, IRequest<PaginatedResult<DailyMealDto>>;

public sealed class GetMealsQueryValidator : AbstractValidator<GetMealsQuery>
{
    public static readonly string[] SortWhitelist = ["date"];

    public GetMealsQueryValidator()
    {
        ListQueryRules.Apply(this, SortWhitelist);
    }
}

public sealed class GetMealsQueryHandler(IApplicationContext db, ITenantContext tenant)
    : IRequestHandler<GetMealsQuery, PaginatedResult<DailyMealDto>>
{
    public async Task<PaginatedResult<DailyMealDto>> Handle(GetMealsQuery request, CancellationToken cancellationToken)
    {
        var page = Pagination.NormalizePage(request.Page);
        var pageSize = Pagination.NormalizePageSize(request.PageSize);

        var query = db.DailyMeals.AsNoTracking().IgnoreQueryFilters()
            .Where(m => m.MessId == tenant.MessId!.Value);
        if (request.MemberId is { } memberId)
        {
            query = query.Where(m => m.MemberId == memberId);
        }

        if (request.From is { } from)
        {
            query = query.Where(m => m.Date >= from);
        }

        if (request.To is { } to)
        {
            query = query.Where(m => m.Date <= to);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        // Single sort key (whitelist: date); MemberId tiebreaker keeps pages deterministic.
        var ordered = request.SortsAscending()
            ? query.OrderBy(m => m.Date).ThenBy(m => m.MemberId)
            : query.OrderByDescending(m => m.Date).ThenBy(m => m.MemberId);

        var items = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResult<DailyMealDto>(
            items.Select(MealsContractsMap.ToDto).ToList(), page, pageSize, totalCount);
    }
}
