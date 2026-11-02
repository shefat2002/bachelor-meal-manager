using MealManager.Application.Abstractions;
using MealManager.Application.Domain;
using MealManager.Application.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MealManager.Application.Features.Utilities;

/// <summary>
/// Hard delete; 409 when payments reference the type (the UI deactivates instead via PUT isActive=false,
/// which keeps history intact).
/// </summary>
public record DeleteUtilityTypeCommand(Guid Id) : ITenantRequest, IRequest<Unit>;

public sealed class DeleteUtilityTypeCommandHandler(IApplicationContext db, ITenantContext tenant)
    : IRequestHandler<DeleteUtilityTypeCommand, Unit>
{
    public async Task<Unit> Handle(DeleteUtilityTypeCommand request, CancellationToken cancellationToken)
    {
        TenantGuards.RequireManager(tenant, "manage utility types");

        var type = await db.UtilityTypes.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.MessId == tenant.MessId!.Value && t.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Utility type not found.");

        var hasPayments = await db.UtilityPayments.AsNoTracking().IgnoreQueryFilters()
            .AnyAsync(p => p.UtilityTypeId == type.Id, cancellationToken);
        if (hasPayments)
        {
            throw new ConflictException("Utility types with recorded payments cannot be deleted; deactivate instead.");
        }

        db.UtilityTypes.Remove(type);
        await db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
