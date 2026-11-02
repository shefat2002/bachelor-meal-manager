using FluentValidation;
using MealManager.Application.Abstractions;
using MealManager.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MealManager.Application.Features.Utilities;

/// <summary>Paginated utility payments — filters (type, date range), sorting (payment date).</summary>
public record GetUtilityPaymentsQuery(
    int? Page,
    int? PageSize,
    Guid? UtilityTypeId,
    DateOnly? From,
    DateOnly? To,
    string? SortBy,
    string? SortDir) : IListQuery, ITenantRequest, IRequest<PaginatedResult<UtilityPaymentDto>>;

public sealed class GetUtilityPaymentsQueryValidator : AbstractValidator<GetUtilityPaymentsQuery>
{
    public static readonly string[] SortWhitelist = ["date"];

    public GetUtilityPaymentsQueryValidator()
    {
        ListQueryRules.Apply(this, SortWhitelist);
    }
}

public sealed class GetUtilityPaymentsQueryHandler(IApplicationContext db, ITenantContext tenant)
    : IRequestHandler<GetUtilityPaymentsQuery, PaginatedResult<UtilityPaymentDto>>
{
    public async Task<PaginatedResult<UtilityPaymentDto>> Handle(GetUtilityPaymentsQuery request, CancellationToken cancellationToken)
    {
        var page = Pagination.NormalizePage(request.Page);
        var pageSize = Pagination.NormalizePageSize(request.PageSize);

        var query = db.UtilityPayments.AsNoTracking().IgnoreQueryFilters()
            .Where(p => p.MessId == tenant.MessId!.Value);
        if (request.UtilityTypeId is { } typeId)
        {
            query = query.Where(p => p.UtilityTypeId == typeId);
        }

        if (request.From is { } from)
        {
            query = query.Where(p => p.PaymentDate >= from);
        }

        if (request.To is { } to)
        {
            query = query.Where(p => p.PaymentDate <= to);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var ordered = request.SortsAscending()
            ? query.OrderBy(p => p.PaymentDate).ThenBy(p => p.CreatedAt)
            : query.OrderByDescending(p => p.PaymentDate).ThenByDescending(p => p.CreatedAt);

        var items = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Join(db.UtilityTypes.AsNoTracking().IgnoreQueryFilters()
                    .Where(t => t.MessId == tenant.MessId!.Value),
                p => p.UtilityTypeId, t => t.Id,
                (p, t) => new UtilityPaymentDto(
                    p.Id, p.UtilityTypeId, t.Name, p.Amount, p.PaymentDate, p.Note, p.CreatedBy, p.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PaginatedResult<UtilityPaymentDto>(items, page, pageSize, totalCount);
    }
}
