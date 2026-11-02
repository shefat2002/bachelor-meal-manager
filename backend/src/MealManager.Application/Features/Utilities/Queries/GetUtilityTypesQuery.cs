using FluentValidation;
using MealManager.Application.Abstractions;
using MealManager.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MealManager.Application.Features.Utilities;

/// <summary>Utility types of the active mess, ordered by name. Small config list, still paginated per conventions.</summary>
public record GetUtilityTypesQuery(int? Page, int? PageSize) : ITenantRequest, IRequest<PaginatedResult<UtilityTypeDto>>;

public sealed class GetUtilityTypesQueryValidator : AbstractValidator<GetUtilityTypesQuery>
{
    public GetUtilityTypesQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, Pagination.MaxPageSize)
            .WithMessage($"pageSize must be between 1 and {Pagination.MaxPageSize}.");
    }
}

public sealed class GetUtilityTypesQueryHandler(IApplicationContext db, ITenantContext tenant)
    : IRequestHandler<GetUtilityTypesQuery, PaginatedResult<UtilityTypeDto>>
{
    public async Task<PaginatedResult<UtilityTypeDto>> Handle(GetUtilityTypesQuery request, CancellationToken cancellationToken)
    {
        var page = Pagination.NormalizePage(request.Page);
        var pageSize = Pagination.NormalizePageSize(request.PageSize);

        var query = db.UtilityTypes.AsNoTracking().IgnoreQueryFilters()
            .Where(t => t.MessId == tenant.MessId!.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(t => t.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new UtilityTypeDto(t.Id, t.Name, t.Description, t.IsActive, t.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PaginatedResult<UtilityTypeDto>(items, page, pageSize, totalCount);
    }
}
