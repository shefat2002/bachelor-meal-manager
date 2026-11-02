using MealManager.Application.Abstractions;
using MealManager.Application.Common;
using MealManager.Application.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MealManager.Application.Features.Messes;

/// <summary>Messes the current user actively belongs to (SuperAdmin: all), paginated per API_CONVENTIONS.</summary>
public record GetMessesQuery(int? Page, int? PageSize) : IRequest<PaginatedResult<MessDto>>;

public sealed class GetMessesQueryHandler(IApplicationContext db, ICurrentUser currentUser)
    : IRequestHandler<GetMessesQuery, PaginatedResult<MessDto>>
{
    public async Task<PaginatedResult<MessDto>> Handle(GetMessesQuery request, CancellationToken cancellationToken)
    {
        var page = Pagination.NormalizePage(request.Page);
        var pageSize = Pagination.NormalizePageSize(request.PageSize);
        var userId = currentUser.UserId!.Value;
        var isSuperAdmin = currentUser.IsSuperAdmin;

        // MembersMesses carries the tenant query filter; this cross-mess query reads memberships explicitly.
        var memberships = await db.MembersMesses.AsNoTracking().IgnoreQueryFilters()
            .Where(mm => mm.MemberId == userId && mm.Status == MemberStatus.Active)
            .ToDictionaryAsync(mm => mm.MessId, mm => mm.Role, cancellationToken);

        var query = db.Messes.AsNoTracking().AsQueryable();
        query = isSuperAdmin ? query : query.Where(m => memberships.Keys.Contains(m.Id));

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new
            {
                m.Id,
                m.Name,
                m.CreatedAt,
                ActiveMembers = db.MembersMesses.IgnoreQueryFilters()
                    .Count(mm => mm.MessId == m.Id && mm.Status == MemberStatus.Active),
            })
            .ToListAsync(cancellationToken);

        var dtos = items.Select(m => new MessDto(
            m.Id,
            m.Name,
            m.CreatedAt,
            memberships.TryGetValue(m.Id, out var role) ? role.ToString() : "superAdmin",
            m.ActiveMembers)).ToList();

        return new PaginatedResult<MessDto>(dtos, page, pageSize, totalCount);
    }
}
