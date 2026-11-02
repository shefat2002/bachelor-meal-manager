using MealManager.Application.Abstractions;
using MealManager.Application.Domain;
using MealManager.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MealManager.Infrastructure;

public sealed class MembershipService(ApplicationContext db) : IMembershipService
{
    public Task<MessRole?> ResolveAsync(Guid memberId, Guid messId, CancellationToken cancellationToken = default) =>
        db.MembersMesses
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(mm => mm.MessId == messId && mm.MemberId == memberId && mm.Status == MemberStatus.Active)
            .Select(mm => (MessRole?)mm.Role)
            .FirstOrDefaultAsync(cancellationToken);
}
