using System.Text.Json;
using FluentValidation;
using MealManager.Application.Abstractions;
using MealManager.Application.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MealManager.Application.Features.Summary;

/// <summary>
/// GET /api/summary/messes/{messId}?year&month — the §5 equations, grain-computed and cached.
/// The path messId must match the X-Mess-Id tenant (else 404, no leakage). Locked months serve
/// the snapshot captured at lock time.
/// </summary>
public record GetMessSummaryQuery(Guid MessId, int Year, int Month) : ITenantRequest, IRequest<MessSummaryDto>;

public sealed class GetMessSummaryQueryValidator : AbstractValidator<GetMessSummaryQuery>
{
    public GetMessSummaryQueryValidator()
    {
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
    }
}

public sealed class GetMessSummaryQueryHandler(IApplicationContext db, ITenantContext tenant, ISummaryProvider summaryProvider)
    : IRequestHandler<GetMessSummaryQuery, MessSummaryDto>
{
    private static readonly JsonSerializerOptions SnapshotJsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<MessSummaryDto> Handle(GetMessSummaryQuery request, CancellationToken cancellationToken)
    {
        if (request.MessId != tenant.MessId!.Value)
        {
            throw new NotFoundException("Mess not found.");
        }

        var lockRow = await db.MonthLocks.AsNoTracking().IgnoreQueryFilters()
            .FirstOrDefaultAsync(l => l.MessId == request.MessId && l.Year == request.Year && l.Month == request.Month, cancellationToken);
        if (lockRow?.SnapshotJson is { Length: > 0 } snapshot)
        {
            var frozen = JsonSerializer.Deserialize<MessSummaryDto>(snapshot, SnapshotJsonOptions);
            if (frozen is not null)
            {
                return frozen with { Locked = true };
            }
        }

        return await summaryProvider.GetAsync(request.MessId, request.Year, request.Month, cancellationToken);
    }
}
