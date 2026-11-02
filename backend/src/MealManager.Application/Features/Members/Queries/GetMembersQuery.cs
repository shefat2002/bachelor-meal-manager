using MealManager.Application.Abstractions;
using MealManager.Application.Common;
using MealManager.Application.Domain;
using MealManager.Application.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MealManager.Application.Features.Members;

/// <summary>Members of the active mess. Pending join requests are visible to managers only.</summary>
public record GetMembersQuery(int? Page, int? PageSize) : ITenantRequest, IRequest<PaginatedResult<MessMemberDto>>;

public sealed class GetMembersQueryHandler(IApplicationContext db, ITenantContext tenant, ICurrentUser currentUser)
    : IRequestHandler<GetMembersQuery, PaginatedResult<MessMemberDto>>
{
    public async Task<PaginatedResult<MessMemberDto>> Handle(GetMembersQuery request, CancellationToken cancellationToken)
    {
        var page = Pagination.NormalizePage(request.Page);
        var pageSize = Pagination.NormalizePageSize(request.PageSize);
        var messId = tenant.MessId!.Value;
        var includePending = tenant.Role == MessRole.MealManager || currentUser.IsSuperAdmin;

        var memberships = db.MembersMesses.AsNoTracking().IgnoreQueryFilters()
            .Where(mm => mm.MessId == messId && mm.Status != MemberStatus.Left && mm.Status != MemberStatus.Rejected);
        if (!includePending)
        {
            memberships = memberships.Where(mm => mm.Status == MemberStatus.Active);
        }

        var totalCount = await memberships.CountAsync(cancellationToken);
        var items = await memberships
            .OrderBy(mm => mm.JoinedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(mm => new MessMemberDto(
                mm.MemberId,
                db.Members.AsNoTracking().Where(u => u.Id == mm.MemberId).Select(u => u.Name).FirstOrDefault() ?? "Unknown",
                db.Members.AsNoTracking().Where(u => u.Id == mm.MemberId).Select(u => u.PhoneNumber ?? "").FirstOrDefault() ?? "",
                mm.Role.ToString(),
                mm.Status.ToString(),
                mm.JoinedAt))
            .ToListAsync(cancellationToken);

        return new PaginatedResult<MessMemberDto>(items, page, pageSize, totalCount);
    }
}
