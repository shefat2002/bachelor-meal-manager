using FluentValidation;
using MealManager.Application.Abstractions;
using MealManager.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MealManager.Application.Features.Bazar;

/// <summary>Paginated bazar expenses — filters (buyer, date range), sorting (date, price=line total).</summary>
public record GetBazarExpensesQuery(
    int? Page,
    int? PageSize,
    Guid? BuyerMemberId,
    DateOnly? From,
    DateOnly? To,
    string? SortBy,
    string? SortDir) : IListQuery, ITenantRequest, IRequest<PaginatedResult<BazarExpenseDto>>;

public sealed class GetBazarExpensesQueryValidator : AbstractValidator<GetBazarExpensesQuery>
{
    public static readonly string[] SortWhitelist = ["date", "price"];

    public GetBazarExpensesQueryValidator()
    {
        ListQueryRules.Apply(this, SortWhitelist);
    }
}

public sealed class GetBazarExpensesQueryHandler(IApplicationContext db, ITenantContext tenant)
    : IRequestHandler<GetBazarExpensesQuery, PaginatedResult<BazarExpenseDto>>
{
    public async Task<PaginatedResult<BazarExpenseDto>> Handle(GetBazarExpensesQuery request, CancellationToken cancellationToken)
    {
        var page = Pagination.NormalizePage(request.Page);
        var pageSize = Pagination.NormalizePageSize(request.PageSize);

        var query = db.BazarExpenses.AsNoTracking().IgnoreQueryFilters()
            .Where(e => e.MessId == tenant.MessId!.Value);
        if (request.BuyerMemberId is { } buyer)
        {
            query = query.Where(e => e.BuyerMemberId == buyer);
        }

        if (request.From is { } from)
        {
            query = query.Where(e => e.Date >= from);
        }

        if (request.To is { } to)
        {
            query = query.Where(e => e.Date <= to);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        // price sorts by the computed line total (Quantity × Price); date is the default key.
        var ascending = request.SortsAscending();
        var ordered = request.SortKey("date") switch
        {
            "price" => ascending
                ? query.OrderBy(e => e.Quantity * e.Price).ThenBy(e => e.Date)
                : query.OrderByDescending(e => e.Quantity * e.Price).ThenBy(e => e.Date),
            _ => ascending
                ? query.OrderBy(e => e.Date).ThenBy(e => e.CreatedAt)
                : query.OrderByDescending(e => e.Date).ThenByDescending(e => e.CreatedAt),
        };

        var items = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new BazarExpenseDto(
                e.Id, e.BuyerMemberId, e.ItemName, e.Quantity, e.Price,
                Math.Round(e.Quantity * e.Price, 2), e.Date, e.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PaginatedResult<BazarExpenseDto>(items, page, pageSize, totalCount);
    }
}
